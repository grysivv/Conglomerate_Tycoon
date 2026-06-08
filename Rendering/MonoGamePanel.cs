using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TycoonGame.Core;
using Color = Microsoft.Xna.Framework.Color;
using Point = System.Drawing.Point;

namespace TycoonGame.Rendering
{
    public enum BuildTool
    {
        Inspect,
        BuildRoad,
        BuildOffice,
        BuildFactory,
        BuildRetail,
        BuildPowerPlant,
        BuildApartment,
        BuildUniversity,
        Bulldozer,
        Upgrade
    }

    public class MonoGamePanel : Control
    {
        private GraphicsDevice? graphicsDevice;
        private SpriteBatch? spriteBatch;
        
        public GameEngine? Engine { get; set; }
        public IsometricRenderer Renderer { get; private set; }
        public BuildTool ActiveTool { get; set; }
        public Tuple<int, int>? HoveredTile { get; private set; }
        public Tuple<int, int>? SelectedTile { get; private set; }

        // Interaction state
        private bool isDragging;
        private Point lastMousePosition;

        // Custom events to communicate clicks back to WinForms UI
        public event EventHandler<Tuple<int, int>?>? TileSelected;
        public event EventHandler? MapChanged;

        // UI Viewport Overlays
        private Panel pnlHoverTooltip = null!;
        private Panel pnlBuildingCustomizer = null!;
        
        // Tooltip Labels
        private Label lblHoverType = null!;
        private Label lblHoverAddress = null!;
        private Label lblHoverStaff = null!;
        private Label lblHoverStatus = null!;
        private Label lblHoverEconomics = null!;
        private Label lblHoverSkills = null!;

        // Customizer Controls
        private Label lblCustType = null!;
        private Label lblCustAddress = null!;
        private Label lblCustStaffVal = null!;
        private Label lblCustTrainVal = null!;
        private Button btnCustUpgrade = null!;

        public MonoGamePanel()
        {
            // Configure control styles for custom GPU rendering
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            
            Engine = null;
            Renderer = new IsometricRenderer();
            ActiveTool = BuildTool.Inspect;
            isDragging = false;
            
            // Allow keyboard focus
            Focus();
            InitializeOverlays();
        }

        private void InitializeOverlays()
        {
            // 1. Hover Tooltip Overlay Panel
            pnlHoverTooltip = new Panel
            {
                Size = new Size(240, 140),
                BackColor = System.Drawing.Color.FromArgb(11, 15, 25),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            pnlHoverTooltip.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 240, 255), 1))
                {
                    var rect = pnlHoverTooltip.ClientRectangle;
                    rect.Width -= 1;
                    rect.Height -= 1;
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            lblHoverType = CreateTooltipLabel(new Point(10, 10), new Size(220, 18), true, 9.5F);
            lblHoverAddress = CreateTooltipLabel(new Point(10, 32), new Size(220, 16), false, 8.5F);
            lblHoverStaff = CreateTooltipLabel(new Point(10, 52), new Size(220, 16), false, 8.5F);
            lblHoverStatus = CreateTooltipLabel(new Point(10, 72), new Size(220, 16), false, 8.5F);
            lblHoverEconomics = CreateTooltipLabel(new Point(10, 92), new Size(220, 16), false, 8.5F);
            lblHoverSkills = CreateTooltipLabel(new Point(10, 112), new Size(220, 16), false, 8.5F);

            pnlHoverTooltip.Controls.Add(lblHoverType);
            pnlHoverTooltip.Controls.Add(lblHoverAddress);
            pnlHoverTooltip.Controls.Add(lblHoverStaff);
            pnlHoverTooltip.Controls.Add(lblHoverStatus);
            pnlHoverTooltip.Controls.Add(lblHoverEconomics);
            pnlHoverTooltip.Controls.Add(lblHoverSkills);
            this.Controls.Add(pnlHoverTooltip);

            // 2. Click Customizer Overlay Panel
            pnlBuildingCustomizer = new Panel
            {
                Size = new Size(300, 245),
                BackColor = System.Drawing.Color.FromArgb(15, 20, 35),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Location = new Point(15, 15)
            };
            pnlBuildingCustomizer.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 255, 102), 1))
                {
                    var rect = pnlBuildingCustomizer.ClientRectangle;
                    rect.Width -= 1;
                    rect.Height -= 1;
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            Label lblCustTitle = new Label
            {
                Text = "DEPARTMENT CONTROL",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(0, 240, 255),
                Location = new Point(15, 15),
                Size = new Size(240, 20)
            };
            pnlBuildingCustomizer.Controls.Add(lblCustTitle);

            Button btnClose = new Button
            {
                Text = "X",
                Location = new Point(265, 10),
                Size = new Size(25, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(48, 56, 70),
                ForeColor = System.Drawing.Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => pnlBuildingCustomizer.Visible = false;
            pnlBuildingCustomizer.Controls.Add(btnClose);

            lblCustType = CreateCustomizerLabel(new Point(15, 45), new Size(270, 16));
            lblCustAddress = CreateCustomizerLabel(new Point(15, 62), new Size(270, 16));

            // Hired workforce adjustment
            Label lblStaffTitle = new Label { Text = "Hired Workforce:", Location = new Point(15, 90), Size = new Size(110, 20), ForeColor = System.Drawing.Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            pnlBuildingCustomizer.Controls.Add(lblStaffTitle);

            Button btnStaffDec = CreateFlatButton("-", new Point(135, 87), new Size(25, 25), (s, e) => AdjustHiredStaff(-1));
            lblCustStaffVal = new Label
            {
                Text = "0 / 0",
                Location = new Point(165, 90),
                Size = new Size(60, 20),
                ForeColor = System.Drawing.Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            Button btnStaffInc = CreateFlatButton("+", new Point(230, 87), new Size(25, 25), (s, e) => AdjustHiredStaff(1));
            pnlBuildingCustomizer.Controls.Add(btnStaffDec);
            pnlBuildingCustomizer.Controls.Add(lblCustStaffVal);
            pnlBuildingCustomizer.Controls.Add(btnStaffInc);

            // Training budget adjustment
            Label lblTrainTitle = new Label { Text = "Hourly Training:", Location = new Point(15, 130), Size = new Size(110, 20), ForeColor = System.Drawing.Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            pnlBuildingCustomizer.Controls.Add(lblTrainTitle);

            Button btnTrainDec = CreateFlatButton("- $5", new Point(135, 127), new Size(40, 25), (s, e) => AdjustTrainingBudget(-5));
            lblCustTrainVal = new Label
            {
                Text = "$0 / hr",
                Location = new Point(180, 130),
                Size = new Size(50, 20),
                ForeColor = System.Drawing.Color.FromArgb(80, 200, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            Button btnTrainInc = CreateFlatButton("+ $5", new Point(235, 127), new Size(40, 25), (s, e) => AdjustTrainingBudget(5));
            pnlBuildingCustomizer.Controls.Add(btnTrainDec);
            pnlBuildingCustomizer.Controls.Add(lblCustTrainVal);
            pnlBuildingCustomizer.Controls.Add(btnTrainInc);

            // Building Upgrade Button
            btnCustUpgrade = new Button
            {
                Text = "UPGRADE STRUCTURAL BLOCK",
                Location = new Point(15, 180),
                Size = new Size(270, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(0, 255, 102),
                ForeColor = System.Drawing.Color.Black,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCustUpgrade.FlatAppearance.BorderSize = 0;
            btnCustUpgrade.Click += BtnCustUpgrade_Click;
            pnlBuildingCustomizer.Controls.Add(btnCustUpgrade);

            this.Controls.Add(pnlBuildingCustomizer);
        }

        private Label CreateTooltipLabel(Point loc, Size sz, bool bold, float fontSize)
        {
            return new Label
            {
                Location = loc,
                Size = sz,
                ForeColor = System.Drawing.Color.White,
                Font = new Font("Segoe UI", fontSize, bold ? FontStyle.Bold : FontStyle.Regular),
                BackColor = System.Drawing.Color.Transparent
            };
        }

        private Label CreateCustomizerLabel(Point loc, Size sz)
        {
            var lbl = new Label
            {
                Location = loc,
                Size = sz,
                ForeColor = System.Drawing.Color.FromArgb(170, 175, 190),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                BackColor = System.Drawing.Color.Transparent
            };
            pnlBuildingCustomizer.Controls.Add(lbl);
            return lbl;
        }

        private Button CreateFlatButton(string text, Point loc, Size sz, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(48, 56, 70),
                ForeColor = System.Drawing.Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void AdjustHiredStaff(int amount)
        {
            if (Engine == null || SelectedTile == null) return;
            var tile = Engine.Grid[SelectedTile.Item1, SelectedTile.Item2];
            int newStaff = Math.Clamp(tile.EmployeeCount + amount, 0, tile.MaxEmployees);
            if (tile.EmployeeCount != newStaff)
            {
                tile.EmployeeCount = newStaff;
                UpdateCustomizerData(tile);
                MapChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AdjustTrainingBudget(double amount)
        {
            if (Engine == null || SelectedTile == null) return;
            var tile = Engine.Grid[SelectedTile.Item1, SelectedTile.Item2];
            double newBudget = Math.Clamp(tile.TrainingBudgetPerHour + amount, 0.0, 100.0);
            if (tile.TrainingBudgetPerHour != newBudget)
            {
                tile.TrainingBudgetPerHour = newBudget;
                UpdateCustomizerData(tile);
                MapChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void BtnCustUpgrade_Click(object? sender, EventArgs e)
        {
            if (Engine == null || SelectedTile == null) return;
            int tx = SelectedTile.Item1;
            int ty = SelectedTile.Item2;

            if (Engine.UpgradeStructure(tx, ty))
            {
                var tile = Engine.Grid[tx, ty];
                UpdateCustomizerData(tile);
                MapChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
            else
            {
                MessageBox.Show("Insufficient funds or maximum building level reached!", "Construction Upgrades", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateCustomizerData(Tile tile)
        {
            lblCustType.Text = $"{tile.Type} (Tier {tile.Level})";
            lblCustAddress.Text = $"Sector Address: [{tile.X}, {tile.Y}]";
            lblCustStaffVal.Text = $"{tile.EmployeeCount} / {tile.MaxEmployees}";
            lblCustTrainVal.Text = $"${tile.TrainingBudgetPerHour:F0} / hr";

            double upgradeCost = tile.Type switch
            {
                TileType.Office => 24000.0,
                TileType.Factory => 48000.0,
                TileType.Retail => 32000.0,
                TileType.PowerPlant => 40000.0,
                TileType.Apartment => 40000.0,
                TileType.University => 60000.0,
                _ => 0
            };
            btnCustUpgrade.Text = tile.Level < 3 ? $"UPGRADE BUILDING (${upgradeCost / 1000:F0}K)" : "MAX LEVEL REACHED";
            btnCustUpgrade.Enabled = tile.Level < 3;
            btnCustUpgrade.BackColor = tile.Level < 3 ? System.Drawing.Color.FromArgb(0, 255, 102) : System.Drawing.Color.FromArgb(48, 56, 70);
            btnCustUpgrade.ForeColor = tile.Level < 3 ? System.Drawing.Color.Black : System.Drawing.Color.FromArgb(170, 175, 190);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            InitializeMonoGame();
        }

        private void InitializeMonoGame()
        {
            try
            {
                var pparams = new PresentationParameters
                {
                    BackBufferWidth = Math.Max(1, ClientSize.Width),
                    BackBufferHeight = Math.Max(1, ClientSize.Height),
                    BackBufferFormat = SurfaceFormat.Color,
                    DepthStencilFormat = DepthFormat.Depth24Stencil8,
                    DeviceWindowHandle = Handle,
                    IsFullScreen = false,
                    PresentationInterval = PresentInterval.Default
                };

                graphicsDevice = new GraphicsDevice(
                    GraphicsAdapter.DefaultAdapter, 
                    GraphicsProfile.Reach, 
                    pparams);

                spriteBatch = new SpriteBatch(graphicsDevice);
                
                Renderer.LoadContent(graphicsDevice);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize GPU rendering context: {ex.Message}", "GPU Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (graphicsDevice != null && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                try
                {
                    var pparams = graphicsDevice.PresentationParameters;
                    pparams.BackBufferWidth = ClientSize.Width;
                    pparams.BackBufferHeight = ClientSize.Height;
                    graphicsDevice.Reset(pparams);
                }
                catch
                {
                    // Fallback re-init if device reset fails
                    InitializeMonoGame();
                }
                Invalidate();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Overridden to do nothing to prevent background flickering
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (graphicsDevice == null || spriteBatch == null || Engine == null)
            {
                // Draw a fallback message if GPU context failed or Engine is not set yet
                using (Brush brush = new SolidBrush(System.Drawing.Color.FromArgb(24, 28, 36)))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
                e.Graphics.DrawString("Game Engine initializing...", Font, Brushes.White, 10, 10);
                return;
            }

            // Clear buffer with deep charcoal dark blue
            graphicsDevice.Clear(new Color(24, 28, 36));

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Renderer.Draw(spriteBatch, Engine, ClientSize.Width, ClientSize.Height, HoveredTile);
            spriteBatch.End();

            try
            {
                graphicsDevice.Present();
            }
            catch
            {
                // Catch swap-chain present failures on window minimize
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            // Intercept clicks on the top-down Mini Map (bottom-right corner)
            int mapSizePx = GameEngine.MapSize;
            int margin = 10;
            int mapX = ClientSize.Width - mapSizePx - margin;
            int mapY = ClientSize.Height - mapSizePx - margin;

            if (e.Button == MouseButtons.Left && e.X >= mapX && e.X < mapX + mapSizePx && e.Y >= mapY && e.Y < mapY + mapSizePx)
            {
                int tx = e.X - mapX;
                int ty = e.Y - mapY;
                // Center camera on the clicked tile
                Renderer.CameraX = (float)((tx - ty) * 64.0);
                Renderer.CameraY = (float)((tx + ty) * 32.0);
                Invalidate();
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                isDragging = true;
                lastMousePosition = e.Location;
                Cursor = Cursors.NoMove2D;
            }
            else if (e.Button == MouseButtons.Left && HoveredTile != null)
            {
                int tx = HoveredTile.Item1;
                int ty = HoveredTile.Item2;

                if (tx >= 0 && tx < GameEngine.MapSize && ty >= 0 && ty < GameEngine.MapSize)
                {
                    ExecuteToolAction(tx, ty);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int mapSizePx = GameEngine.MapSize;
            int margin = 10;
            int mapX = ClientSize.Width - mapSizePx - margin;
            int mapY = ClientSize.Height - mapSizePx - margin;

            // Handle dragging on the mini-map to pan camera
            if (e.Button == MouseButtons.Left && e.X >= mapX && e.X < mapX + mapSizePx && e.Y >= mapY && e.Y < mapY + mapSizePx)
            {
                int tx = e.X - mapX;
                int ty = e.Y - mapY;
                Renderer.CameraX = (float)((tx - ty) * 64.0);
                Renderer.CameraY = (float)((tx + ty) * 32.0);
                Invalidate();
                return;
            }

            // 1. Camera Panning (Right Mouse Drag)
            if (isDragging)
            {
                float dx = (e.X - lastMousePosition.X) / Renderer.Zoom;
                float dy = (e.Y - lastMousePosition.Y) / Renderer.Zoom;

                Renderer.CameraX -= dx;
                Renderer.CameraY -= dy;
                lastMousePosition = e.Location;
                Invalidate();
            }
            // 2. Mouse Hover Tracking
            else
            {
                Tuple<int, int> newHover = Renderer.ScreenToTile(e.X, e.Y, ClientSize.Width, ClientSize.Height);
                if (HoveredTile == null || HoveredTile.Item1 != newHover.Item1 || HoveredTile.Item2 != newHover.Item2)
                {
                    HoveredTile = newHover;
                    Invalidate();
                }

                // Update Hover Tooltip overlay
                if (Engine != null && HoveredTile != null &&
                    HoveredTile.Item1 >= 0 && HoveredTile.Item1 < GameEngine.MapSize &&
                    HoveredTile.Item2 >= 0 && HoveredTile.Item2 < GameEngine.MapSize)
                {
                    Tile tile = Engine.Grid[HoveredTile.Item1, HoveredTile.Item2];
                    if (tile.Type != TileType.Grass && tile.Type != TileType.Road)
                    {
                        lblHoverType.Text = $"{tile.Type} (Tier {tile.Level})";
                        lblHoverAddress.Text = $"Sector Address: [{tile.X}, {tile.Y}]";
                        lblHoverStaff.Text = $"Staff Hired: {tile.EmployeeCount} / {tile.MaxEmployees}";
                        lblHoverStatus.Text = $"Powered: {(tile.IsPowered ? "YES" : "NO")} | Road Access: {(tile.HasRoadAccess ? "YES" : "NO")}";
                        lblHoverEconomics.Text = $"Upkeep: ${tile.MaintenanceCost:F0}/hr | Value: ${tile.GetAssetValue() / 1000:F0}K";
                        lblHoverSkills.Text = $"Skill: {(tile.SkillLevel * 100):F0}% | Morale: {(tile.Morale * 100):F0}%";

                        // Offset the tooltip location so it doesn't wrap off the window edges
                        int tx = e.X + 15;
                        int ty = e.Y + 15;
                        if (tx + pnlHoverTooltip.Width > ClientSize.Width) tx = e.X - pnlHoverTooltip.Width - 15;
                        if (ty + pnlHoverTooltip.Height > ClientSize.Height) ty = e.Y - pnlHoverTooltip.Height - 15;

                        pnlHoverTooltip.Location = new Point(Math.Max(0, tx), Math.Max(0, ty));
                        pnlHoverTooltip.Visible = true;
                    }
                    else
                    {
                        pnlHoverTooltip.Visible = false;
                    }
                }
                else
                {
                    pnlHoverTooltip.Visible = false;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right)
            {
                isDragging = false;
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            // Zoom scaling
            float zoomFactor = e.Delta > 0 ? 0.1f : -0.1f;
            float targetZoom = Math.Clamp(Renderer.Zoom + zoomFactor, 0.5f, 2.0f);

            if (Math.Abs(Renderer.Zoom - targetZoom) > 0.01f)
            {
                // Center zoom under mouse pointer (adjust camera position)
                Point mousePos = e.Location;
                
                // Get mouse position in world coordinates before zoom
                double worldX = (mousePos.X - ClientSize.Width / 2.0) / Renderer.Zoom + Renderer.CameraX;
                double worldY = (mousePos.Y - ClientSize.Height / 2.0) / Renderer.Zoom + Renderer.CameraY;

                Renderer.Zoom = targetZoom;

                // Adjust camera so mouse position matches the same world coordinate after zoom
                Renderer.CameraX = (float)(worldX - (mousePos.X - ClientSize.Width / 2.0) / Renderer.Zoom);
                Renderer.CameraY = (float)(worldY - (mousePos.Y - ClientSize.Height / 2.0) / Renderer.Zoom);

                Invalidate();
            }
        }

        private void ExecuteToolAction(int tx, int ty)
        {
            if (Engine == null) return;
            pnlBuildingCustomizer.Visible = false; // default hide customizer on action
            bool actionSuccess = false;

            switch (ActiveTool)
            {
                case BuildTool.Inspect:
                    SelectedTile = new Tuple<int, int>(tx, ty);
                    TileSelected?.Invoke(this, SelectedTile);
                    actionSuccess = true;

                    // Display building customizer overlay if inspect target is an active department
                    Tile tile = Engine.Grid[tx, ty];
                    if (tile.Type != TileType.Grass && tile.Type != TileType.Road)
                    {
                        UpdateCustomizerData(tile);
                        pnlBuildingCustomizer.Visible = true;
                    }
                    break;

                case BuildTool.BuildRoad:
                    actionSuccess = Engine.BuildStructure(tx, ty, TileType.Road);
                    break;

                case BuildTool.BuildOffice:
                    actionSuccess = Engine.BuildStructure(tx, ty, TileType.Office);
                    break;

                case BuildTool.BuildFactory:
                    actionSuccess = Engine.BuildStructure(tx, ty, TileType.Factory);
                    break;

                case BuildTool.BuildRetail:
                    actionSuccess = Engine.BuildStructure(tx, ty, TileType.Retail);
                    break;

                case BuildTool.BuildPowerPlant:
                    actionSuccess = Engine.BuildStructure(tx, ty, TileType.PowerPlant);
                    break;

                case BuildTool.BuildApartment:
                    actionSuccess = Engine.BuildStructure(tx, ty, TileType.Apartment);
                    break;

                case BuildTool.BuildUniversity:
                    actionSuccess = Engine.BuildStructure(tx, ty, TileType.University);
                    break;

                case BuildTool.Bulldozer:
                    actionSuccess = Engine.DemolishStructure(tx, ty);
                    if (actionSuccess && SelectedTile != null && SelectedTile.Item1 == tx && SelectedTile.Item2 == ty)
                    {
                        SelectedTile = null;
                        TileSelected?.Invoke(this, null);
                    }
                    break;

                case BuildTool.Upgrade:
                    actionSuccess = Engine.UpgradeStructure(tx, ty);
                    // Refresh inspection display
                    if (actionSuccess && SelectedTile != null && SelectedTile.Item1 == tx && SelectedTile.Item2 == ty)
                    {
                        TileSelected?.Invoke(this, SelectedTile);
                    }
                    break;
            }

            if (actionSuccess)
            {
                MapChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public void SelectTileCoordinates(int tx, int ty)
        {
            if (Engine == null) return;
            if (tx >= 0 && tx < GameEngine.MapSize && ty >= 0 && ty < GameEngine.MapSize)
            {
                SelectedTile = new Tuple<int, int>(tx, ty);
                TileSelected?.Invoke(this, SelectedTile);

                Tile tile = Engine.Grid[tx, ty];
                if (tile.Type != TileType.Grass && tile.Type != TileType.Road)
                {
                    UpdateCustomizerData(tile);
                    pnlBuildingCustomizer.Visible = true;
                }
                else
                {
                    pnlBuildingCustomizer.Visible = false;
                }
                
                // Pan camera to center on selected tile
                Vector2 pos = Renderer.TileToScreen(tx, ty, ClientSize.Width, ClientSize.Height);
                Renderer.CameraX += (float)((pos.X - ClientSize.Width / 2.0) / Renderer.Zoom);
                Renderer.CameraY += (float)((pos.Y - ClientSize.Height / 2.0) / Renderer.Zoom);
                
                Invalidate();
            }
        }
    }
}

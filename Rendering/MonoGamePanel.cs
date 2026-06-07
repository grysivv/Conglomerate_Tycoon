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
        
        public GameEngine Engine { get; set; }
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

        public MonoGamePanel()
        {
            // Configure control styles for custom GPU rendering
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            
            Engine = new GameEngine();
            Renderer = new IsometricRenderer();
            ActiveTool = BuildTool.Inspect;
            isDragging = false;
            
            // Allow keyboard focus
            Focus();
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
            if (graphicsDevice == null || spriteBatch == null)
            {
                // Draw a fallback message if GPU context failed
                using (Brush brush = new SolidBrush(System.Drawing.Color.DarkSlateGray))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
                e.Graphics.DrawString("GPU Render context initializing...", Font, Brushes.White, 10, 10);
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
            bool actionSuccess = false;

            switch (ActiveTool)
            {
                case BuildTool.Inspect:
                    SelectedTile = new Tuple<int, int>(tx, ty);
                    TileSelected?.Invoke(this, SelectedTile);
                    actionSuccess = true;
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
            if (tx >= 0 && tx < GameEngine.MapSize && ty >= 0 && ty < GameEngine.MapSize)
            {
                SelectedTile = new Tuple<int, int>(tx, ty);
                TileSelected?.Invoke(this, SelectedTile);
                
                // Pan camera to center on selected tile
                Vector2 pos = Renderer.TileToScreen(tx, ty, ClientSize.Width, ClientSize.Height);
                Renderer.CameraX += (float)((pos.X - ClientSize.Width / 2.0) / Renderer.Zoom);
                Renderer.CameraY += (float)((pos.Y - ClientSize.Height / 2.0) / Renderer.Zoom);
                
                Invalidate();
            }
        }
    }
}

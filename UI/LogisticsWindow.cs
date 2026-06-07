using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TycoonGame.Core;

namespace TycoonGame.UI
{
    public class LogisticsWindow : Form
    {
        private readonly GameEngine engine;
        private ListView factoryListView;
        private ListView retailListView;
        private Label lblTotalGoodsProduced;
        private Label lblTotalGoodsRetail;
        private Label lblLogisticsCost;
        private Label lblPriceIndex;
        private Label lblMarketShare;

        // Step 4: Operations Desk Controls
        private Label lblSelFactory;
        private Label lblSelRetail;
        private Button btnLinkContract;
        private Button btnUnlinkContract;
        private TrackBar trackPrice;
        private Label lblPriceVal;

        private Tile? selectedFactoryTile;
        private Tile? selectedRetailTile;

        public LogisticsWindow(GameEngine gameEngine)
        {
            engine = gameEngine;
            InitializeComponent();
            RefreshLogisticsData();
        }

        private void InitializeComponent()
        {
            Text = "Logistics, Supply Chain & Marketing";
            Size = new Size(885, 660);
            MinimumSize = new Size(885, 660);
            BackColor = Color.FromArgb(24, 28, 36);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Title
            Label lblTitle = new Label
            {
                Text = "Supply Chain Operations Desk",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 140, 200),
                Location = new Point(20, 15),
                Size = new Size(400, 35),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            // Left Side: Factories (Source)
            Label lblFactories = new Label
            {
                Text = "Production Facilities (Factories)",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 175, 190),
                Location = new Point(20, 60),
                Size = new Size(250, 20),
                AutoSize = true
            };
            Controls.Add(lblFactories);

            factoryListView = new ListView
            {
                Location = new Point(20, 85),
                Size = new Size(400, 220),
                View = View.Details,
                FullRowSelect = true,
                BackColor = Color.FromArgb(32, 38, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                OwnerDraw = true
            };
            factoryListView.Columns.Add("Location", 90);
            factoryListView.Columns.Add("Level", 50);
            factoryListView.Columns.Add("Staff", 60);
            factoryListView.Columns.Add("Inventory", 100);
            factoryListView.Columns.Add("Status", 100);
            
            SetupCustomDrawing(factoryListView);
            factoryListView.SelectedIndexChanged += FactoryListView_SelectedIndexChanged;
            Controls.Add(factoryListView);

            // Left Bottom: Retail Outlets (Destination)
            Label lblRetail = new Label
            {
                Text = "Distribution Outlets (Retail Stores)",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 175, 190),
                Location = new Point(20, 320),
                Size = new Size(250, 20),
                AutoSize = true
            };
            Controls.Add(lblRetail);

            retailListView = new ListView
            {
                Location = new Point(20, 345),
                Size = new Size(400, 220),
                View = View.Details,
                FullRowSelect = true,
                BackColor = Color.FromArgb(32, 38, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                OwnerDraw = true
            };
            retailListView.Columns.Add("Location", 90);
            retailListView.Columns.Add("Level", 50);
            retailListView.Columns.Add("Staff", 60);
            retailListView.Columns.Add("Inventory", 100);
            retailListView.Columns.Add("Status", 100);

            SetupCustomDrawing(retailListView);
            retailListView.SelectedIndexChanged += RetailListView_SelectedIndexChanged;
            Controls.Add(retailListView);

            // Right Top: Market Dynamics
            GroupBox grpMarket = new GroupBox
            {
                Text = "Market Dynamics",
                Location = new Point(445, 65),
                Size = new Size(400, 200),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            lblPriceIndex = new Label
            {
                Text = $"Retail Market Price: ${engine.CurrentMarketPrice:F2} / unit",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 200, 120),
                Location = new Point(15, 25),
                Size = new Size(370, 25)
            };
            grpMarket.Controls.Add(lblPriceIndex);

            lblMarketShare = new Label
            {
                Text = $"Player Market Share: {(engine.PlayerMarketShare * 100):F1}%",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.White,
                Location = new Point(15, 55),
                Size = new Size(370, 20)
            };
            grpMarket.Controls.Add(lblMarketShare);

            Label lblCompetitors = new Label
            {
                Text = "Market Competitors:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(15, 80),
                Size = new Size(200, 15),
                ForeColor = Color.White
            };
            grpMarket.Controls.Add(lblCompetitors);

            ListView competitorListView = new ListView
            {
                Location = new Point(15, 100),
                Size = new Size(370, 90),
                View = View.Details,
                BackColor = Color.FromArgb(24, 28, 36),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                OwnerDraw = true
            };
            competitorListView.Columns.Add("Competitor Name", 150);
            competitorListView.Columns.Add("Market Share", 100);
            competitorListView.Columns.Add("Avg Price", 100);
            SetupCustomDrawing(competitorListView);

            foreach (var comp in engine.Competitors)
            {
                ListViewItem item = new ListViewItem(comp.Name);
                item.SubItems.Add($"{(comp.MarketShare * 100):F1}%");
                item.SubItems.Add($"${comp.AveragePrice:F2}");
                competitorListView.Items.Add(item);
            }
            grpMarket.Controls.Add(competitorListView);
            Controls.Add(grpMarket);

            // Right Middle: Operations Desk (Step 4)
            GroupBox grpOperations = new GroupBox
            {
                Text = "Operations Desk",
                Location = new Point(445, 275),
                Size = new Size(400, 185),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            lblSelFactory = new Label
            {
                Text = "Selected Factory: None",
                ForeColor = Color.White,
                Location = new Point(15, 22),
                Size = new Size(370, 18)
            };
            grpOperations.Controls.Add(lblSelFactory);

            lblSelRetail = new Label
            {
                Text = "Selected Retail: None",
                ForeColor = Color.White,
                Location = new Point(15, 42),
                Size = new Size(370, 18)
            };
            grpOperations.Controls.Add(lblSelRetail);

            btnLinkContract = new Button
            {
                Text = "Establish Freight Contract",
                Location = new Point(15, 65),
                Size = new Size(175, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(64, 100, 150),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnLinkContract.FlatAppearance.BorderSize = 0;
            btnLinkContract.Click += BtnLinkContract_Click;
            grpOperations.Controls.Add(btnLinkContract);

            btnUnlinkContract = new Button
            {
                Text = "Cancel Active Contract",
                Location = new Point(210, 65),
                Size = new Size(175, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(150, 60, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnUnlinkContract.FlatAppearance.BorderSize = 0;
            btnUnlinkContract.Click += BtnUnlinkContract_Click;
            grpOperations.Controls.Add(btnUnlinkContract);

            Label lblAdjustPrice = new Label
            {
                Text = "Adjust Outlet Unit Price ($10 - $100):",
                ForeColor = Color.White,
                Location = new Point(15, 105),
                Size = new Size(220, 18)
            };
            grpOperations.Controls.Add(lblAdjustPrice);

            trackPrice = new TrackBar
            {
                Location = new Point(15, 128),
                Size = new Size(280, 45),
                Minimum = 10,
                Maximum = 100,
                Value = 45,
                TickFrequency = 5,
                BackColor = Color.FromArgb(24, 28, 36),
                Enabled = false
            };
            trackPrice.Scroll += TrackPrice_Scroll;
            grpOperations.Controls.Add(trackPrice);

            lblPriceVal = new Label
            {
                Text = "$45.00",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 140, 200),
                Location = new Point(310, 128),
                Size = new Size(80, 30),
                TextAlign = ContentAlignment.TopCenter
            };
            grpOperations.Controls.Add(lblPriceVal);

            Controls.Add(grpOperations);

            // Right Bottom: Logistics Optimization Report
            GroupBox grpLogistics = new GroupBox
            {
                Text = "Freight Optimization Log",
                Location = new Point(445, 470),
                Size = new Size(400, 100),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            lblTotalGoodsProduced = new Label { Location = new Point(15, 20), Size = new Size(370, 18), ForeColor = Color.White, Text = "Goods in Factory Storage: 0" };
            lblTotalGoodsRetail = new Label { Location = new Point(15, 40), Size = new Size(370, 18), ForeColor = Color.White, Text = "Goods in Retail Warehouses: 0" };

            double logisticsDiscount = (1.0 - engine.GetActiveEffectMultiplier(ResearchEffect.LogisticsSavings)) * 100.0;
            lblLogisticsCost = new Label 
            { 
                Location = new Point(15, 60), 
                Size = new Size(370, 32), 
                ForeColor = Color.White, 
                Text = $"Freight pricing: Dist * Volume * $0.15\nR&D Transport Savings: {logisticsDiscount:F0}% reduction" 
            };

            grpLogistics.Controls.Add(lblTotalGoodsProduced);
            grpLogistics.Controls.Add(lblTotalGoodsRetail);
            grpLogistics.Controls.Add(lblLogisticsCost);
            Controls.Add(grpLogistics);

            // Close button
            Button btnClose = new Button
            {
                Text = "Close Operations Control",
                Location = new Point(445, 580),
                Size = new Size(400, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 56, 70),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 100, 150);
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
        }

        private void SetupCustomDrawing(ListView lv)
        {
            lv.OwnerDraw = true;
            lv.DrawColumnHeader += (s, e) =>
            {
                using Brush brush = new SolidBrush(Color.FromArgb(48, 56, 70));
                e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, lv.Font, e.Bounds, Color.FromArgb(180, 200, 230), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
            lv.DrawSubItem += (s, e) =>
            {
                bool isSelected = e.Item.Selected;
                using Brush bgBrush = new SolidBrush(isSelected ? Color.FromArgb(64, 100, 150) : (lv.BackColor));
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

                Color itemTextColor = Color.White;
                if (e.Item.SubItems[e.ColumnIndex].Text == "Offline" || e.Item.SubItems[e.ColumnIndex].Text == "Unpowered")
                {
                    itemTextColor = Color.FromArgb(240, 100, 100);
                }
                else if (e.Item.SubItems[e.ColumnIndex].Text == "Active" || e.Item.SubItems[e.ColumnIndex].Text == "Powered")
                {
                    itemTextColor = Color.FromArgb(100, 240, 140);
                }

                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, lv.Font, e.Bounds, itemTextColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
        }

        private void FactoryListView_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (factoryListView.SelectedItems.Count > 0)
            {
                var text = factoryListView.SelectedItems[0].Text;
                var coords = ParseCoords(text);
                selectedFactoryTile = engine.Grid[coords.Item1, coords.Item2];
                lblSelFactory.Text = $"Selected Factory: [{selectedFactoryTile.X}, {selectedFactoryTile.Y}]";
            }
            else
            {
                selectedFactoryTile = null;
                lblSelFactory.Text = "Selected Factory: None";
            }
            UpdateButtonsState();
        }

        private void RetailListView_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (retailListView.SelectedItems.Count > 0)
            {
                var text = retailListView.SelectedItems[0].Text;
                var coords = ParseCoords(text);
                selectedRetailTile = engine.Grid[coords.Item1, coords.Item2];

                lblSelRetail.Text = $"Selected Retail: [{selectedRetailTile.X}, {selectedRetailTile.Y}]";
                var rKey = new Tuple<int, int>(selectedRetailTile.X, selectedRetailTile.Y);
                if (engine.FreightContracts.TryGetValue(rKey, out var fCoords))
                {
                    lblSelRetail.Text += $" (Linked to Factory [{fCoords.Item1},{fCoords.Item2}])";
                }
                else
                {
                    lblSelRetail.Text += " (Free Delivery)";
                }

                trackPrice.Enabled = true;
                trackPrice.Value = (int)Math.Clamp(selectedRetailTile.RetailPrice, 10.0, 100.0);
                lblPriceVal.Text = $"${selectedRetailTile.RetailPrice:F2}";
            }
            else
            {
                selectedRetailTile = null;
                lblSelRetail.Text = "Selected Retail: None";
                trackPrice.Enabled = false;
                lblPriceVal.Text = "$45.00";
            }
            UpdateButtonsState();
        }

        private void TrackPrice_Scroll(object? sender, EventArgs? e)
        {
            if (selectedRetailTile != null)
            {
                selectedRetailTile.RetailPrice = trackPrice.Value;
                lblPriceVal.Text = $"${selectedRetailTile.RetailPrice:F2}";
                
                // Refresh retail ListView item values immediately
                foreach (ListViewItem item in retailListView.Items)
                {
                    if (item.Text == $"[{selectedRetailTile.X},{selectedRetailTile.Y}]")
                    {
                        item.SubItems[3].Text = $"{selectedRetailTile.Inventory:F0}/{selectedRetailTile.MaxInventory:F0}";
                    }
                }
            }
        }

        private void BtnLinkContract_Click(object? sender, EventArgs? e)
        {
            if (selectedFactoryTile != null && selectedRetailTile != null)
            {
                var rKey = new Tuple<int, int>(selectedRetailTile.X, selectedRetailTile.Y);
                var fKey = new Tuple<int, int>(selectedFactoryTile.X, selectedFactoryTile.Y);
                engine.FreightContracts[rKey] = fKey;

                RefreshLogisticsData();
                UpdateButtonsState();
                
                // Trigger selected label refresh
                RetailListView_SelectedIndexChanged(null, null);
            }
        }

        private void BtnUnlinkContract_Click(object? sender, EventArgs? e)
        {
            if (selectedRetailTile != null)
            {
                var rKey = new Tuple<int, int>(selectedRetailTile.X, selectedRetailTile.Y);
                engine.FreightContracts.Remove(rKey);

                RefreshLogisticsData();
                UpdateButtonsState();

                // Trigger selected label refresh
                RetailListView_SelectedIndexChanged(null, null);
            }
        }

        private void UpdateButtonsState()
        {
            btnLinkContract.Enabled = (selectedFactoryTile != null && selectedRetailTile != null);
            
            if (selectedRetailTile != null)
            {
                var rKey = new Tuple<int, int>(selectedRetailTile.X, selectedRetailTile.Y);
                btnUnlinkContract.Enabled = engine.FreightContracts.ContainsKey(rKey);
            }
            else
            {
                btnUnlinkContract.Enabled = false;
            }
        }

        private Tuple<int, int> ParseCoords(string text)
        {
            // Parses coords in format "[x,y]"
            var parts = text.Trim('[', ']').Split(',');
            return new Tuple<int, int>(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        private void RefreshLogisticsData()
        {
            factoryListView.Items.Clear();
            retailListView.Items.Clear();

            double totalFactoryGoods = 0;
            double totalRetailGoods = 0;

            for (int x = 0; x < GameEngine.MapSize; x++)
            {
                for (int y = 0; y < GameEngine.MapSize; y++)
                {
                    Tile tile = engine.Grid[x, y];
                    if (tile.Type == TileType.Factory)
                    {
                        ListViewItem item = new ListViewItem($"[{x},{y}]");
                        item.SubItems.Add(tile.Level.ToString());
                        item.SubItems.Add($"{tile.EmployeeCount}/{tile.MaxEmployees}");
                        item.SubItems.Add($"{tile.Inventory:F0}/{tile.MaxInventory:F0}");
                        
                        string status = "Active";
                        if (!tile.IsPowered) status = "Unpowered";
                        else if (!tile.HasRoadAccess) status = "No Road";
                        else if (tile.EmployeeCount == 0) status = "No Staff";
                        item.SubItems.Add(status);

                        factoryListView.Items.Add(item);
                        totalFactoryGoods += tile.Inventory;
                    }
                    else if (tile.Type == TileType.Retail)
                    {
                        ListViewItem item = new ListViewItem($"[{x},{y}]");
                        item.SubItems.Add(tile.Level.ToString());
                        item.SubItems.Add($"{tile.EmployeeCount}/{tile.MaxEmployees}");
                        item.SubItems.Add($"{tile.Inventory:F0}/{tile.MaxInventory:F0}");

                        string status = "Active";
                        if (!tile.IsPowered) status = "Unpowered";
                        else if (!tile.HasRoadAccess) status = "No Road";
                        else if (tile.EmployeeCount == 0) status = "No Staff";
                        item.SubItems.Add(status);

                        retailListView.Items.Add(item);
                        totalRetailGoods += tile.Inventory;
                    }
                }
            }

            lblTotalGoodsProduced.Text = $"Goods in Factory Storage: {totalFactoryGoods:F0} units";
            lblTotalGoodsRetail.Text = $"Goods in Retail Warehouses: {totalRetailGoods:F0} units";
            
            lblPriceIndex.Text = $"Retail Market Price: ${engine.CurrentMarketPrice:F2} / unit";
            lblMarketShare.Text = $"Player Market Share: {(engine.PlayerMarketShare * 100):F1}%";
        }
    }
}

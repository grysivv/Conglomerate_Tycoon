using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using TycoonGame.Core;
using TycoonGame.Rendering;

namespace TycoonGame.UI
{
    public class MainForm : Form
    {
        private readonly GameEngine engine;
        private MonoGamePanel gamePanel;
        
        // Timer for simulation ticks (runs at 60 FPS for fluid updates)
        private Timer simulationTimer;
        private int currentSpeedMultiplier = 1; // 0 = Pause, 1 = 1x, 2 = 2x, 3 = 5x
        private DateTime lastUpdateTime;
        
        // Sidebar controls
        private Label lblSelectedTileCoords;
        private Label lblSelectedTileType;
        private Label lblSelectedTileLevel;
        private Label lblSelectedTileMaint;
        private Label lblSelectedTilePower;
        private Label lblSelectedTileRoad;
        private Label lblSelectedTileInventory;
        private Label lblSelectedTileStaff;
        
        private ListBox lstAssignedEmployees;
        private ComboBox cmbUnassignedEmployees;
        private Button btnAssignEmployee;
        private Button btnUnassignEmployee;
        private Button btnUpgradeBuilding;

        // Bottom Bar controls
        private Label lblBottomCash;
        private Label lblBottomDate;
        private Label lblBottomShare;
        private Label lblBottomResearch;
        private Panel pnlBottomResearchBar;
        private Panel pnlBottomResearchFill;
        
        // Buttons
        private Button btnPause;
        private Button btnSpeed1x;
        private Button btnSpeed2x;
        private Button btnSpeed5x;
        
        private Button[] buildToolButtons;
        private BuildTool[] toolTypes;

        public MainForm()
        {
            engine = new GameEngine();
            InitializeComponent();
            
            lastUpdateTime = DateTime.Now;
            
            // Start simulation clock at 60 FPS for smooth time ticking
            simulationTimer = new Timer { Interval = 16 };
            simulationTimer.Tick += GameLoopTimer_Tick;
            simulationTimer.Start();
            
            // Trigger initial UI update
            UpdateBottomBar();
            UpdateSidebar(null);
        }

        private void InitializeComponent()
        {
            Text = "Business Tycoon Simulator (WinForms + MonoGame Hybrid)";
            Size = new Size(1280, 800);
            MinimumSize = new Size(1024, 768);
            BackColor = Color.FromArgb(24, 28, 36);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            StartPosition = FormStartPosition.CenterScreen;

            // Main Layout Panels
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.FromArgb(24, 28, 36)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78F)); // MonoGame Panel
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F)); // Sidebar Control Panel
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 90F)); // Game Grid area
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10F)); // Bottom Stats Bar
            Controls.Add(mainLayout);

            // 1. MonoGame Panel Setup
            gamePanel = new MonoGamePanel
            {
                Engine = engine,
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            gamePanel.TileSelected += GamePanel_TileSelected;
            gamePanel.MapChanged += (s, e) => UpdateSidebar(gamePanel.SelectedTile);
            mainLayout.Controls.Add(gamePanel, 0, 0);

            // 2. Sidebar Control Panel
            FlowLayoutPanel pnlSidebar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(32, 38, 48),
                Padding = new Padding(10),
                AutoScroll = true
            };
            mainLayout.Controls.Add(pnlSidebar, 1, 0);

            // Sidebar: Control Tickers
            Label lblControlsTitle = new Label { Text = "Simulation Controls", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(80, 140, 200), Size = new Size(220, 20), Margin = new Padding(0, 5, 0, 2) };
            pnlSidebar.Controls.Add(lblControlsTitle);

            // Simulation Speed panel
            FlowLayoutPanel pnlSpeed = new FlowLayoutPanel { Size = new Size(220, 38), FlowDirection = FlowDirection.LeftToRight };
            btnPause = CreateSpeedButton("||", 0);
            btnSpeed1x = CreateSpeedButton("1x", 1);
            btnSpeed2x = CreateSpeedButton("2x", 2);
            btnSpeed5x = CreateSpeedButton("5x", 3);
            
            pnlSpeed.Controls.Add(btnPause);
            pnlSpeed.Controls.Add(btnSpeed1x);
            pnlSpeed.Controls.Add(btnSpeed2x);
            pnlSpeed.Controls.Add(btnSpeed5x);
            pnlSidebar.Controls.Add(pnlSpeed);
            UpdateSpeedButtonColors();

            // Divider
            pnlSidebar.Controls.Add(new Label { Size = new Size(220, 1), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 5, 0, 5) });

            // Build Toolbox
            Label lblBuildTitle = new Label { Text = "Construction Toolbelt", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(80, 140, 200), Size = new Size(220, 20), Margin = new Padding(0, 5, 0, 5) };
            pnlSidebar.Controls.Add(lblBuildTitle);

            toolTypes = new[] 
            { 
                BuildTool.Inspect, 
                BuildTool.BuildRoad, 
                BuildTool.BuildOffice, 
                BuildTool.BuildFactory, 
                BuildTool.BuildRetail, 
                BuildTool.BuildPowerPlant, 
                BuildTool.Bulldozer 
            };
            
            string[] toolLabels = new[] 
            { 
                "Inspect / Select", 
                "Asphalt Road ($1K)", 
                "Office Complex ($30K)", 
                "Industrial Factory ($60K)", 
                "Retail Outlet ($40K)", 
                "Power Plant ($50K)", 
                "Heavy Bulldozer ($1K)" 
            };

            buildToolButtons = new Button[toolTypes.Length];
            for (int i = 0; i < toolTypes.Length; i++)
            {
                int index = i;
                buildToolButtons[i] = new Button
                {
                    Text = toolLabels[i],
                    Size = new Size(220, 30),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(48, 56, 70),
                    ForeColor = Color.White,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 2, 0, 2)
                };
                buildToolButtons[i].FlatAppearance.BorderSize = 0;
                buildToolButtons[i].Click += (s, e) => SetActiveTool(toolTypes[index]);
                pnlSidebar.Controls.Add(buildToolButtons[i]);
            }
            SetActiveTool(BuildTool.Inspect); // Select default

            // Divider
            pnlSidebar.Controls.Add(new Label { Size = new Size(220, 1), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 5, 0, 5) });

            // Inspector Details Panel
            Label lblInspectTitle = new Label { Text = "Selected Tile Details", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(80, 140, 200), Size = new Size(220, 20), Margin = new Padding(0, 5, 0, 5) };
            pnlSidebar.Controls.Add(lblInspectTitle);

            lblSelectedTileCoords = new Label { Text = "Grid Address: None", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileType = new Label { Text = "Structure Type: Grass", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileLevel = new Label { Text = "Structure Level: 0", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileMaint = new Label { Text = "Hourly Upkeep: $0.00", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTilePower = new Label { Text = "Grid Electricity: Offline", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileRoad = new Label { Text = "Road Accessibility: No", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileInventory = new Label { Text = "Inventory Stocks: 0 / 0", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileStaff = new Label { Text = "Employees Assigned: 0 / 0", Size = new Size(220, 18), ForeColor = Color.White };

            pnlSidebar.Controls.Add(lblSelectedTileCoords);
            pnlSidebar.Controls.Add(lblSelectedTileType);
            pnlSidebar.Controls.Add(lblSelectedTileLevel);
            pnlSidebar.Controls.Add(lblSelectedTileMaint);
            pnlSidebar.Controls.Add(lblSelectedTilePower);
            pnlSidebar.Controls.Add(lblSelectedTileRoad);
            pnlSidebar.Controls.Add(lblSelectedTileInventory);
            pnlSidebar.Controls.Add(lblSelectedTileStaff);

            btnUpgradeBuilding = new Button
            {
                Text = "Upgrade Building",
                Size = new Size(220, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 200, 120),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0, 5, 0, 5)
            };
            btnUpgradeBuilding.FlatAppearance.BorderSize = 0;
            btnUpgradeBuilding.Click += BtnUpgradeBuilding_Click;
            pnlSidebar.Controls.Add(btnUpgradeBuilding);

            // Employee assignment section inside sidebar
            Label lblStaffTitle = new Label { Text = "Assigned Personnel:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Size = new Size(220, 18), Margin = new Padding(0, 5, 0, 2) };
            pnlSidebar.Controls.Add(lblStaffTitle);

            lstAssignedEmployees = new ListBox
            {
                Size = new Size(220, 60),
                BackColor = Color.FromArgb(48, 52, 64),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            pnlSidebar.Controls.Add(lstAssignedEmployees);

            btnUnassignEmployee = new Button
            {
                Text = "Unassign Staff Member",
                Size = new Size(220, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(140, 40, 40),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 5)
            };
            btnUnassignEmployee.FlatAppearance.BorderSize = 0;
            btnUnassignEmployee.Click += BtnUnassignEmployee_Click;
            pnlSidebar.Controls.Add(btnUnassignEmployee);

            Label lblAssignTitle = new Label { Text = "Deploy Available Staff:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Size = new Size(220, 18), Margin = new Padding(0, 5, 0, 2) };
            pnlSidebar.Controls.Add(lblAssignTitle);

            cmbUnassignedEmployees = new ComboBox
            {
                Size = new Size(220, 25),
                BackColor = Color.FromArgb(48, 52, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            pnlSidebar.Controls.Add(cmbUnassignedEmployees);

            btnAssignEmployee = new Button
            {
                Text = "Deploy Staff Member",
                Size = new Size(220, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(64, 100, 150),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2)
            };
            btnAssignEmployee.FlatAppearance.BorderSize = 0;
            btnAssignEmployee.Click += BtnAssignEmployee_Click;
            pnlSidebar.Controls.Add(btnAssignEmployee);

            // 3. Bottom Dashboard panel
            TableLayoutPanel pnlBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.FromArgb(32, 38, 48),
                Padding = new Padding(10, 5, 10, 5)
            };
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F)); // Capital info
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Time info
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F)); // Macro info (increased size slightly)
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Active Research info
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F)); // Executive buttons
            mainLayout.Controls.Add(pnlBottom, 0, 1);
            mainLayout.SetColumnSpan(pnlBottom, 2);

            // Bottom Column 1: Financial status
            lblBottomCash = new Label
            {
                Text = "Cash Reserves:\n$500,000.00",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 240, 140),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBottom.Controls.Add(lblBottomCash, 0, 0);

            // Bottom Column 2: Date clock
            lblBottomDate = new Label
            {
                Text = "Calendar Date:\n06/06/2026 08:00",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBottom.Controls.Add(lblBottomDate, 1, 0);

            // Bottom Column 3: Macroeconomic indices
            lblBottomShare = new Label
            {
                Text = "Macro Climate:\nGDP: 100.0 (Recovery)\nInt: 5.0% / CCI: 1.00",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBottom.Controls.Add(lblBottomShare, 2, 0);

            // Bottom Column 4: R&D Tech Ticker
            FlowLayoutPanel pnlResearchTally = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 0)
            };
            lblBottomResearch = new Label
            {
                Text = "Research Progress: Idle",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.White,
                Size = new Size(220, 15),
                AutoSize = true
            };
            pnlResearchTally.Controls.Add(lblBottomResearch);

            pnlBottomResearchBar = new Panel
            {
                Size = new Size(220, 10),
                BackColor = Color.FromArgb(48, 52, 64),
                Margin = new Padding(0, 5, 0, 0)
            };
            pnlBottomResearchFill = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(0, 10),
                BackColor = Color.FromArgb(230, 140, 80)
            };
            pnlBottomResearchBar.Controls.Add(pnlBottomResearchFill);
            pnlResearchTally.Controls.Add(pnlBottomResearchBar);
            pnlBottom.Controls.Add(pnlResearchTally, 3, 0);

            // Bottom Column 5: Executive windows buttons
            FlowLayoutPanel pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 5, 0, 0)
            };
            
            Button btnHR = CreateExecutiveButton("Staff", (s, e) => new HRWindow(engine).ShowDialog(this));
            Button btnRD = CreateExecutiveButton("R&D", (s, e) => new ResearchWindow(engine).ShowDialog(this));
            Button btnLogistics = CreateExecutiveButton("Logistics", (s, e) => new LogisticsWindow(engine).ShowDialog(this));
            Button btnFinance = CreateExecutiveButton("Finance", (s, e) => new AnalyticsWindow(engine).ShowDialog(this));

            pnlButtons.Controls.Add(btnHR);
            pnlButtons.Controls.Add(btnRD);
            pnlButtons.Controls.Add(btnLogistics);
            pnlButtons.Controls.Add(btnFinance);
            pnlBottom.Controls.Add(pnlButtons, 4, 0);
        }

        private Button CreateSpeedButton(string txt, int speedVal)
        {
            Button btn = new Button
            {
                Text = txt,
                Size = new Size(45, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => SetSimulationSpeed(speedVal);
            return btn;
        }

        private Button CreateExecutiveButton(string txt, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = txt,
                Size = new Size(54, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 56, 70),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 140, 200);
            btn.Click += onClick;
            return btn;
        }

        private void SetSimulationSpeed(int speedVal)
        {
            currentSpeedMultiplier = speedVal;
            UpdateSpeedButtonColors();
        }

        private void UpdateSpeedButtonColors()
        {
            Color activeColor = Color.FromArgb(80, 140, 200);
            Color idleColor = Color.FromArgb(48, 56, 70);

            btnPause.BackColor = currentSpeedMultiplier == 0 ? activeColor : idleColor;
            btnSpeed1x.BackColor = currentSpeedMultiplier == 1 ? activeColor : idleColor;
            btnSpeed2x.BackColor = currentSpeedMultiplier == 2 ? activeColor : idleColor;
            btnSpeed5x.BackColor = currentSpeedMultiplier == 3 ? activeColor : idleColor;
        }

        private void SetActiveTool(BuildTool tool)
        {
            gamePanel.ActiveTool = tool;

            for (int i = 0; i < toolTypes.Length; i++)
            {
                buildToolButtons[i].BackColor = toolTypes[i] == tool ? Color.FromArgb(80, 140, 200) : Color.FromArgb(48, 56, 70);
            }
        }

        private void GameLoopTimer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double dt = (now - lastUpdateTime).TotalSeconds;
            lastUpdateTime = now;

            // Clamp delta time to prevent massive jumps on window drag
            dt = Math.Min(dt, 0.1);

            double speed = currentSpeedMultiplier switch
            {
                0 => 0.0,
                1 => 1.0,
                2 => 2.0,
                3 => 5.0,
                _ => 0.0
            };

            // Fluid simulation update
            engine.Update(dt, speed);

            // Redraw game grid
            gamePanel.Invalidate();
            
            // Sync bottom stats and sidebar
            UpdateBottomBar();
            UpdateSidebar(gamePanel.SelectedTile);
        }

        private void UpdateBottomBar()
        {
            lblBottomCash.Text = $"Cash Reserves:\n${engine.Stats.Cash:N2}";
            lblBottomCash.ForeColor = engine.Stats.Cash < 0 ? Color.FromArgb(240, 100, 100) : Color.FromArgb(100, 240, 140);
            
            lblBottomDate.Text = $"Calendar Date:\n{engine.CurrentDate:MM/dd/yyyy HH:mm}";

            // Dynamically show the cycle phase and key macro indicators
            string phaseStr = engine.CyclePhase.ToString();
            lblBottomShare.Text = $"Macro: GDP {engine.GDP_Index:F1} ({phaseStr})\nInt: {engine.Interest_Rate * 100:F1}% / CCI: {engine.ConsumerConfidenceIndex:F2}";
            lblBottomShare.ForeColor = engine.CyclePhase switch
            {
                CyclePhase.Boom => Color.FromArgb(100, 240, 140),
                CyclePhase.Recovery => Color.FromArgb(140, 200, 250),
                CyclePhase.Slowdown => Color.FromArgb(230, 140, 80),
                CyclePhase.Recession => Color.FromArgb(240, 100, 100),
                _ => Color.White
            };

            if (engine.ActiveResearch != null)
            {
                double progress = engine.ActiveResearch.GetProgressPercentage();
                lblBottomResearch.Text = $"Research: {engine.ActiveResearch.Name} ({(progress * 100):F0}%)";
                pnlBottomResearchFill.Width = (int)(pnlBottomResearchBar.Width * progress);
            }
            else
            {
                lblBottomResearch.Text = "Research: Idle";
                pnlBottomResearchFill.Width = 0;
            }
        }

        private void GamePanel_TileSelected(object? sender, Tuple<int, int>? tileCoords)
        {
            UpdateSidebar(tileCoords);
        }

        private void UpdateSidebar(Tuple<int, int>? tileCoords)
        {
            if (tileCoords == null)
            {
                lblSelectedTileCoords.Text = "Grid Address: None";
                lblSelectedTileType.Text = "Structure Type: Grass";
                lblSelectedTileLevel.Text = "Structure Level: 0";
                lblSelectedTileMaint.Text = "Hourly Upkeep: $0.00";
                lblSelectedTilePower.Text = "Grid Electricity: Offline";
                lblSelectedTileRoad.Text = "Road Accessibility: No";
                lblSelectedTileInventory.Text = "Inventory Stocks: 0 / 0";
                lblSelectedTileStaff.Text = "Employees Assigned: 0 / 0";
                
                btnUpgradeBuilding.Visible = false;
                lstAssignedEmployees.Items.Clear();
                cmbUnassignedEmployees.Items.Clear();
                btnAssignEmployee.Enabled = false;
                btnUnassignEmployee.Enabled = false;
                return;
            }

            int tx = tileCoords.Item1;
            int ty = tileCoords.Item2;
            Tile tile = engine.Grid[tx, ty];

            lblSelectedTileCoords.Text = $"Grid Address: [{tx}, {ty}]";
            lblSelectedTileType.Text = $"Structure Type: {tile.Type}";
            lblSelectedTileLevel.Text = $"Structure Level: {tile.Level}";
            lblSelectedTileMaint.Text = $"Hourly Upkeep: ${tile.MaintenanceCost:F2}";
            
            lblSelectedTilePower.Text = tile.IsPowered ? "Grid Electricity: Powered" : "Grid Electricity: Offline";
            lblSelectedTilePower.ForeColor = tile.IsPowered ? Color.FromArgb(100, 240, 140) : Color.FromArgb(240, 100, 100);

            lblSelectedTileRoad.Text = tile.HasRoadAccess ? "Road Accessibility: Active" : "Road Accessibility: No";
            lblSelectedTileRoad.ForeColor = tile.HasRoadAccess ? Color.FromArgb(100, 240, 140) : Color.FromArgb(240, 100, 100);

            if (tile.Type == TileType.Factory || tile.Type == TileType.Retail)
            {
                lblSelectedTileInventory.Text = $"Inventory Stocks: {tile.Inventory:F0} / {tile.MaxInventory:F0}";
            }
            else
            {
                lblSelectedTileInventory.Text = "Inventory Stocks: N/A";
            }

            if (tile.Type != TileType.Grass && tile.Type != TileType.Road)
            {
                lblSelectedTileStaff.Text = $"Employees Assigned: {tile.EmployeeCount} / {tile.MaxEmployees}";
                btnUpgradeBuilding.Visible = tile.Level < 3;
                
                double upgradeCost = tile.Type switch
                {
                    TileType.Office => 24000.0,
                    TileType.Factory => 48000.0,
                    TileType.Retail => 32000.0,
                    TileType.PowerPlant => 40000.0,
                    _ => 0
                };
                btnUpgradeBuilding.Text = $"Upgrade Block (${upgradeCost / 1000:F0}K)";
            }
            else
            {
                lblSelectedTileStaff.Text = "Employees Assigned: N/A";
                btnUpgradeBuilding.Visible = false;
            }

            // Populate assigned employees list
            lstAssignedEmployees.Items.Clear();
            var assigned = engine.Employees.Where(e => e.AssignedX == tx && e.AssignedY == ty).ToList();
            foreach (var emp in assigned)
            {
                lstAssignedEmployees.Items.Add($"{emp.Name} ({emp.Role})");
            }
            btnUnassignEmployee.Enabled = lstAssignedEmployees.Items.Count > 0;

            // Populate unassigned employee combobox based on building worker requirements
            cmbUnassignedEmployees.Items.Clear();
            var unassigned = engine.Employees.Where(e => e.AssignedX == -1).ToList();

            // Filter available workers by appropriate role
            // PowerPlant only takes Workers or Managers, etc.
            if (tile.Type == TileType.PowerPlant)
            {
                unassigned = unassigned.Where(e => e.Role == EmployeeRole.Worker || e.Role == EmployeeRole.Manager).ToList();
            }
            else if (tile.Type == TileType.Office)
            {
                // Offices accept any employee type
            }
            else if (tile.Type == TileType.Factory)
            {
                unassigned = unassigned.Where(e => e.Role == EmployeeRole.Worker || e.Role == EmployeeRole.Manager).ToList();
            }
            else if (tile.Type == TileType.Retail)
            {
                unassigned = unassigned.Where(e => e.Role == EmployeeRole.Worker || e.Role == EmployeeRole.Manager).ToList();
            }

            foreach (var emp in unassigned)
            {
                cmbUnassignedEmployees.Items.Add(new ComboBoxEmployeeItem(emp));
            }

            bool spaceAvailable = tile.EmployeeCount < tile.MaxEmployees;
            btnAssignEmployee.Enabled = spaceAvailable && cmbUnassignedEmployees.Items.Count > 0 && tile.Type != TileType.Grass && tile.Type != TileType.Road;
        }

        private void BtnAssignEmployee_Click(object? sender, EventArgs e)
        {
            if (gamePanel.SelectedTile == null || cmbUnassignedEmployees.SelectedItem == null) return;
            var item = (ComboBoxEmployeeItem)cmbUnassignedEmployees.SelectedItem;
            
            if (engine.AssignEmployee(item.Emp.Id, gamePanel.SelectedTile.Item1, gamePanel.SelectedTile.Item2))
            {
                UpdateSidebar(gamePanel.SelectedTile);
                gamePanel.Invalidate();
            }
        }

        private void BtnUnassignEmployee_Click(object? sender, EventArgs e)
        {
            if (gamePanel.SelectedTile == null || lstAssignedEmployees.SelectedIndex == -1) return;
            int idx = lstAssignedEmployees.SelectedIndex;
            var assigned = engine.Employees.Where(e => e.AssignedX == gamePanel.SelectedTile.Item1 && e.AssignedY == gamePanel.SelectedTile.Item2).ToList();
            
            if (idx >= 0 && idx < assigned.Count)
            {
                engine.UnassignEmployee(assigned[idx].Id);
                UpdateSidebar(gamePanel.SelectedTile);
                gamePanel.Invalidate();
            }
        }

        private void BtnUpgradeBuilding_Click(object? sender, EventArgs e)
        {
            if (gamePanel.SelectedTile == null) return;
            int tx = gamePanel.SelectedTile.Item1;
            int ty = gamePanel.SelectedTile.Item2;

            if (engine.UpgradeStructure(tx, ty))
            {
                UpdateSidebar(gamePanel.SelectedTile);
                UpdateBottomBar();
                gamePanel.Invalidate();
            }
            else
            {
                MessageBox.Show("Insufficient funds or maximum building level reached!", "Construction Services", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private class ComboBoxEmployeeItem
        {
            public Employee Emp { get; }
            public ComboBoxEmployeeItem(Employee emp) => Emp = emp;
            public override string ToString() => $"{Emp.Name} ({Emp.Role})";
        }
    }
}

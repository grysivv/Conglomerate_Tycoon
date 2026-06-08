using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using TycoonGame.Core;
using TycoonGame.Rendering;

namespace TycoonGame.UI
{
    public class MainForm : Form
    {
        // Lazy loaded game state
        private GameEngine? engine;
        private MonoGamePanel? gamePanel;
        
        // Simulation clock and ticking parameters
        private Timer? simulationTimer;
        private int currentSpeedMultiplier = 1; // 0 = Pause, 1 = 1x, 2 = 2x, 3 = 5x
        private DateTime lastUpdateTime;
        private double uiUpdateAccumulator = 0.0;
        private Tuple<int, int>? lastSelectedTileCoords = null;
        
        // Main Menu controls and layouts
        private Panel pnlMainMenu;
        private Panel pnlLeftMenu;
        private Panel pnlRightDetails;
        private TextBox txtCompanyName;
        private string selectedDifficulty = "Medium";
        private Button btnDiffEasy;
        private Button btnDiffMedium;
        private Button btnDiffHard;

        // Active game session UI panels
        private Panel pnlGameContainer;

        // Sidebar controls
        private Label lblSelectedTileCoords;
        private Label lblSelectedTileType;
        private Label lblSelectedTileLevel;
        private Label lblSelectedTileMaint;
        private Label lblSelectedTileLandValue;
        private Label lblSelectedTileTraffic;
        private Label lblSelectedTilePower;
        private Label lblSelectedTileRoad;
        private Label lblSelectedTileInventory;
        private Label lblSelectedTileStaff;
        
        private ListBox lstAssignedEmployees;
        private ComboBox cmbUnassignedEmployees;
        private Button btnAssignEmployee;
        private Button btnUnassignEmployee;
        private Button btnUpgradeBuilding;

        // Top Bar controls
        private Label lblTopCash;
        private Label lblTopCashflow;
        private Label lblTopStockValue;
        private Button btnTopLaunchIpo;
        private Label lblTopDate;

        // Bottom Bar controls
        private Label lblBottomShare;
        private Label lblBottomResearch;
        private Panel pnlBottomResearchBar;
        private Panel pnlBottomResearchFill;
        
        // Speed Buttons
        private Button btnPause;
        private Button btnSpeed1x;
        private Button btnSpeed2x;
        private Button btnSpeed5x;
        
        private Button[] buildToolButtons;
        private BuildTool[] toolTypes;

        public MainForm()
        {
            InitializeMainMenu();
        }

        #region Main Menu Layout & Logics

        private void InitializeMainMenu()
        {
            // Primary Application Window Parameters
            Text = "Business Tycoon Simulator (WinForms + MonoGame Hybrid)";
            Size = new Size(1280, 720);
            MinimumSize = new Size(1024, 720);
            BackColor = Color.FromArgb(11, 15, 25);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            StartPosition = FormStartPosition.CenterScreen;

            // Safely wipe any existing active controls
            this.Controls.Clear();

            // Root Main Menu Viewport Panel
            pnlMainMenu = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(11, 15, 25)
            };
            this.Controls.Add(pnlMainMenu);

            // Left vertical stack menu bar
            pnlLeftMenu = new Panel
            {
                Width = 320,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(15, 20, 35),
                Padding = new Padding(20)
            };
            pnlMainMenu.Controls.Add(pnlLeftMenu);

            // Accent separating line
            Panel pnlSeparator = new Panel
            {
                Width = 2,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(40, 48, 68)
            };
            pnlMainMenu.Controls.Add(pnlSeparator);

            // Dynamic view area for setup / grids
            pnlRightDetails = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(11, 15, 25),
                Padding = new Padding(40)
            };
            pnlMainMenu.Controls.Add(pnlRightDetails);

            // Title accent blocks
            Label lblTitle = new Label
            {
                Text = "KRONOS NETWORKS",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 240, 255),
                AutoSize = true,
                Location = new Point(20, 30)
            };
            pnlLeftMenu.Controls.Add(lblTitle);

            Label lblSubTitle = new Label
            {
                Text = "TYCOON SIMULATION CONSOLE",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(113, 128, 150),
                AutoSize = true,
                Location = new Point(22, 65)
            };
            pnlLeftMenu.Controls.Add(lblSubTitle);

            // Stack panel for button columns
            FlowLayoutPanel pnlButtonsFlow = new FlowLayoutPanel
            {
                Location = new Point(20, 120),
                Size = new Size(280, 500),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            pnlLeftMenu.Controls.Add(pnlButtonsFlow);

            // 1. Continue option (safely checks autosave.sav presence)
            Button btnContinue = CreateMenuButton("CONTINUE (Kontynuuj)", BtnContinue_Click);
            bool autosaveExists = File.Exists("autosave.sav");
            if (autosaveExists)
            {
                btnContinue.Enabled = true;
                btnContinue.ForeColor = Color.FromArgb(0, 255, 102);
                btnContinue.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 102);
                btnContinue.MouseEnter += (s, e) => btnContinue.ForeColor = Color.Black;
                btnContinue.MouseLeave += (s, e) => btnContinue.ForeColor = Color.FromArgb(0, 255, 102);
            }
            else
            {
                btnContinue.Enabled = false;
                btnContinue.BackColor = Color.FromArgb(20, 24, 33);
                btnContinue.ForeColor = Color.FromArgb(70, 80, 95);
                btnContinue.FlatAppearance.BorderColor = Color.FromArgb(40, 48, 60);
            }
            pnlButtonsFlow.Controls.Add(btnContinue);

            // 2. New Game option (Nowa Gra)
            Button btnNewGame = CreateMenuButton("NEW GAME (Nowa Gra)", BtnNewGame_Click);
            pnlButtonsFlow.Controls.Add(btnNewGame);

            // 3. Load Game slot subgrid (Wczytaj)
            Button btnLoadGame = CreateMenuButton("LOAD GAME (Wczytaj)", BtnLoadGame_Click);
            pnlButtonsFlow.Controls.Add(btnLoadGame);

            // 4. Settings placeholder
            Button btnSettings = CreateMenuButton("SETTINGS (Ustawienia)", BtnSettings_Click);
            pnlButtonsFlow.Controls.Add(btnSettings);

            // 5. Hard Exit command
            Button btnExit = CreateMenuButton("EXIT SYSTEM (Wyjście)", (s, e) => Application.Exit());
            btnExit.ForeColor = Color.FromArgb(255, 80, 80);
            btnExit.FlatAppearance.BorderColor = Color.FromArgb(255, 80, 80);
            btnExit.MouseEnter += (s, e) => btnExit.ForeColor = Color.Black;
            btnExit.MouseLeave += (s, e) => btnExit.ForeColor = Color.FromArgb(255, 80, 80);
            pnlButtonsFlow.Controls.Add(btnExit);

            ShowDefaultWelcomeScreen();

            EnableDoubleBuffered(this);
            EnableDoubleBuffered(pnlLeftMenu);
            EnableDoubleBuffered(pnlRightDetails);
        }

        private void ShowDefaultWelcomeScreen()
        {
            pnlRightDetails.Controls.Clear();

            TextBox txtTerminal = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(11, 15, 25),
                ForeColor = Color.FromArgb(0, 240, 255),
                Font = new Font("Consolas", 10F, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Text = "========================================================================\r\n" +
                       "              KRONOS OPERATING SYSTEM - VERTEX ENGINE v2.5\r\n" +
                       "              ESTABLISHING QUANTUM ENCRYPTED SIMULATION CLIENT...\r\n" +
                       "========================================================================\r\n\r\n" +
                       " [SYSTEM LOG] SECURE NODE CONNECTION: ESTABLISHED\r\n" +
                       " [SYSTEM LOG] CORE MEMORY STACK: READY\r\n" +
                       " [SYSTEM LOG] GRAPHICS PIPELINE: MONOGAME LAZY PIPELINE STANDBY\r\n" +
                       " [SYSTEM LOG] GRID SYSTEMS: 120 x 120 SECTORS (14,400 TILE CHANNELS)\r\n" +
                       " [SYSTEM LOG] COMPETITOR SIMULATORS: STANDBY\r\n\r\n" +
                       " SELECT CORRESPONDING COMMAND OPERATION FROM THE LEFT CONSOLE PANEL..."
            };
            pnlRightDetails.Controls.Add(txtTerminal);
        }

        private void BtnNewGame_Click(object? sender, EventArgs e)
        {
            pnlRightDetails.Controls.Clear();
            pnlLeftMenu.Visible = false; // Hide primary button column

            TableLayoutPanel pnlSetup = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(15, 20, 35)
            };
            pnlSetup.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); // Title
            pnlSetup.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Name Label
            pnlSetup.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); // Name TextBox
            pnlSetup.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Difficulty Label
            pnlSetup.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F)); // Difficulty buttons row
            pnlSetup.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Spacer
            pnlSetup.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Launch empire
            pnlSetup.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Abort
            pnlRightDetails.Controls.Add(pnlSetup);

            Label lblSetupTitle = new Label
            {
                Text = "INITIALIZE NEW CORPORATE EMPIRE CORE",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 240, 255),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlSetup.Controls.Add(lblSetupTitle, 0, 0);

            Label lblName = new Label
            {
                Text = "CORPORATE NAME (Nazwa firmy):",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlSetup.Controls.Add(lblName, 0, 1);

            txtCompanyName = new TextBox
            {
                Text = "KRONOS INDUSTRIES",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                BackColor = Color.FromArgb(26, 32, 44),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 10)
            };
            pnlSetup.Controls.Add(txtCompanyName, 0, 2);

            Label lblDiff = new Label
            {
                Text = "SELECT INITIAL FINANCE STRATEGY:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlSetup.Controls.Add(lblDiff, 0, 3);

            TableLayoutPanel pnlDiffButtons = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            pnlDiffButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlDiffButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlDiffButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlSetup.Controls.Add(pnlDiffButtons, 0, 4);

            btnDiffEasy = new Button
            {
                Text = "CONSERVATIVE SEEDING\r\n$800K Cash / $100K Debt",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(26, 32, 44),
                Margin = new Padding(2)
            };
            btnDiffEasy.FlatAppearance.BorderSize = 1;
            btnDiffEasy.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
            btnDiffEasy.Click += (s, e) => SelectDifficulty("Easy");

            btnDiffMedium = new Button
            {
                Text = "BALANCED ENTRY\r\n$500K Cash / $250K Debt",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(26, 32, 44),
                Margin = new Padding(2)
            };
            btnDiffMedium.FlatAppearance.BorderSize = 1;
            btnDiffMedium.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
            btnDiffMedium.Click += (s, e) => SelectDifficulty("Medium");

            btnDiffHard = new Button
            {
                Text = "AGGRESSIVE LEVERAGE\r\n$250K Cash / $400K Debt",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(26, 32, 44),
                Margin = new Padding(2)
            };
            btnDiffHard.FlatAppearance.BorderSize = 1;
            btnDiffHard.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
            btnDiffHard.Click += (s, e) => SelectDifficulty("Hard");

            pnlDiffButtons.Controls.Add(btnDiffEasy, 0, 0);
            pnlDiffButtons.Controls.Add(btnDiffMedium, 1, 0);
            pnlDiffButtons.Controls.Add(btnDiffHard, 2, 0);

            SelectDifficulty("Medium"); // default strategy

            Button btnLaunch = new Button
            {
                Text = "[LAUNCH EMPIRE]",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 255, 102),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 5, 0, 5)
            };
            btnLaunch.FlatAppearance.BorderSize = 0;
            btnLaunch.Click += BtnLaunch_Click;
            pnlSetup.Controls.Add(btnLaunch, 0, 6);

            Button btnCancel = new Button
            {
                Text = "[ABORT & RETURN]",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 5, 0, 5)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => {
                pnlLeftMenu.Visible = true;
                ShowDefaultWelcomeScreen();
            };
            pnlSetup.Controls.Add(btnCancel, 0, 7);
        }

        private void SelectDifficulty(string diff)
        {
            selectedDifficulty = diff;
            Color activeBorderColor = Color.FromArgb(0, 240, 255);
            Color idleBorderColor = Color.FromArgb(45, 55, 72);
            Color activeBgColor = Color.FromArgb(30, 45, 65);
            Color idleBgColor = Color.FromArgb(26, 32, 44);

            btnDiffEasy.FlatAppearance.BorderColor = diff == "Easy" ? activeBorderColor : idleBorderColor;
            btnDiffEasy.BackColor = diff == "Easy" ? activeBgColor : idleBgColor;
            btnDiffEasy.ForeColor = diff == "Easy" ? Color.FromArgb(0, 240, 255) : Color.White;

            btnDiffMedium.FlatAppearance.BorderColor = diff == "Medium" ? activeBorderColor : idleBorderColor;
            btnDiffMedium.BackColor = diff == "Medium" ? activeBgColor : idleBgColor;
            btnDiffMedium.ForeColor = diff == "Medium" ? Color.FromArgb(0, 240, 255) : Color.White;

            btnDiffHard.FlatAppearance.BorderColor = diff == "Hard" ? activeBorderColor : idleBorderColor;
            btnDiffHard.BackColor = diff == "Hard" ? activeBgColor : idleBgColor;
            btnDiffHard.ForeColor = diff == "Hard" ? Color.FromArgb(0, 240, 255) : Color.White;
        }

        private void BtnLaunch_Click(object? sender, EventArgs e)
        {
            string name = txtCompanyName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a valid Corporate Name!", "Simulation Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Lazy initialization of game engine (grid memory allocated now)
            engine = new GameEngine();
            engine.Stats.CompanyName = name;

            // Set cash/debt allocation configurations
            if (selectedDifficulty == "Easy")
            {
                engine.Stats.Cash = 800000.0;
                engine.Stats.LoanBalance = 100000.0;
            }
            else if (selectedDifficulty == "Medium")
            {
                engine.Stats.Cash = 500000.0;
                engine.Stats.LoanBalance = 250000.0;
            }
            else if (selectedDifficulty == "Hard")
            {
                engine.Stats.Cash = 250000.0;
                engine.Stats.LoanBalance = 400000.0;
            }

            double initialAssetVal = 0;
            for (int x = 0; x < GameEngine.MapSize; x++)
            {
                for (int y = 0; y < GameEngine.MapSize; y++)
                {
                    initialAssetVal += engine.Grid[x, y].GetAssetValue();
                }
            }
            engine.Stats.UpdateCachedValues(initialAssetVal, 0.0);
            engine.Stats.UpdatePlayerStockPrice(initialAssetVal);
            engine.Stats.UpdateAiStockPrice();

            // Populate the fully functional tycoon viewport and controls
            InitializeGameUI();
            
            pnlMainMenu.Visible = false;
            pnlGameContainer.Visible = true;

            // Start simulation cycle
            lastUpdateTime = DateTime.Now;
            simulationTimer.Start();
        }

        private void BtnLoadGame_Click(object? sender, EventArgs e)
        {
            pnlRightDetails.Controls.Clear();
            pnlLeftMenu.Visible = false;

            TableLayoutPanel pnlLoad = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 13,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(15, 20, 35)
            };
            pnlLoad.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Title
            pnlLoad.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F)); // Spacer
            for (int i = 0; i < 10; i++)
            {
                pnlLoad.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            }
            pnlLoad.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlLoad.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Return
            pnlRightDetails.Controls.Add(pnlLoad);

            Label lblTitle = new Label
            {
                Text = "RESTORE CORPORATE SIMULATION STATE",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 240, 255),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlLoad.Controls.Add(lblTitle, 0, 0);

            // Populate itemized subgrid (10 slots)
            for (int i = 1; i <= 10; i++)
            {
                int slotIndex = i;
                string metadata = GetSlotMetadataText(slotIndex);
                bool hasSave = !string.IsNullOrEmpty(metadata);

                Button btnSlot = new Button
                {
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = hasSave ? Cursors.Hand : Cursors.Default,
                    Margin = new Padding(0, 3, 0, 3)
                };

                if (hasSave)
                {
                    btnSlot.Text = $"[ SLOT {slotIndex} ] - {metadata}";
                    btnSlot.BackColor = Color.FromArgb(26, 40, 55);
                    btnSlot.ForeColor = Color.FromArgb(0, 255, 102);
                    btnSlot.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 102);
                    btnSlot.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 255, 102);
                    btnSlot.MouseEnter += (s, e) => btnSlot.ForeColor = Color.Black;
                    btnSlot.MouseLeave += (s, e) => btnSlot.ForeColor = Color.FromArgb(0, 255, 102);
                    btnSlot.Click += (s, e) => LoadSlotGame(slotIndex);
                }
                else
                {
                    btnSlot.Text = $"[ EMPTY SLOT {slotIndex} ]";
                    btnSlot.BackColor = Color.FromArgb(20, 24, 33);
                    btnSlot.ForeColor = Color.FromArgb(70, 80, 95);
                    btnSlot.FlatAppearance.BorderColor = Color.FromArgb(40, 48, 60);
                    btnSlot.Enabled = false;
                }

                pnlLoad.Controls.Add(btnSlot, 0, i + 1);
            }

            Button btnBack = new Button
            {
                Text = "[ RETURN TO CORE MENU ]",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 5, 0, 5)
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => {
                pnlLeftMenu.Visible = true;
                ShowDefaultWelcomeScreen();
            };
            pnlLoad.Controls.Add(btnBack, 0, 12);
        }

        private void LoadSlotGame(int slotIndex)
        {
            string fileName = $"save_slot_{slotIndex}.json";
            if (!File.Exists(fileName)) return;

            try
            {
                engine = new GameEngine();
                engine.LoadFromFile(fileName);

                InitializeGameUI();

                pnlMainMenu.Visible = false;
                pnlGameContainer.Visible = true;

                lastUpdateTime = DateTime.Now;
                simulationTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restore simulation state: {ex.Message}", "State Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnContinue_Click(object? sender, EventArgs e)
        {
            if (!File.Exists("autosave.sav")) return;

            try
            {
                engine = new GameEngine();
                engine.LoadFromFile("autosave.sav");

                InitializeGameUI();

                pnlMainMenu.Visible = false;
                pnlGameContainer.Visible = true;

                lastUpdateTime = DateTime.Now;
                simulationTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to continue autosave state: {ex.Message}", "Quicksave Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            pnlRightDetails.Controls.Clear();
            pnlLeftMenu.Visible = false;

            TableLayoutPanel pnlSettings = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(15, 20, 35)
            };
            pnlSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Title
            pnlSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F)); // Terminal placeholder
            pnlSettings.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Spacer
            pnlSettings.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Back
            pnlRightDetails.Controls.Add(pnlSettings);

            Label lblTitle = new Label
            {
                Text = "SYSTEM CONFIGURATION DESK",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 240, 255),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlSettings.Controls.Add(lblTitle, 0, 0);

            TextBox txtTerminal = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(20, 25, 40),
                ForeColor = Color.FromArgb(255, 153, 0), // Amber terminal text
                Font = new Font("Consolas", 10F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Text = "+-------------------------------------------------------------+\r\n" +
                       "|          SYSTEM CONFIGURATION - UNDER CONSTRUCTION          |\r\n" +
                       "+-------------------------------------------------------------+\r\n" +
                       "| [INFO]  AUDIO INTERRUPTS: ENABLED                           |\r\n" +
                       "| [INFO]  NEURAL SYNAPSE RATE: 60Hz                           |\r\n" +
                       "| [WARN]  EXTERNAL NETWORK ACCESS: LOCKED BY FIREWALL         |\r\n" +
                       "| [INFO]  RENDER MULTITHREADING: ACTIVE                       |\r\n" +
                       "| [ERR]   CONFIG PROTOCOL: ENHANCED RESOLUTION NEEDED         |\r\n" +
                       "+-------------------------------------------------------------+"
            };
            pnlSettings.Controls.Add(txtTerminal, 0, 1);

            Button btnBack = new Button
            {
                Text = "[ RETURN TO CORE MENU ]",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 72),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 5, 0, 5)
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => {
                pnlLeftMenu.Visible = true;
                ShowDefaultWelcomeScreen();
            };
            pnlSettings.Controls.Add(btnBack, 0, 3);
        }

        private string GetSlotMetadataText(int slotIndex)
        {
            string fileName = $"save_slot_{slotIndex}.json";
            if (File.Exists(fileName))
            {
                try
                {
                    string json = File.ReadAllText(fileName);
                    var data = JsonSerializer.Deserialize<SaveData>(json);
                    if (data != null)
                    {
                        string formattedDate = "Unknown Date";
                        if (DateTime.TryParse(data.CurrentDate, out DateTime parsedDate))
                        {
                            formattedDate = parsedDate.ToString("dd MMM yyyy HH:mm");
                        }
                        else if (!string.IsNullOrEmpty(data.CurrentDate))
                        {
                            formattedDate = data.CurrentDate;
                        }
                        return $"{data.CompanyName} ({formattedDate})";
                    }
                }
                catch
                {
                    return "[ CORRUPT SAVE ]";
                }
            }
            return "";
        }

        private Button CreateMenuButton(string text, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(280, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(26, 32, 44),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 5, 0, 5)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 240, 255);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 180, 200);

            btn.MouseEnter += (s, e) => {
                if (btn.Enabled) btn.ForeColor = Color.Black;
            };
            btn.MouseLeave += (s, e) => {
                if (btn.Enabled) btn.ForeColor = (text.StartsWith("CONTINUE") ? Color.FromArgb(0, 255, 102) : Color.White);
            };
            btn.Click += onClick;
            return btn;
        }

        #endregion

        #region Game Simulation UI Layout & Logic

        private void InitializeGameUI()
        {
            // Wipe main menu controls
            this.Controls.Remove(pnlMainMenu);

            // Create simulation main container
            pnlGameContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 28, 36)
            };
            this.Controls.Add(pnlGameContainer);

            // Primary Layout Panels
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.FromArgb(24, 28, 36)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78F)); // MonoGame Panel
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F)); // Sidebar Control Panel
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); // Top Bar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Middle (Viewport + Sidebar)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F)); // Bottom Bar
            pnlGameContainer.Controls.Add(mainLayout);

            // 0. Top Bar Panel Setup
            TableLayoutPanel pnlTop = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(32, 38, 48),
                Padding = new Padding(10, 5, 10, 5)
            };
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Left aligned group
            pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Right aligned group
            mainLayout.Controls.Add(pnlTop, 0, 0);
            mainLayout.SetColumnSpan(pnlTop, 2);

            // Top Bar LEFT Flow Group
            FlowLayoutPanel pnlTopLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 5, 0, 0)
            };

            lblTopCash = new Label
            {
                Text = "Cash Reserves: $500,000.00",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 240, 140),
                AutoSize = true,
                Margin = new Padding(0, 0, 20, 0)
            };
            pnlTopLeft.Controls.Add(lblTopCash);

            lblTopCashflow = new Label
            {
                Text = "Monthly Cashflow: +$30,000.00",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 200, 250),
                AutoSize = true,
                Margin = new Padding(0, 0, 20, 0)
            };
            pnlTopLeft.Controls.Add(lblTopCashflow);

            lblTopStockValue = new Label
            {
                Text = "Player Stock Value: #N/A",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 175, 190),
                AutoSize = true,
                Margin = new Padding(0, 0, 10, 0)
            };
            pnlTopLeft.Controls.Add(lblTopStockValue);

            btnTopLaunchIpo = new Button
            {
                Text = "Launch IPO",
                Size = new Size(90, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 200, 120),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Margin = new Padding(0)
            };
            btnTopLaunchIpo.FlatAppearance.BorderSize = 0;
            btnTopLaunchIpo.Click += BtnTopLaunchIpo_Click;
            pnlTopLeft.Controls.Add(btnTopLaunchIpo);

            pnlTop.Controls.Add(pnlTopLeft, 0, 0);

            // Top Bar RIGHT Flow Group
            FlowLayoutPanel pnlTopRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 3, 0, 0),
                AutoSize = true
            };

            lblTopDate = new Label
            {
                Text = "06 June 2026",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Margin = new Padding(0, 5, 20, 0),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlTopRight.Controls.Add(lblTopDate);

            btnPause = CreateSpeedButton("||", 0);
            btnSpeed1x = CreateSpeedButton("1x", 1);
            btnSpeed2x = CreateSpeedButton("2x", 2);
            btnSpeed5x = CreateSpeedButton("5x", 3);

            btnPause.Size = new Size(32, 26);
            btnSpeed1x.Size = new Size(32, 26);
            btnSpeed2x.Size = new Size(32, 26);
            btnSpeed5x.Size = new Size(32, 26);

            pnlTopRight.Controls.Add(btnPause);
            pnlTopRight.Controls.Add(btnSpeed1x);
            pnlTopRight.Controls.Add(btnSpeed2x);
            pnlTopRight.Controls.Add(btnSpeed5x);

            pnlTop.Controls.Add(pnlTopRight, 1, 0);

            // 1. MonoGame Viewport Panel (Hot-swap binded)
            gamePanel = new MonoGamePanel
            {
                Engine = engine,
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            gamePanel.TileSelected += GamePanel_TileSelected;
            gamePanel.MapChanged += (s, e) => UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: true);
            mainLayout.Controls.Add(gamePanel, 0, 1);

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
            mainLayout.Controls.Add(pnlSidebar, 1, 1);

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
                BuildTool.BuildApartment,
                BuildTool.BuildUniversity,
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
                "Residential Apartment ($50K)",
                "Collegiate University ($80K)",
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
            lblSelectedTileLandValue = new Label { Text = "Local Land Value: $0.00", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileTraffic = new Label { Text = "Foot Traffic Index: 0", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTilePower = new Label { Text = "Grid Electricity: Offline", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileRoad = new Label { Text = "Road Accessibility: No", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileInventory = new Label { Text = "Inventory Stocks: 0 / 0", Size = new Size(220, 18), ForeColor = Color.White };
            lblSelectedTileStaff = new Label { Text = "Employees Assigned: 0 / 0", Size = new Size(220, 18), ForeColor = Color.White };

            pnlSidebar.Controls.Add(lblSelectedTileCoords);
            pnlSidebar.Controls.Add(lblSelectedTileType);
            pnlSidebar.Controls.Add(lblSelectedTileLevel);
            pnlSidebar.Controls.Add(lblSelectedTileMaint);
            pnlSidebar.Controls.Add(lblSelectedTileLandValue);
            pnlSidebar.Controls.Add(lblSelectedTileTraffic);
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
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.FromArgb(32, 38, 48),
                Padding = new Padding(10, 5, 10, 5)
            };
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F)); // Macro info
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F)); // Active Research info
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F)); // Executive buttons
            mainLayout.Controls.Add(pnlBottom, 0, 2);
            mainLayout.SetColumnSpan(pnlBottom, 2);

            // Bottom Column 0: Macroeconomic Climate
            lblBottomShare = new Label
            {
                Text = "Macro Climate:\nGDP: 100.0 (Recovery)\nInt: 5.0% / CCI: 1.00",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBottom.Controls.Add(lblBottomShare, 0, 0);

            // Bottom Column 1: R&D Tech Ticker
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
            pnlBottom.Controls.Add(pnlResearchTally, 1, 0);

            // Bottom Column 2: Executive windows buttons
            FlowLayoutPanel pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 5, 0, 0)
            };
            
            Button btnHR = CreateExecutiveButton("Staff", (s, e) => {
                new HRWindow(engine).ShowDialog(this);
                UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: true);
                UpdateBottomBar();
            });
            Button btnRD = CreateExecutiveButton("R&D", (s, e) => {
                new ResearchWindow(engine).ShowDialog(this);
                UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: true);
                UpdateBottomBar();
            });
            Button btnLogistics = CreateExecutiveButton("Logistics", (s, e) => {
                new LogisticsWindow(engine).ShowDialog(this);
                UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: true);
                UpdateBottomBar();
            });
            Button btnFinance = CreateExecutiveButton("Finance", (s, e) => {
                new AnalyticsWindow(engine).ShowDialog(this);
                UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: true);
                UpdateBottomBar();
            });
            Button btnSystem = CreateExecutiveButton("System", (s, e) => {
                ShowSystemMenu();
            });

            pnlButtons.Controls.Add(btnHR);
            pnlButtons.Controls.Add(btnRD);
            pnlButtons.Controls.Add(btnLogistics);
            pnlButtons.Controls.Add(btnFinance);
            pnlButtons.Controls.Add(btnSystem);
            pnlBottom.Controls.Add(pnlButtons, 2, 0);

            // Setup Simulation Timer (Ticking loop)
            simulationTimer = new Timer { Interval = 16 };
            simulationTimer.Tick += GameLoopTimer_Tick;

            // Enable Double Buffering to reduce repaint flickering
            EnableDoubleBuffered(pnlGameContainer);
            EnableDoubleBuffered(mainLayout);
            EnableDoubleBuffered(pnlSidebar);
            EnableDoubleBuffered(pnlBottom);
            EnableDoubleBuffered(pnlResearchTally);
            EnableDoubleBuffered(pnlButtons);
            EnableDoubleBuffered(pnlBottomResearchBar);
            EnableDoubleBuffered(pnlTop);
            EnableDoubleBuffered(pnlTopLeft);
            EnableDoubleBuffered(pnlTopRight);
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
            if (gamePanel == null) return;
            gamePanel.ActiveTool = tool;

            for (int i = 0; i < toolTypes.Length; i++)
            {
                buildToolButtons[i].BackColor = toolTypes[i] == tool ? Color.FromArgb(80, 140, 200) : Color.FromArgb(48, 56, 70);
            }
        }

        private void GameLoopTimer_Tick(object? sender, EventArgs e)
        {
            if (engine == null || gamePanel == null) return;

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
            
            // Decouple and throttle textual UI updates to 10 FPS (100ms) to prevent flickering
            uiUpdateAccumulator += dt;
            if (uiUpdateAccumulator >= 0.1)
            {
                UpdateBottomBar();
                UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: false);
                uiUpdateAccumulator = 0.0;
            }
        }

        private void SetLabelText(Label label, string newText)
        {
            if (label.Text != newText)
            {
                label.Text = newText;
            }
        }

        private void SetLabelForeColor(Label label, Color newColor)
        {
            if (label.ForeColor != newColor)
            {
                label.ForeColor = newColor;
            }
        }

        private void SetControlVisible(Control control, bool visible)
        {
            if (control.Visible != visible)
            {
                control.Visible = visible;
            }
        }

        private void SetControlEnabled(Control control, bool enabled)
        {
            if (control.Enabled != enabled)
            {
                control.Enabled = enabled;
            }
        }

        private void EnableDoubleBuffered(Control control)
        {
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", 
                     System.Reflection.BindingFlags.NonPublic | 
                     System.Reflection.BindingFlags.Instance)
                    ?.SetValue(control, true);
            }
            catch { }
        }

        private void UpdateBottomBar()
        {
            if (engine == null) return;

            // Update Top Bar
            SetLabelText(lblTopCash, $"Cash Reserves: ${engine.Stats.Cash:N2}");
            SetLabelForeColor(lblTopCash, engine.Stats.Cash < 0 ? Color.FromArgb(240, 100, 100) : Color.FromArgb(100, 240, 140));

            double cf = engine.Stats.PreviousMonthCashflow;
            SetLabelText(lblTopCashflow, $"Monthly Cashflow: {(cf >= 0 ? "+" : "")}${cf:N2}");
            SetLabelForeColor(lblTopCashflow, cf < 0 ? Color.FromArgb(240, 100, 100) : Color.FromArgb(140, 200, 250));

            if (engine.Stats.IsPubliclyTraded)
            {
                SetLabelText(lblTopStockValue, $"Player Stock Value: ${engine.Stats.PlayerStockPrice:F2}");
                SetLabelForeColor(lblTopStockValue, Color.FromArgb(100, 240, 140));
                SetControlVisible(btnTopLaunchIpo, false);
            }
            else
            {
                SetLabelText(lblTopStockValue, "Player Stock Value: #N/A");
                SetLabelForeColor(lblTopStockValue, Color.FromArgb(170, 175, 190));
                SetControlVisible(btnTopLaunchIpo, true);
            }

            // Update Top Bar Date
            string formattedDate = engine.CurrentDate.ToString("dd MMMM yyyy, HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            SetLabelText(lblTopDate, formattedDate);

            // Cycle phase and macro indicators
            string phaseStr = engine.CyclePhase.ToString();
            SetLabelText(lblBottomShare, $"Macro: GDP {engine.GDP_Index:F1} ({phaseStr})\nInt: {engine.Interest_Rate * 100:F1}% / CCI: {engine.ConsumerConfidenceIndex:F2}");
            
            Color phaseColor = engine.CyclePhase switch
            {
                CyclePhase.Boom => Color.FromArgb(100, 240, 140),
                CyclePhase.Recovery => Color.FromArgb(140, 200, 250),
                CyclePhase.Slowdown => Color.FromArgb(230, 140, 80),
                CyclePhase.Recession => Color.FromArgb(240, 100, 100),
                _ => Color.White
            };
            SetLabelForeColor(lblBottomShare, phaseColor);

            if (engine.ActiveResearch != null)
            {
                double progress = engine.ActiveResearch.GetProgressPercentage();
                SetLabelText(lblBottomResearch, $"Research: {engine.ActiveResearch.Name} ({(progress * 100):F0}%)");
                int newWidth = (int)(pnlBottomResearchBar.Width * progress);
                if (pnlBottomResearchFill.Width != newWidth)
                {
                    pnlBottomResearchFill.Width = newWidth;
                }
            }
            else
            {
                SetLabelText(lblBottomResearch, "Research: Idle");
                if (pnlBottomResearchFill.Width != 0)
                {
                    pnlBottomResearchFill.Width = 0;
                }
            }
        }

        private void GamePanel_TileSelected(object? sender, Tuple<int, int>? tileCoords)
        {
            UpdateSidebar(tileCoords, forceRepopulate: true);
        }

        private void UpdateSidebar(Tuple<int, int>? tileCoords, bool forceRepopulate = false)
        {
            if (engine == null) return;

            bool selectionChanged = false;
            if (tileCoords == null && lastSelectedTileCoords != null)
            {
                selectionChanged = true;
                lastSelectedTileCoords = null;
            }
            else if (tileCoords != null && (lastSelectedTileCoords == null || 
                                           lastSelectedTileCoords.Item1 != tileCoords.Item1 || 
                                           lastSelectedTileCoords.Item2 != tileCoords.Item2))
            {
                selectionChanged = true;
                lastSelectedTileCoords = tileCoords;
            }

            bool repopulate = forceRepopulate || selectionChanged;

            if (tileCoords == null)
            {
                SetLabelText(lblSelectedTileCoords, "Grid Address: None");
                SetLabelText(lblSelectedTileType, "Structure Type: Grass");
                SetLabelText(lblSelectedTileLevel, "Structure Level: 0");
                SetLabelText(lblSelectedTileMaint, "Hourly Upkeep: $0.00");
                SetLabelText(lblSelectedTileLandValue, "Local Land Value: $0.00");
                SetLabelText(lblSelectedTileTraffic, "Foot Traffic Index: 0");
                SetLabelText(lblSelectedTilePower, "Grid Electricity: Offline");
                SetLabelForeColor(lblSelectedTilePower, Color.White);
                SetLabelText(lblSelectedTileRoad, "Road Accessibility: No");
                SetLabelForeColor(lblSelectedTileRoad, Color.White);
                SetLabelText(lblSelectedTileInventory, "Inventory Stocks: 0 / 0");
                SetLabelText(lblSelectedTileStaff, "Employees Assigned: 0 / 0");
                
                SetControlVisible(btnUpgradeBuilding, false);
                SetControlEnabled(btnAssignEmployee, false);
                SetControlEnabled(btnUnassignEmployee, false);

                if (repopulate)
                {
                    lstAssignedEmployees.Items.Clear();
                    cmbUnassignedEmployees.Items.Clear();
                }
                return;
            }

            int tx = tileCoords.Item1;
            int ty = tileCoords.Item2;
            Tile tile = engine.Grid[tx, ty];

            SetLabelText(lblSelectedTileCoords, $"Grid Address: [{tx}, {ty}]");
            SetLabelText(lblSelectedTileType, $"Structure Type: {tile.Type}");
            SetLabelText(lblSelectedTileLevel, $"Structure Level: {tile.Level}");
            SetLabelText(lblSelectedTileMaint, $"Hourly Upkeep: ${tile.MaintenanceCost:F2}");
            SetLabelText(lblSelectedTileLandValue, $"Local Land Value: ${tile.LandValue:N2}");
            SetLabelText(lblSelectedTileTraffic, $"Foot Traffic Index: {tile.TrafficIndex:F0}");
            
            SetLabelText(lblSelectedTilePower, tile.IsPowered ? "Grid Electricity: Powered" : "Grid Electricity: Offline");
            SetLabelForeColor(lblSelectedTilePower, tile.IsPowered ? Color.FromArgb(100, 240, 140) : Color.FromArgb(240, 100, 100));

            SetLabelText(lblSelectedTileRoad, tile.HasRoadAccess ? "Road Accessibility: Active" : "Road Accessibility: No");
            SetLabelForeColor(lblSelectedTileRoad, tile.HasRoadAccess ? Color.FromArgb(100, 240, 140) : Color.FromArgb(240, 100, 100));

            if (tile.Type == TileType.Factory || tile.Type == TileType.Retail)
            {
                SetLabelText(lblSelectedTileInventory, $"Inventory Stocks: {tile.Inventory:F0} / {tile.MaxInventory:F0}");
            }
            else if (tile.Type == TileType.Apartment)
            {
                SetLabelText(lblSelectedTileInventory, $"Occupant Tenants: {tile.Inventory:F0} / {tile.MaxInventory:F0}");
            }
            else if (tile.Type == TileType.University)
            {
                SetLabelText(lblSelectedTileInventory, $"Training Progress: {tile.Inventory:F0}% / 100%");
            }
            else
            {
                SetLabelText(lblSelectedTileInventory, "Inventory Stocks: N/A");
            }

            bool isBuilding = tile.Type != TileType.Grass && tile.Type != TileType.Road;

            if (isBuilding)
            {
                SetLabelText(lblSelectedTileStaff, $"Employees Assigned: {tile.EmployeeCount} / {tile.MaxEmployees}");
                SetControlVisible(btnUpgradeBuilding, tile.Level < 3);
                
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
                string upgradeText = $"Upgrade Block (${upgradeCost / 1000:F0}K)";
                if (btnUpgradeBuilding.Text != upgradeText)
                {
                    btnUpgradeBuilding.Text = upgradeText;
                }
            }
            else
            {
                SetLabelText(lblSelectedTileStaff, "Employees Assigned: N/A");
                SetControlVisible(btnUpgradeBuilding, false);
            }

            if (repopulate)
            {
                lstAssignedEmployees.Items.Clear();
                var assigned = engine.Employees.Where(e => e.AssignedX == tx && e.AssignedY == ty).ToList();
                foreach (var emp in assigned)
                {
                    lstAssignedEmployees.Items.Add($"{emp.Name} ({emp.Role})");
                }

                cmbUnassignedEmployees.Items.Clear();
                var unassigned = engine.Employees.Where(e => e.AssignedX == -1).ToList();

                if (tile.Type == TileType.PowerPlant)
                {
                    unassigned = unassigned.Where(e => e.Role == EmployeeRole.Worker || e.Role == EmployeeRole.Manager).ToList();
                }
                else if (tile.Type == TileType.Office)
                {
                    // Offices accept all roles
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
            }

            SetControlEnabled(btnUnassignEmployee, lstAssignedEmployees.Items.Count > 0);
            
            bool spaceAvailable = tile.EmployeeCount < tile.MaxEmployees;
            SetControlEnabled(btnAssignEmployee, spaceAvailable && cmbUnassignedEmployees.Items.Count > 0 && isBuilding);
        }

        private void BtnAssignEmployee_Click(object? sender, EventArgs e)
        {
            if (engine == null || gamePanel == null || gamePanel.SelectedTile == null || cmbUnassignedEmployees.SelectedItem == null) return;
            var item = (ComboBoxEmployeeItem)cmbUnassignedEmployees.SelectedItem;
            
            if (engine.AssignEmployee(item.Emp.Id, gamePanel.SelectedTile.Item1, gamePanel.SelectedTile.Item2))
            {
                UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: true);
                gamePanel.Invalidate();
            }
        }

        private void BtnUnassignEmployee_Click(object? sender, EventArgs e)
        {
            if (engine == null || gamePanel == null || gamePanel.SelectedTile == null || lstAssignedEmployees.SelectedIndex == -1) return;
            int idx = lstAssignedEmployees.SelectedIndex;
            var assigned = engine.Employees.Where(e => e.AssignedX == gamePanel.SelectedTile.Item1 && e.AssignedY == gamePanel.SelectedTile.Item2).ToList();
            
            if (idx >= 0 && idx < assigned.Count)
            {
                engine.UnassignEmployee(assigned[idx].Id);
                UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: true);
                gamePanel.Invalidate();
            }
        }

        private void BtnUpgradeBuilding_Click(object? sender, EventArgs e)
        {
            if (engine == null || gamePanel == null || gamePanel.SelectedTile == null) return;
            int tx = gamePanel.SelectedTile.Item1;
            int ty = gamePanel.SelectedTile.Item2;

            if (engine.UpgradeStructure(tx, ty))
            {
                UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: true);
                UpdateBottomBar();
                gamePanel.Invalidate();
            }
            else
            {
                MessageBox.Show("Insufficient funds or maximum building level reached!", "Construction Services", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnTopLaunchIpo_Click(object? sender, EventArgs e)
        {
            if (engine == null || gamePanel == null || engine.Stats.IsIpoLaunched) return;

            double initialAssetVal = 0;
            for (int x = 0; x < GameEngine.MapSize; x++)
            {
                for (int y = 0; y < GameEngine.MapSize; y++)
                {
                    initialAssetVal += engine.Grid[x, y].GetAssetValue();
                }
            }
            engine.Stats.UpdatePlayerStockPrice(initialAssetVal);
            double ipoPrice = engine.Stats.PlayerStockPrice;

            double sharesSold = 400000.0;
            double capitalRaised = sharesSold * ipoPrice;

            engine.Stats.PlayerSharesOwnedByPlayer = 600000.0; // Keep 60%
            engine.Stats.Cash += capitalRaised;
            engine.Stats.IsIpoLaunched = true;

            MessageBox.Show(
                $"IPO Successful on Warsaw Stock Exchange (GPW)!\n\n" +
                $"Sold 400,000 shares (40%) at ${ipoPrice:F2} per share.\n" +
                $"Raised ${capitalRaised:N2} in liquid capital!",
                "GPW Public Offering",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            btnTopLaunchIpo.Visible = false;
            UpdateBottomBar();
            gamePanel.Invalidate();
        }

        private void ShowSystemMenu()
        {
            if (engine == null) return;

            // Hold simulation clock during system interactions
            int oldSpeed = currentSpeedMultiplier;
            SetSimulationSpeed(0);

            using (var sysMenu = new SystemMenuWindow(engine))
            {
                sysMenu.ShowDialog(this);
                if (sysMenu.ShouldExitToMainMenu)
                {
                    ExitToMainMenu();
                    return;
                }
            }

            // Resume simulation speed
            SetSimulationSpeed(oldSpeed);
            if (gamePanel != null)
            {
                UpdateSidebar(gamePanel.SelectedTile, forceRepopulate: true);
                UpdateBottomBar();
            }
        }

        private void ExitToMainMenu()
        {
            // Stop simulation ticking
            simulationTimer?.Stop();

            // Clear controls
            this.Controls.Clear();

            // Destroy game viewport (free GPU context) and unload grid memory
            if (gamePanel != null)
            {
                gamePanel.TileSelected -= GamePanel_TileSelected;
                gamePanel.Dispose();
                gamePanel = null;
            }

            engine = null;

            // Reload Main Menu
            InitializeMainMenu();
        }

        private class ComboBoxEmployeeItem
        {
            public Employee Emp { get; }
            public ComboBoxEmployeeItem(Employee emp) => Emp = emp;
            public override string ToString() => $"{Emp.Name} ({Emp.Role})";
        }

        #endregion
    }
}

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TycoonGame.Core;

namespace TycoonGame.UI
{
    public class AnalyticsWindow : Form
    {
        private readonly GameEngine engine;
        private ListView financialHistoryView;
        private Label lblCash;
        private Label lblLoanDebt;
        private Label lblAssetsVal;
        private Label lblNetWorthVal;
        
        private Label lblRevVal;
        private Label lblWageVal;
        private Label lblMaintVal;
        private Label lblLogisticsVal;
        private Label lblInterestVal;
        private Label lblTaxVal;
        private Label lblNetProfitVal;

        // Macroeconomic Climate labels
        private Label lblMacroGdp;
        private Label lblMacroRates;
        private Label lblMacroUnemployment;
        private Label lblMacroCci;

        private Button btnBorrow50k;
        private Button btnBorrow100k;
        private Button btnRepay50k;
        private Button btnRepay100k;

        public AnalyticsWindow(GameEngine gameEngine)
        {
            engine = gameEngine;
            InitializeComponent();
            RefreshFinancialData();

            // Enable Double Buffering to reduce repaint flickering
            EnableDoubleBuffered(this);
            EnableDoubleBuffered(financialHistoryView);
        }

        private void InitializeComponent()
        {
            Text = "Corporate Finance & Market Analytics";
            Size = new Size(880, 580);
            MinimumSize = new Size(880, 580);
            BackColor = Color.FromArgb(24, 28, 36);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Title
            Label lblTitle = new Label
            {
                Text = "Corporate Financial Center",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 140, 200),
                Location = new Point(20, 15),
                Size = new Size(400, 35),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            // Left Panel: Balance Sheet & Income Statement
            GroupBox grpBalanceSheet = new GroupBox
            {
                Text = "Corporate Balance Sheet",
                Location = new Point(20, 60),
                Size = new Size(400, 150),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            lblCash = new Label { Location = new Point(15, 25), Size = new Size(370, 20), ForeColor = Color.White, Text = "Liquid Cash Balance: $0.00" };
            lblLoanDebt = new Label { Location = new Point(15, 50), Size = new Size(370, 20), ForeColor = Color.White, Text = "Outstanding Liabilities (Loan): $0.00" };
            lblAssetsVal = new Label { Location = new Point(15, 75), Size = new Size(370, 20), ForeColor = Color.White, Text = "Property & Equipment Assets: $0.00" };
            lblNetWorthVal = new Label 
            { 
                Location = new Point(15, 105), 
                Size = new Size(370, 25), 
                ForeColor = Color.FromArgb(80, 200, 120), 
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Text = "Net Corporate Worth: $0.00" 
            };

            grpBalanceSheet.Controls.Add(lblCash);
            grpBalanceSheet.Controls.Add(lblLoanDebt);
            grpBalanceSheet.Controls.Add(lblAssetsVal);
            grpBalanceSheet.Controls.Add(lblNetWorthVal);
            Controls.Add(grpBalanceSheet);

            // Group: Daily Income Statement
            GroupBox grpIncome = new GroupBox
            {
                Text = "Daily Income Statement (Est.)",
                Location = new Point(20, 225),
                Size = new Size(400, 300),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            int xLabel = 15, xVal = 220, yStart = 30, yDiff = 30;

            // Labels
            grpIncome.Controls.Add(new Label { Text = "Revenue (Sales/Contracts):", Location = new Point(xLabel, yStart), Size = new Size(200, 20), ForeColor = Color.White });
            lblRevVal = new Label { Text = "$0.00", Location = new Point(xVal, yStart), Size = new Size(160, 20), ForeColor = Color.FromArgb(100, 240, 140), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            grpIncome.Controls.Add(lblRevVal);

            grpIncome.Controls.Add(new Label { Text = "Operating Wages Expense:", Location = new Point(xLabel, yStart + yDiff), Size = new Size(200, 20), ForeColor = Color.White });
            lblWageVal = new Label { Text = "$0.00", Location = new Point(xVal, yStart + yDiff), Size = new Size(160, 20), ForeColor = Color.FromArgb(240, 100, 100) };
            grpIncome.Controls.Add(lblWageVal);

            grpIncome.Controls.Add(new Label { Text = "Building Maintenance:", Location = new Point(xLabel, yStart + 2 * yDiff), Size = new Size(200, 20), ForeColor = Color.White });
            lblMaintVal = new Label { Text = "$0.00", Location = new Point(xVal, yStart + 2 * yDiff), Size = new Size(160, 20), ForeColor = Color.FromArgb(240, 100, 100) };
            grpIncome.Controls.Add(lblMaintVal);

            grpIncome.Controls.Add(new Label { Text = "Logistics Shipping Costs:", Location = new Point(xLabel, yStart + 3 * yDiff), Size = new Size(200, 20), ForeColor = Color.White });
            lblLogisticsVal = new Label { Text = "$0.00", Location = new Point(xVal, yStart + 3 * yDiff), Size = new Size(160, 20), ForeColor = Color.FromArgb(240, 100, 100) };
            grpIncome.Controls.Add(lblLogisticsVal);

            grpIncome.Controls.Add(new Label { Text = "Interest Charge (Dynamic APR):", Location = new Point(xLabel, yStart + 4 * yDiff), Size = new Size(200, 20), ForeColor = Color.White });
            lblInterestVal = new Label { Text = "$0.00", Location = new Point(xVal, yStart + 4 * yDiff), Size = new Size(160, 20), ForeColor = Color.FromArgb(240, 100, 100) };
            grpIncome.Controls.Add(lblInterestVal);

            grpIncome.Controls.Add(new Label { Text = "Corporate Taxes (20%):", Location = new Point(xLabel, yStart + 5 * yDiff), Size = new Size(200, 20), ForeColor = Color.White });
            lblTaxVal = new Label { Text = "$0.00", Location = new Point(xVal, yStart + 5 * yDiff), Size = new Size(160, 20), ForeColor = Color.FromArgb(240, 100, 100) };
            grpIncome.Controls.Add(lblTaxVal);

            // Divider Line
            Label lblDivider = new Label { Location = new Point(15, yStart + 6 * yDiff - 10), Size = new Size(370, 2), BackColor = Color.FromArgb(48, 56, 70), BorderStyle = BorderStyle.Fixed3D };
            grpIncome.Controls.Add(lblDivider);

            grpIncome.Controls.Add(new Label { Text = "Net Operating Income:", Location = new Point(xLabel, yStart + 6 * yDiff), Size = new Size(200, 20), ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold) });
            lblNetProfitVal = new Label { Text = "$0.00", Location = new Point(xVal, yStart + 6 * yDiff), Size = new Size(160, 20), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            grpIncome.Controls.Add(lblNetProfitVal);

            Controls.Add(grpIncome);

            // Right Panel: Loan Manager & Macro Report
            GroupBox grpBank = new GroupBox
            {
                Text = "Corporate Bank Loan Manager",
                Location = new Point(445, 60),
                Size = new Size(400, 115),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            btnBorrow50k = CreateStyledButton("Borrow $50K", new Point(15, 25), new Size(175, 30));
            btnBorrow50k.Click += (s, e) => HandleLoanBorrow(50000.0);

            btnRepay50k = CreateStyledButton("Repay $50K", new Point(210, 25), new Size(175, 30));
            btnRepay50k.Click += (s, e) => HandleLoanRepay(50000.0);

            btnBorrow100k = CreateStyledButton("Borrow $100K", new Point(15, 65), new Size(175, 30));
            btnBorrow100k.Click += (s, e) => HandleLoanBorrow(100000.0);

            btnRepay100k = CreateStyledButton("Repay $100K", new Point(210, 65), new Size(175, 30));
            btnRepay100k.Click += (s, e) => HandleLoanRepay(100000.0);

            grpBank.Controls.Add(btnBorrow50k);
            grpBank.Controls.Add(btnBorrow100k);
            grpBank.Controls.Add(btnRepay50k);
            grpBank.Controls.Add(btnRepay100k);
            Controls.Add(grpBank);

            // Group: Macroeconomic Report Card
            GroupBox grpMacro = new GroupBox
            {
                Text = "Macroeconomic Climate Indicators",
                Location = new Point(445, 185),
                Size = new Size(400, 115),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            lblMacroGdp = new Label { Location = new Point(15, 22), Size = new Size(370, 18), ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblMacroRates = new Label { Location = new Point(15, 42), Size = new Size(370, 18), ForeColor = Color.White };
            lblMacroUnemployment = new Label { Location = new Point(15, 62), Size = new Size(370, 18), ForeColor = Color.White };
            lblMacroCci = new Label { Location = new Point(15, 82), Size = new Size(370, 18), ForeColor = Color.White };

            grpMacro.Controls.Add(lblMacroGdp);
            grpMacro.Controls.Add(lblMacroRates);
            grpMacro.Controls.Add(lblMacroUnemployment);
            grpMacro.Controls.Add(lblMacroCci);
            Controls.Add(grpMacro);

            // Financial Statements listview
            Label lblHist = new Label
            {
                Text = "Historical Daily Ledger Statements",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 175, 190),
                Location = new Point(445, 310),
                Size = new Size(300, 20),
                AutoSize = true
            };
            Controls.Add(lblHist);

            financialHistoryView = new ListView
            {
                Location = new Point(445, 330),
                Size = new Size(400, 155),
                View = View.Details,
                FullRowSelect = true,
                BackColor = Color.FromArgb(32, 38, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                OwnerDraw = true
            };
            financialHistoryView.Columns.Add("Date", 95);
            financialHistoryView.Columns.Add("Revenue", 95);
            financialHistoryView.Columns.Add("Expenses", 95);
            financialHistoryView.Columns.Add("Net profit", 95);

            SetupCustomDrawing(financialHistoryView);
            Controls.Add(financialHistoryView);

            // Close button
            Button btnClose = CreateStyledButton("Close Finance Panel", new Point(445, 495), new Size(400, 32));
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
        }

        private Button CreateStyledButton(string text, Point loc, Size sz)
        {
            Button btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 56, 70),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 100, 150);
            return btn;
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
                using Brush bgBrush = new SolidBrush(isSelected ? Color.FromArgb(64, 100, 150) : lv.BackColor);
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

                Color itemTextColor = Color.White;
                if (e.ColumnIndex == 3) // Net profit
                {
                    double netVal = double.Parse(e.SubItem.Text.Replace("$", "").Replace(",", "").Trim());
                    itemTextColor = netVal < 0 ? Color.FromArgb(240, 100, 100) : Color.FromArgb(100, 240, 140);
                }

                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, lv.Font, e.Bounds, itemTextColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
        }

        private void RefreshFinancialData()
        {
            // Update macro report card labels
            string phaseStr = engine.CyclePhase.ToString();
            lblMacroGdp.Text = $"GDP Index: {engine.GDP_Index:F1} ({phaseStr})";
            lblMacroRates.Text = $"Central Bank Rate: {engine.Interest_Rate * 100:F1}% | Inflation: {engine.Inflation_Rate * 100:F1}%";
            lblMacroUnemployment.Text = $"Labor Market Unemployment: {engine.Unemployment_Rate * 100:F1}%";
            lblMacroCci.Text = $"Consumer Confidence Index (CCI): {engine.ConsumerConfidenceIndex:F2}";

            // Color code GDP label inside report card based on phase
            lblMacroGdp.ForeColor = engine.CyclePhase switch
            {
                CyclePhase.Boom => Color.FromArgb(100, 240, 140),
                CyclePhase.Recovery => Color.FromArgb(140, 200, 250),
                CyclePhase.Slowdown => Color.FromArgb(230, 140, 80),
                CyclePhase.Recession => Color.FromArgb(240, 100, 100),
                _ => Color.White
            };

            // Calculate building asset values
            double assetVal = 0;
            for (int x = 0; x < GameEngine.MapSize; x++)
            {
                for (int y = 0; y < GameEngine.MapSize; y++)
                {
                    Tile tile = engine.Grid[x, y];
                    double buildCost = tile.Type switch
                    {
                        TileType.Road => 1000.0,
                        TileType.Office => 30000.0,
                        TileType.Factory => 60000.0,
                        TileType.Retail => 40000.0,
                        TileType.PowerPlant => 50000.0,
                        _ => 0
                    };
                    assetVal += buildCost;

                    // Add upgrades
                    if (tile.Level > 1)
                    {
                        double upgradeCost = tile.Type switch
                        {
                            TileType.Office => 24000.0,
                            TileType.Factory => 48000.0,
                            TileType.Retail => 32000.0,
                            TileType.PowerPlant => 40000.0,
                            _ => 0
                        };
                        assetVal += upgradeCost * (tile.Level - 1);
                    }
                }
            }

            double netWorth = engine.Stats.Cash + assetVal - engine.Stats.LoanBalance;

            lblCash.Text = $"Liquid Cash Balance: ${engine.Stats.Cash:N2}";
            lblLoanDebt.Text = $"Outstanding Liabilities (Loan): ${engine.Stats.LoanBalance:N2}";
            lblAssetsVal.Text = $"Property & Equipment Assets: ${assetVal:N2}";
            lblNetWorthVal.Text = $"Net Corporate Worth: ${netWorth:N2}";

            // Estimate hourly billing times 24 for a daily projection
            double wageProj = engine.Stats.CurrentHourWages * 24;
            double maintProj = engine.Stats.CurrentHourMaintenance * 24;
            double logProj = engine.Stats.CurrentHourLogistics * 24;
            double intProj = engine.Stats.CurrentHourInterest * 24;
            
            // Actually get average revenue based on active building throughput
            double revProj = 0;
            for (int x = 0; x < GameEngine.MapSize; x++)
            {
                for (int y = 0; y < GameEngine.MapSize; y++)
                {
                    Tile tile = engine.Grid[x, y];
                    if (tile.Type == TileType.Office && tile.EmployeeCount > 0 && tile.IsPowered)
                    {
                        double performance = engine.Employees.Where(e => e.AssignedX == x && e.AssignedY == y).Sum(e => e.GetPerformanceMultiplier());
                        double officeYield = engine.GetActiveEffectMultiplier(ResearchEffect.OfficeYield);
                        revProj += performance * tile.ProductionRate * tile.Level * officeYield * 24;
                    }
                    else if (tile.Type == TileType.Retail && tile.EmployeeCount > 0 && tile.IsPowered && tile.Inventory > 0)
                    {
                        double performance = engine.Employees.Where(e => e.AssignedX == x && e.AssignedY == y).Sum(e => e.GetPerformanceMultiplier());
                        double retailDemand = engine.GetActiveEffectMultiplier(ResearchEffect.RetailDemand);
                        double marketDemand = (1.0 + (1.0 - engine.PlayerMarketShare) * 0.3) * engine.ConsumerConfidenceIndex;
                        double cap = performance * tile.SalesRate * tile.Level * retailDemand * marketDemand;
                        revProj += Math.Min(tile.Inventory, cap) * engine.CurrentMarketPrice * 24;
                    }
                }
            }

            double expenseSumProj = wageProj + maintProj + logProj + intProj;
            double netProfitProjBeforeTax = revProj - expenseSumProj;
            double taxProj = netProfitProjBeforeTax > 0 ? netProfitProjBeforeTax * engine.Stats.TaxRate : 0;
            double netProfitProj = netProfitProjBeforeTax - taxProj;

            lblRevVal.Text = $"${revProj:N2}";
            lblWageVal.Text = $"-${wageProj:N2}";
            lblMaintVal.Text = $"-${maintProj:N2}";
            lblLogisticsVal.Text = $"-${logProj:N2}";
            lblInterestVal.Text = $"-${intProj:N2}";
            lblTaxVal.Text = $"-${taxProj:N2}";

            lblNetProfitVal.Text = $"${netProfitProj:N2}";
            lblNetProfitVal.ForeColor = netProfitProj < 0 ? Color.FromArgb(240, 100, 100) : Color.FromArgb(100, 240, 140);

            // Disable buttons based on loan boundaries
            btnBorrow50k.Enabled = (engine.Stats.LoanBalance + 50000.0 <= engine.Stats.MaxLoanLimit);
            btnBorrow100k.Enabled = (engine.Stats.LoanBalance + 100000.0 <= engine.Stats.MaxLoanLimit);
            btnRepay50k.Enabled = (engine.Stats.LoanBalance >= 50000.0 && engine.Stats.Cash >= 50000.0);
            btnRepay100k.Enabled = (engine.Stats.LoanBalance >= 100000.0 && engine.Stats.Cash >= 100000.0);

            // Populate Financial History Grid
            financialHistoryView.Items.Clear();
            foreach (var record in engine.Stats.FinancialHistory)
            {
                ListViewItem item = new ListViewItem(record.Timestamp.ToString("MM/dd HH:mm"));
                item.SubItems.Add($"${record.Revenue:N0}");
                double totalExpenses = record.Revenue - record.NetProfit;
                item.SubItems.Add($"${totalExpenses:N0}");
                item.SubItems.Add($" {record.NetProfit:N0}");
                financialHistoryView.Items.Add(item);
            }
        }

        private void HandleLoanBorrow(double amount)
        {
            if (engine.Stats.BorrowLoan(amount))
            {
                RefreshFinancialData();
            }
            else
            {
                MessageBox.Show("Maximum corporate loan limit reached!", "Bank Loan Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void HandleLoanRepay(double amount)
        {
            if (engine.Stats.PaybackLoan(amount))
            {
                RefreshFinancialData();
            }
            else
            {
                MessageBox.Show("Insufficient liquid funds to complete this loan repayment installment!", "Bank Loan Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
    }
}

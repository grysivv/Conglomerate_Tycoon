using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Globalization;
using TycoonGame.Core;

namespace TycoonGame.UI
{
    public class AnalyticsWindow : Form
    {
        private readonly GameEngine engine;
        private ListView financialHistoryView;

        // Balance Sheet Controls
        private Label lblCashVal;
        private Label lblPropertyVal;
        private Label lblInventoryVal;
        private Label lblTotalAssets;
        private Label lblLoanDebt;
        private Label lblAccruedInterest;
        private Label lblTotalLiabilities;
        private Label lblBookValue;
        private Label lblTotalLiabilitiesEquity;

        // P&L Statement Controls
        private Label lblPLRetailRev;
        private Label lblPLApartmentRev;
        private Label lblPLOfficeRev;
        private Label lblPLTotalRev;
        private Label lblPLSalaries;
        private Label lblPLLandTaxes;
        private Label lblPLLogistics;
        private Label lblPLPowerMaint;
        private Label lblPLTotalOpex;
        private Label lblPLEbitda;
        private Label lblPLInterest;
        private Label lblPLTaxes;
        private Label lblPLNetProfit;
        private Label lblPLEbitdaMargin;
        private Label lblPLNetMargin;

        // Debt & Leverage Controls
        private Label lblBenchmarkRate;
        private Label lblRating;
        private Label lblMarkup;
        private Label lblEffectiveRate;
        private Label lblCashToDebt;
        private Button btnBorrow50k;
        private Button btnRepay50k;

        public AnalyticsWindow(GameEngine gameEngine)
        {
            engine = gameEngine;
            InitializeComponent();
            RefreshFinancialData();

            EnableDoubleBuffered(this);
            EnableDoubleBuffered(financialHistoryView);
        }

        private void InitializeComponent()
        {
            Text = "Corporate Finance & Market Analytics";
            Size = new Size(1020, 720);
            MinimumSize = new Size(1020, 720);
            BackColor = Color.FromArgb(24, 28, 36);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Main Grid Layout (2 columns, Header + Middle Grid + Footer button)
            TableLayoutPanel outerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(15)
            };
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Header
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Middle Content Panel
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Close Button Footer
            Controls.Add(outerLayout);

            // 1. Header Panel
            Label lblTitle = new Label
            {
                Text = "RACHUNKI I ANALIZY FINANSOWE PRZEDSIĘBIORSTWA",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 140, 200),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            outerLayout.Controls.Add(lblTitle, 0, 0);

            // 2. Middle Content Grid (2 Columns, 2 Rows)
            TableLayoutPanel contentGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0)
            };
            contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 40F)); // Balance Sheet & Debt Desk
            contentGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 60F)); // P&L Statement & Ledger list
            outerLayout.Controls.Add(contentGrid, 0, 1);

            // 2A. Balance Sheet Group Box (Row 0, Col 0)
            GroupBox grpBalanceSheet = new GroupBox
            {
                Text = "BILANS STANU MAJĄTKOWEGO (BALANCE SHEET)",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5)
            };
            contentGrid.Controls.Add(grpBalanceSheet, 0, 0);

            TableLayoutPanel bsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(5)
            };
            bsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // Assets column
            bsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // Liabilities + Equity column
            grpBalanceSheet.Controls.Add(bsTable);

            // Assets Panel
            FlowLayoutPanel pnlAssets = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            pnlAssets.Controls.Add(new Label { Text = "AKTYWA (ASSETS)", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(80, 140, 200), Size = new Size(180, 18) });
            
            lblCashVal = new Label { Text = "Środki pieniężne: $0.00", ForeColor = Color.White, Size = new Size(180, 18) };
            lblPropertyVal = new Label { Text = "Nieruchomości: $0.00", ForeColor = Color.White, Size = new Size(180, 18) };
            lblInventoryVal = new Label { Text = "Zapasy: $0.00", ForeColor = Color.White, Size = new Size(180, 18) };
            lblTotalAssets = new Label { Text = "SUMA AKTYWÓW: $0.00", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 240, 140), Size = new Size(180, 22) };

            pnlAssets.Controls.Add(lblCashVal);
            pnlAssets.Controls.Add(lblPropertyVal);
            pnlAssets.Controls.Add(lblInventoryVal);
            pnlAssets.Controls.Add(new Label { Size = new Size(180, 1), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 5, 0, 5) });
            pnlAssets.Controls.Add(lblTotalAssets);
            bsTable.Controls.Add(pnlAssets, 0, 0);

            // Liabilities & Equity Panel
            FlowLayoutPanel pnlLiabilities = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            pnlLiabilities.Controls.Add(new Label { Text = "PASYWA (LIABILITIES & EQUITY)", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(80, 140, 200), Size = new Size(180, 18) });

            lblLoanDebt = new Label { Text = "Zadłużenie kredytowe: $0.00", ForeColor = Color.White, Size = new Size(180, 18) };
            lblAccruedInterest = new Label { Text = "Naliczone odsetki: $0.00", ForeColor = Color.White, Size = new Size(180, 18) };
            lblTotalLiabilities = new Label { Text = "Suma zobowiązań: $0.00", ForeColor = Color.White, Size = new Size(180, 18) };
            lblBookValue = new Label { Text = "Kapitał własny (Book Value): $0.00", ForeColor = Color.FromArgb(140, 200, 250), Size = new Size(180, 18) };
            lblTotalLiabilitiesEquity = new Label { Text = "SUMA PASYWÓW: $0.00", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 240, 140), Size = new Size(180, 22) };

            pnlLiabilities.Controls.Add(lblLoanDebt);
            pnlLiabilities.Controls.Add(lblAccruedInterest);
            pnlLiabilities.Controls.Add(lblTotalLiabilities);
            pnlLiabilities.Controls.Add(lblBookValue);
            pnlLiabilities.Controls.Add(new Label { Size = new Size(180, 1), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 5, 0, 5) });
            pnlLiabilities.Controls.Add(lblTotalLiabilitiesEquity);
            bsTable.Controls.Add(pnlLiabilities, 1, 0);


            // 2B. Debt & Leverage Group Box (Row 0, Col 1)
            GroupBox grpDebtDesk = new GroupBox
            {
                Text = "ZARZĄDZANIE ZADŁUŻENIEM (DEBT & LEVERAGE DESK)",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5)
            };
            contentGrid.Controls.Add(grpDebtDesk, 1, 0);

            TableLayoutPanel debtLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(5)
            };
            debtLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            debtLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            grpDebtDesk.Controls.Add(debtLayout);

            // Left info panel
            FlowLayoutPanel pnlDebtInfo = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            lblBenchmarkRate = new Label { Text = "Stopa referencyjna banku centralnego: 0.00%", ForeColor = Color.White, Size = new Size(240, 18) };
            lblRating = new Label { Text = "Ocena wiarygodności kredytowej: A", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 240, 140), Size = new Size(240, 18) };
            lblMarkup = new Label { Text = "Marża bankowa (ryzyko kredytowe): +0.00%", ForeColor = Color.White, Size = new Size(240, 18) };
            lblEffectiveRate = new Label { Text = "Efektywne oprocentowanie (APR): 0.00%", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Size = new Size(240, 18) };
            lblCashToDebt = new Label { Text = "Wskaźnik pokrycia długu (Cash-to-Debt): 0.00", ForeColor = Color.White, Size = new Size(240, 18) };

            pnlDebtInfo.Controls.Add(lblBenchmarkRate);
            pnlDebtInfo.Controls.Add(lblRating);
            pnlDebtInfo.Controls.Add(lblMarkup);
            pnlDebtInfo.Controls.Add(lblEffectiveRate);
            pnlDebtInfo.Controls.Add(lblCashToDebt);
            debtLayout.Controls.Add(pnlDebtInfo, 0, 0);

            // Right action buttons panel
            FlowLayoutPanel pnlDebtActions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(5, 10, 5, 5) };
            btnBorrow50k = CreateStyledButton("Zaciągnij Kredyt $50,000", new Point(0, 0), new Size(160, 32));
            btnBorrow50k.Click += (s, e) => HandleLoanBorrow(50000.0);

            btnRepay50k = CreateStyledButton("Spłać Kredyt $50,000", new Point(0, 0), new Size(160, 32));
            btnRepay50k.Click += (s, e) => HandleLoanRepay(50000.0);

            pnlDebtActions.Controls.Add(btnBorrow50k);
            pnlDebtActions.Controls.Add(new Label { Height = 10 });
            pnlDebtActions.Controls.Add(btnRepay50k);
            debtLayout.Controls.Add(pnlDebtActions, 1, 0);


            // 2C. Profit & Loss Group Box (Row 1, Col 0)
            GroupBox grpPLStatement = new GroupBox
            {
                Text = "RACHUNEK ZYSKÓW I STRAT (ROLLING MONTHLY P&L)",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5)
            };
            contentGrid.Controls.Add(grpPLStatement, 0, 1);

            TableLayoutPanel plGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(5)
            };
            plGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            plGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            grpPLStatement.Controls.Add(plGrid);

            int fontWidth = 240;

            // Row 0: Revenues
            plGrid.Controls.Add(new Label { Text = "PRZYCHODY OPERACYJNE (OPERATING REVENUE)", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(80, 140, 200), Size = new Size(fontWidth, 18) }, 0, 0);
            
            FlowLayoutPanel pnlRevenues = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            lblPLRetailRev = new Label { Text = "Sprzedaż detaliczna (Retail): $0.00", ForeColor = Color.White, Size = new Size(fontWidth, 16) };
            lblPLApartmentRev = new Label { Text = "Wynajem mieszkań (Apartment): $0.00", ForeColor = Color.White, Size = new Size(fontWidth, 16) };
            lblPLOfficeRev = new Label { Text = "Usługi biurowe (Office Consulting): $0.00", ForeColor = Color.White, Size = new Size(fontWidth, 16) };
            lblPLTotalRev = new Label { Text = "Suma przychodów: $0.00", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 240, 140), Size = new Size(fontWidth, 18) };
            pnlRevenues.Controls.Add(lblPLRetailRev);
            pnlRevenues.Controls.Add(lblPLApartmentRev);
            pnlRevenues.Controls.Add(lblPLOfficeRev);
            pnlRevenues.Controls.Add(lblPLTotalRev);
            plGrid.Controls.Add(pnlRevenues, 0, 1);
            plGrid.SetColumnSpan(pnlRevenues, 2);

            // Row 2: OPEX
            plGrid.Controls.Add(new Label { Text = "KOSZTY OPERACYJNE (OPERATIONAL EXPENSES - OPEX)", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(80, 140, 200), Size = new Size(fontWidth, 18) }, 0, 2);

            FlowLayoutPanel pnlOpex = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            lblPLSalaries = new Label { Text = "Wynagrodzenia personelu (Wages): $0.00", ForeColor = Color.White, Size = new Size(fontWidth, 16) };
            lblPLLandTaxes = new Label { Text = "Podatki gruntowe (Land Taxes): $0.00", ForeColor = Color.White, Size = new Size(fontWidth, 16) };
            lblPLLogistics = new Label { Text = "Koszty spedycji i logistyki (Freight Logistics): $0.00", ForeColor = Color.White, Size = new Size(fontWidth, 16) };
            lblPLPowerMaint = new Label { Text = "Utrzymanie sieci i energii (Infrastructure Power): $0.00", ForeColor = Color.White, Size = new Size(fontWidth, 16) };
            lblPLTotalOpex = new Label { Text = "Suma kosztów operacyjnych: $0.00", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(240, 100, 100), Size = new Size(fontWidth, 18) };
            pnlOpex.Controls.Add(lblPLSalaries);
            pnlOpex.Controls.Add(lblPLLandTaxes);
            pnlOpex.Controls.Add(lblPLLogistics);
            pnlOpex.Controls.Add(lblPLPowerMaint);
            pnlOpex.Controls.Add(lblPLTotalOpex);
            plGrid.Controls.Add(pnlOpex, 0, 3);
            plGrid.SetColumnSpan(pnlOpex, 2);

            // Row 4: EBITDA
            lblPLEbitda = new Label { Text = "Monthly EBITDA: $0.00", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(140, 200, 250), Size = new Size(240, 20) };
            plGrid.Controls.Add(lblPLEbitda, 0, 4);

            lblPLEbitdaMargin = new Label { Text = "Marża EBITDA: 0.00%", Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.White, Size = new Size(180, 20), TextAlign = ContentAlignment.MiddleRight };
            plGrid.Controls.Add(lblPLEbitdaMargin, 1, 4);

            // Row 5: Interest & Taxes
            FlowLayoutPanel pnlFinancing = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            lblPLInterest = new Label { Text = "Odsetki kredytowe: $0.00", ForeColor = Color.White, Size = new Size(220, 18) };
            lblPLTaxes = new Label { Text = "Podatek dochodowy (CIT): $0.00", ForeColor = Color.White, Size = new Size(220, 18) };
            pnlFinancing.Controls.Add(lblPLInterest);
            pnlFinancing.Controls.Add(lblPLTaxes);
            plGrid.Controls.Add(pnlFinancing, 0, 5);
            plGrid.SetColumnSpan(pnlFinancing, 2);

            // Row 6: Net Profit
            lblPLNetProfit = new Label { Text = "Zysk Netto (Net Profit): $0.00", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 240, 140), Size = new Size(240, 22) };
            plGrid.Controls.Add(lblPLNetProfit, 0, 6);

            lblPLNetMargin = new Label { Text = "Rentowność Netto: 0.00%", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Size = new Size(180, 22), TextAlign = ContentAlignment.MiddleRight };
            plGrid.Controls.Add(lblPLNetMargin, 1, 6);


            // 2D. Rejestr Ledger Group Box (Row 1, Col 1)
            GroupBox grpLedger = new GroupBox
            {
                Text = "REJESTR DZIENNYCH WYNIKÓW (HISTORICAL DAILY LEDGER)",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5)
            };
            contentGrid.Controls.Add(grpLedger, 1, 1);

            financialHistoryView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BackColor = Color.FromArgb(32, 38, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                OwnerDraw = true,
                Margin = new Padding(10)
            };
            financialHistoryView.Columns.Add("Date", 110);
            financialHistoryView.Columns.Add("Revenue", 110);
            financialHistoryView.Columns.Add("Expenses", 110);
            financialHistoryView.Columns.Add("Net profit", 110);
            SetupCustomDrawing(financialHistoryView);
            grpLedger.Controls.Add(financialHistoryView);

            // 3. Footer CLOSE button
            Button btnClose = CreateStyledButton("Zamknij Panel Finansowy", new Point(0, 0), new Size(250, 32));
            btnClose.Anchor = AnchorStyles.None;
            btnClose.Click += (s, e) => Close();
            outerLayout.Controls.Add(btnClose, 0, 2);
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
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 100, 150);
            return btn;
        }

        private void SetupCustomDrawing(ListView lv)
        {
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
                    double netVal = 0.0;
                    string rawText = e.SubItem.Text.Replace("$", "").Replace("zł", "").Replace(",", "").Trim();
                    double.TryParse(rawText, out netVal);
                    itemTextColor = netVal < 0 ? Color.FromArgb(240, 100, 100) : Color.FromArgb(100, 240, 140);
                }

                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, lv.Font, e.Bounds, itemTextColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
        }

        private void RefreshFinancialData()
        {
            CultureInfo culture = CultureInfo.CurrentCulture;

            // 1. Balance Sheet Values (Aktywa i Pasywa)
            double cash = engine.Stats.Cash;
            double propertyAssets = engine.Stats.CachedPropertyAssetValuation;
            double inventoryVal = engine.Stats.CachedTotalInventoryValuation;
            double totalAssets = cash + propertyAssets + inventoryVal;

            double loanDebt = engine.Stats.LoanBalance;
            double accruedInterest = engine.Stats.CachedAccruedInterest;
            double totalLiabilities = loanDebt + accruedInterest;
            double bookValue = engine.Stats.CachedBookValue;
            double totalLiabilitiesEquity = totalLiabilities + bookValue;

            lblCashVal.Text = $"Środki pieniężne (Cash): {cash.ToString("C", culture)}";
            lblPropertyVal.Text = $"Nieruchomości (Property): {propertyAssets.ToString("C", culture)}";
            lblInventoryVal.Text = $"Zapasy (Inventory): {inventoryVal.ToString("C", culture)}";
            lblTotalAssets.Text = $"SUMA AKTYWÓW: {totalAssets.ToString("C", culture)}";

            lblLoanDebt.Text = $"Kredyty bankowe: {loanDebt.ToString("C", culture)}";
            lblAccruedInterest.Text = $"Naliczone odsetki: {accruedInterest.ToString("C", culture)}";
            lblTotalLiabilities.Text = $"Suma zobowiązań: {totalLiabilities.ToString("C", culture)}";
            lblBookValue.Text = $"Wartość księgowa (Equity): {bookValue.ToString("C", culture)}";
            lblTotalLiabilitiesEquity.Text = $"SUMA PASYWÓW: {totalLiabilitiesEquity.ToString("C", culture)}";


            // 2. Debt Desk Values
            string rating = engine.Stats.GetCreditRating();
            double benchmark = engine.Interest_Rate;
            double markup = engine.Stats.GetLendingMarkup();
            double effectiveRate = benchmark + markup;
            double cashToDebt = loanDebt > 0 ? cash / loanDebt : 99.9;

            lblBenchmarkRate.Text = $"Benchmark Banku Centralnego: {benchmark.ToString("P2", culture)}";
            lblRating.Text = $"Ocena Kredytowa (Credit Rating): {rating}";
            lblRating.ForeColor = rating switch
            {
                "A" => Color.FromArgb(100, 240, 140),
                "B" => Color.FromArgb(140, 200, 250),
                "C" => Color.FromArgb(230, 140, 80),
                _ => Color.FromArgb(240, 100, 100)
            };
            lblMarkup.Text = $"Marża ryzyka bankowego: +{markup.ToString("P2", culture)}";
            lblEffectiveRate.Text = $"Efektywne oprocentowanie (APR): {effectiveRate.ToString("P2", culture)}";
            lblCashToDebt.Text = $"Cash-to-Debt Ratio: {cashToDebt:F2}";

            // Enable/disable buttons based on loan capacities
            btnBorrow50k.Enabled = (engine.Stats.LoanBalance + 50000.0 <= engine.Stats.MaxLoanLimit);
            btnRepay50k.Enabled = (engine.Stats.LoanBalance >= 50000.0 && engine.Stats.Cash >= 50000.0);


            // 3. Profit & Loss Statement (P&L rolling sum)
            double retailRevSum = 0;
            double apartmentRevSum = 0;
            double officeRevSum = 0;
            double wagesSum = 0;
            double baseMaintSum = 0;
            double landTaxesSum = 0;
            double logisticsSum = 0;
            double interestSum = 0;
            double taxesPaidSum = 0;

            if (engine.Stats.FinancialHistory.Count == 0)
            {
                // Day 1 projection: extrapolate hourly numbers * 24 * 30
                double scale = 24 * 30;
                retailRevSum = engine.Stats.CurrentHourRetailRevenue * scale;
                apartmentRevSum = engine.Stats.CurrentHourApartmentRevenue * scale;
                officeRevSum = engine.Stats.CurrentHourOfficeRevenue * scale;
                wagesSum = engine.Stats.CurrentHourWages * scale;
                baseMaintSum = engine.Stats.CurrentHourBaseMaintenance * scale;
                landTaxesSum = engine.Stats.CurrentHourLandTaxes * scale;
                logisticsSum = engine.Stats.CurrentHourFreightCost * scale;
                interestSum = engine.Stats.CurrentHourInterest * scale;
                taxesPaidSum = 0;

                if (retailRevSum == 0 && apartmentRevSum == 0 && officeRevSum == 0)
                {
                    officeRevSum = 30000.0; // Startup baseline projection
                }
            }
            else
            {
                foreach (var record in engine.Stats.FinancialHistory)
                {
                    retailRevSum += record.RetailRevenue;
                    apartmentRevSum += record.ApartmentRevenue;
                    officeRevSum += record.OfficeRevenue;
                    wagesSum += record.Wages;
                    baseMaintSum += record.BaseMaintenance;
                    landTaxesSum += record.LandTaxes;
                    logisticsSum += record.Logistics;
                    interestSum += record.Interest;
                    taxesPaidSum += record.TaxesPaid;
                }
            }

            double totalRev = retailRevSum + apartmentRevSum + officeRevSum;
            double totalOpex = wagesSum + baseMaintSum + landTaxesSum + logisticsSum;
            double ebitda = totalRev - totalOpex;
            double netProfit = ebitda - interestSum - taxesPaidSum;

            double ebitdaMargin = totalRev > 0 ? ebitda / totalRev : 0.0;
            double netMargin = totalRev > 0 ? netProfit / totalRev : 0.0;

            lblPLRetailRev.Text = $"Sprzedaż detaliczna (Retail): {retailRevSum.ToString("C", culture)}";
            lblPLApartmentRev.Text = $"Wynajem mieszkań (Apartment): {apartmentRevSum.ToString("C", culture)}";
            lblPLOfficeRev.Text = $"Usługi biurowe (Office Consulting): {officeRevSum.ToString("C", culture)}";
            lblPLTotalRev.Text = $"SUMA PRZYCHODÓW OPERACYJNYCH: {totalRev.ToString("C", culture)}";

            lblPLSalaries.Text = $"Wynagrodzenia personelu (Wages): {wagesSum.ToString("C", culture)}";
            lblPLLandTaxes.Text = $"Podatki gruntowe (Land Taxes): {landTaxesSum.ToString("C", culture)}";
            lblPLLogistics.Text = $"Koszty spedycji i logistyki: {logisticsSum.ToString("C", culture)}";
            lblPLPowerMaint.Text = $"Utrzymanie sieci i energii: {baseMaintSum.ToString("C", culture)}";
            lblPLTotalOpex.Text = $"SUMA KOSZTÓW OPERACYJNYCH (OPEX): {totalOpex.ToString("C", culture)}";

            lblPLEbitda.Text = $"Monthly EBITDA: {ebitda.ToString("C", culture)}";
            lblPLEbitda.ForeColor = ebitda < 0 ? Color.FromArgb(240, 100, 100) : Color.FromArgb(140, 200, 250);
            lblPLEbitdaMargin.Text = $"Marża EBITDA: {ebitdaMargin.ToString("P2", culture)}";

            lblPLInterest.Text = $"Odsetki kredytowe: {interestSum.ToString("C", culture)}";
            lblPLTaxes.Text = $"Podatek dochodowy (CIT): {taxesPaidSum.ToString("C", culture)}";

            lblPLNetProfit.Text = $"Zysk Netto (Net Profit): {netProfit.ToString("C", culture)}";
            lblPLNetProfit.ForeColor = netProfit < 0 ? Color.FromArgb(240, 100, 100) : Color.FromArgb(100, 240, 140);
            lblPLNetMargin.Text = $"Rentowność Netto: {netMargin.ToString("P2", culture)}";


            // 4. Daily Ledger History Grid
            financialHistoryView.Items.Clear();
            foreach (var record in engine.Stats.FinancialHistory)
            {
                ListViewItem item = new ListViewItem(record.Timestamp.ToString("MM/dd HH:mm"));
                item.SubItems.Add(record.Revenue.ToString("C0", culture));
                double totalExpenses = record.Revenue - record.NetProfit;
                item.SubItems.Add(totalExpenses.ToString("C0", culture));
                item.SubItems.Add(record.NetProfit.ToString("C0", culture));
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

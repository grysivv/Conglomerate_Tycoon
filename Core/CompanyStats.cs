using System;
using System.Collections.Generic;
using System.Linq;

namespace TycoonGame.Core
{
    public class FinancialRecord
    {
        public DateTime Timestamp { get; }
        public double Revenue { get; }
        public double Wages { get; }
        public double Maintenance { get; }
        public double Logistics { get; }
        public double Interest { get; }
        public double TaxesPaid { get; }
        public double NetProfit => Revenue - (Wages + Maintenance + Logistics + Interest + TaxesPaid);

        public FinancialRecord(DateTime timestamp, double revenue, double wages, double maintenance, double logistics, double interest, double taxesPaid)
        {
            Timestamp = timestamp;
            Revenue = revenue;
            Wages = wages;
            Maintenance = maintenance;
            Logistics = logistics;
            Interest = interest;
            TaxesPaid = taxesPaid;
        }
    }

    public class CompanyStats
    {
        public double Cash { get; set; }
        public double LoanBalance { get; set; }
        public double InterestRate { get; set; } // Annualized rate, dynamically set by game engine
        public double TaxRate { get; } // Corporate tax rate, e.g. 0.20 for 20%
        public double MaxLoanLimit => 1000000.0; // Clamped at $1,000,000 max borrowing limit

        // Current Hour Tickers
        public double CurrentHourRevenue { get; set; }
        public double CurrentHourWages { get; set; }
        public double CurrentHourMaintenance { get; set; }
        public double CurrentHourLogistics { get; set; }
        public double CurrentHourInterest => (LoanBalance * (InterestRate / (365.0 * 24.0))); // Hourly portion of annual interest

        // Daily accumulator (for tax calculation and reports)
        public double DailyRevenueAccumulator { get; set; }
        public double DailyExpenseAccumulator { get; set; }
        public double DailyTaxesPaid { get; set; }
        
        // History ledger for UI graphs and spreadsheets
        public List<FinancialRecord> FinancialHistory { get; }

        // GPW Stock Market registries (Step 3)
        public double PlayerTotalShares { get; set; }
        public double PlayerSharesOwnedByPlayer { get; set; }
        public double PlayerSharesOwnedByAi { get; set; }
        public double PlayerStockPrice { get; set; }

        public double AiTotalShares { get; set; }
        public double AiSharesOwnedByPlayer { get; set; }
        public double AiSharesOwnedByAi { get; set; }
        public double AiStockPrice { get; set; }

        public double AiCash { get; set; }
        public double AiNetIncome { get; set; }
        public double AiBookValue { get; set; }

        public CompanyStats()
        {
            Cash = 500000.0; // $500,000 cash starting capital
            LoanBalance = 250000.0; // $250,000 starting loan debt
            InterestRate = 0.06; // 6% annual interest
            TaxRate = 0.20; // 20% corporate tax
            
            CurrentHourRevenue = 0;
            CurrentHourWages = 0;
            CurrentHourMaintenance = 0;
            CurrentHourLogistics = 0;

            DailyRevenueAccumulator = 0;
            DailyExpenseAccumulator = 0;
            DailyTaxesPaid = 0;
            
            FinancialHistory = new List<FinancialRecord>();

            // Stock market initialization
            PlayerTotalShares = 1000000.0;
            PlayerSharesOwnedByPlayer = 600000.0; // Player starts owning 60% of their company (600,000 shares)
            PlayerSharesOwnedByAi = 0.0; // Competitors own 0% initially
            PlayerStockPrice = 10.0; // Starting share price is $10.0

            AiTotalShares = 1000000.0;
            AiSharesOwnedByPlayer = 0.0; // Player owns 0% of AI initially
            AiSharesOwnedByAi = 700000.0; // AI owns 70% of itself
            AiStockPrice = 12.0; // Starting share price of AI is $12.0

            AiCash = 500000.0; // AI competitor starts with cash
            AiNetIncome = 40000.0;
            AiBookValue = 600000.0;
        }

        public bool BorrowLoan(double amount)
        {
            if (LoanBalance + amount > MaxLoanLimit) return false;
            
            LoanBalance += amount;
            Cash += amount;
            return true;
        }

        public bool PaybackLoan(double amount)
        {
            if (amount > Cash) return false;
            if (amount > LoanBalance) amount = LoanBalance;

            LoanBalance -= amount;
            Cash -= amount;
            return true;
        }

        public void ProcessHourlyBilling()
        {
            double hourlyInterest = CurrentHourInterest;
            double hourlyExpenses = CurrentHourWages + CurrentHourMaintenance + CurrentHourLogistics + hourlyInterest;
            
            // Subtract expenses and add revenue to active liquid assets
            Cash += CurrentHourRevenue - hourlyExpenses;

            // Accumulate daily stats
            DailyRevenueAccumulator += CurrentHourRevenue;
            DailyExpenseAccumulator += hourlyExpenses;

            // Reset current hour tickers for next hour cycle
            CurrentHourRevenue = 0;
            CurrentHourWages = 0;
            CurrentHourMaintenance = 0;
            CurrentHourLogistics = 0;
        }

        public void CycleDay(DateTime date)
        {
            // Calculate taxes on net profit for the day (if positive)
            double dailyNetProfitBeforeTax = DailyRevenueAccumulator - DailyExpenseAccumulator;
            double taxesDue = 0;
            if (dailyNetProfitBeforeTax > 0)
            {
                taxesDue = dailyNetProfitBeforeTax * TaxRate;
                Cash -= taxesDue;
                DailyTaxesPaid = taxesDue;
            }

            // Create historic record
            FinancialHistory.Add(new FinancialRecord(
                date,
                DailyRevenueAccumulator,
                DailyExpenseAccumulator - CurrentHourInterest, // Total wages, maintenance, logistics
                0, // Split values are kept inside the accumulator
                0,
                CurrentHourInterest * 24, // Estimate interest paid
                taxesDue
            ));

            // Keep history list capped at 30 days to avoid memory leaking
            if (FinancialHistory.Count > 30)
            {
                FinancialHistory.RemoveAt(0);
            }

            // Reset daily counters
            DailyRevenueAccumulator = 0;
            DailyExpenseAccumulator = 0;
            DailyTaxesPaid = 0;
        }

        // GPW Stock valuation and trades (Step 3)
        public double GetTrailingMonthlyNetIncome()
        {
            if (FinancialHistory.Count == 0) return 30000.0; // Default baseline starting monthly profit
            double sum = FinancialHistory.Sum(r => r.NetProfit);
            if (FinancialHistory.Count < 30)
            {
                // Extrapolate daily profits to 30 days
                sum = (sum / FinancialHistory.Count) * 30.0;
            }
            return sum;
        }

        public void UpdatePlayerStockPrice(double totalAssetValue)
        {
            // P/E valuation component (50% weight) - 15x multiplier on trailing monthly net income
            double netIncome = GetTrailingMonthlyNetIncome();
            double peValue = Math.Max(0.0, netIncome) * 15.0;

            // Book value component (50% weight)
            double bookValue = Cash + totalAssetValue - LoanBalance;
            bookValue = Math.Max(0.0, bookValue);

            double totalEquityValue = 0.50 * peValue + 0.50 * bookValue;
            
            // Share price is equity value divided by total shares
            PlayerStockPrice = Math.Max(0.10, totalEquityValue / PlayerTotalShares);
        }

        public void UpdateAiStockPrice()
        {
            // P/E valuation component (50% weight)
            double peValue = Math.Max(0.0, AiNetIncome) * 15.0;

            // Book value component (50% weight)
            double bookValue = Math.Max(0.0, AiBookValue);

            double totalEquityValue = 0.50 * peValue + 0.50 * bookValue;

            AiStockPrice = Math.Max(0.10, totalEquityValue / AiTotalShares);
        }

        public void ExecuteAiTakeoverPass(double playerCash, double interestRate, int cyclePhaseInt)
        {
            // Simulate AI cash reserve growth
            AiCash += AiNetIncome / 12.0;
            if (AiCash < 0) AiCash = 10000.0;

            // 0 = Recovery, 1 = Boom, 2 = Slowdown, 3 = Recession (matches CyclePhase enum cast)
            bool isRecession = (cyclePhaseInt == 3);

            // Distress condition: low player cash, high central bank interest rate (>= 6%)
            bool playerInDistress = isRecession && (playerCash < 150000.0) && (interestRate >= 0.06);

            if (playerInDistress && AiCash > 0)
            {
                double playerFreeFloat = PlayerTotalShares - PlayerSharesOwnedByPlayer - PlayerSharesOwnedByAi;
                if (playerFreeFloat > 0)
                {
                    // AI spends up to 40% of its cash reserve to buy player's free-float shares
                    double maxCashToSpend = AiCash * 0.40;
                    double targetSharesToBuy = maxCashToSpend / PlayerStockPrice;
                    targetSharesToBuy = Math.Min(targetSharesToBuy, playerFreeFloat);

                    double cost = targetSharesToBuy * PlayerStockPrice;
                    if (cost > 0)
                    {
                        PlayerSharesOwnedByAi += targetSharesToBuy;
                        AiCash -= cost;
                    }
                }
            }
        }

        // Trading actions
        public bool BuyAiShares(double shares)
        {
            double cost = shares * AiStockPrice;
            if (Cash < cost) return false;

            double aiFreeFloat = AiTotalShares - AiSharesOwnedByAi - AiSharesOwnedByPlayer;
            if (shares > aiFreeFloat) shares = aiFreeFloat;

            if (shares <= 0) return false;

            Cash -= cost;
            AiSharesOwnedByPlayer += shares;
            AiCash += cost; // Proceeds go to AI cash reserve
            return true;
        }

        public bool SellAiShares(double shares)
        {
            if (shares > AiSharesOwnedByPlayer) shares = AiSharesOwnedByPlayer;
            if (shares <= 0) return false;

            double proceeds = shares * AiStockPrice;
            Cash += proceeds;
            AiSharesOwnedByPlayer -= shares;
            AiCash -= proceeds; // AI pays back player
            return true;
        }

        public bool BuybackPlayerShares(double shares)
        {
            double cost = shares * PlayerStockPrice;
            if (Cash < cost) return false;

            double playerFreeFloat = PlayerTotalShares - PlayerSharesOwnedByPlayer - PlayerSharesOwnedByAi;
            if (shares > playerFreeFloat) shares = playerFreeFloat;

            if (shares <= 0) return false;

            Cash -= cost;
            PlayerSharesOwnedByPlayer += shares;
            return true;
        }

        public void DilutePlayerShares(double sharesToIssue)
        {
            if (sharesToIssue <= 0) return;

            double capitalRaised = sharesToIssue * PlayerStockPrice;
            Cash += capitalRaised;
            PlayerTotalShares += sharesToIssue;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace TycoonGame.Core
{
    public class FinancialRecord
    {
        public DateTime Timestamp { get; }
        public double RetailRevenue { get; }
        public double ApartmentRevenue { get; }
        public double OfficeRevenue { get; }
        public double Wages { get; }
        public double BaseMaintenance { get; }
        public double LandTaxes { get; }
        public double Logistics { get; }
        public double Interest { get; }
        public double TaxesPaid { get; }

        public double Revenue => RetailRevenue + ApartmentRevenue + OfficeRevenue;
        public double NetProfit => Revenue - (Wages + BaseMaintenance + LandTaxes + Logistics + Interest + TaxesPaid);

        public FinancialRecord(DateTime timestamp, double retailRev, double apartmentRev, double officeRev, double wages, double baseMaint, double landTaxes, double logistics, double interest, double taxesPaid)
        {
            Timestamp = timestamp;
            RetailRevenue = retailRev;
            ApartmentRevenue = apartmentRev;
            OfficeRevenue = officeRev;
            Wages = wages;
            BaseMaintenance = baseMaint;
            LandTaxes = landTaxes;
            Logistics = logistics;
            Interest = interest;
            TaxesPaid = taxesPaid;
        }
    }

    public class CompanyStats
    {
        public double Cash { get; set; }
        public double LoanBalance { get; set; }
        public double InterestRate { get; set; } // Annualized benchmark rate, set by game engine
        public double TaxRate { get; } // Corporate tax rate, e.g. 0.20 for 20%
        public double MaxLoanLimit => 1000000.0; // Clamped at $1,000,000 max borrowing limit
        public string CompanyName { get; set; }

        // Cached Fields for high-performance UI retrieval (Step 7)
        public double CachedPropertyAssetValuation { get; set; }
        public double CachedTotalInventoryValuation { get; set; }
        public double CachedBookValue { get; set; }
        public double CachedAccruedInterest { get; set; }

        // Granular Current Hour Tickers
        public double CurrentHourRetailRevenue { get; set; }
        public double CurrentHourApartmentRevenue { get; set; }
        public double CurrentHourOfficeRevenue { get; set; }
        public double CurrentHourWages { get; set; }
        public double CurrentHourBaseMaintenance { get; set; }
        public double CurrentHourLandTaxes { get; set; }
        public double CurrentHourFreightCost { get; set; }

        // Legacy compatibility properties
        public double CurrentHourRevenue 
        { 
            get => CurrentHourRetailRevenue + CurrentHourApartmentRevenue + CurrentHourOfficeRevenue; 
            set => CurrentHourRetailRevenue = value; 
        }
        public double CurrentHourMaintenance 
        { 
            get => CurrentHourBaseMaintenance + CurrentHourLandTaxes; 
            set => CurrentHourBaseMaintenance = value; 
        }
        public double CurrentHourLogistics 
        { 
            get => CurrentHourFreightCost; 
            set => CurrentHourFreightCost = value; 
        }

        public double CurrentHourInterest
        {
            get
            {
                double markup = GetLendingMarkup();
                return LoanBalance * ((InterestRate + markup) / (365.0 * 24.0));
            }
        }

        // Daily accumulators (for tax calculation and daily historical ledger)
        public double DailyRetailRevenue { get; set; }
        public double DailyApartmentRevenue { get; set; }
        public double DailyOfficeRevenue { get; set; }
        public double DailyWages { get; set; }
        public double DailyBaseMaintenance { get; set; }
        public double DailyLandTaxes { get; set; }
        public double DailyLogistics { get; set; }
        public double DailyInterest { get; set; }
        public double DailyTaxesPaid { get; set; }
        
        public double DailyRevenueAccumulator => DailyRetailRevenue + DailyApartmentRevenue + DailyOfficeRevenue;
        public double DailyExpenseAccumulator => DailyWages + DailyBaseMaintenance + DailyLandTaxes + DailyLogistics + DailyInterest;
        
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

        // IPO and cashflow tracking
        public bool IsIpoLaunched { get; set; }
        public bool IsPubliclyTraded { get => IsIpoLaunched; set => IsIpoLaunched = value; }
        public double PreviousMonthCashflow { get; set; }

        public CompanyStats()
        {
            CompanyName = "Player Corp";
            Cash = 500000.0; // $500,000 cash starting capital
            LoanBalance = 250000.0; // $250,000 starting loan debt
            InterestRate = 0.06; // 6% annual interest
            TaxRate = 0.20; // 20% corporate tax
            
            CachedPropertyAssetValuation = 0.0;
            CachedTotalInventoryValuation = 0.0;
            CachedBookValue = 250000.0;
            CachedAccruedInterest = 0.0;

            CurrentHourRetailRevenue = 0;
            CurrentHourApartmentRevenue = 0;
            CurrentHourOfficeRevenue = 0;
            CurrentHourWages = 0;
            CurrentHourBaseMaintenance = 0;
            CurrentHourLandTaxes = 0;
            CurrentHourFreightCost = 0;

            DailyRetailRevenue = 0;
            DailyApartmentRevenue = 0;
            DailyOfficeRevenue = 0;
            DailyWages = 0;
            DailyBaseMaintenance = 0;
            DailyLandTaxes = 0;
            DailyLogistics = 0;
            DailyInterest = 0;
            DailyTaxesPaid = 0;
            
            FinancialHistory = new List<FinancialRecord>();

            // Stock market initialization
            PlayerTotalShares = 1000000.0;
            PlayerSharesOwnedByPlayer = 1000000.0; // Starts 100% private
            PlayerSharesOwnedByAi = 0.0;
            PlayerStockPrice = 10.0;
            IsIpoLaunched = false;
            PreviousMonthCashflow = 30000.0; // Default baseline starting monthly profit

            AiTotalShares = 1000000.0;
            AiSharesOwnedByPlayer = 0.0; // Player owns 0% of AI initially
            AiSharesOwnedByAi = 700000.0; // AI owns 70% of itself
            AiStockPrice = 12.0; // Starting share price of AI is $12.0

            AiCash = 500000.0; // AI competitor starts with cash
            AiNetIncome = 40000.0;
            AiBookValue = 600000.0;
        }

        public string GetCreditRating()
        {
            double debt = LoanBalance;
            double cash = Cash;
            double netIncome = GetTrailingMonthlyNetIncome();

            if (debt <= 0) return "AAA";

            double cashToDebt = cash / debt;
            
            int score = 0;
            if (cashToDebt > 1.5) score += 4;
            else if (cashToDebt > 0.8) score += 3;
            else if (cashToDebt > 0.3) score += 2;
            else if (cashToDebt > 0.1) score += 1;

            if (netIncome > 100000) score += 4;
            else if (netIncome > 40000) score += 3;
            else if (netIncome > 10000) score += 2;
            else if (netIncome > 0) score += 1;

            return score switch
            {
                >= 7 => "A",
                6 => "B",
                5 => "C",
                4 => "D",
                3 => "E",
                _ => "F"
            };
        }

        public double GetLendingMarkup()
        {
            string rating = GetCreditRating();
            return rating switch
            {
                "AAA" => 0.0,
                "A" => 0.005,  // 0.5%
                "B" => 0.015,  // 1.5%
                "C" => 0.030,  // 3.0%
                "D" => 0.050,  // 5.0%
                "E" => 0.075,  // 7.5%
                "F" => 0.100,  // 10.0%
                _ => 0.10
            };
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
            double hourlyExpenses = CurrentHourWages + CurrentHourBaseMaintenance + CurrentHourLandTaxes + CurrentHourFreightCost + hourlyInterest;
            double hourlyRevenue = CurrentHourRetailRevenue + CurrentHourApartmentRevenue + CurrentHourOfficeRevenue;

            // Subtract expenses and add revenue to active liquid assets
            Cash += hourlyRevenue - hourlyExpenses;

            // Accumulate daily stats
            DailyRetailRevenue += CurrentHourRetailRevenue;
            DailyApartmentRevenue += CurrentHourApartmentRevenue;
            DailyOfficeRevenue += CurrentHourOfficeRevenue;

            DailyWages += CurrentHourWages;
            DailyLandTaxes += CurrentHourLandTaxes;
            DailyBaseMaintenance += CurrentHourBaseMaintenance;
            DailyLogistics += CurrentHourFreightCost;
            DailyInterest += hourlyInterest;
            CachedAccruedInterest += hourlyInterest;

            // Reset current hour tickers for next hour cycle
            CurrentHourRetailRevenue = 0;
            CurrentHourApartmentRevenue = 0;
            CurrentHourOfficeRevenue = 0;
            CurrentHourWages = 0;
            CurrentHourBaseMaintenance = 0;
            CurrentHourLandTaxes = 0;
            CurrentHourFreightCost = 0;
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
                DailyRetailRevenue,
                DailyApartmentRevenue,
                DailyOfficeRevenue,
                DailyWages,
                DailyBaseMaintenance,
                DailyLandTaxes,
                DailyLogistics,
                DailyInterest,
                taxesDue
            ));

            // Keep history list capped at 30 days to avoid memory leaking
            if (FinancialHistory.Count > 30)
            {
                FinancialHistory.RemoveAt(0);
            }

            // Reset daily counters
            DailyRetailRevenue = 0;
            DailyApartmentRevenue = 0;
            DailyOfficeRevenue = 0;
            DailyWages = 0;
            DailyLandTaxes = 0;
            DailyBaseMaintenance = 0;
            DailyLogistics = 0;
            DailyInterest = 0;
            DailyTaxesPaid = 0;
            CachedAccruedInterest = 0.0;
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

        public void UpdateCachedValues(double totalAssets, double totalInventory)
        {
            CachedPropertyAssetValuation = totalAssets;
            CachedTotalInventoryValuation = totalInventory;
            CachedBookValue = Cash + totalAssets + totalInventory - LoanBalance;
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

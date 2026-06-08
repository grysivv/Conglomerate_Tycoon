using System;

namespace TycoonGame.Core
{
    public class Tile
    {
        public int X { get; }
        public int Y { get; }
        public TileType Type { get; set; }
        public int Level { get; set; }
        public bool IsPowered { get; set; }
        public bool HasRoadAccess { get; set; }
        public int EmployeeCount { get; set; }
        public int MaxEmployees { get; set; }
        public double TrainingBudgetPerHour { get; set; }
        public double SkillLevel { get; set; }
        public double Morale { get; set; }
        public bool IsOwnedByPlayer { get; set; }
        public bool HasOilDeposit { get; set; }

        public double GetPerformanceMultiplier()
        {
            return (0.4 + 0.6 * SkillLevel) * (0.5 + 0.5 * Morale);
        }
        
        // Supply Chain / Inventory / Tenant parameters
        // Note: For Apartments, Inventory represents active tenants count, and MaxInventory represents tenant capacity.
        // For Universities, Inventory represents training progress (0-100%), and MaxInventory represents 100%.
        public double Inventory { get; set; }
        public double MaxInventory { get; set; }
        public double ProductionRate { get; set; } // Units per tick per worker
        public double SalesRate { get; set; } // Units per tick per worker/traffic
        
        // Economics
        public double MaintenanceCost { get; set; } // Paid every hour tick
        public double HistoricalEarnings { get; set; } // Lifetime cumulative earnings
        public double LastDayEarnings { get; set; } // Earnings during the last 24-hour cycle

        // Zoning and Urban Space stats
        public decimal LandValue { get; set; } // Land pricing gradient
        public double TrafficIndex { get; set; } // Foot traffic rate (0-100)

        // Step 4: Infrastructure & Supply Chain
        public double RetailPrice { get; set; } // Store-specific selling price
        public double DepreciatedValue { get; set; }

        public Tile(int x, int y)
        {
            X = x;
            Y = y;
            Type = TileType.Grass;
            Level = 0;
            IsPowered = false;
            HasRoadAccess = false;
            IsOwnedByPlayer = false;
            HasOilDeposit = false;
            EmployeeCount = 0;
            MaxEmployees = 0;
            Inventory = 0;
            MaxInventory = 0;
            ProductionRate = 0;
            SalesRate = 0;
            MaintenanceCost = 0;
            HistoricalEarnings = 0;
            LastDayEarnings = 0;
            LandValue = 0m;
            TrafficIndex = 0.0;
            RetailPrice = 0.0;
            DepreciatedValue = 0.0;
            TrainingBudgetPerHour = 0.0;
            SkillLevel = 0.1;
            Morale = 0.8;
        }

        public void ResetToGrass()
        {
            Type = TileType.Grass;
            Level = 0;
            IsPowered = false;
            HasRoadAccess = false;
            EmployeeCount = 0;
            MaxEmployees = 0;
            Inventory = 0;
            MaxInventory = 0;
            ProductionRate = 0;
            SalesRate = 0;
            MaintenanceCost = 0;
            HistoricalEarnings = 0;
            LastDayEarnings = 0;
            RetailPrice = 0.0;
            DepreciatedValue = 0.0;
            TrainingBudgetPerHour = 0.0;
            SkillLevel = 0.1;
            Morale = 0.8;
            // Note: We retain LandValue and base TrafficIndex so the geography is static
        }

        public void SetupBuilding(TileType type)
        {
            Type = type;
            Level = 1;
            IsPowered = false;
            HasRoadAccess = false;
            EmployeeCount = 0;
            HistoricalEarnings = 0;
            LastDayEarnings = 0;
            RetailPrice = 0.0;
            TrainingBudgetPerHour = 0.0;
            SkillLevel = 0.1;
            Morale = 0.8;

            switch (type)
            {
                case TileType.Road:
                    MaxEmployees = 0;
                    Inventory = 0;
                    MaxInventory = 0;
                    ProductionRate = 0;
                    SalesRate = 0;
                    MaintenanceCost = 2.0; // Minimal road upkeep
                    break;

                case TileType.Office:
                    MaxEmployees = 15;
                    Inventory = 0;
                    MaxInventory = 0;
                    ProductionRate = 25.0; // $25 contract value generated per employee/hr
                    SalesRate = 0;
                    MaintenanceCost = 75.0; // Higher rent/facilities upkeep
                    break;

                case TileType.Factory:
                    MaxEmployees = 25;
                    Inventory = 0;
                    MaxInventory = 200;
                    ProductionRate = 1.5; // Produces 1.5 units of Goods per employee/hr
                    SalesRate = 0;
                    MaintenanceCost = 150.0; // Heavy machinery upkeep
                    break;

                case TileType.Retail:
                    MaxEmployees = 10;
                    Inventory = 0;
                    MaxInventory = 100;
                    ProductionRate = 0;
                    SalesRate = 1.2; // Sells up to 1.2 units of Goods per employee/hr
                    MaintenanceCost = 80.0; // Shopfront upkeep
                    RetailPrice = 45.00; // Default selling price
                    break;

                case TileType.PowerPlant:
                    MaxEmployees = 8;
                    Inventory = 0;
                    MaxInventory = 0;
                    ProductionRate = 150.0; // Generates 150 units of electricity
                    SalesRate = 0;
                    MaintenanceCost = 200.0; // High plant upkeep
                    break;

                case TileType.Apartment:
                    MaxEmployees = 0; // Passive residential space
                    Inventory = 0; // Starts with 0 tenants
                    MaxInventory = 50; // Tenant capacity
                    ProductionRate = 0;
                    SalesRate = 0;
                    MaintenanceCost = 100.0; // High default structural maintenance
                    break;

                case TileType.University:
                    MaxEmployees = 0; // Passive science generator (no workers to assign)
                    Inventory = 0; // Starts with 0% progress
                    MaxInventory = 100.0; // 100% required to train a scientist
                    ProductionRate = 4.0; // 4% training progress per hour
                    SalesRate = 0;
                    MaintenanceCost = 400.0; // High academic facility upkeep
                    break;

                case TileType.Farm:
                    MaxEmployees = 12;
                    Inventory = 0;
                    MaxInventory = 150;
                    ProductionRate = 2.0;
                    SalesRate = 0;
                    MaintenanceCost = 40.0;
                    break;

                case TileType.OilWell:
                    MaxEmployees = 10;
                    Inventory = 0;
                    MaxInventory = 100;
                    ProductionRate = 1.5;
                    SalesRate = 0;
                    MaintenanceCost = 180.0;
                    break;

                case TileType.Grass:
                default:
                    ResetToGrass();
                    break;
            }
            DepreciatedValue = GetAssetValue();
        }

        public void Upgrade()
        {
            if (Type == TileType.Grass || Type == TileType.Road) return;

            Level++;
            MaintenanceCost *= 1.4; // 40% maintenance increase
            
            if (Type != TileType.Apartment && Type != TileType.University)
            {
                MaxEmployees = (int)(MaxEmployees * 1.5); // 50% capacity expansion
            }

            if (Type == TileType.Factory || Type == TileType.Retail || Type == TileType.Apartment || Type == TileType.Farm || Type == TileType.OilWell)
            {
                MaxInventory = (int)(MaxInventory * 1.5); // 50% storage/occupancy capacity increase
            }

            double upgradeCost = Type switch
            {
                TileType.Office => 24000.0,
                TileType.Factory => 48000.0,
                TileType.Retail => 32000.0,
                TileType.PowerPlant => 40000.0,
                TileType.Apartment => 40000.0,
                TileType.University => 60000.0,
                TileType.Farm => 16000.0,
                TileType.OilWell => 28000.0,
                _ => 0.0
            };
            DepreciatedValue += upgradeCost;
        }

        public double GetPowerConsumption()
        {
            if (Level == 0) return 0;

            switch (Type)
            {
                case TileType.Office: return 8.0 * Level;
                case TileType.Factory: return 15.0 * Level;
                case TileType.Retail: return 5.0 * Level;
                case TileType.Road: return 0.2;
                case TileType.Apartment: return 6.0 * Level; // Apartments need grid electricity
                case TileType.University: return 10.0 * Level; // Universities need high grid electricity
                case TileType.Farm: return 2.0 * Level;
                case TileType.OilWell: return 12.0 * Level;
                case TileType.PowerPlant: return 0;
                default: return 0;
            }
        }

        public double GetPowerProduction()
        {
            if (Type != TileType.PowerPlant || Level == 0) return 0;
            // Production scaled by workers & level
            double workerRatio = MaxEmployees > 0 ? (double)EmployeeCount / MaxEmployees : 1.0;
            return ProductionRate * Level * (0.3 + 0.7 * workerRatio);
        }

        public decimal GetLandTax()
        {
            // Roads and Grass are exempt from land taxes
            if (Type == TileType.Grass || Type == TileType.Road) return 0m;

            // Hourly land tax based on property location value and structure size
            return LandValue * Level * 0.50m;
        }

        public double GetAssetValue()
        {
            if (Type == TileType.Grass) return 0.0;
            double baseCost = Type switch
            {
                TileType.Road => 1000.0,
                TileType.Office => 30000.0,
                TileType.Factory => 60000.0,
                TileType.Retail => 40000.0,
                TileType.PowerPlant => 50000.0,
                TileType.Apartment => 50000.0,
                TileType.University => 80000.0,
                TileType.Farm => 20000.0,
                TileType.OilWell => 35000.0,
                _ => 0.0
            };
            double upgradeCost = Type switch
            {
                TileType.Office => 24000.0,
                TileType.Factory => 48000.0,
                TileType.Retail => 32000.0,
                TileType.PowerPlant => 40000.0,
                TileType.Apartment => 40000.0,
                TileType.University => 60000.0,
                TileType.Farm => 16000.0,
                TileType.OilWell => 28000.0,
                _ => 0.0
            };
            return baseCost + (Level - 1) * upgradeCost;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace TycoonGame.Core
{
    public enum CyclePhase
    {
        Recovery,
        Boom,
        Slowdown,
        Recession
    }

    public class Competitor
    {
        public string Name { get; }
        public double MarketShare { get; set; }
        public double AveragePrice { get; set; }

        public Competitor(string name, double share, double price)
        {
            Name = name;
            MarketShare = share;
            AveragePrice = price;
        }
    }

    // JSON Serialization DTOs
    public class SaveData
    {
        public string CompanyName { get; set; } = "Player Corp";
        public double Cash { get; set; }
        public double LoanBalance { get; set; }
        public bool IsIpoLaunched { get; set; }
        public double PreviousMonthCashflow { get; set; }
        
        public string CurrentDate { get; set; } = "";
        public double GDP_Index { get; set; }
        public double Inflation_Rate { get; set; }
        public double Unemployment_Rate { get; set; }
        public double Interest_Rate { get; set; }
        public double ConsumerConfidenceIndex { get; set; }
        public int CyclePhase { get; set; }

        public List<string> CompletedResearch { get; set; } = new List<string>();
        public string ActiveResearchName { get; set; } = "";
        public double ActiveResearchPoints { get; set; }

        public List<TileSaveData> Tiles { get; set; } = new List<TileSaveData>();
        public List<EmployeeSaveData> Employees { get; set; } = new List<EmployeeSaveData>();
    }

    public class TileSaveData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Type { get; set; }
        public int Level { get; set; }
        public double Inventory { get; set; }
        public double MaxInventory { get; set; }
        public double MaintenanceCost { get; set; }
        public decimal LandValue { get; set; }
        public double TrafficIndex { get; set; }
        public double RetailPrice { get; set; }
        public double DepreciatedValue { get; set; }
    }

    public class EmployeeSaveData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Role { get; set; }
        public double HourlyWage { get; set; }
        public double Morale { get; set; }
        public double SkillLevel { get; set; }
        public int AssignedX { get; set; }
        public int AssignedY { get; set; }
    }

    public class GameEngine
    {
        public const int MapSize = 120;
        public Tile[,] Grid { get; }
        public List<Employee> Employees { get; }
        public CompanyStats Stats { get; }
        public List<ResearchNode> TechTree { get; }
        public ResearchNode? ActiveResearch { get; set; }
        
        // Time parameters
        public DateTime CurrentDate { get; private set; }
        public double TrainingBudgetPerHourPerEmployee { get; set; } // training investment, e.g. $0 to $50
        private double hourAccumulator = 0.0;
        private int previousMonth = 6; // starts at June

        // Macroeconomic properties
        public double GDP_Index { get; private set; }
        public double Inflation_Rate { get; private set; }
        public double Unemployment_Rate { get; private set; }
        public double Interest_Rate { get; private set; }
        public double ConsumerConfidenceIndex { get; private set; }
        public CyclePhase CyclePhase { get; private set; }
        private double previousGdp = 100.0;

        // Market Economics
        public double CurrentMarketPrice { get; private set; } // Price per retail unit sold
        public List<Competitor> Competitors { get; }
        public double PlayerMarketShare { get; private set; }

        // Road Highway Entrance Coordinate
        public int EntranceX => 0;
        public int EntranceY => MapSize / 2;

        // Step 3 Hostile Takeover states
        public bool IsGameOver { get; private set; } = false;
        public string GameOverReason { get; private set; } = "";

        // Step 4: Supply Chain Freight Contracts
        // Maps Retail coordinates (rx, ry) to Factory coordinates (fx, fy)
        public Dictionary<Tuple<int, int>, Tuple<int, int>> FreightContracts { get; } = new Dictionary<Tuple<int, int>, Tuple<int, int>>();

        // Pre-calculated distance gradient matrix from center (60,60)
        private readonly double[,] distanceGradientMatrix = new double[MapSize, MapSize];

        public GameEngine()
        {
            Grid = new Tile[MapSize, MapSize];
            
            // Pre-calculate distance gradient matrix from center (MapSize/2, MapSize/2)
            double halfSize = MapSize / 2.0;
            double maxDist = Math.Sqrt(halfSize * halfSize + halfSize * halfSize);
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    double dx = x - halfSize;
                    double dy = y - halfSize;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    distanceGradientMatrix[x, y] = Math.Clamp(1.0 - (dist / maxDist), 0.0, 1.0);
                }
            }

            // Initialize Grid using distanceGradientMatrix
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Grid[x, y] = new Tile(x, y);
                    double pct = distanceGradientMatrix[x, y];
                    Grid[x, y].LandValue = (decimal)Math.Round(pct * 100.0, 2);
                    Grid[x, y].TrafficIndex = Math.Round(pct * 60.0, 2); // Baseline foot traffic
                }
            }

            // Create highway entrance road automatically
            Grid[EntranceX, EntranceY].SetupBuilding(TileType.Road);

            Employees = new List<Employee>();
            Stats = new CompanyStats();
            TechTree = new List<ResearchNode>();
            
            // Set starting date: June 6th, 2026, 8:00 AM
            CurrentDate = new DateTime(2026, 6, 6, 8, 0, 0);
            TrainingBudgetPerHourPerEmployee = 0;

            // Macro economics init
            GDP_Index = 100.0;
            Inflation_Rate = 0.03;
            Unemployment_Rate = 0.06;
            Interest_Rate = 0.05;
            ConsumerConfidenceIndex = 1.0;
            CyclePhase = CyclePhase.Recovery;
            Stats.InterestRate = Interest_Rate;

            CurrentMarketPrice = 45.00; // Base market price
            PlayerMarketShare = 0.05; // Starts with 5% market share

            Competitors = new List<Competitor>
            {
                new Competitor("Apex Corp", 0.45, 46.50),
                new Competitor("Globex Tycoon", 0.30, 44.00),
                new Competitor("Omni Industries", 0.20, 48.00)
            };

            InitializeTechTree();
            InitializeStartingStaff();
            
            // Initial network calculations
            UpdateRoadAccess();

            // Setup initial stock valuations and cached values
            double initialAssetVal = 0;
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    initialAssetVal += Grid[x, y].GetAssetValue();
                }
            }
            Stats.UpdateCachedValues(initialAssetVal, 0.0);
            Stats.UpdatePlayerStockPrice(initialAssetVal);
            Stats.UpdateAiStockPrice();
        }

        private void InitializeTechTree()
        {
            TechTree.Add(new ResearchNode(
                "automation_1", "Automation Basics", 
                "Streamlines manufacturing processes. Factory production rates increased by 15%.", 
                100, null, ResearchEffect.FactorySpeed, 1.15));

            TechTree.Add(new ResearchNode(
                "logistics_1", "Logistics Routing", 
                "Reduces waste and shipping costs. Logistics transport fees reduced by 25%.", 
                150, null, ResearchEffect.LogisticsSavings, 0.75));

            TechTree.Add(new ResearchNode(
                "solar_power", "Solar Grid Integration", 
                "Augments cooling towers. Increases Power Plant output by 20%.", 
                200, null, ResearchEffect.SolarGrid, 1.20));

            TechTree.Add(new ResearchNode(
                "marketing_1", "Targeted Advertising", 
                "Increases brand presence. Retail sales rates increased by 15%.", 
                250, "logistics_1", ResearchEffect.RetailDemand, 1.15));

            TechTree.Add(new ResearchNode(
                "office_efficiency", "Corporate Restructuring", 
                "Optimizes workplace output. Office consulting yields increased by 20%.", 
                300, "automation_1", ResearchEffect.OfficeYield, 1.20));
        }

        private void InitializeStartingStaff()
        {
            // Recruit a starter set of employees
            for (int i = 0; i < 5; i++) Employees.Add(new Employee(EmployeeRole.Worker));
            Employees.Add(new Employee(EmployeeRole.Manager));
            Employees.Add(new Employee(EmployeeRole.Scientist));
        }

        public double GetActiveEffectMultiplier(ResearchEffect effect)
        {
            double multiplier = 1.0;
            foreach (var node in TechTree)
            {
                if (node.IsCompleted && node.Effect == effect)
                {
                    if (effect == ResearchEffect.LogisticsSavings)
                    {
                        // Multiplicative savings (lower is better)
                        multiplier *= node.EffectMultiplier;
                    }
                    else
                    {
                        // Additive performance (higher is better)
                        multiplier += (node.EffectMultiplier - 1.0);
                    }
                }
            }
            return multiplier;
        }

        public bool BuildStructure(int x, int y, TileType type)
        {
            if (x < 0 || x >= MapSize || y < 0 || y >= MapSize) return false;
            if (Grid[x, y].Type != TileType.Grass) return false; // Must be empty

            // Highway cannot be demolished or built over
            if (x == EntranceX && y == EntranceY) return false;

            double cost = type switch
            {
                TileType.Road => 1000.0,
                TileType.Office => 30000.0,
                TileType.Factory => 60000.0,
                TileType.Retail => 40000.0,
                TileType.PowerPlant => 50000.0,
                TileType.Apartment => 50000.0,
                TileType.University => 80000.0, // Academic facility cost
                _ => 0
            };

            if (Stats.Cash < cost) return false; // Insufficient funds

            Stats.Cash -= cost;
            Grid[x, y].SetupBuilding(type);
            
            UpdateRoadAccess();
            if (type == TileType.Road)
            {
                UpdateLocalTraffic(x, y);
            }
            UpdatePowerGrid();
            return true;
        }

        public bool DemolishStructure(int x, int y)
        {
            if (x < 0 || x >= MapSize || y < 0 || y >= MapSize) return false;
            
            // Cannot bulldoze grass or the highway entrance
            if (Grid[x, y].Type == TileType.Grass) return false;
            if (x == EntranceX && y == EntranceY) return false;

            double demolishFee = 1000.0;
            if (Stats.Cash < demolishFee) return false;

            Stats.Cash -= demolishFee;

            // Evict any workers assigned here
            var assignedStaff = Employees.Where(e => e.AssignedX == x && e.AssignedY == y).ToList();
            foreach (var employee in assignedStaff)
            {
                employee.AssignedX = -1;
                employee.AssignedY = -1;
            }

            Grid[x, y].ResetToGrass();

            // Evict contracts linked to this tile if demolished
            var contractsToRemove = FreightContracts.Where(k => 
                (k.Key.Item1 == x && k.Key.Item2 == y) || 
                (k.Value.Item1 == x && k.Value.Item2 == y)).Select(k => k.Key).ToList();
            foreach (var key in contractsToRemove)
            {
                FreightContracts.Remove(key);
            }

            UpdateRoadAccess();
            UpdateLocalTraffic(x, y);
            UpdatePowerGrid();
            return true;
        }

        public bool UpgradeStructure(int x, int y)
        {
            if (x < 0 || x >= MapSize || y < 0 || y >= MapSize) return false;
            Tile tile = Grid[x, y];
            
            if (tile.Type == TileType.Grass || tile.Type == TileType.Road) return false;
            if (tile.Level >= 3) return false; // Maximum level 3

            double upgradeCost = tile.Type switch
            {
                TileType.Office => 24000.0,
                TileType.Factory => 48000.0,
                TileType.Retail => 32000.0,
                TileType.PowerPlant => 40000.0,
                TileType.Apartment => 40000.0,
                TileType.University => 60000.0, // High-level upgrade cost
                _ => 0
            };

            if (Stats.Cash < upgradeCost) return false;

            Stats.Cash -= upgradeCost;
            tile.Upgrade();

            UpdatePowerGrid();
            return true;
        }

        public Employee HireEmployee(EmployeeRole role)
        {
            double hireBonusCost = role switch
            {
                EmployeeRole.Worker => 500.0,
                EmployeeRole.Manager => 1500.0,
                EmployeeRole.Scientist => 2000.0,
                _ => 250.0
            };

            if (Stats.Cash < hireBonusCost) return null;

            Stats.Cash -= hireBonusCost;
            Employee employee = new Employee(role);
            Employees.Add(employee);
            return employee;
        }

        public void FireEmployee(string employeeId)
        {
            Employee employee = Employees.FirstOrDefault(e => e.Id == employeeId);
            if (employee == null) return;

            // Remove assignments
            if (employee.AssignedX != -1)
            {
                Grid[employee.AssignedX, employee.AssignedY].EmployeeCount--;
            }

            Employees.Remove(employee);
        }

        public bool AssignEmployee(string employeeId, int x, int y)
        {
            if (x < 0 || x >= MapSize || y < 0 || y >= MapSize) return false;
            Tile tile = Grid[x, y];

            if (tile.Type == TileType.Grass || tile.Type == TileType.Road) return false;
            if (tile.EmployeeCount >= tile.MaxEmployees) return false; // Full

            Employee employee = Employees.FirstOrDefault(e => e.Id == employeeId);
            if (employee == null) return false;

            // Unassign from old post
            if (employee.AssignedX != -1)
            {
                Grid[employee.AssignedX, employee.AssignedY].EmployeeCount--;
            }

            employee.AssignedX = x;
            employee.AssignedY = y;
            tile.EmployeeCount++;
            return true;
        }

        public void UnassignEmployee(string employeeId)
        {
            Employee employee = Employees.FirstOrDefault(e => e.Id == employeeId);
            if (employee == null || employee.AssignedX == -1) return;

            Grid[employee.AssignedX, employee.AssignedY].EmployeeCount--;
            employee.AssignedX = -1;
            employee.AssignedY = -1;
        }

        public void UpdateRoadAccess()
        {
            // Store previous road states to optimize traffic updates
            bool[,] oldRoadAccess = new bool[MapSize, MapSize];
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    oldRoadAccess[x, y] = Grid[x, y].HasRoadAccess;
                    Grid[x, y].HasRoadAccess = false;
                }
            }

            // Perform Breadth-First Search (BFS) for road connectivity starting from entrance
            bool[,] visited = new bool[MapSize, MapSize];
            Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();

            if (Grid[EntranceX, EntranceY].Type == TileType.Road)
            {
                queue.Enqueue(new Tuple<int, int>(EntranceX, EntranceY));
                visited[EntranceX, EntranceY] = true;
                Grid[EntranceX, EntranceY].HasRoadAccess = true;
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int cx = current.Item1;
                int cy = current.Item2;

                for (int i = 0; i < 4; i++)
                {
                    int nx = cx + dx[i];
                    int ny = cy + dy[i];

                    if (nx >= 0 && nx < MapSize && ny >= 0 && ny < MapSize)
                    {
                        if (!visited[nx, ny] && Grid[nx, ny].Type == TileType.Road)
                        {
                            visited[nx, ny] = true;
                            Grid[nx, ny].HasRoadAccess = true;
                            queue.Enqueue(new Tuple<int, int>(nx, ny));
                        }
                    }
                }
            }

            // Mark buildings that are adjacent to an active road
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Tile tile = Grid[x, y];
                    if (tile.Type == TileType.Grass || tile.Type == TileType.Road) continue;

                    // Check neighbors
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx[i];
                        int ny = y + dy[i];

                        if (nx >= 0 && nx < MapSize && ny >= 0 && ny < MapSize)
                        {
                            if (Grid[nx, ny].Type == TileType.Road && Grid[nx, ny].HasRoadAccess)
                            {
                                tile.HasRoadAccess = true;
                                break;
                            }
                        }
                    }
                }
            }

            // Performance Optimization: Only update traffic in sectors where road access changed
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    if (Grid[x, y].Type == TileType.Road && Grid[x, y].HasRoadAccess != oldRoadAccess[x, y])
                    {
                        UpdateLocalTraffic(x, y);
                    }
                }
            }
        }

        public void UpdateLocalTraffic(int cx, int cy)
        {
            // Localized Update Pattern: Recalculate only a 5x5 area around the altered tile.
            // This prevents expensive O(N^2) full-grid updates during construction.
            for (int x = Math.Max(0, cx - 2); x <= Math.Min(MapSize - 1, cx + 2); x++)
            {
                for (int y = Math.Max(0, cy - 2); y <= Math.Min(MapSize - 1, cy + 2); y++)
                {
                    RecalculateSingleTileTraffic(x, y);
                }
            }
        }

        private void RecalculateSingleTileTraffic(int x, int y)
        {
            // 1. Get baseline distance value from the pre-calculated gradient matrix
            double pct = distanceGradientMatrix[x, y];
            double baselineTraffic = pct * 60.0; // max center baseline is 60

            // 2. Scan adjacent tiles for active roads (roads with highway connection)
            bool adjacentToActiveRoad = false;
            int[] dxNeighbors = { 1, -1, 0, 0 };
            int[] dyNeighbors = { 0, 0, 1, -1 };
            
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dxNeighbors[i];
                int ny = y + dyNeighbors[i];
                if (nx >= 0 && nx < MapSize && ny >= 0 && ny < MapSize)
                {
                    if (Grid[nx, ny].Type == TileType.Road && Grid[nx, ny].HasRoadAccess)
                    {
                        adjacentToActiveRoad = true;
                        break;
                    }
                }
            }

            // 3. Apply road traffic index boost (+40 index)
            double traffic = baselineTraffic + (adjacentToActiveRoad ? 40.0 : 0.0);
            Grid[x, y].TrafficIndex = Math.Clamp(Math.Round(traffic, 2), 0.0, 100.0);
        }

        public void UpdatePowerGrid()
        {
            double totalPowerGenerated = 0;
            List<Tile> consumers = new List<Tile>();

            // 1. Road-Driven Power Grid (Step 4)
            // Electricity can only propagate across connected Road tiles.
            bool[,] roadHasPower = new bool[MapSize, MapSize];
            Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();

            // Power plants inject power into adjacent roads
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Tile tile = Grid[x, y];
                    if (tile.Type == TileType.PowerPlant && tile.Level > 0)
                    {
                        tile.IsPowered = true; // Power plants are self-powered
                        double powerOutput = tile.GetPowerProduction() * GetActiveEffectMultiplier(ResearchEffect.SolarGrid);
                        totalPowerGenerated += powerOutput;

                        int[] dx = { 1, -1, 0, 0 };
                        int[] dy = { 0, 0, 1, -1 };
                        for (int i = 0; i < 4; i++)
                        {
                            int nx = x + dx[i];
                            int ny = y + dy[i];
                            if (nx >= 0 && nx < MapSize && ny >= 0 && ny < MapSize)
                            {
                                if (Grid[nx, ny].Type == TileType.Road && !roadHasPower[nx, ny])
                                {
                                    roadHasPower[nx, ny] = true;
                                    queue.Enqueue(new Tuple<int, int>(nx, ny));
                                }
                            }
                        }
                    }
                }
            }

            // BFS power flow along connected roads
            int[] rDx = { 1, -1, 0, 0 };
            int[] rDy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int cx = current.Item1;
                int cy = current.Item2;

                for (int i = 0; i < 4; i++)
                {
                    int nx = cx + rDx[i];
                    int ny = cy + rDy[i];

                    if (nx >= 0 && nx < MapSize && ny >= 0 && ny < MapSize)
                    {
                        if (Grid[nx, ny].Type == TileType.Road && !roadHasPower[nx, ny])
                        {
                            roadHasPower[nx, ny] = true;
                            queue.Enqueue(new Tuple<int, int>(nx, ny));
                        }
                    }
                }
            }

            // 2. Identify consumers adjacent to electrified roads
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Tile tile = Grid[x, y];
                    if (tile.Type == TileType.Grass || tile.Type == TileType.Road || tile.Type == TileType.PowerPlant)
                    {
                        continue;
                    }

                    // Check neighbors for electrified road
                    bool adjacentToElectrifiedRoad = false;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + rDx[i];
                        int ny = y + rDy[i];
                        if (nx >= 0 && nx < MapSize && ny >= 0 && ny < MapSize)
                        {
                            if (roadHasPower[nx, ny])
                            {
                                adjacentToElectrifiedRoad = true;
                                break;
                            }
                        }
                    }

                    if (adjacentToElectrifiedRoad)
                    {
                        consumers.Add(tile);
                    }
                    else
                    {
                        tile.IsPowered = false; // Disconnected from power plant
                    }
                }
            }

            // Power prioritization: Factory > Retail > Office > Apartment > University > Roads
            consumers = consumers
                .OrderBy(c => c.Type switch
                {
                    TileType.Factory => 1,
                    TileType.Retail => 2,
                    TileType.Office => 3,
                    TileType.Apartment => 4,
                    TileType.University => 5,
                    _ => 6
                })
                .ToList();

            double remainingPower = totalPowerGenerated;

            foreach (var tile in consumers)
            {
                double consumption = tile.GetPowerConsumption();
                if (remainingPower >= consumption)
                {
                    tile.IsPowered = true;
                    remainingPower -= consumption;
                }
                else
                {
                    tile.IsPowered = false;
                }
            }
        }

        public void Update(double elapsedRealSeconds, double speedMultiplier)
        {
            if (IsGameOver) return; // Freeze simulation if takeover is complete
            if (speedMultiplier <= 0) return; // Paused

            double hoursToAdvance = elapsedRealSeconds * speedMultiplier;
            CurrentDate = CurrentDate.AddHours(hoursToAdvance);

            hourAccumulator += hoursToAdvance;
            while (hourAccumulator >= 1.0)
            {
                if (IsGameOver) break;
                ExecuteHourlyTick();
                hourAccumulator -= 1.0;
            }
        }

        private void ExecuteHourlyTick()
        {
            UpdateRoadAccess();
            UpdatePowerGrid();

            // Month shift triggers macro adjustments and AI stock pass
            if (CurrentDate.Month != previousMonth)
            {
                previousMonth = CurrentDate.Month;
                UpdateMacroEconomy();
                ExecuteMonthlyStockMarketPass();
            }

            // Perform Wage updates and training costs
            double totalWages = 0;
            double totalTrainingCost = 0;
            double wageScaleFactor = 1.0 + (0.06 - Unemployment_Rate) * 1.5;

            foreach (var employee in Employees)
            {
                double marketAverageWage = employee.Role switch
                {
                    EmployeeRole.Worker => 17.50 * wageScaleFactor,
                    EmployeeRole.Manager => 30.00 * wageScaleFactor,
                    EmployeeRole.Scientist => 36.00 * wageScaleFactor,
                    _ => 16.00 * wageScaleFactor
                };

                employee.Update(marketAverageWage, TrainingBudgetPerHourPerEmployee);

                // High Scientist morale sensitivity to global Unemployment_Rate
                if (employee.Role == EmployeeRole.Scientist)
                {
                    double unemploymentPenalty = (Unemployment_Rate - 0.06) * 5.0; // scales above baseline 6%
                    if (unemploymentPenalty > 0)
                    {
                        employee.Morale = Math.Clamp(employee.Morale - unemploymentPenalty * 0.05, 0.05, 1.0);
                    }
                }

                if (employee.AssignedX != -1)
                {
                    totalWages += employee.HourlyWage;
                    totalTrainingCost += TrainingBudgetPerHourPerEmployee;
                }
            }

            Stats.CurrentHourWages = totalWages;
            Stats.Cash -= totalTrainingCost;

            // Process Production, Sales, Rent, and Logistics
            UpdateProductionAndContracts();
            UpdateLogisticsTransfers();
            UpdateRetailSales();

            // Science research point updates
            UpdateResearchPoints();

            // Finish hourly ledger payments
            Stats.ProcessHourlyBilling();

            // Step 7: Apply hourly building depreciation
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Tile tile = Grid[x, y];
                    if (tile.Type != TileType.Grass && tile.Type != TileType.Road)
                    {
                        double origAssetValue = tile.GetAssetValue();
                        tile.DepreciatedValue = Math.Max(origAssetValue * 0.40, tile.DepreciatedValue - origAssetValue * 0.0001);
                    }
                }
            }

            // Step 7: Perform O(N) grid scan to calculate cached property assets & inventories once per hour
            double totalPropertyAssets = 0.0;
            double totalInventoryValue = 0.0;
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Tile tile = Grid[x, y];
                    totalPropertyAssets += tile.DepreciatedValue;

                    if (tile.Type == TileType.Factory)
                    {
                        totalInventoryValue += tile.Inventory * 10.0; // Wholesale cost basis
                    }
                    else if (tile.Type == TileType.Retail)
                    {
                        totalInventoryValue += tile.Inventory * 20.0; // Shelf stock cost basis
                    }
                }
            }

            // Update cached statistics
            Stats.UpdateCachedValues(totalPropertyAssets, totalInventoryValue);

            // Check Hostile Takeover Win/Loss Condition
            double playerAiOwnedPct = Stats.PlayerSharesOwnedByAi / Stats.PlayerTotalShares;
            double aiPlayerOwnedPct = Stats.AiSharesOwnedByPlayer / Stats.AiTotalShares;

            if (playerAiOwnedPct > 0.50)
            {
                IsGameOver = true;
                GameOverReason = "HOSTILE TAKEOVER LOSS: AI competitors have acquired over 50% of your shares. You have lost control of your corporation!";
            }
            else if (aiPlayerOwnedPct > 0.50)
            {
                IsGameOver = true;
                GameOverReason = "HOSTILE TAKEOVER WIN: You have acquired over 50% of Apex Corp's shares and successfully took control of your competitor! You win!";
            }

            // Daily updates (at midnight)
            if (CurrentDate.Hour == 0)
            {
                UpdateCompetitors();
                Stats.CycleDay(CurrentDate);
            }
        }

        public void UpdateMacroEconomy()
        {
            Random random = new Random();
            int monthIndex = (CurrentDate.Year - 2026) * 12 + (CurrentDate.Month - 6);

            // GDP Sinusoid cycle spanning approx. 72 months (6 years)
            double gdpBase = 100.0 + Math.Sin(monthIndex * (2.0 * Math.PI / 72.0)) * 15.0;
            GDP_Index = gdpBase + random.NextDouble() * 3.0 - 1.5;
            GDP_Index = Math.Clamp(GDP_Index, 85.0, 120.0);

            // Compute growth rate
            double gdpGrowth = GDP_Index - previousGdp;
            previousGdp = GDP_Index;

            // Classify Cycle Phase
            if (gdpGrowth >= 0)
            {
                CyclePhase = GDP_Index >= 105.0 ? CyclePhase.Boom : CyclePhase.Recovery;
            }
            else
            {
                CyclePhase = GDP_Index >= 95.0 ? CyclePhase.Slowdown : CyclePhase.Recession;
            }

            // Inflation Rate based on GDP
            Inflation_Rate = 0.03 + (GDP_Index - 100.0) * 0.005;
            Inflation_Rate = Math.Clamp(Inflation_Rate, -0.02, 0.18);

            // Unemployment Rate
            Unemployment_Rate = 0.07 - (GDP_Index - 100.0) * 0.005;
            Unemployment_Rate = Math.Clamp(Unemployment_Rate, 0.025, 0.22);

            // Bank Central Interest Rate (APR) reacts to inflation and GDP
            Interest_Rate = 0.05 + (Inflation_Rate * 0.4) + (GDP_Index - 100.0) * 0.002;
            Interest_Rate = Math.Clamp(Interest_Rate, 0.01, 0.15);

            // Override borrowing rates
            Stats.InterestRate = Interest_Rate;

            // Consumer Confidence Index (CCI)
            ConsumerConfidenceIndex = 1.0 + (GDP_Index - 100.0) * 0.02 - (Unemployment_Rate - 0.06) * 2.0;
            ConsumerConfidenceIndex = Math.Clamp(ConsumerConfidenceIndex, 0.4, 1.5);
        }

        private void UpdateProductionAndContracts()
        {
            double factorySpeedMultiplier = GetActiveEffectMultiplier(ResearchEffect.FactorySpeed);
            double officeYieldMultiplier = GetActiveEffectMultiplier(ResearchEffect.OfficeYield);

            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Tile tile = Grid[x, y];
                    if (tile.Level == 0) continue;

                    // 1. Add base maintenance upkeep
                    Stats.CurrentHourBaseMaintenance += tile.MaintenanceCost;

                    // 2. Add dynamic Land Tax based on location value and building tier (using decimal)
                    Stats.CurrentHourLandTaxes += (double)tile.GetLandTax();

                    if (tile.Type == TileType.Office)
                    {
                        var assignedStaff = Employees.Where(e => e.AssignedX == x && e.AssignedY == y).ToList();
                        if (assignedStaff.Count > 0)
                        {
                            double performanceSum = assignedStaff.Sum(e => e.GetPerformanceMultiplier());
                            double powerFactor = tile.IsPowered ? 1.0 : 0.1;
                            double roadFactor = tile.HasRoadAccess ? 1.0 : 0.4;
                            
                            double earnings = performanceSum * tile.ProductionRate * tile.Level * powerFactor * roadFactor * officeYieldMultiplier;
                            
                            Stats.CurrentHourOfficeRevenue += earnings;
                            tile.HistoricalEarnings += earnings;
                            tile.LastDayEarnings += earnings;
                        }
                    }
                    else if (tile.Type == TileType.Factory)
                    {
                        var assignedStaff = Employees.Where(e => e.AssignedX == x && e.AssignedY == y).ToList();
                        if (assignedStaff.Count > 0)
                        {
                            double performanceSum = assignedStaff.Sum(e => e.GetPerformanceMultiplier());
                            double powerFactor = tile.IsPowered ? 1.0 : 0.05;
                            double roadFactor = tile.HasRoadAccess ? 1.0 : 0.1;

                            double output = performanceSum * tile.ProductionRate * tile.Level * powerFactor * roadFactor * factorySpeedMultiplier;
                            
                            double maxProduce = tile.MaxInventory - tile.Inventory;
                            double actualProduction = Math.Clamp(output, 0, maxProduce);

                            if (actualProduction > 0)
                            {
                                // Factory buys raw materials wholesale ($6.00 per unit)
                                double rawMaterialExpense = actualProduction * 6.00;
                                Stats.CurrentHourBaseMaintenance += rawMaterialExpense;
                                
                                tile.Inventory += actualProduction;
                            }
                        }
                    }
                    else if (tile.Type == TileType.Apartment)
                    {
                        // Apartment Residential Rent Simulation
                        if (!tile.IsPowered)
                        {
                            tile.Inventory = 0; // Deficits in electricity drop occupancy to 0%
                        }
                        else
                        {
                            double occupancyRate = 0.90 + (GDP_Index - 100.0) * 0.005 - (Unemployment_Rate - 0.06) * 1.5;
                            occupancyRate = Math.Clamp(occupancyRate, 0.40, 1.0);

                            if (!tile.HasRoadAccess)
                            {
                                occupancyRate *= 0.5; // Lack of road access drops desirability by 50%
                            }

                            tile.Inventory = Math.Round(tile.MaxInventory * occupancyRate); // Stores tenant count
                        }

                        // Collect rent per tenant using decimal for precise finance
                        decimal tenantsDec = (decimal)tile.Inventory;
                        decimal baseRentRateDec = 2.50m;
                        decimal gdpFactorDec = (decimal)(GDP_Index / 100.0);
                        decimal landValueFactorDec = 1.0m + tile.LandValue / 100.0m;
                        decimal rentRateDec = baseRentRateDec * gdpFactorDec * landValueFactorDec;
                        decimal collectedRentDec = tenantsDec * rentRateDec;
                        double collectedRent = (double)collectedRentDec;

                        Stats.CurrentHourApartmentRevenue += collectedRent;
                        tile.HistoricalEarnings += collectedRent;
                        tile.LastDayEarnings += collectedRent;

                        // Structural depreciation / maintenance hikes in recessions
                        decimal baseMaintenanceDec = (decimal)tile.MaintenanceCost;
                        decimal cciDec = (decimal)ConsumerConfidenceIndex;
                        decimal recessionScaleDec = 1.0m;
                        if (cciDec < 1.0m)
                        {
                            recessionScaleDec = 1.0m + (1.0m - cciDec) * 3.33m;
                        }
                        decimal actualMaintenanceDec = baseMaintenanceDec * recessionScaleDec;
                        
                        Stats.CurrentHourBaseMaintenance += (double)(actualMaintenanceDec - baseMaintenanceDec);
                    }
                    else if (tile.Type == TileType.University)
                    {
                        // University training progress for Scientists
                        if (tile.IsPowered)
                        {
                            // Brain Drain penalty: cut training progress by 40% if global unemployment is high (>= 8%)
                            double brainDrainMultiplier = Unemployment_Rate >= 0.08 ? 0.60 : 1.0;
                            double progressGain = tile.ProductionRate * tile.Level * brainDrainMultiplier;
                            tile.Inventory = Math.Min(tile.MaxInventory, tile.Inventory + progressGain);

                            if (tile.Inventory >= tile.MaxInventory)
                            {
                                // Spawn a new Scientist!
                                Employees.Add(new Employee(EmployeeRole.Scientist));
                                tile.Inventory = 0.0; // Reset progress
                            }
                        }
                    }
                }
            }
        }

        private void UpdateLogisticsTransfers()
        {
            List<Tile> factories = new List<Tile>();
            List<Tile> retailOutlets = new List<Tile>();

            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    if (Grid[x, y].Type == TileType.Factory && Grid[x, y].Inventory > 0)
                    {
                        factories.Add(Grid[x, y]);
                    }
                    else if (Grid[x, y].Type == TileType.Retail && Grid[x, y].Inventory < Grid[x, y].MaxInventory)
                    {
                        retailOutlets.Add(Grid[x, y]);
                    }
                }
            }

            // Distribute goods
            double logisticsSavingsMultiplier = GetActiveEffectMultiplier(ResearchEffect.LogisticsSavings);

            foreach (var retail in retailOutlets)
            {
                double spaceNeeded = retail.MaxInventory - retail.Inventory;
                if (spaceNeeded <= 0) continue;

                var retailKey = new Tuple<int, int>(retail.X, retail.Y);

                // If a freight contract is active for this retail outlet, only source from the linked factory
                if (FreightContracts.TryGetValue(retailKey, out var factoryCoords))
                {
                    Tile factory = Grid[factoryCoords.Item1, factoryCoords.Item2];
                    if (factory.Type == TileType.Factory && factory.Inventory > 0)
                    {
                        double qtyToTransfer = Math.Min(factory.Inventory, spaceNeeded);
                        factory.Inventory -= qtyToTransfer;
                        retail.Inventory += qtyToTransfer;

                        // Distance-based Manhattan freight cost (Distance * Volume * 0.15)
                        int distance = Math.Abs(factory.X - retail.X) + Math.Abs(factory.Y - retail.Y);
                        decimal distDec = (decimal)distance;
                        decimal qtyDec = (decimal)qtyToTransfer;
                        decimal costMultiplierDec = 0.15m;
                        decimal savingsMultiplierDec = (decimal)logisticsSavingsMultiplier;
                        decimal freightCostDec = distDec * qtyDec * costMultiplierDec * savingsMultiplierDec;

                        Stats.CurrentHourFreightCost += (double)freightCostDec;
                    }
                }
                else
                {
                    // Fallback to default greedy logistics
                    foreach (var factory in factories)
                    {
                        if (factory.Inventory <= 0) continue;

                        double qtyToTransfer = Math.Min(factory.Inventory, spaceNeeded);
                        factory.Inventory -= qtyToTransfer;
                        retail.Inventory += qtyToTransfer;
                        spaceNeeded -= qtyToTransfer;

                        // Distance-based Manhattan freight cost
                        int distance = Math.Abs(factory.X - retail.X) + Math.Abs(factory.Y - retail.Y);
                        decimal distDec = (decimal)distance;
                        decimal qtyDec = (decimal)qtyToTransfer;
                        decimal costMultiplierDec = 0.15m;
                        decimal savingsMultiplierDec = (decimal)logisticsSavingsMultiplier;
                        decimal freightCostDec = distDec * qtyDec * costMultiplierDec * savingsMultiplierDec;

                        Stats.CurrentHourFreightCost += (double)freightCostDec;

                        if (spaceNeeded <= 0) break;
                    }
                }
            }
        }

        private void UpdateRetailSales()
        {
            double retailDemandMultiplier = GetActiveEffectMultiplier(ResearchEffect.RetailDemand);

            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Tile tile = Grid[x, y];
                    if (tile.Type == TileType.Retail && tile.Inventory > 0)
                    {
                        var assignedStaff = Employees.Where(e => e.AssignedX == x && e.AssignedY == y).ToList();
                        if (assignedStaff.Count > 0)
                        {
                            double performanceSum = assignedStaff.Sum(e => e.GetPerformanceMultiplier());
                            double powerFactor = tile.IsPowered ? 1.0 : 0.05;
                            double roadFactor = tile.HasRoadAccess ? 1.0 : 0.05;

                            // Scale demand by Consumer Confidence (CCI)
                            double marketDemand = (1.0 + (1.0 - PlayerMarketShare) * 0.3) * ConsumerConfidenceIndex;
                            
                            // Scale throughput by local foot traffic (TrafficIndex / 100.0)
                            double trafficFactor = tile.TrafficIndex / 100.0;

                            // Pricing Sensitivity (Step 4)
                            // High pricing during low Consumer Confidence (CCI) severely throttles sales.
                            double priceRatio = tile.RetailPrice / Math.Max(0.01, CurrentMarketPrice);
                            double priceFactor = 1.0;
                            if (priceRatio > 1.0)
                            {
                                // Penalty amplified under low CCI
                                double cciMultiplier = 1.0 + (1.5 - ConsumerConfidenceIndex) * 3.0; // scales up to 4.3x at CCI=0.4
                                priceFactor = Math.Pow(1.0 / priceRatio, cciMultiplier);
                            }
                            else
                            {
                                // Small sales boost for lower prices
                                priceFactor = Math.Min(1.3, Math.Sqrt(1.0 / priceRatio));
                            }

                            double maxSalesPossible = performanceSum * tile.SalesRate * tile.Level * powerFactor * roadFactor * retailDemandMultiplier * marketDemand * trafficFactor * priceFactor;
                            double actualSales = Math.Min(tile.Inventory, maxSalesPossible);

                            if (actualSales > 0)
                            {
                                tile.Inventory -= actualSales;
                                double salesRevenue = actualSales * tile.RetailPrice;

                                Stats.CurrentHourRetailRevenue += salesRevenue;
                                tile.HistoricalEarnings += salesRevenue;
                                tile.LastDayEarnings += salesRevenue;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateResearchPoints()
        {
            if (ActiveResearch == null || ActiveResearch.IsCompleted) return;

            double generatedResearchPoints = 0;
            // Brain Drain check: cut scientist productivity by 40% (0.60 multiplier) if unemployment is high
            double brainDrainMultiplier = Unemployment_Rate >= 0.08 ? 0.60 : 1.0;

            foreach (var employee in Employees)
            {
                if (employee.AssignedX != -1)
                {
                    Tile tile = Grid[employee.AssignedX, employee.AssignedY];
                    if (tile.Type == TileType.Office)
                    {
                        double powerFactor = tile.IsPowered ? 1.0 : 0.2;
                        if (employee.Role == EmployeeRole.Scientist)
                        {
                            // Scientists generate at 3x base speed, but are affected by Brain Drain
                            generatedResearchPoints += 3.0 * employee.GetPerformanceMultiplier() * powerFactor * brainDrainMultiplier;
                        }
                        else
                        {
                            // Normal workers generate at 1x base speed
                            generatedResearchPoints += 1.0 * employee.GetPerformanceMultiplier() * powerFactor;
                        }
                    }
                }
            }

            if (generatedResearchPoints > 0)
            {
                ActiveResearch.InvestPoints(generatedResearchPoints);
                if (ActiveResearch.IsCompleted)
                {
                    UpdatePowerGrid();
                    ActiveResearch = null; // Reset selection
                }
            }
        }

        private void ExecuteMonthlyStockMarketPass()
        {
            // Pull cached property asset valuation from stats directly (O(1)) instead of grid scan
            double totalAssetValue = Stats.CachedPropertyAssetValuation;

            // Update player financials and stock price
            Stats.UpdatePlayerStockPrice(totalAssetValue);

            // Simulate AI competitor trailing financials monthly
            double aiBaseNetIncome = CyclePhase switch
            {
                CyclePhase.Boom => 80000.0,
                CyclePhase.Recovery => 50000.0,
                CyclePhase.Slowdown => 20000.0,
                CyclePhase.Recession => -15000.0,
                _ => 30000.0
            };

            Stats.AiNetIncome = aiBaseNetIncome + (new Random().NextDouble() * 20000.0 - 10000.0);
            Stats.AiBookValue = Stats.AiCash + 500000.0;
            Stats.UpdateAiStockPrice();

            // Calculate actual cashflow (net profit) of the month that just ended
            Stats.PreviousMonthCashflow = Stats.GetTrailingMonthlyNetIncome();

            // Run Hostile Takeover AI logic (only if IPO has been launched!)
            if (Stats.IsIpoLaunched)
            {
                Stats.ExecuteAiTakeoverPass(Stats.Cash, Interest_Rate, (int)CyclePhase);
            }
        }

        public void UpdateCompetitors()
        {
            Random random = new Random();
            double totalMarketDemand = 1000.0;
            
            double playerDailySales = 0;
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    if (Grid[x, y].Type == TileType.Retail)
                    {
                        playerDailySales += Grid[x, y].LastDayEarnings / CurrentMarketPrice;
                        Grid[x, y].LastDayEarnings = 0;
                    }
                    else if (Grid[x, y].Type == TileType.Office || Grid[x, y].Type == TileType.Apartment || Grid[x, y].Type == TileType.University)
                    {
                        Grid[x, y].LastDayEarnings = 0;
                    }
                }
            }

            double totalProductQty = playerDailySales;
            foreach (var comp in Competitors)
            {
                double compDailyQty = totalMarketDemand * comp.MarketShare;
                totalProductQty += compDailyQty;
            }

            PlayerMarketShare = totalProductQty > 0 ? playerDailySales / totalProductQty : 0.05;

            double supplyDemandRatio = totalProductQty / totalMarketDemand;
            double targetPrice = 45.00 * (1.1 - 0.25 * supplyDemandRatio);
            CurrentMarketPrice = CurrentMarketPrice * 0.9 + targetPrice * 0.1;
            CurrentMarketPrice = Math.Clamp(CurrentMarketPrice, 25.00, 65.00);

            double remainingShare = 1.0 - PlayerMarketShare;
            double currentSumShare = Competitors.Sum(c => c.MarketShare);
            
            foreach (var comp in Competitors)
            {
                if (currentSumShare > 0)
                {
                    comp.MarketShare = (comp.MarketShare / currentSumShare) * remainingShare;
                }
                
                double targetCompPrice = CurrentMarketPrice + (random.NextDouble() - 0.5) * 5.0;
                comp.AveragePrice = comp.AveragePrice * 0.95 + targetCompPrice * 0.05;
                comp.AveragePrice = Math.Clamp(comp.AveragePrice, 20.00, 70.00);
            }
        }

        // Save & Load Game state implementation
        public void SaveToFile(string filePath)
        {
            var data = new SaveData
            {
                CompanyName = Stats.CompanyName,
                Cash = Stats.Cash,
                LoanBalance = Stats.LoanBalance,
                IsIpoLaunched = Stats.IsIpoLaunched,
                PreviousMonthCashflow = Stats.PreviousMonthCashflow,
                CurrentDate = CurrentDate.ToString("o"),
                GDP_Index = GDP_Index,
                Inflation_Rate = Inflation_Rate,
                Unemployment_Rate = Unemployment_Rate,
                Interest_Rate = Interest_Rate,
                ConsumerConfidenceIndex = ConsumerConfidenceIndex,
                CyclePhase = (int)CyclePhase,
                ActiveResearchPoints = ActiveResearch?.PointsInvested ?? 0.0,
                ActiveResearchName = ActiveResearch?.Id ?? ""
            };

            foreach (var node in TechTree)
            {
                if (node.IsCompleted)
                {
                    data.CompletedResearch.Add(node.Id);
                }
            }

            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Tile t = Grid[x, y];
                    if (t.Type != TileType.Grass)
                    {
                        data.Tiles.Add(new TileSaveData
                        {
                            X = t.X,
                            Y = t.Y,
                            Type = (int)t.Type,
                            Level = t.Level,
                            Inventory = t.Inventory,
                            MaxInventory = t.MaxInventory,
                            MaintenanceCost = t.MaintenanceCost,
                            LandValue = t.LandValue,
                            TrafficIndex = t.TrafficIndex,
                            RetailPrice = t.RetailPrice,
                            DepreciatedValue = t.DepreciatedValue
                        });
                    }
                }
            }

            foreach (var emp in Employees)
            {
                data.Employees.Add(new EmployeeSaveData
                {
                    Id = emp.Id,
                    Name = emp.Name,
                    Role = (int)emp.Role,
                    HourlyWage = emp.HourlyWage,
                    Morale = emp.Morale,
                    SkillLevel = emp.SkillLevel,
                    AssignedX = emp.AssignedX,
                    AssignedY = emp.AssignedY
                });
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<SaveData>(json);
            if (data == null) return;

            Stats.CompanyName = data.CompanyName;
            Stats.Cash = data.Cash;
            Stats.LoanBalance = data.LoanBalance;
            Stats.IsIpoLaunched = data.IsIpoLaunched;
            Stats.PreviousMonthCashflow = data.PreviousMonthCashflow;

            CurrentDate = DateTime.Parse(data.CurrentDate);
            GDP_Index = data.GDP_Index;
            Inflation_Rate = data.Inflation_Rate;
            Unemployment_Rate = data.Unemployment_Rate;
            Interest_Rate = data.Interest_Rate;
            ConsumerConfidenceIndex = data.ConsumerConfidenceIndex;
            CyclePhase = (CyclePhase)data.CyclePhase;

            // Reset tech tree nodes
            foreach (var node in TechTree)
            {
                node.IsCompleted = data.CompletedResearch.Contains(node.Id);
            }
            if (!string.IsNullOrEmpty(data.ActiveResearchName))
            {
                ActiveResearch = TechTree.FirstOrDefault(n => n.Id == data.ActiveResearchName);
                if (ActiveResearch != null)
                {
                    ActiveResearch.PointsInvested = data.ActiveResearchPoints;
                }
            }
            else
            {
                ActiveResearch = null;
            }

            // Reset grid to grass first
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    Grid[x, y].ResetToGrass();
                }
            }

            // Restore buildings
            foreach (var tData in data.Tiles)
            {
                Tile t = Grid[tData.X, tData.Y];
                t.Type = (TileType)tData.Type;
                t.Level = tData.Level;
                t.Inventory = tData.Inventory;
                t.MaxInventory = tData.MaxInventory;
                t.MaintenanceCost = tData.MaintenanceCost;
                t.LandValue = tData.LandValue;
                t.TrafficIndex = tData.TrafficIndex;
                t.RetailPrice = tData.RetailPrice;
                t.DepreciatedValue = tData.DepreciatedValue;
            }

            // Restore employees
            Employees.Clear();
            foreach (var empData in data.Employees)
            {
                Employee emp = new Employee((EmployeeRole)empData.Role)
                {
                    Id = empData.Id,
                    Name = empData.Name,
                    HourlyWage = empData.HourlyWage,
                    Morale = empData.Morale,
                    SkillLevel = empData.SkillLevel,
                    AssignedX = empData.AssignedX,
                    AssignedY = empData.AssignedY
                };
                Employees.Add(emp);
            }

            UpdateRoadAccess();
            UpdatePowerGrid();
            
            // Recompute initial cached financial properties
            double totalAssets = 0.0;
            double totalInventory = 0.0;
            for (int x = 0; x < MapSize; x++)
            {
                for (int y = 0; y < MapSize; y++)
                {
                    totalAssets += Grid[x, y].DepreciatedValue;
                    if (Grid[x, y].Type == TileType.Factory) totalInventory += Grid[x, y].Inventory * 10.0;
                    else if (Grid[x, y].Type == TileType.Retail) totalInventory += Grid[x, y].Inventory * 20.0;
                }
            }
            Stats.UpdateCachedValues(totalAssets, totalInventory);
        }
    }
}

using System;

namespace TycoonGame.Core
{
    public enum EmployeeRole
    {
        Worker,
        Manager,
        Scientist
    }

    public class Employee
    {
        private static readonly Random Random = new Random();
        
        private static readonly string[] FirstNames = { "Alice", "Bob", "Charlie", "David", "Emma", "Frank", "Grace", "Henry", "Ivy", "Jack", "Kate", "Leo", "Mia", "Nathan", "Olivia", "Peter", "Quinn", "Rachel", "Sam", "Tina", "Victor", "Wendy", "Zack" };
        private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson" };

        public string Id { get; set; }
        public string Name { get; set; }
        public EmployeeRole Role { get; }
        public double HourlyWage { get; set; }
        public double Morale { get; set; } // 0.0 to 1.0
        public double SkillLevel { get; set; } // 0.0 to 1.0
        public int AssignedX { get; set; } // -1 if unemployed
        public int AssignedY { get; set; } // -1 if unemployed

        public Employee(EmployeeRole role)
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8);
            Name = FirstNames[Random.Next(FirstNames.Length)] + " " + LastNames[Random.Next(LastNames.Length)];
            Role = role;
            HourlyWage = role switch
            {
                EmployeeRole.Worker => 16.50,
                EmployeeRole.Manager => 28.00,
                EmployeeRole.Scientist => 35.00,
                _ => 15.00
            };
            Morale = 0.8; // Starts satisfied
            SkillLevel = 0.1 + Random.NextDouble() * 0.2; // Starts low, increases with training/time
            AssignedX = -1;
            AssignedY = -1;
        }

        public void Update(double marketAverageWage, double trainingBudgetPerEmployee)
        {
            // Morale formula based on wages relative to market average
            double wageRatio = HourlyWage / Math.Max(1.0, marketAverageWage);
            
            // Adjust morale
            double moraleTarget = 0.5 + (wageRatio - 1.0) * 0.8;
            moraleTarget = Math.Clamp(moraleTarget, 0.05, 1.0);

            // Add training budget benefit to morale
            if (trainingBudgetPerEmployee > 0)
            {
                moraleTarget += Math.Min(0.15, trainingBudgetPerEmployee / 100.0);
            }

            // Lerp towards target morale
            Morale = Morale * 0.95 + moraleTarget * 0.05;
            Morale = Math.Clamp(Morale, 0.0, 1.0);

            // Skill progression based on training investment and work
            if (AssignedX != -1)
            {
                double skillGain = 0.0001; // Base gain from working
                if (trainingBudgetPerEmployee > 0)
                {
                    // Scale skill gain by training budget (up to $50/hr training = 5x skill gain speed)
                    skillGain += Math.Min(0.002, trainingBudgetPerEmployee * 0.00004);
                }
                SkillLevel = Math.Clamp(SkillLevel + skillGain, 0.0, 1.0);
            }
        }

        public double GetPerformanceMultiplier()
        {
            // Productivity is driven by Morale (50% weight) and SkillLevel (50% weight)
            double performance = (Morale * 0.5) + (SkillLevel * 0.5);
            
            // Boost manager impact or scientest impact depending on role
            if (Role == EmployeeRole.Manager)
            {
                performance *= 1.1; // Managers get a small default boost
            }
            
            return Math.Clamp(performance, 0.1, 1.5);
        }
    }
}

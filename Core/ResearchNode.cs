using System;

namespace TycoonGame.Core
{
    public enum ResearchEffect
    {
        None,
        OfficeYield,       // Boosts office hourly revenue
        FactorySpeed,      // Boosts factory production rate
        RetailDemand,      // Boosts retail sales rates
        LogisticsSavings,   // Reductions in transport logistics fees
        SolarGrid          // Reduces power maintenance cost and increases power production efficiency
    }

    public class ResearchNode
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public double ResearchPointCost { get; }
        public double PointsInvested { get; set; }
        public bool IsCompleted { get; set; }
        public string ParentNodeId { get; } // Requires this node to be completed first
        public ResearchEffect Effect { get; }
        public double EffectMultiplier { get; } // e.g. 1.15 is +15% performance

        public ResearchNode(string id, string name, string description, double cost, string parentId, ResearchEffect effect, double multiplier)
        {
            Id = id;
            Name = name;
            Description = description;
            ResearchPointCost = cost;
            PointsInvested = 0;
            IsCompleted = false;
            ParentNodeId = parentId;
            Effect = effect;
            EffectMultiplier = multiplier;
        }

        public double GetProgressPercentage()
        {
            if (IsCompleted) return 1.0;
            if (ResearchPointCost <= 0) return 0.0;
            return Math.Clamp(PointsInvested / ResearchPointCost, 0.0, 1.0);
        }

        public void InvestPoints(double points)
        {
            if (IsCompleted) return;

            PointsInvested = Math.Min(ResearchPointCost, PointsInvested + points);
            if (PointsInvested >= ResearchPointCost)
            {
                IsCompleted = true;
            }
        }
    }
}

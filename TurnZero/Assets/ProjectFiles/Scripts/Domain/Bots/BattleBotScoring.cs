using System;
using System.Collections.Generic;

namespace TurnZero
{
    public static class BattleBotScoring
    {
        private const int DamageWeight = 4;
        private const int DefeatWeight = 12;
        private const int BaseDamageWeight = 2;
        private const int BaseDestructionWeight = 40;
        private const int RootWeight = 2;
        private const int RevivalWeight = 20;
        private const int ApproachWeight = 3;
        private const int StealthWeight = 2;

        public static BattleBotScore Evaluate(
            BattleBotCandidate candidate,
            Dictionary<int, BattleBotTarget> targets,
            Dictionary<int, int> remainingHealth,
            HashSet<int> roots,
            HashSet<GridCell> explored)
        {
            BattleBotScore score = new BattleBotScore
            {
                Cost = candidate.Cost,
                Approach = candidate.ReviveHealth == 0 ? candidate.DistanceGain * ApproachWeight : 0,
                Stealth = candidate.Stealth ? StealthWeight : 0,
                Revival = candidate.ReviveHealth > 0 ? RevivalWeight + candidate.ReviveHealth * DamageWeight : 0
            };

            foreach (int id in candidate.Targets)
            {
                BattleBotTarget target = targets[id];
                int health = remainingHealth[id];
                int damage = Math.Min(health, candidate.Damage);
                score.Damage += damage * DamageWeight;

                if (target.Character && health > 0 && damage == health)
                {
                    score.Defeats += DefeatWeight;
                }

                if (target.Base)
                {
                    score.BasePressure += damage * BaseDamageWeight;
                    score.BaseDestruction += health > 0 && damage == health ? BaseDestructionWeight : 0;
                }

                if (candidate.Root && target.Character && damage > 0 && damage < health && !target.RootNextTurn && !roots.Contains(id))
                {
                    score.Root += RootWeight;
                }
            }

            if (candidate.ReviveHealth == 0)
            {
                foreach (GridCell cell in candidate.Exploration)
                {
                    if (!explored.Contains(cell))
                    {
                        score.Exploration++;
                    }
                }
            }

            return score;
        }
    }
}

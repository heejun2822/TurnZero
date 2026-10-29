using System.Collections.Generic;

namespace TurnZero
{
    public sealed class BattleBotCandidate
    {
        public BattleOrder Order;
        public int Cost;
        public int Damage;
        public bool Root;
        public bool Stealth;
        public int ReviveHealth;
        public int DistanceGain;
        public int PositionDistance;
        public List<int> Targets = new List<int>();
        public List<GridCell> Exploration = new List<GridCell>();
    }
}

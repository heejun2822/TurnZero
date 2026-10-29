using System;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleBotDecision
    {
        public BattleOrder Order;
        public BattleBotScore Score;
        public bool Accepted;
        public string Result;
    }
}

using System;
using System.Collections.Generic;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleBotPlan
    {
        public string Version;
        public string RulesSignature;
        public int Seed;
        public BattleBotObservation Observation;
        public int ReservedAP;
        public List<BattleBotDecision> Decisions = new List<BattleBotDecision>();
    }
}

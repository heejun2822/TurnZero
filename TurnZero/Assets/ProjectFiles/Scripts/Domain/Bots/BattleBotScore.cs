using System;

namespace TurnZero
{
    [Serializable]
    public struct BattleBotScore
    {
        public int Damage;
        public int Defeats;
        public int BasePressure;
        public int BaseDestruction;
        public int Root;
        public int Revival;
        public int Approach;
        public int Exploration;
        public int Stealth;
        public int Cost;

        public int Total => Damage + Defeats + BasePressure + BaseDestruction + Root + Revival + Approach + Exploration + Stealth - Cost;

        public override string ToString()
        {
            return $"{Total}: damage={Damage}, defeats={Defeats}, base={BasePressure + BaseDestruction}, root={Root}, revive={Revival}, approach={Approach}, explore={Exploration}, stealth={Stealth}, AP=-{Cost}";
        }
    }
}

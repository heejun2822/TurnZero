using System;
using System.Linq;

namespace TurnZero
{
    public static class BattleVictory
    {
        public static BattleWinner Determine(MatchState state)
        {
            int one = state.Bases[0].Health;
            int two = state.Bases[1].Health;

            if (one <= 0 || two <= 0)
            {
                return one <= 0 && two <= 0 ? BattleWinner.Draw : one > 0 ? BattleWinner.PlayerOne : BattleWinner.PlayerTwo;
            }

            bool a = state.Units.Any(u => u.Owner == 0 && u.Alive);
            bool b = state.Units.Any(u => u.Owner == 1 && u.Alive);

            if (a && b)
            {
                return BattleWinner.None;
            }

            if (a || b)
            {
                return a ? BattleWinner.PlayerOne : BattleWinner.PlayerTwo;
            }

            return one == two ? BattleWinner.Draw : one > two ? BattleWinner.PlayerOne : BattleWinner.PlayerTwo;
        }
    }
}

using System;
using System.Linq;

namespace TurnZero
{
    public static class BattlePhaseCompletion
    {
        public static void FinishPhase(MatchState state, GameRules rules, int phase)
        {
            foreach (BattleUnit unit in state.Units.Where(u => u.DefeatPending))
            {
                unit.DefeatPending = false;
                unit.DiedTurn = state.Turn;
                unit.Equipment = -1;
                unit.ReadyOn = new int[GameDefinitions.CooldownActionCount];

                state.PendingKillAP[1 - unit.Owner] += rules.KillReward;
            }

            foreach (BattleStructure structure in state.Structures)
            {
                structure.PendingDestroy = false;
                structure.Active = true;

                if (phase == GameDefinitions.LastActionPhase && structure.ReadyTurn <= state.Turn)
                {
                    structure.Completed = true;
                }
            }
        }
    }
}

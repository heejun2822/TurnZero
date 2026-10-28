using System;
using System.Linq;
namespace TurnZero.Game
{
    public static class BattlePhaseCompletion
    {
        public static void FinishPhase(MatchState state, GameRules rules, int phase)
        {
            foreach (var unit in state.Units.Where(u => u.DefeatPending))
            {
                unit.DefeatPending = false; unit.DiedTurn = state.Turn; unit.Equipment = -1; unit.ReadyOn = new int[4];
                state.PendingKillAP[1-unit.Owner] += rules.KillReward;
            }
            foreach (var structure in state.Structures)
            {
                structure.PendingDestroy = false; structure.Active = true;
                if (phase == 4 && structure.ReadyTurn <= state.Turn) structure.Completed = true;
            }
        }
    }
}

using System.Linq;

namespace TurnZero
{
    public static class BattleStatusEffects
    {
        public static bool Has(BattleUnit unit, StatusEffectKind kind, int turn)
        {
            return unit.Effects.Any(e => e.Kind == kind && e.StartTurn <= turn && turn <= e.ExpiresTurn);
        }

        public static void Apply(BattleUnit unit, StatusEffectDefinition definition, int turn, int source)
        {
            int start = turn + definition.Delay;
            int end = start + definition.Duration - 1;
            StatusEffect existing = unit.Effects.FirstOrDefault(e => e.Kind == definition.Kind &&
                (definition.Kind == StatusEffectKind.Stealth || e.StartTurn == start));

            if (existing == null)
            {
                unit.Effects.Add(new StatusEffect
                {
                    Kind = definition.Kind,
                    SourceId = source,
                    StartTurn = start,
                    ExpiresTurn = end
                });
            }
            else
            {
                existing.ExpiresTurn = System.Math.Max(existing.ExpiresTurn, end);
            }
        }

        public static void EndTurn(MatchState state)
        {
            foreach (BattleUnit unit in state.Units)
            {
                unit.Effects.RemoveAll(e => e.ExpiresTurn <= state.Turn);
            }
        }

        public static void Clear(BattleUnit unit)
        {
            unit.Effects.Clear();
        }
    }
}

namespace TurnZero
{
    public static class BattleCosts
    {
        public static bool TryStart(MatchState state, GameRules rules, BattleOrder order)
        {
            var unit = state.Units.Find(u => u.Id == order.UnitId);
            if (unit == null || order.Action != BattleAction.Revive && (!unit.Alive || unit.Health <= 0)) return false;
            int cost = BattleCommands.Cost(state,rules,order);
            if (state.AP[unit.Owner] < cost) return false;
            state.AP[unit.Owner] -= cost;
            if ((int)order.Action < 4) unit.ReadyOn[(int)order.Action] = state.Turn + System.Math.Max(1,BattleCommands.Spec(state,rules,order).Cooldown);
            return true;
        }
    }
}

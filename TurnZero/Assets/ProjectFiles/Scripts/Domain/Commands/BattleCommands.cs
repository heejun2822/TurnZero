using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public static class BattleCommands
    {
        public static ActionSpec Spec(MatchState state, GameRules rules, BattleOrder order)
        {
            var unit = state.Units.Find(u => u.Id == order.UnitId);
            return Spec(rules,unit,order.Action);
        }
        public static ActionSpec Spec(GameRules rules, BattleUnit unit, BattleAction action)
        {
            if (unit == null) return null;
            var character = rules.Characters[unit.Slot];
            switch (action)
            {
                case BattleAction.Move: return character.Move;
                case BattleAction.Attack: return character.Attack;
                case BattleAction.Skill: return character.Skill;
                case BattleAction.Equipment: return unit.Equipment >= 0 ? rules.Equipment[unit.Equipment].Action : null;
                default: return null;
            }
        }
        public static int Cost(MatchState state, GameRules rules, BattleOrder order)
        {
            return Cost(rules,state.Units.Find(u => u.Id == order.UnitId),order.Action,state.Turn);
        }
        public static int Cost(GameRules rules, BattleUnit unit, BattleAction action, int turn) => action == BattleAction.Revive ?
            Math.Max(rules.MinimumReviveCost,rules.ReviveCost-Math.Max(0,turn-unit.DiedTurn-1)) : Spec(rules,unit,action)?.Cost ?? 0;
        public static int Priority(MatchState state, GameRules rules, BattleOrder order) => order.Action == BattleAction.Revive ? GameDefinitions.RevivePhase : Spec(state, rules, order)?.Priority ?? GameDefinitions.TurnWrapPhase;
        public static bool BaseAt(MatchState state, GridCell cell) => state.Bases.Any(b => b.Placed && b.Cell == cell);
        public static bool Movable(MatchState state, GameRules rules, GridCell cell, int player = -1)
        {
            if (!rules.Contains(cell)) return false;
            if (player < 0) return !BaseAt(state, cell);
            var vision = BattleVisibility.Vision(state, rules, player);
            return !state.Bases.Any(b => b.Placed && b.Cell == cell && (b.Owner == player || vision.Contains(cell) || state.KnownBases[player]?.Cell == cell));
        }

        public static bool Installable(MatchState state, GameRules rules, GridCell cell, int player = -1)
        {
            if (!Movable(state,rules,cell,player)) return false;
            var known = player < 0 ? state.Structures.Where(s => s.Operational) : BattleVisibility.View(state,rules,player,0).Structures;
            return !known.Any(s => s.Cell == cell);
        }

        public static string Validate(MatchState state, GameRules rules, int player, BattleOrder order)
        {
            if (order == null || player < 0 || player >= GameDefinitions.PlayerCount || state.Phase != BattlePhase.Planning || state.Ready[player]) return "This planning phase is closed.";
            var unit = state.Units.Find(u => u.Id == order.UnitId && u.Owner == player);
            if (unit == null) return "Select one of your characters.";
            if (order.Action == BattleAction.Wait) return null;
            if (!Enum.IsDefined(typeof(BattleAction), order.Action)) return "Unknown action.";
            if (order.Action == BattleAction.Revive)
            {
                if (unit.Alive || unit.DiedTurn >= state.Turn || !state.Units.Any(u => u.Owner == player && u.Alive)) return "Revive next turn while an ally is still alive.";
                var home = state.Bases[player];
                return rules.Home(player, order.Target) && Movable(state, rules, order.Target) && home.Cell.Distance(order.Target) <= rules.BaseVision ? null : "Revive inside your base vision on your field.";
            }
            if (!unit.Alive) return "This character is defeated. Select REVIVE.";
            var spec = Spec(state, rules, order);
            if (spec == null) return "Craft an equipment item first.";
            if (unit.ReadyOn[(int)order.Action] > state.Turn) return $"Available on turn {unit.ReadyOn[(int)order.Action]}.";
            if (!rules.Contains(order.Target)) return "Choose a tile on the battlefield.";
            bool movement = order.Action == BattleAction.Move || order.Action == BattleAction.Skill && rules.Characters[unit.Slot].SkillEffect == SkillEffect.StealthMove;
            if (movement)
            {
                if (unit.Rooted(state.Turn)) return "ROOT prevents movement this turn.";
                if (order.Path == null || order.Path.Count == 0 || order.Path.Count > spec.Range || order.Path.Last() != order.Target) return "Draw a path within movement range.";
                var previous = unit.Cell;
                foreach (var cell in order.Path)
                {
                    if (previous.Distance(cell) != 1 || !Movable(state, rules, cell, player)) return "The path must use adjacent tiles and avoid bases.";
                    previous = cell;
                }
                return null;
            }
            if (order.Action != BattleAction.Equipment)
                return unit.Cell.Distance(order.Target) <= spec.Range ? null : "Target is outside action range.";
            var equipment = rules.Equipment[unit.Equipment];
            if (equipment.Effect == EquipmentEffect.Swap)
            {
                var ally = state.Units.Find(u => u.Id == order.TargetUnit && u.Owner == player && u.Id != unit.Id && u.Alive);
                return ally != null && !unit.Rooted(state.Turn) && !ally.Rooted(state.Turn) ? null : "Choose another living, movable ally.";
            }
            if (unit.Cell.Distance(order.Target) > spec.Range || !Installable(state, rules, order.Target, player)) return "Choose a valid structure tile within range.";
            if (!state.Explored[player].Contains(order.Target) && !BattleVisibility.Vision(state,rules,player).Contains(order.Target)) return "Choose an explored structure tile.";
            if (equipment.Effect == EquipmentEffect.Portal)
            {
                if (!Installable(state, rules, order.Exit, player) || order.Exit == order.Target || order.Target.Distance(order.Exit) > equipment.ExitRange) return "Choose a separate exit within portal range.";
                var known = state.Explored[player];
                if (!known.Contains(order.Target) || !known.Contains(order.Exit)) return "Portal entrance and exit must be explored.";
            }
            return null;
        }
    }
}

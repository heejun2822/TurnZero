using System.Collections.Generic;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalTileHighlightsView : MonoBehaviour
    {
        [SerializeField]
        private LocalFieldMap map;

        private BattleSession session;
        private LocalTileView[] tiles;
        private readonly HashSet<GridCell> reservedTargets = new HashSet<GridCell>();
        private readonly HashSet<GridCell> swapTargets = new HashSet<GridCell>();
        private readonly HashSet<GridCell> previewCells = new HashSet<GridCell>();

        public void Bind(BattleSession value)
        {
            session = value;
            tiles = map.Tiles;
        }

        public void Render(BattleView view)
        {
            GameRules rules = session.Rules;

            BattleUnit selected = view.FindUnit(session.SelectedId);
            bool planning = view.Phase == BattlePhase.Planning && !view.Ready && selected != null && selected.Owner == view.Player;
            ActionSpec spec = selected == null
                ? null
                : session.Action == BattleAction.Move
                ? rules.Characters[selected.Slot].Move
                : session.Action == BattleAction.Attack
                ? rules.Characters[selected.Slot].Attack
                : session.Action == BattleAction.Skill
                ? rules.Characters[selected.Slot].Skill
                : session.Action == BattleAction.Equipment && selected.Equipment >= 0
                ? rules.Equipment[selected.Equipment].Action
                : null;
            bool moving = selected != null &&
                (session.Action == BattleAction.Move ||
                session.Action == BattleAction.Skill &&
                rules.Characters[selected.Slot].SkillEffect == SkillEffect.StealthMove);
            HashSet<GridCell> reachable = planning && session.Armed && moving
                ? session.Match.Reachable(selected.Id, session.Action == BattleAction.Skill)
                : null;
            bool swapping = selected != null && session.Action == BattleAction.Equipment &&
                selected.Equipment >= 0 && rules.Equipment[selected.Equipment].Effect == EquipmentEffect.Swap;

            reservedTargets.Clear();
            swapTargets.Clear();
            previewCells.Clear();

            foreach (BattleOrder order in view.Orders)
            {
                reservedTargets.Add(order.Target);
            }

            foreach (GridCell cell in session.PathPreview)
            {
                previewCells.Add(cell);
            }

            if (planning && session.Armed && swapping)
            {
                foreach (BattleUnit unit in view.Units)
                {
                    if (unit.Owner == view.Player && unit.Alive && unit.Id != selected.Id)
                    {
                        swapTargets.Add(unit.Cell);
                    }
                }
            }

            foreach (LocalTileView tile in tiles)
            {
                GridCell cell = tile.Cell;
                Color? highlight = null;

                bool deploy = rules.Home(view.Player, cell) &&
                    (!view.OwnBase.Placed || cell != view.OwnBase.Cell) &&
                    (view.Phase == BattlePhase.BasePlacement ||
                    view.Phase == BattlePhase.UnitPlacement &&
                    view.OwnBase.Cell.Distance(cell) <= rules.BaseVision);

                if (deploy)
                {
                    highlight = new Color(0.08f, 0.48f, 0.45f);
                }

                if (planning && session.Armed)
                {
                    bool range = spec != null && selected.Cell.Distance(cell) <= spec.Range;

                    if (moving)
                    {
                        range = reachable.Contains(cell);
                    }

                    if (session.Action == BattleAction.Revive)
                    {
                        range = rules.Home(view.Player, cell) &&
                            view.OwnBase.Cell.Distance(cell) <= rules.BaseVision &&
                            cell != view.OwnBase.Cell;
                    }

                    if (session.PortalEntrance.HasValue)
                    {
                        range = session.PortalEntrance.Value.Distance(cell) <= rules.Equipment[selected.Equipment].ExitRange &&
                            view.Explored.Contains(cell);
                    }

                    if (swapping)
                    {
                        range = swapTargets.Contains(cell);
                    }
                    else if (session.Action == BattleAction.Equipment &&
                        (cell == view.OwnBase.Cell || view.EnemyBase != null && cell == view.EnemyBase.Cell))
                    {
                        range = false;
                    }

                    if (range)
                    {
                        highlight = session.Action == BattleAction.Move || session.Action == BattleAction.Revive
                            ? new Color(0.06f, 0.45f, 0.45f)
                            : new Color(0.54f, 0.31f, 0.12f);
                    }

                    int previewRadius = spec == null ? 0 : spec.Area;

                    if (session.Action == BattleAction.Equipment &&
                        selected.Equipment >= 0 &&
                        rules.Equipment[selected.Equipment].Effect == EquipmentEffect.Beacon)
                    {
                        previewRadius = spec.Area;
                    }

                    if (session.HoverCell.HasValue &&
                        spec != null &&
                        session.Action != BattleAction.Move &&
                        cell.Distance(session.HoverCell.Value) <= previewRadius &&
                        selected.Cell.Distance(session.HoverCell.Value) <= spec.Range)
                    {
                        highlight = new Color(0.95f, 0.47f, 0.19f);
                    }
                }

                if (reservedTargets.Contains(cell))
                {
                    highlight = new Color(0.12f, 0.65f, 0.73f);
                }

                if (previewCells.Contains(cell))
                {
                    highlight = new Color(0.15f, 0.9f, 0.8f);
                }

                if (selected != null && selected.Alive && selected.Cell == cell)
                {
                    highlight = new Color(0.3f, 0.95f, 0.84f);
                }

                int owner = cell.Y < rules.HomeHeight ? 0 : cell.Y >= rules.HomeHeight + rules.NeutralHeight ? 1 : -1;
                FieldDefinition field = session.Configuration.Fields[session.Fields[owner < 0 ? 0 : owner]];
                Color ground = owner < 0
                    ? new Color(0.28f, 0.25f, 0.20f)
                    : field.TileColor(cell.X, owner == 1 ? rules.Height - 1 - cell.Y : cell.Y);

                tile.Render(ground, view.Sight(cell), highlight);
            }

        }
    }
}

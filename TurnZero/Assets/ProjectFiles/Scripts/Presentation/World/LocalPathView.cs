using System.Collections.Generic;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalPathView : MonoBehaviour
    {
        [SerializeField]
        private LocalFieldMap map;
        [SerializeField]
        private LineRenderer pathLine;

        private BattleSession session;
        private readonly List<GridCell> points = new List<GridCell>();

        public void Bind(BattleSession value)
        {
            session = value;
        }

        public void Render(BattleView view)
        {
            points.Clear();
            BattleUnit selected = view.Units.Find(u => u.Id == session.SelectedId);

            if (selected != null && selected.Alive && selected.Owner == view.Player)
            {
                points.Add(selected.Cell);
                BattleOrder order = view.Orders.Find(o => o.UnitId == selected.Id);

                if (session.PathPreview.Count > 0)
                {
                    points.AddRange(session.PathPreview);
                }
                else if (session.Armed &&
                    session.HoverCell.HasValue &&
                    (session.Action == BattleAction.Move ||
                    session.Action == BattleAction.Skill &&
                    session.Rules.Characters[selected.Slot].SkillEffect == SkillEffect.StealthMove))
                {
                    points.AddRange(session.Match.Path(selected.Id, session.HoverCell.Value, session.Action == BattleAction.Skill));
                }
                else if (order != null)
                {
                    if (order.Path.Count > 0)
                    {
                        points.AddRange(order.Path);
                    }
                    else
                    {
                        points.Add(order.Target);
                    }
                }
            }

            pathLine.gameObject.SetActive(points.Count > 1);

            if (points.Count > 1)
            {
                pathLine.positionCount = points.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    pathLine.SetPosition(i, map.Position(points[i]) + Vector3.up * 0.18f);
                }
            }
        }
    }
}

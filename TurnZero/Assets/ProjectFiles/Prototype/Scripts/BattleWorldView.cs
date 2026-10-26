using UnityEngine;

namespace TurnZero
{
    public sealed class BattleWorldView : MonoBehaviour
    {
        [SerializeField] private BattleMapView map;
        [SerializeField] private BattleActorView[] actors;
        [SerializeField] private BattleLastSeenView[] lastSeen;
        [SerializeField] private LineRenderer commandPath;
        [SerializeField] private Material movePathMaterial;
        [SerializeField] private Material attackPathMaterial;
        private BattleRules rules;
        public BattleMapView Map => map;
        public Vector3 Position(Cell cell) => map.Position(cell);

        public void Bind(BattleRules configuration, Camera camera, System.Action<Cell> click)
        {
            rules = configuration;
            map.Bind(click);
            foreach (var actor in actors) actor.Bind(click, camera);
            foreach (var marker in lastSeen) marker.Bind(camera);
        }

        public void Refresh(PlayerView view, int selectedId, ActionKind mode)
        {
            map.Render(view, rules, selectedId, mode);
            foreach (var actor in actors) actor.Render(view.Entities.Find(e => e.Id == actor.EntityId), map);
            foreach (var marker in lastSeen) marker.Render(view.LastSeen.Find(o => o.Entity.Id == marker.EntityId), map);
            var order = view.Orders.Find(o => o.UnitId == selectedId);
            var unit = view.Entities.Find(e => e.Id == selectedId && e.Health > 0);
            bool show = order != null && unit != null && order.Kind != ActionKind.Wait;
            commandPath.gameObject.SetActive(show);
            if (!show) return;
            var target = Position(order.Kind == ActionKind.Move ? order.Destination :
                view.Entities.Find(e => e.Id == order.TargetId)?.Position ?? unit.Position);
            var start = Position(unit.Position);
            var direction = (target - start).normalized;
            var side = Vector3.Cross(Vector3.up, direction);
            commandPath.sharedMaterial = order.Kind == ActionKind.Move ? movePathMaterial : attackPathMaterial;
            commandPath.SetPositions(new[] { start + Vector3.up * 0.12f, target + Vector3.up * 0.12f,
                target - direction * 0.22f + side * 0.15f + Vector3.up * 0.12f, target + Vector3.up * 0.12f,
                target - direction * 0.22f - side * 0.15f + Vector3.up * 0.12f });
        }
    }
}

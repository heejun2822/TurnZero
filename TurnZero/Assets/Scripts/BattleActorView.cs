using UnityEngine;

namespace TurnZero
{
    // One authored model per entity. Hidden enemies never receive current authoritative state.
    public sealed class BattleActorView : MonoBehaviour
    {
        [SerializeField] private int entityId;
        [SerializeField] private BattleWorldTarget target;
        [SerializeField] private TextMesh label;
        private Camera labelCamera;
        public int EntityId => entityId;

        public void Bind(System.Action<Cell> click, Camera camera)
        {
            target.Click = click;
            labelCamera = camera;
        }

        public void Render(EntityState entity, BattleMapView map)
        {
            gameObject.SetActive(entity != null && entity.Health > 0);
            if (!gameObject.activeSelf) return;
            transform.position = map.Position(entity.Position);
            target.Cell = entity.Position;
            label.text = entity.Kind == EntityKind.Base ? $"{entity.Health} HP" : (entity.Id - entity.Owner * 4).ToString();
        }

        private void LateUpdate()
        {
            if (labelCamera != null) label.transform.rotation = labelCamera.transform.rotation;
        }
    }
}

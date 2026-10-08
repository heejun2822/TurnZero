using UnityEngine;

namespace TurnZero
{
    public sealed class BattleLastSeenView : MonoBehaviour
    {
        [SerializeField] private int entityId;
        [SerializeField] private TextMesh label;
        private Camera labelCamera;
        public int EntityId => entityId;
        public void Bind(Camera camera) => labelCamera = camera;
        public void Render(Observation observation, BattleMapView map)
        {
            gameObject.SetActive(observation != null);
            if (observation == null) return;
            transform.position = map.Position(observation.Entity.Position);
            label.text = $"? P{observation.Entity.Owner + 1}\n{observation.Entity.Health} HP / T{observation.SeenOnTurn}";
        }
        private void LateUpdate()
        {
            if (labelCamera != null) label.transform.rotation = labelCamera.transform.rotation;
        }
    }
}

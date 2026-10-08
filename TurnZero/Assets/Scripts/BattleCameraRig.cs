using UnityEngine;
namespace TurnZero
{
    [ExecuteAlways]
    public sealed class BattleCameraRig : MonoBehaviour
    {
        [SerializeField] private Camera cameraView;
        [SerializeField] private BattleMapView map;
        [SerializeField] private BattleHudLayout layout;
        [SerializeField] private float pitch = 56;
        [SerializeField] private float yaw;
        [SerializeField] private float zoom = 1;
        private Rect viewport = new Rect(0.025f, 0.13f, 0.535f, 0.75f);
        public Camera Camera => cameraView;
        private void LateUpdate() => Reframe();
        public void Rotate() { yaw = (yaw + 45) % 360; Reframe(); }
        public void Zoom(float change) { zoom = Mathf.Clamp(zoom + change, 0.75f, 1.4f); Reframe(); }
        public void Reframe()
        {
            if (map == null || layout == null || cameraView == null) return;
            layout.Refresh();
            viewport = layout.FieldViewport;
            if (viewport.width <= 0 || viewport.height <= 0) return;
            FrameCamera();
        }
        private void FrameCamera()
        {
            // Shift a normal perspective camera so the board fits the HUD's available rectangle.
            // Keeping a full-screen camera also keeps native physics picking and UI projection aligned.
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var inverse = Quaternion.Inverse(rotation);
            var focus = map.transform.position + Vector3.up * 0.4f;
            float vertical = Mathf.Tan(cameraView.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float horizontal = vertical * cameraView.aspect;

            float centerX = 2 * viewport.center.x - 1;
            float centerY = 2 * viewport.center.y - 1;
            float distance = 1;
            foreach (float x in new[] { -map.Width * map.Spacing * 0.5f - 0.2f, map.Width * map.Spacing * 0.5f + 0.2f })
                foreach (float y in new[] { -0.55f, 2f })
                    foreach (float z in new[] { -map.Height * map.Spacing * 0.5f - 0.2f, map.Height * map.Spacing * 0.5f + 0.2f })
                    {
                        var local = inverse * (new Vector3(x, y, z) - focus);
                        distance = Mathf.Max(distance, Mathf.Abs(local.x - centerX * horizontal * local.z) / (viewport.width * 0.94f * horizontal) - local.z,
                            Mathf.Abs(local.y - centerY * vertical * local.z) / (viewport.height * 0.94f * vertical) - local.z);
                    }
            distance *= zoom;
            cameraView.transform.SetPositionAndRotation(focus + rotation * new Vector3(-centerX * distance * horizontal,
                -centerY * distance * vertical, -distance), rotation);
        }


    }
}

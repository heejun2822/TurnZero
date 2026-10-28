using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TurnZero
{
    [ExecuteAlways]
    public sealed class BattleCameraRig : MonoBehaviour
    {
        [SerializeField]
        private Camera cameraView;
        [SerializeField]
        private BattleMapView map;
        [SerializeField]
        private BattleHudLayout layout;
        [SerializeField]
        private float pitch = 56;
        [SerializeField]
        private float yaw;
        [SerializeField]
        private float zoom = 1;
        [SerializeField, Range(0.1f, 1)]
        private float minimumZoom = 0.15f;
        [SerializeField, Range(0.1f, 1)]
        private float maximumZoom = 1;
        [SerializeField, Min(0)]
        private float wheelSensitivity = 0.1f;
        [SerializeField, Min(0)]
        private float edgeSize = 24;
        [SerializeField, Min(0)]
        private float panSpeed = 12;
        [SerializeField, Min(0)]
        private float fieldPadding = 0.65f;
        private Vector2 pan;
        private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
        private Rect viewport = new Rect(0.025f, 0.13f, 0.535f, 0.75f);

        public Camera Camera => cameraView;

        private void LateUpdate()
        {
            Reframe();

            if (Application.IsPlaying(gameObject) && Application.isFocused)
            {
                ReadMouse(Time.unscaledDeltaTime);
            }
        }

        public void Rotate()
        {
            yaw = (yaw + 45) % 360;
            Reframe();
        }

        public void Zoom(float change)
        {
            zoom = Mathf.Clamp(zoom + change, minimumZoom, maximumZoom);
            Reframe();
        }

        private void OnValidate()
        {
            minimumZoom = Mathf.Clamp(minimumZoom, 0.1f, 1);
            maximumZoom = Mathf.Clamp(maximumZoom, minimumZoom, 1);
            zoom = Mathf.Clamp(zoom, minimumZoom, maximumZoom);
        }

        private void ReadMouse(float deltaTime)
        {
            Mouse mouse = Mouse.current;

            if (mouse == null || map == null || cameraView == null || layout == null)
            {
                return;
            }

            Vector2 pointer = mouse.position.ReadValue();

            if (!new Rect(0, 0, Screen.width, Screen.height).Contains(pointer) ||
                mouse.leftButton.isPressed ||
                mouse.rightButton.isPressed ||
                mouse.middleButton.isPressed ||
                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed))
            {
                return;
            }

            Vector2 normalized = new Vector2(pointer.x / Screen.width, pointer.y / Screen.height);
            float wheel = mouse.scroll.ReadValue().y;

            if (wheel != 0 && viewport.Contains(normalized) && !OverHud(pointer))
            {
                Zoom(-wheel * wheelSensitivity);
            }

            Vector2 direction = new Vector2(pointer.x < edgeSize ? -1 : pointer.x >= Screen.width - edgeSize ? 1 : 0,
                pointer.y < edgeSize ? -1 : pointer.y >= Screen.height - edgeSize ? 1 : 0);

            if (direction == Vector2.zero)
            {
                return;
            }

            Vector3 right = Vector3.ProjectOnPlane(cameraView.transform.right, map.transform.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(cameraView.transform.forward, map.transform.up).normalized;
            Vector3 motion = map.transform.InverseTransformVector((right * direction.x + forward * direction.y).normalized);

            pan += new Vector2(motion.x, motion.z) * (panSpeed * map.Spacing * zoom * deltaTime);
            Reframe();
        }

        private bool OverHud(Vector2 pointer)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            pointerHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
            {
                position = pointer
            }, pointerHits);

            // Physics hits on the field must still allow wheel zoom.
            foreach (RaycastResult hit in pointerHits)
            {
                if (hit.module is GraphicRaycaster)
                {
                    return true;
                }
            }

            return false;
        }

        public void Reframe()
        {
            if (map == null || map.AlliedField == null || layout == null || cameraView == null)
            {
                return;
            }

            layout.Refresh();
            viewport = layout.FieldViewport;

            if (viewport.width <= 0 || viewport.height <= 0)
            {
                return;
            }

            FrameCamera();
        }

        private void FrameCamera()
        {
            // Shift a normal perspective camera so the board fits the HUD's available rectangle.
            // Keeping a full-screen camera also keeps native physics picking and UI projection aligned.
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Quaternion inverse = Quaternion.Inverse(rotation);
            Vector3 focus = map.transform.TransformPoint(Vector3.up * 0.4f);
            float vertical = Mathf.Tan(cameraView.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float horizontal = vertical * cameraView.aspect;

            float centerX = 2 * viewport.center.x - 1;
            float centerY = 2 * viewport.center.y - 1;
            float distance = 1;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = new Vector3(
                            x * (map.Width * map.Spacing * 0.5f + 0.2f),
                            y == 0 ? -0.55f : 2f,
                            z * (map.Height * map.Spacing * 0.5f + 0.2f));
                        Vector3 local = inverse * (map.transform.TransformPoint(corner) - focus);

                        distance = Mathf.Max(
                            distance,
                            Mathf.Abs(local.x - centerX * horizontal * local.z) / (viewport.width * 0.94f * horizontal) - local.z,
                            Mathf.Abs(local.y - centerY * vertical * local.z) / (viewport.height * 0.94f * vertical) - local.z);
                    }
                }
            }

            distance *= zoom;
            cameraView.farClipPlane = Mathf.Max(200, distance + (map.Width + map.Height) * map.Spacing + 4);
            Vector3 offset = rotation * new Vector3(-centerX * distance * horizontal, -centerY * distance * vertical, -distance);

            SetPose(rotation, offset);
            ClampPan();
            SetPose(rotation, offset);
        }

        private void SetPose(Quaternion rotation, Vector3 offset)
        {
            cameraView.transform.SetPositionAndRotation(
                map.transform.TransformPoint(new Vector3(pan.x, 0.4f, pan.y)) + offset, rotation);
        }

        private void ClampPan()
        {
            Plane ground = new Plane(map.transform.up, map.transform.position);
            Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Ray ray = cameraView.ViewportPointToRay(new Vector3(
                        x == 0 ? viewport.xMin : viewport.xMax,
                        y == 0 ? viewport.yMin : viewport.yMax,
                        0));

                    if (!ground.Raycast(ray, out float distance))
                    {
                        pan = Vector2.zero;

                        return;
                    }

                    Vector3 point = map.transform.InverseTransformPoint(ray.GetPoint(distance));

                    minimum = Vector2.Min(minimum, new Vector2(point.x, point.z));
                    maximum = Vector2.Max(maximum, new Vector2(point.x, point.z));
                }
            }

            Vector2 half = new Vector2(map.Width * 0.5f + fieldPadding, map.Height * 0.5f + fieldPadding) * map.Spacing;

            // When the whole field fits on an axis, keep its original centered framing.
            pan.x = maximum.x - minimum.x >= half.x * 2
                ? 0
                : Mathf.Clamp(pan.x,
                    pan.x - half.x - minimum.x, pan.x + half.x - maximum.x);
            pan.y = maximum.y - minimum.y >= half.y * 2
                ? 0
                : Mathf.Clamp(pan.y,
                    pan.y - half.y - minimum.y, pan.y + half.y - maximum.y);
        }
    }
}

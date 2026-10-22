using UnityEngine;

namespace TurnZero.Game
{
    public sealed class LocalBattleCamera : MonoBehaviour
    {
        [SerializeField] private Camera cameraView;
        private BattleSession session;
        private LocalBattleWorld world;
        private Vector3 focus, target;
        private float zoom = 0.9f;
        private int player = -1;
        public Camera Camera => cameraView;
        public Vector3 FocusPoint => target;
        public float Zoom => zoom;
        public bool ManualEnabled => session != null && session.CanUseCamera && !session.ShowHelp;
        public void Bind(BattleSession value, LocalBattleWorld board)
        {
            session = value; world = board;
            session.Events.PanRequested += Pan;
            session.Events.ZoomRequested += ChangeZoom;
            session.Events.FocusRequested += Focus;
            session.Events.Changed += Refresh;
            Refresh();
        }
        private void Refresh()
        {
            if (session.Player == player) return;
            player = session.Player;
            int row = player == 0 ? session.Rules.HomeHeight - 2 : session.Rules.Height - session.Rules.HomeHeight + 1;
            target = world.Position(new GridCell(session.Rules.Width/2,row)); target.x = 0;
            focus = target;
        }
        private void LateUpdate()
        {
            if (session == null) return;
            // Clear and draw the whole screen so dismissed overlay UI cannot remain outside the viewport.
            cameraView.rect = new Rect(0,0,1,1);
            focus = Vector3.Lerp(focus,target,1 - Mathf.Exp(-Time.unscaledDeltaTime * 12));
            float pitch = session.Configuration.CameraPitch;
            cameraView.transform.rotation = Quaternion.Euler(pitch,player * 180,0);
            float boardHeight = session.Rules.Height * world.Spacing;
            float boardWidth = session.Rules.Width * world.Spacing;
            float aspect = (float)Screen.width / Mathf.Max(1,Screen.height);
            float vertical = Mathf.Tan(cameraView.fieldOfView * Mathf.Deg2Rad / 2);
            float fit = Mathf.Max(boardHeight * Mathf.Sin(pitch * Mathf.Deg2Rad) / (2 * vertical),boardWidth / (2 * vertical * aspect)) + boardHeight * Mathf.Cos(pitch * Mathf.Deg2Rad) / 2;
            float distance = fit * zoom * 1.07f;
            cameraView.transform.position = focus - cameraView.transform.forward * distance;
            cameraView.farClipPlane = distance + boardHeight * 3;
        }
        private void Pan(Vector2 pixels)
        {
            if (!ManualEnabled) return;
            float factor = world.Spacing * session.Rules.Height * zoom / Mathf.Max(100,Screen.height);
            Vector3 right = cameraView.transform.right;
            Vector3 forward = Vector3.ProjectOnPlane(cameraView.transform.forward,Vector3.up).normalized;
            target -= (right * pixels.x + forward * pixels.y) * factor;
            Clamp();
        }
        private void ChangeZoom(float amount)
        {
            if (!ManualEnabled) return;
            zoom = Mathf.Clamp(zoom * Mathf.Exp(-amount * session.Configuration.ZoomSensitivity),session.Configuration.MinimumZoom,session.Configuration.MaximumZoom);
            Clamp();
        }
        private void Focus(GridCell cell, bool immediate)
        {
            target = world.Position(cell); Clamp();
            if (immediate) focus = target;
        }
        private void Clamp()
        {
            float x = session.Rules.Width * world.Spacing * 0.5f, z = session.Rules.Height * world.Spacing * 0.5f;
            target.x = Mathf.Clamp(target.x,-x,x); target.z = Mathf.Clamp(target.z,-z,z); target.y = 0;
        }
        public bool OnBoard(Vector2 screen) => cameraView.pixelRect.Contains(screen);
        private void OnDestroy()
        {
            if (session == null) return;
            session.Events.PanRequested -= Pan; session.Events.ZoomRequested -= ChangeZoom;
            session.Events.FocusRequested -= Focus; session.Events.Changed -= Refresh;
        }
    }
}

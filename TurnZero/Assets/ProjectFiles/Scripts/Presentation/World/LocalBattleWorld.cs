using UnityEngine;

namespace TurnZero
{
    public sealed class LocalBattleWorld : MonoBehaviour
    {
        [SerializeField]
        private LocalFieldMap map;
        [SerializeField]
        private LocalTileHighlightsView highlights;
        [SerializeField]
        private LocalActorsView actors;
        [SerializeField]
        private LocalWorldMarkersView markers;
        [SerializeField]
        private LocalPathView path;
        [SerializeField]
        private LocalBattleEffectsView effects;
        private BattleSession session;

        public float Spacing => map.Spacing;

        public Vector3 Position(GridCell cell)
        {
            return map.Position(cell);
        }

        public void Bind(BattleSession value, Camera camera)
        {
            if (session != null)
            {
                session.Events.Changed -= Refresh;
                session.Events.CueRequested -= Cue;
            }

            string error = map.LayoutError();

            if (error != null)
            {
                throw new System.InvalidOperationException(error);
            }

            session = value;
            highlights.Bind(value);
            actors.Bind(value, camera);
            markers.Bind(value, camera);
            path.Bind(value);
            effects.Bind(value);
            session.Events.Changed += Refresh;
            session.Events.CueRequested += Cue;
        }

        public bool TryCell(Camera camera, Vector2 screen, out GridCell cell)
        {
            Ray ray = camera.ScreenPointToRay(screen);

            if (Physics.Raycast(ray, out RaycastHit hit, 300))
            {
                LocalActorView actor = hit.collider.GetComponentInParent<LocalActorView>();

                if (actor != null)
                {
                    cell = actor.Cell;

                    return true;
                }

                LocalTileView tile = hit.collider.GetComponentInParent<LocalTileView>();

                if (tile != null)
                {
                    cell = tile.Cell;

                    return true;
                }
            }

            Plane plane = new Plane(Vector3.up, Vector3.zero);

            if (plane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);

                cell = map.WorldToCell(point);

                return session.Rules.Contains(cell);
            }

            cell = default;

            return false;
        }

        public bool TryActor(Camera camera, Vector2 screen, out int id)
        {
            id = 0;

            if (!Physics.Raycast(camera.ScreenPointToRay(screen), out RaycastHit hit, 300))
            {
                return false;
            }

            LocalActorView actor = hit.collider.GetComponentInParent<LocalActorView>();

            if (actor == null || actor.EntityId >= GameDefinitions.FirstBaseId)
            {
                return false;
            }

            id = actor.EntityId;

            return true;
        }

        private void Refresh()
        {
            BattleView view = session.View;

            highlights.Render(view);
            actors.Render(view);
            markers.Render(view);
            path.Render(view);

            if (view.Phase == BattlePhase.Setup)
            {
                effects.Clear();
            }
        }

        private void Cue(BattleCue cue)
        {
            if (cue.Kind == CueKind.Reward || actors.Play(cue))
            {
                return;
            }

            effects.Play(cue);
        }

        public static Color Team(int player)
        {
            return player == 0 ? new Color(0.12f, 0.8f, 0.76f) : new Color(1, 0.42f, 0.32f);
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.Events.Changed -= Refresh;
                session.Events.CueRequested -= Cue;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TurnZero
{
    public sealed class BattleWorldView : MonoBehaviour
    {
        private sealed class Actor
        {
            public GameObject Root;
            public TextMesh Label;
            public BattleWorldTarget Target;
        }

        private const float Spacing = 1.1f;
        private static readonly Color Blue = new Color(0.06f, 0.62f, 0.65f);
        private static readonly Color Red = new Color(0.9f, 0.25f, 0.23f);
        private static readonly Color Gold = new Color(0.02f, 0.95f, 0.98f);
        private static readonly Color Fog = new Color(0.065f, 0.075f, 0.083f);
        private static readonly Color Ghost = new Color(0.3f, 0.33f, 0.38f);
        private BattleRules rules;
        private Camera cameraView;
        private Font font;
        private Material surface;
        private Action<Cell> click;
        private Renderer[] tiles;
        private Renderer[] borders;
        private GameObject commandPath;
        private Mesh facetedMesh;
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private readonly Dictionary<int, Actor> actors = new Dictionary<int, Actor>();
        private readonly Dictionary<int, Actor> ghosts = new Dictionary<int, Actor>();
        private Rect viewport = new Rect(0.025f, 0.13f, 0.535f, 0.75f);
        private float yaw;
        private float zoom = 1;
        private float framedAspect;

        public Vector3 Position(Cell cell) => new Vector3((cell.X - (rules.Width - 1) * 0.5f) * Spacing,
            0, (cell.Y - (rules.Height - 1) * 0.5f) * Spacing);

        public void Initialize(BattleRules configuration, Material material, Font labelFont, Camera camera, Action<Cell> onClick)
        {
            rules = configuration;
            surface = material;
            font = labelFont;
            cameraView = camera;
            click = onClick;
            cameraView.orthographic = false;
            cameraView.fieldOfView = 36;
            cameraView.nearClipPlane = 0.1f;
            cameraView.farClipPlane = 200;
            cameraView.clearFlags = CameraClearFlags.SolidColor;
            cameraView.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
            if (cameraView.GetComponent<PhysicsRaycaster>() == null) cameraView.gameObject.AddComponent<PhysicsRaycaster>();
            facetedMesh = FacetedMesh();
            BuildSurroundings();
            tiles = new Renderer[rules.Width * rules.Height];
            borders = new Renderer[tiles.Length];
            for (int y = 0; y < rules.Height; y++)
                for (int x = 0; x < rules.Width; x++)
                {
                    var cell = new Cell(x, y);
                    int index = y * rules.Width + x;
                    var tile = Shape($"Tile {x},{y}", transform, PrimitiveType.Cube, Position(cell) + Vector3.down * 0.08f,
                        new Vector3(1, 0.16f, 1), Fog, true);
                    var target = tile.AddComponent<BattleWorldTarget>();
                    target.Cell = cell;
                    target.Click = click;
                    tiles[index] = tile.GetComponent<Renderer>();
                    var border = Shape($"Tile Border {x},{y}", transform, PrimitiveType.Cube, Position(cell) + Vector3.down * 0.06f,
                        new Vector3(1.07f, 0.08f, 1.07f), Gold, false);
                    borders[index] = border.GetComponent<Renderer>();
                    border.SetActive(false);
                }
            FrameCamera();
        }

        public void SetViewport(Rect area)
        {
            if (viewport == area && Mathf.Approximately(framedAspect, cameraView.aspect)) return;
            viewport = area;
            FrameCamera();
        }

        public void Rotate() { yaw = (yaw + 45) % 360; FrameCamera(); }
        public void Zoom(float change) { zoom = Mathf.Clamp(zoom + change, 0.75f, 1.4f); FrameCamera(); }

        private void FrameCamera()
        {
            // Shift a normal perspective camera so the board fits the HUD's available rectangle.
            // Keeping a full-screen camera also keeps native physics picking and UI projection aligned.
            var rotation = Quaternion.Euler(56, yaw, 0);
            var inverse = Quaternion.Inverse(rotation);
            var focus = new Vector3(0, 0.4f, 0);
            float vertical = Mathf.Tan(cameraView.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float horizontal = vertical * cameraView.aspect;
            framedAspect = cameraView.aspect;
            float centerX = 2 * viewport.center.x - 1;
            float centerY = 2 * viewport.center.y - 1;
            float distance = 1;
            foreach (float x in new[] { -rules.Width * Spacing * 0.5f - 0.2f, rules.Width * Spacing * 0.5f + 0.2f })
                foreach (float y in new[] { -0.55f, 2f })
                    foreach (float z in new[] { -rules.Height * Spacing * 0.5f - 0.2f, rules.Height * Spacing * 0.5f + 0.2f })
                    {
                        var local = inverse * (new Vector3(x, y, z) - focus);
                        distance = Mathf.Max(distance, Mathf.Abs(local.x - centerX * horizontal * local.z) / (viewport.width * 0.94f * horizontal) - local.z,
                            Mathf.Abs(local.y - centerY * vertical * local.z) / (viewport.height * 0.94f * vertical) - local.z);
                    }
            distance *= zoom;
            cameraView.transform.SetPositionAndRotation(focus + rotation * new Vector3(-centerX * distance * horizontal,
                -centerY * distance * vertical, -distance), rotation);
        }

        public void Refresh(PlayerView view, int selectedId, ActionKind mode)
        {
            var selected = view.Entities.Find(e => e.Id == selectedId && e.Health > 0);
            bool canPlan = view.Phase == MatchPhase.Planning && !view.Locked && selected != null;
            for (int y = 0; y < rules.Height; y++)
                for (int x = 0; x < rules.Width; x++)
                {
                    var cell = new Cell(x, y);
                    int index = y * rules.Width + x;
                    bool visible = view.VisibleCells.Contains(cell);
                    bool placement = view.Phase == MatchPhase.Placement && rules.CanPlaceBase(view.Player, cell);
                    bool grass = (x * 13 + y * 7) % 5 < 2;
                    Color color = placement ? new Color(0.19f, 0.5f, 0.4f) : visible ?
                        (grass ? new Color(0.44f, 0.53f, 0.25f) : (x + y) % 2 == 0 ? new Color(0.62f, 0.58f, 0.49f) : new Color(0.53f, 0.52f, 0.45f)) : Fog;
                    tiles[index].sharedMaterial = Material(color);
                    var entity = view.Entities.Find(e => e.Position == cell && e.Health > 0);
                    bool range = canPlan && (mode == ActionKind.Move ? selected.Position.Distance(cell) == 1 :
                        visible && selected.Position.Distance(cell) <= rules.AttackRange && cell != selected.Position);
                    bool ordered = view.Orders.Any(o => o.Kind == ActionKind.Move && o.Destination == cell ||
                        o.Kind == ActionKind.Attack && entity != null && o.TargetId == entity.Id);
                    bool selection = entity != null && entity.Id == selectedId;
                    if (range || ordered || selection) color = Color.Lerp(color, Gold, selection ? 0.44f : 0.23f);
                    tiles[index].sharedMaterial = Material(color);
                    borders[index].gameObject.SetActive(selection || range || ordered);
                    borders[index].sharedMaterial = Material(Gold);
                }

            // Build every visible model exclusively from the player's detached observations.
            var living = view.Entities.Where(e => e.Health > 0).ToList();
            RemoveMissing(actors, living.Select(e => e.Id));
            foreach (var entity in living)
            {
                if (!actors.TryGetValue(entity.Id, out var actor)) actors[entity.Id] = actor = CreateActor(entity, false);
                actor.Root.transform.localPosition = Position(entity.Position);
                actor.Target.Cell = entity.Position;
                actor.Label.text = entity.Kind == EntityKind.Base ? $"{entity.Health} HP" : (entity.Id - entity.Owner * 4).ToString();
            }
            RemoveMissing(ghosts, view.LastSeen.Select(o => o.Entity.Id));
            foreach (var observation in view.LastSeen)
            {
                var entity = observation.Entity;
                if (!ghosts.TryGetValue(entity.Id, out var actor)) ghosts[entity.Id] = actor = CreateActor(entity, true);
                actor.Root.transform.localPosition = Position(entity.Position);
                actor.Label.text = $"? P{entity.Owner + 1}\n{entity.Health} HP / T{observation.SeenOnTurn}";
            }
            ShowCommandPath(view, selectedId);
        }

        private static void RemoveMissing(Dictionary<int, Actor> collection, IEnumerable<int> ids)
        {
            var keep = new HashSet<int>(ids);
            foreach (int id in collection.Keys.Where(id => !keep.Contains(id)).ToArray())
            {
                collection[id].Root.SetActive(false);
                Destroy(collection[id].Root);
                collection.Remove(id);
            }
        }

        private Actor CreateActor(EntityState entity, bool ghost)
        {
            var root = new GameObject($"{(ghost ? "Last Seen" : "Entity")} {entity.Id}");
            root.transform.SetParent(transform, false);
            root.transform.localRotation = Quaternion.Euler(0, entity.Kind == EntityKind.Base ? entity.Owner * 180 : entity.Owner == 0 ? 150 : -30, 0);
            Color color = ghost ? Ghost : entity.Owner == 0 ? Blue : Red;
            var result = new Actor { Root = root };
            if (!ghost)
            {
                result.Target = root.AddComponent<BattleWorldTarget>();
                result.Target.Click = click;
            }
            if (entity.Kind == EntityKind.Base)
            {
                var stone = ghost ? Ghost : new Color(0.42f, 0.44f, 0.4f);
                Shape("Castle Keep", root.transform, PrimitiveType.Cube, new Vector3(0, 0.5f, 0), new Vector3(0.75f, 0.9f, 0.75f), stone, !ghost);
                foreach (float x in new[] { -0.36f, 0.36f })
                    foreach (float z in new[] { -0.36f, 0.36f })
                    {
                        Shape("Turret", root.transform, PrimitiveType.Cube, new Vector3(x, 0.62f, z), new Vector3(0.28f, 1.2f, 0.28f), stone, !ghost);
                        Shape("Battlement", root.transform, PrimitiveType.Cube, new Vector3(x, 1.23f, z), new Vector3(0.34f, 0.14f, 0.34f), stone, !ghost);
                    }
                Shape("Gate", root.transform, PrimitiveType.Cube, new Vector3(0, 0.31f, -0.4f), new Vector3(0.25f, 0.5f, 0.08f), new Color(0.07f, 0.095f, 0.09f), !ghost);
                Shape("Banner", root.transform, PrimitiveType.Cube, new Vector3(-0.25f, 0.76f, -0.41f), new Vector3(0.16f, 0.37f, 0.035f), color, !ghost);
                Shape("Banner", root.transform, PrimitiveType.Cube, new Vector3(0.25f, 0.76f, -0.41f), new Vector3(0.16f, 0.37f, 0.035f), color, !ghost);
            }
            else
            {
                int slot = entity.Id - entity.Owner * 4 - 1;
                Color clothing = ghost ? Ghost : slot == 0 ? new Color(0.57f, 0.62f, 0.64f) : slot == 1 ? new Color(0.4f, 0.44f, 0.22f) : slot == 3 ? new Color(0.8f, 0.83f, 0.77f) : color;
                Color leather = ghost ? Ghost : new Color(0.23f, 0.16f, 0.11f);
                Shape("Tunic", root.transform, PrimitiveType.Cube, new Vector3(0, 0.69f, 0), new Vector3(0.36f, 0.45f, 0.27f), clothing, !ghost);
                Shape("Team Scarf", root.transform, PrimitiveType.Cube, new Vector3(0, 0.9f, 0.05f), new Vector3(0.39f, 0.09f, 0.3f), color, !ghost);
                foreach (float x in new[] { -0.12f, 0.12f })
                {
                    Shape("Leg", root.transform, PrimitiveType.Cube, new Vector3(x, 0.33f, 0), new Vector3(0.14f, 0.33f, 0.18f), leather, !ghost);
                    Shape("Boot", root.transform, PrimitiveType.Cube, new Vector3(x, 0.13f, 0.05f), new Vector3(0.17f, 0.14f, 0.26f), leather, !ghost);
                    Shape("Arm", root.transform, PrimitiveType.Cube, new Vector3(x * 2, 0.68f, 0), new Vector3(0.14f, 0.32f, 0.19f), clothing, !ghost);
                }
                Shape(slot == 0 ? "Helmet" : "Hood", root.transform, PrimitiveType.Sphere, new Vector3(0, 1.13f, 0), new Vector3(0.49f, 0.48f, 0.45f), clothing, !ghost);
                Shape("Face", root.transform, PrimitiveType.Cube, new Vector3(0, 1.1f, 0.18f), new Vector3(0.23f, 0.23f, 0.08f), ghost ? Ghost : slot == 0 ? new Color(0.05f, 0.075f, 0.085f) : new Color(0.72f, 0.51f, 0.34f), !ghost);
                if (slot == 0)
                {
                    Shape("Shield", root.transform, PrimitiveType.Cube, new Vector3(-0.32f, 0.63f, 0.17f), new Vector3(0.25f, 0.44f, 0.1f), color, !ghost);
                    Shape("Sword", root.transform, PrimitiveType.Cube, new Vector3(0.32f, 0.76f, 0.15f), new Vector3(0.06f, 0.74f, 0.1f), clothing, !ghost);
                }
                else if (slot == 3)
                {
                    Shape("Staff", root.transform, PrimitiveType.Cylinder, new Vector3(0.32f, 0.71f, 0.05f), new Vector3(0.06f, 0.65f, 0.06f), leather, !ghost);
                    Shape("Crystal", root.transform, PrimitiveType.Sphere, new Vector3(0.32f, 1.44f, 0.05f), new Vector3(0.19f, 0.3f, 0.19f), ghost ? Ghost : Gold, !ghost);
                }
                else
                {
                    Shape("Quiver", root.transform, PrimitiveType.Cube, new Vector3(0, 0.84f, -0.22f), new Vector3(0.19f, 0.58f, 0.18f), leather, !ghost);
                    Shape("Bow", root.transform, PrimitiveType.Cube, new Vector3(0.32f, 0.72f, 0.17f), new Vector3(0.06f, 0.65f, 0.08f), leather, !ghost);
                }
            }
            var labelObject = new GameObject("World HP Label", typeof(TextMesh));
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = Vector3.up * (entity.Kind == EntityKind.Base ? 1.9f : 1.6f);
            result.Label = labelObject.GetComponent<TextMesh>();
            result.Label.font = font;
            result.Label.fontSize = 40;
            result.Label.characterSize = ghost || entity.Kind == EntityKind.Base ? 0.065f : 0.1f;
            result.Label.anchor = TextAnchor.MiddleCenter;
            result.Label.alignment = TextAlignment.Center;
            result.Label.color = ghost ? new Color(0.65f, 0.68f, 0.73f) : Color.white;
            result.Label.GetComponent<Renderer>().sharedMaterial = font.material;
            return result;
        }

        private void LateUpdate()
        {
            foreach (var actor in actors.Values) actor.Label.transform.rotation = cameraView.transform.rotation;
            foreach (var actor in ghosts.Values) actor.Label.transform.rotation = cameraView.transform.rotation;
        }

        private Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            material = new Material(surface) { color = color, enableInstancing = true };
            materials.Add(color, material);
            return material;
        }

        private GameObject Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool pickable)
        {
            var shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            if (type == PrimitiveType.Sphere) shape.GetComponent<MeshFilter>().sharedMesh = facetedMesh;
            shape.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!pickable)
            {
                var collider = shape.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
            }
            return shape;
        }

        private void BuildSurroundings()
        {
            Shape("Terrain Foundation", transform, PrimitiveType.Cube, new Vector3(0, -0.27f, 0), new Vector3(40, 0.35f, 40), Fog, false);
            for (int y = -5; y < rules.Height + 5; y++)
                for (int x = -5; x < rules.Width + 5; x++)
                {
                    if (x >= 0 && x < rules.Width && y >= 0 && y < rules.Height) continue;
                    var cell = new Cell(x, y);
                    Color terrain = (x * 13 + y * 7) % 3 == 0 ? new Color(0.13f, 0.17f, 0.1f) : new Color(0.15f, 0.17f, 0.16f);
                    Shape("Surrounding Tile", transform, PrimitiveType.Cube, Position(cell) + Vector3.down * 0.08f, new Vector3(1.07f, 0.16f, 1.07f), terrain, false);
                    if ((x * x + y * y) % 4 != 0 || x == -1 || x == rules.Width || y == -1 || y == rules.Height) continue;
                    var point = Position(cell);
                    Shape("Tree Trunk", transform, PrimitiveType.Cube, point + Vector3.up * 0.38f, new Vector3(0.13f, 0.75f, 0.13f), new Color(0.12f, 0.105f, 0.08f), false);
                    Shape("Tree Crown", transform, PrimitiveType.Sphere, point + Vector3.up * 0.85f, new Vector3(0.9f, 1.25f, 0.9f), new Color(0.105f, 0.19f, 0.13f), false);
                }
            foreach (int x in new[] { -1, rules.Width })
                for (int y = 0; y < rules.Height; y += 2)
                {
                    var point = Position(new Cell(x, y));
                    Shape("Ruined Wall", transform, PrimitiveType.Cube, point + Vector3.up * 0.29f, new Vector3(0.4f, 0.58f, 1.3f), new Color(0.22f, 0.25f, 0.23f), false);
                    Shape("Wall Cap", transform, PrimitiveType.Cube, point + Vector3.up * 0.65f, new Vector3(0.55f, 0.2f, 0.5f), new Color(0.26f, 0.28f, 0.25f), false);
                }
        }

        private void ShowCommandPath(PlayerView view, int selectedId)
        {
            if (commandPath != null) { commandPath.SetActive(false); Destroy(commandPath); }
            var order = view.Orders.Find(o => o.UnitId == selectedId);
            var unit = view.Entities.Find(e => e.Id == selectedId && e.Health > 0);
            if (order == null || unit == null) return;
            var target = order.Kind == ActionKind.Move ? Position(order.Destination) :
                Position(view.Entities.Find(e => e.Id == order.TargetId)?.Position ?? unit.Position);
            var start = Position(unit.Position);
            var direction = (target - start).normalized;
            var side = Vector3.Cross(Vector3.up, direction);
            commandPath = new GameObject("Command Path", typeof(LineRenderer));
            commandPath.transform.SetParent(transform, false);
            var line = commandPath.GetComponent<LineRenderer>();
            line.sharedMaterial = Material(order.Kind == ActionKind.Move ? Gold : Red);
            line.startWidth = line.endWidth = 0.075f;
            line.positionCount = 5;
            line.SetPositions(new[] { start + Vector3.up * 0.12f, target + Vector3.up * 0.12f,
                target - direction * 0.22f + side * 0.15f + Vector3.up * 0.12f, target + Vector3.up * 0.12f,
                target - direction * 0.22f - side * 0.15f + Vector3.up * 0.12f });
        }

        private static Mesh FacetedMesh()
        {
            var axes = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            var faces = new[] { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
            var mesh = new Mesh { name = "Prototype Faceted Mesh" };
            mesh.vertices = faces.Select(index => axes[index] * 0.5f).ToArray();
            mesh.triangles = Enumerable.Range(0, faces.Length).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            foreach (var material in materials.Values) Destroy(material);
            if (facetedMesh != null) Destroy(facetedMesh);
        }
    }
}

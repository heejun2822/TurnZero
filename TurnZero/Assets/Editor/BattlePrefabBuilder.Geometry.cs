using System.Linq;
using UnityEngine;
namespace TurnZero.Editor
{
    internal sealed partial class BattlePrefabBuilder
    {
        private static readonly Color TeamBlue = new Color(0.06f, 0.62f, 0.65f);
        private static readonly Color Red = new Color(0.9f, 0.25f, 0.23f);
        private static readonly Color WorldCyan = new Color(0.02f, 0.95f, 0.98f);
        private static readonly Color Fog = new Color(0.065f, 0.075f, 0.083f);
        private static readonly Color Ghost = new Color(0.3f, 0.33f, 0.38f);
        private const float Spacing = 1.1f;
        private sealed class Actor { public GameObject Root; public TextMesh Label; public BattleWorldTarget Target; }
        private Vector3 MapPosition(Cell cell) => new Vector3((cell.X - (rules.Width - 1) * 0.5f) * Spacing, 0, (cell.Y - (rules.Height - 1) * 0.5f) * Spacing);
        private Actor CreateActor(EntityState entity, bool ghost)
        {
            var root = new GameObject($"{(ghost ? "Last Seen" : "Entity")} {entity.Id}");
            root.transform.SetParent(worldRoot.transform, false);
            root.transform.localRotation = Quaternion.Euler(0, entity.Kind == EntityKind.Base ? entity.Owner * 180 : entity.Owner == 0 ? 150 : -30, 0);
            Color color = ghost ? Ghost : entity.Owner == 0 ? TeamBlue : Red;
            var result = new Actor { Root = root };
            if (!ghost)
            {
                result.Target = root.AddComponent<BattleWorldTarget>();

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
                    Shape("Crystal", root.transform, PrimitiveType.Sphere, new Vector3(0.32f, 1.44f, 0.05f), new Vector3(0.19f, 0.3f, 0.19f), ghost ? Ghost : WorldCyan, !ghost);
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
            if (!ghost && entity.Kind == EntityKind.Unit)
            {
                var badge = new GameObject("Unit Number Badge", typeof(SpriteRenderer));
                badge.transform.SetParent(labelObject.transform, false);
                badge.transform.localPosition = Vector3.forward * 0.015f;
                badge.transform.localScale = Vector3.one * 0.55f;
                badge.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("PrototypeUI/badge");
                badge.GetComponent<SpriteRenderer>().color = entity.Owner == 0 ? WorldCyan : Red;
            }
            return result;
        }
        private GameObject Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool pickable)
        {
            var shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            if (type == PrimitiveType.Sphere) shape.GetComponent<MeshFilter>().sharedMesh = facetedMesh;
            shape.GetComponent<Renderer>().sharedMaterial = Surface(color);
            if (!pickable)
            {
                var collider = shape.GetComponent<Collider>();
                collider.enabled = false;
                UnityEngine.Object.DestroyImmediate(collider);
            }
            return shape;
        }
        private void BuildSurroundings()
        {
            Shape("Terrain Foundation", mapRoot.transform, PrimitiveType.Cube, new Vector3(0, -0.27f, 0), new Vector3(40, 0.35f, 40), Fog, false);
            for (int y = -5; y < rules.Height + 5; y++)
                for (int x = -5; x < rules.Width + 5; x++)
                {
                    if (x >= 0 && x < rules.Width && y >= 0 && y < rules.Height) continue;
                    var cell = new Cell(x, y);
                    Color terrain = (x * 13 + y * 7) % 3 == 0 ? new Color(0.13f, 0.17f, 0.1f) : new Color(0.15f, 0.17f, 0.16f);
                    Shape("Surrounding Tile", mapRoot.transform, PrimitiveType.Cube, MapPosition(cell) + Vector3.down * 0.08f, new Vector3(1.07f, 0.16f, 1.07f), terrain, false);
                    if ((x * x + y * y) % 4 != 0 || x == -1 || x == rules.Width || y == -1 || y == rules.Height) continue;
                    var point = MapPosition(cell);
                    Shape("Tree Trunk", mapRoot.transform, PrimitiveType.Cube, point + Vector3.up * 0.38f, new Vector3(0.13f, 0.75f, 0.13f), new Color(0.12f, 0.105f, 0.08f), false);
                    Shape("Tree Crown", mapRoot.transform, PrimitiveType.Sphere, point + Vector3.up * 0.85f, new Vector3(0.9f, 1.25f, 0.9f), new Color(0.105f, 0.19f, 0.13f), false);
                }
            foreach (int x in new[] { -1, rules.Width })
                for (int y = 0; y < rules.Height; y += 2)
                {
                    var point = MapPosition(new Cell(x, y));
                    Shape("Ruined Wall", mapRoot.transform, PrimitiveType.Cube, point + Vector3.up * 0.29f, new Vector3(0.4f, 0.58f, 1.3f), new Color(0.22f, 0.25f, 0.23f), false);
                    Shape("Wall Cap", mapRoot.transform, PrimitiveType.Cube, point + Vector3.up * 0.65f, new Vector3(0.55f, 0.2f, 0.5f), new Color(0.26f, 0.28f, 0.25f), false);
                }
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
    }
}

using System.Linq;
using TMPro;
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

        private sealed class Actor { public GameObject Root; public TMP_Text Label; public BattleWorldTarget Target; }

        private Actor CreateActor(EntityState entity, bool ghost)
        {
            GameObject root = new GameObject($"{(ghost ? "Last Seen" : "Entity")} {entity.Id}");

            root.transform.SetParent(worldRoot.transform, false);
            root.transform.localRotation = Quaternion.Euler(0, entity.Kind == EntityKind.Base ? entity.Owner * 180 : entity.Owner == 0 ? 150 : -30, 0);
            Color color = ghost ? Ghost : entity.Owner == 0 ? TeamBlue : Red;
            Actor result = new Actor
            {
                Root = root
            };

            if (!ghost)
            {
                result.Target = root.AddComponent<BattleWorldTarget>();
            }

            if (entity.Kind == EntityKind.Base)
            {
                Color stone = ghost ? Ghost : new Color(0.42f, 0.44f, 0.4f);

                Shape(
                    "Castle Keep",
                    root.transform,
                    PrimitiveType.Cube,
                    new Vector3(0, 0.5f, 0),
                    new Vector3(0.75f, 0.9f, 0.75f),
                    stone,
                    !ghost);

                foreach (float x in new[] { -0.36f, 0.36f })
                {
                    foreach (float z in new[] { -0.36f, 0.36f })
                    {
                        Shape(
                            "Turret",
                            root.transform,
                            PrimitiveType.Cube,
                            new Vector3(x, 0.62f, z),
                            new Vector3(0.28f, 1.2f, 0.28f),
                            stone,
                            !ghost);
                        Shape(
                            "Battlement",
                            root.transform,
                            PrimitiveType.Cube,
                            new Vector3(x, 1.23f, z),
                            new Vector3(0.34f, 0.14f, 0.34f),
                            stone,
                            !ghost);
                    }
                }

                Shape(
                    "Gate",
                    root.transform,
                    PrimitiveType.Cube,
                    new Vector3(0, 0.31f, -0.4f),
                    new Vector3(0.25f, 0.5f, 0.08f),
                    new Color(0.07f, 0.095f, 0.09f),
                    !ghost);
                Shape(
                    "Banner",
                    root.transform,
                    PrimitiveType.Cube,
                    new Vector3(-0.25f, 0.76f, -0.41f),
                    new Vector3(0.16f, 0.37f, 0.035f),
                    color,
                    !ghost);
                Shape(
                    "Banner",
                    root.transform,
                    PrimitiveType.Cube,
                    new Vector3(0.25f, 0.76f, -0.41f),
                    new Vector3(0.16f, 0.37f, 0.035f),
                    color,
                    !ghost);
            }
            else
            {
                int slot = entity.Id - entity.Owner * 4 - 1;
                Color clothing = ghost
                    ? Ghost
                    : slot == 0
                    ? new Color(0.57f, 0.62f, 0.64f)
                    : slot == 1 ? new Color(0.4f, 0.44f, 0.22f) : slot == 3 ? new Color(0.8f, 0.83f, 0.77f) : color;
                Color leather = ghost ? Ghost : new Color(0.23f, 0.16f, 0.11f);

                Shape(
                    "Tunic",
                    root.transform,
                    PrimitiveType.Cube,
                    new Vector3(0, 0.69f, 0),
                    new Vector3(0.36f, 0.45f, 0.27f),
                    clothing,
                    !ghost);
                Shape(
                    "Team Scarf",
                    root.transform,
                    PrimitiveType.Cube,
                    new Vector3(0, 0.9f, 0.05f),
                    new Vector3(0.39f, 0.09f, 0.3f),
                    color,
                    !ghost);

                foreach (float x in new[] { -0.12f, 0.12f })
                {
                    Shape(
                        "Leg",
                        root.transform,
                        PrimitiveType.Cube,
                        new Vector3(x, 0.33f, 0),
                        new Vector3(0.14f, 0.33f, 0.18f),
                        leather,
                        !ghost);
                    Shape(
                        "Boot",
                        root.transform,
                        PrimitiveType.Cube,
                        new Vector3(x, 0.13f, 0.05f),
                        new Vector3(0.17f, 0.14f, 0.26f),
                        leather,
                        !ghost);
                    Shape(
                        "Arm",
                        root.transform,
                        PrimitiveType.Cube,
                        new Vector3(x * 2, 0.68f, 0),
                        new Vector3(0.14f, 0.32f, 0.19f),
                        clothing,
                        !ghost);
                }

                Shape(
                    slot == 0 ? "Helmet" : "Hood",
                    root.transform,
                    PrimitiveType.Sphere,
                    new Vector3(0, 1.13f, 0),
                    new Vector3(0.49f, 0.48f, 0.45f),
                    clothing,
                    !ghost);
                Shape(
                    "Face",
                    root.transform,
                    PrimitiveType.Cube,
                    new Vector3(0, 1.1f, 0.18f),
                    new Vector3(0.23f, 0.23f, 0.08f),
                    ghost ? Ghost : slot == 0 ? new Color(0.05f, 0.075f, 0.085f) : new Color(0.72f, 0.51f, 0.34f),
                    !ghost);

                if (slot == 0)
                {
                    Shape(
                        "Shield",
                        root.transform,
                        PrimitiveType.Cube,
                        new Vector3(-0.32f, 0.63f, 0.17f),
                        new Vector3(0.25f, 0.44f, 0.1f),
                        color,
                        !ghost);
                    Shape(
                        "Sword",
                        root.transform,
                        PrimitiveType.Cube,
                        new Vector3(0.32f, 0.76f, 0.15f),
                        new Vector3(0.06f, 0.74f, 0.1f),
                        clothing,
                        !ghost);
                }
                else if (slot == 3)
                {
                    Shape(
                        "Staff",
                        root.transform,
                        PrimitiveType.Cylinder,
                        new Vector3(0.32f, 0.71f, 0.05f),
                        new Vector3(0.06f, 0.65f, 0.06f),
                        leather,
                        !ghost);
                    Shape(
                        "Crystal",
                        root.transform,
                        PrimitiveType.Sphere,
                        new Vector3(0.32f, 1.44f, 0.05f),
                        new Vector3(0.19f, 0.3f, 0.19f),
                        ghost ? Ghost : WorldCyan,
                        !ghost);
                }
                else
                {
                    Shape(
                        "Quiver",
                        root.transform,
                        PrimitiveType.Cube,
                        new Vector3(0, 0.84f, -0.22f),
                        new Vector3(0.19f, 0.58f, 0.18f),
                        leather,
                        !ghost);
                    Shape(
                        "Bow",
                        root.transform,
                        PrimitiveType.Cube,
                        new Vector3(0.32f, 0.72f, 0.17f),
                        new Vector3(0.06f, 0.65f, 0.08f),
                        leather,
                        !ghost);
                }
            }

            GameObject labelObject = new GameObject("World HP Label", typeof(TextMeshPro));

            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = Vector3.up * (entity.Kind == EntityKind.Base ? 1.9f : 1.6f);
            result.Label = labelObject.GetComponent<TextMeshPro>();
            result.Label.font = font;
            result.Label.fontSize = ghost || entity.Kind == EntityKind.Base ? 2.6f : 4;
            result.Label.rectTransform.sizeDelta = new Vector2(2, 1);
            result.Label.alignment = TextAlignmentOptions.Center;
            result.Label.isOrthographic = false;
            result.Label.textWrappingMode = TextWrappingModes.NoWrap;
            result.Label.raycastTarget = false;
            result.Label.color = ghost ? new Color(0.65f, 0.68f, 0.73f) : Color.white;

            if (!ghost && entity.Kind == EntityKind.Unit)
            {
                GameObject badge = new GameObject("Unit Number Badge", typeof(SpriteRenderer));

                badge.transform.SetParent(labelObject.transform, false);
                badge.transform.localPosition = Vector3.forward * 0.015f;
                badge.transform.localScale = Vector3.one * 0.55f;
                badge.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("PrototypeUI/badge");
                badge.GetComponent<SpriteRenderer>().color = entity.Owner == 0 ? WorldCyan : Red;
            }

            return result;
        }

        private GameObject Shape(
            string name,
            Transform parent,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool pickable)
        {
            GameObject shape = GameObject.CreatePrimitive(type);

            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;

            if (type == PrimitiveType.Sphere)
            {
                shape.GetComponent<MeshFilter>().sharedMesh = facetedMesh;
            }

            shape.GetComponent<Renderer>().sharedMaterial = Surface(color);

            if (!pickable)
            {
                Collider collider = shape.GetComponent<Collider>();

                collider.enabled = false;
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return shape;
        }

        private static Mesh FacetedMesh()
        {
            Vector3[] axes = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            int[] faces = new[] { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
            Mesh mesh = new Mesh
            {
                name = "Prototype Faceted Mesh"
            };

            mesh.vertices = faces.Select(index => axes[index] * 0.5f).ToArray();
            mesh.triangles = Enumerable.Range(0, faces.Length).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}

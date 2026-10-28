using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine;

namespace TurnZero.Editor
{
    internal sealed class LocalPlaytestBuilder
    {
        private readonly Color cyan = new Color(0.15f, 0.82f, 0.75f);
        private readonly Color amber = new Color(0.96f, 0.7f, 0.32f);
        private BattleConfiguration configuration;
        private GameRules rules;
        private GameObject root;
        private BattleSession session;
        private TMP_FontAsset font;

        internal void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            configuration = LocalGameContent.Create();
            rules = configuration.CreateRules();

            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/External/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.67f, 0.75f);

            root = new GameObject("Local Battle");
            session = root.AddComponent<BattleSession>();

            LocalBattleWorld world = BuildWorld();
            GameObject cameraObject = new GameObject("Battle Camera", typeof(Camera), typeof(AudioListener), typeof(LocalBattleCamera));

            cameraObject.transform.SetParent(root.transform, false);

            Camera camera = cameraObject.GetComponent<Camera>();

            camera.tag = "MainCamera";
            camera.fieldOfView = 36;
            camera.nearClipPlane = 0.1f;

            camera.backgroundColor = new Color(0.015f, 0.025f, 0.04f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.farClipPlane = 200;

            camera.transform.rotation = Quaternion.Euler(configuration.CameraPitch, 0, 0);
            camera.transform.position = -camera.transform.forward * 48;

            LocalBattleCamera rig = cameraObject.GetComponent<LocalBattleCamera>();

            Wire(rig, ("cameraView", camera));

            LocalBattleHud hud = new LocalHudBuilder(root, configuration, rules, font).Build();

            GameObject inputObject = new GameObject("Battle Input", typeof(BattleInput), typeof(EventSystem), typeof(InputSystemUIInputModule));

            inputObject.transform.SetParent(root.transform, false);

            InputSystemUIInputModule module = inputObject.GetComponent<InputSystemUIInputModule>();

            module.AssignDefaultActions();
            module.submit = null;
            module.cancel = null;
            module.move = null;

            GameObject audioObject = new GameObject("Battle Audio", typeof(AudioSource), typeof(BattleAudio));

            audioObject.transform.SetParent(root.transform, false);

            AudioSource audio = audioObject.GetComponent<AudioSource>();

            audio.playOnAwake = false;

            Wire(
                audioObject.GetComponent<BattleAudio>(),
                ("source", audio),
                ("turnStart", LocalGameContent.TurnSound()));

            GameObject sunObject = new GameObject("Soft Key Light", typeof(Light));

            sunObject.transform.SetParent(root.transform, false);

            sunObject.transform.rotation = Quaternion.Euler(48, -34, 0);
            Light light = sunObject.GetComponent<Light>();

            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;

            BattleBootstrap bootstrap = root.AddComponent<BattleBootstrap>();
            BattleConfiguration storedConfiguration = AssetDatabase.LoadAssetAtPath<BattleConfiguration>(GameDefinitions.AssetRoot + "Data/LocalBattleConfiguration.asset");

            Wire(
                bootstrap,
                ("configuration", storedConfiguration),
                ("session", session),
                ("world", world),
                ("hud", hud),
                ("cameraRig", rig),
                ("input", inputObject.GetComponent<BattleInput>()),
                ("audioPlayer", audioObject.GetComponent<BattleAudio>()));

            Canvas.ForceUpdateCanvases();
            AssetDatabase.SaveAssets();

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                root,
                GameDefinitions.AssetRoot + "Prefabs/LocalBattleSession.prefab",
                InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), GameDefinitions.LocalBattleScenePath);

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != GameDefinitions.LocalBattleScenePath).ToList();

            scenes.Insert(0, new EditorBuildSettingsScene(GameDefinitions.LocalBattleScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        private LocalBattleWorld BuildWorld()
        {
            GameObject worldObject = new GameObject("World", typeof(LocalBattleWorld));

            worldObject.transform.SetParent(root.transform, false);

            GameObject board = new GameObject("Field / 12 x 20", typeof(LocalFieldMap));

            board.transform.SetParent(worldObject.transform, false);

            GameObject template = new GameObject("Battle tile", typeof(LocalTileView));
            GameObject floor = Shape(
                "Surface",
                template.transform,
                PrimitiveType.Cube,
                new Vector3(0, -0.08f, 0),
                new Vector3(2, 0.16f, 2),
                Color.white,
                true);

            GameObject outline = Shape(
                "Range outline",
                template.transform,
                PrimitiveType.Cube,
                new Vector3(0, -0.10f, 0),
                new Vector3(2.14f, 0.08f, 2.14f),
                cyan,
                false);

            outline.SetActive(false);

            Wire(
                template.GetComponent<LocalTileView>(),
                ("surface", floor.GetComponent<Renderer>()),
                ("outline", outline.GetComponent<Renderer>()));

            GameObject tilePrefab = PrefabUtility.SaveAsPrefabAsset(template, GameDefinitions.AssetRoot + "Prefabs/World/Tile.prefab");

            UnityEngine.Object.DestroyImmediate(template);

            LocalFieldMap map = LocalFieldMapBuilder.Build(board, tilePrefab, configuration.Fields[0], rules);

            foreach (int row in new[] { rules.HomeHeight, rules.HomeHeight + rules.NeutralHeight })
            {
                Shape(
                    "Field boundary",
                    board.transform,
                    PrimitiveType.Cube,
                    new Vector3(0, 0.01f, (row - rules.Height * 0.5f) * configuration.Fields[0].TileSpacing),
                    new Vector3(rules.Width * configuration.Fields[0].TileSpacing, 0.025f, 0.045f),
                    amber,
                    false);
            }

            GameObject actorRoot = new GameObject("Characters and bases");

            actorRoot.transform.SetParent(worldObject.transform, false);

            List<LocalActorView> actors = new List<LocalActorView>();
            List<LocalActorView> ghosts = new List<LocalActorView>();

            for (int owner = 0; owner < GameDefinitions.PlayerCount; owner++)
            {
                for (int slot = 0; slot < GameDefinitions.CharactersPerTeam; slot++)
                {
                    actors.Add(Actor(
                        actorRoot.transform,
                        owner * GameDefinitions.CharactersPerTeam + slot + 1,
                        configuration.Characters[slot].Model,
                        false));
                    ghosts.Add(Actor(
                        actorRoot.transform,
                        owner * GameDefinitions.CharactersPerTeam + slot + 1,
                        configuration.Characters[slot].Model,
                        true));
                }

                actors.Add(Actor(actorRoot.transform, GameDefinitions.FirstBaseId + owner, configuration.BaseModel, false));
            }

            GameObject dynamic = new GameObject("Items, structures and effects");

            dynamic.transform.SetParent(worldObject.transform, false);

            GameObject path = new GameObject("Planned path", typeof(LineRenderer));

            path.transform.SetParent(worldObject.transform, false);

            LineRenderer lineRenderer = path.GetComponent<LineRenderer>();

            lineRenderer.sharedMaterial = Material(cyan, true);
            lineRenderer.startWidth = lineRenderer.endWidth = 0.055f;
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            path.SetActive(false);

            LocalBattleWorld world = worldObject.GetComponent<LocalBattleWorld>();

            WireWorldViews(
                world,
                ("map", map),
                ("actors", actors.ToArray()),
                ("ghosts", ghosts.ToArray()),
                ("pathLine", lineRenderer),
                ("dynamicRoot", dynamic.transform),
                ("itemPrefab", MarkerPrefab("Craft material", PrimitiveType.Cube, new Vector3(0.25f, 0.25f, 0.25f), amber, true)),
                ("structurePrefab", MarkerPrefab("Structure", PrimitiveType.Cylinder, new Vector3(0.45f, 0.65f, 0.45f), cyan, true)),
                ("impactPrefab", MarkerPrefab(
                    "Impact",
                    PrimitiveType.Cylinder,
                    new Vector3(0.62f, 0.025f, 0.62f),
                    new Color(1, 0.42f, 0.16f),
                    false)),
                ("projectilePrefab", MarkerPrefab("Projectile", PrimitiveType.Sphere, Vector3.one * 0.12f, amber, false)));

            return world;
        }

        internal static void WireWorldViews(LocalBattleWorld world, params (string, object)[] references)
        {
            Dictionary<string, object> fields = references.ToDictionary(pair => pair.Item1, pair => pair.Item2);
            LocalFieldMap map = (LocalFieldMap)fields["map"];
            Array actorReferences = (Array)fields["actors"];
            LocalActorView first = (LocalActorView)actorReferences.GetValue(0);
            LineRenderer line = (LineRenderer)fields["pathLine"];
            Transform dynamicRoot = (Transform)fields["dynamicRoot"];

            T Attach<T>(GameObject owner, params (string, object)[] values) where T : Component
            {
                T component = owner.GetComponent<T>();

                if (component == null)
                {
                    component = owner.AddComponent<T>();
                }

                Wire(component, values);

                return component;
            }

            GameObject Child(string name)
            {
                Transform child = dynamicRoot.Find(name);

                if (child != null)
                {
                    return child.gameObject;
                }

                GameObject owner = new GameObject(name);

                owner.transform.SetParent(dynamicRoot, false);

                return owner;
            }

            LocalTileHighlightsView highlights = Attach<LocalTileHighlightsView>(
                map.gameObject,
                ("map", map));
            LocalActorsView actors = Attach<LocalActorsView>(
                first.transform.parent.gameObject,
                ("map", map),
                ("actors", fields["actors"]),
                ("ghosts", fields["ghosts"]));
            LocalWorldMarkersView markers = Attach<LocalWorldMarkersView>(
                Child("Field markers"),
                ("map", map),
                ("itemPrefab", fields["itemPrefab"]),
                ("structurePrefab", fields["structurePrefab"]),
                ("impactPrefab", fields["impactPrefab"]));
            LocalPathView path = Attach<LocalPathView>(
                line.gameObject,
                ("map", map),
                ("pathLine", line));
            LocalBattleEffectsView effects = Attach<LocalBattleEffectsView>(
                Child("Battle effects"),
                ("map", map),
                ("impactPrefab", fields["impactPrefab"]),
                ("projectilePrefab", fields["projectilePrefab"]));

            Wire(
                world,
                ("map", map),
                ("highlights", highlights),
                ("actors", actors),
                ("markers", markers),
                ("path", path),
                ("effects", effects));
        }

        private LocalActorView Actor(Transform parent, int id, GameObject modelPrefab, bool ghost)
        {
            GameObject actorObject = new GameObject($"{(ghost ? "Last seen" : "Entity")} {id}", typeof(LocalActorView));

            actorObject.transform.SetParent(parent, false);

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, actorObject.transform);

            model.name = "Model";

            model.transform.localRotation = Quaternion.Euler(
                0,
                id >= GameDefinitions.FirstBaseId
                ? (id - GameDefinitions.FirstBaseId) * 180
                : id <= GameDefinitions.CharactersPerTeam ? 150 : -30,
                0);

            if (ghost)
            {
                foreach (Collider collider in model.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
            }

            GameObject selection = Shape(
                "Selection",
                actorObject.transform,
                PrimitiveType.Cylinder,
                new Vector3(0, 0.035f, 0),
                new Vector3(0.6f, 0.015f, 0.6f),
                cyan,
                false);

            selection.SetActive(false);

            GameObject labelObject = new GameObject("Unit label", typeof(TextMeshPro));

            labelObject.transform.SetParent(actorObject.transform, false);
            labelObject.transform.localPosition = Vector3.up * (id >= GameDefinitions.FirstBaseId ? 1.9f : 1.7f);

            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();

            label.font = font;
            label.fontSize = ghost ? 2.2f : 3;
            label.alignment = TextAlignmentOptions.Center;

            label.rectTransform.sizeDelta = new Vector2(2.8f, 0.75f);
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.transform.rotation = Quaternion.Euler(56, 0, 0);

            LocalActorView actor = actorObject.GetComponent<LocalActorView>();

            Wire(
                actor,
                ("entityId", id),
                ("label", label),
                ("selection", selection.GetComponent<Renderer>()),
                ("modelRoot", model),
                ("modelPrefab", modelPrefab),
                ("ghostModel", ghost),
                ("modelParts", model.GetComponentsInChildren<MeshRenderer>()),
                ("teamParts", model.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.name.Contains("Scarf") || r.name.Contains("Banner"))
                .ToArray()));
            int owner = id >= GameDefinitions.FirstBaseId
                ? id - GameDefinitions.FirstBaseId
                : (id - 1) / GameDefinitions.CharactersPerTeam;
            int slot = (id - 1) % GameDefinitions.CharactersPerTeam;
            GridCell cell = new GridCell(rules.Width / 2 + (id >= GameDefinitions.FirstBaseId ? 0 : slot % 3 - 1), owner == 0
                ? rules.HomeHeight - 3 + (id >= GameDefinitions.FirstBaseId ? 0 : slot / 3 + 1)
                : rules.HomeHeight + rules.NeutralHeight + 2 - (id >= GameDefinitions.FirstBaseId ? 0 : slot / 3 + 1));

            actor.Render(
                !ghost,
                cell,
                Position(cell),
                id >= GameDefinitions.FirstBaseId
                ? "30 / 30"
                : $"{slot + 1}  {configuration.Characters[slot].Values.Health} HP",
                LocalBattleWorld.Team(owner),
                false);

            return actor;
        }

        private GameObject MarkerPrefab(string name, PrimitiveType type, Vector3 scale, Color color, bool label)
        {
            string path = GameDefinitions.AssetRoot + $"Prefabs/World/{name.Replace(" ", "")}.prefab";

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (existing != null)
            {
                return existing;
            }

            GameObject marker = new GameObject(name);

            Shape("Visual", marker.transform, type, Vector3.up * scale.y, scale, color, false);

            if (label)
            {
                GameObject textObject = new GameObject("Label", typeof(TextMeshPro));

                textObject.transform.SetParent(marker.transform, false);
                textObject.transform.localPosition = Vector3.up * (scale.y * 2 + 0.45f);

                TextMeshPro text = textObject.GetComponent<TextMeshPro>();

                text.font = font;
                text.fontSize = 2;
                text.alignment = TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(2.5f, 1);
                text.raycastTarget = false;
            }

            existing = PrefabUtility.SaveAsPrefabAsset(marker, path);
            UnityEngine.Object.DestroyImmediate(marker);

            return existing;
        }

        private Vector3 Position(GridCell cell)
        {
            return new Vector3(
                (cell.X - (rules.Width - 1) * 0.5f) * configuration.Fields[0].TileSpacing,
                0,
                (cell.Y - (rules.Height - 1) * 0.5f) * configuration.Fields[0].TileSpacing);
        }

        private GameObject Shape(
            string name,
            Transform parent,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool collider)
        {
            GameObject shape = GameObject.CreatePrimitive(type);

            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;

            shape.GetComponent<Renderer>().sharedMaterial = Material(color);

            if (!collider)
            {
                UnityEngine.Object.DestroyImmediate(shape.GetComponent<Collider>());
            }

            return shape;
        }

        private Material Material(Color color, bool unlit = false)
        {
            string path = GameDefinitions.AssetRoot + $"Art/Materials/{(unlit ? "Unlit" : "Surface")}-{ColorUtility.ToHtmlStringRGBA(color)}.mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);

            AssetDatabase.CreateAsset(material, path);

            return material;
        }

        internal static void Wire(UnityEngine.Object target, params (string, object)[] values)
        {
            SerializedObject serialized = new SerializedObject(target);

            foreach ((string name, object value) in values)
            {
                SerializedProperty property = serialized.FindProperty(name);

                if (property == null)
                {
                    throw new InvalidOperationException($"Missing serialized field {target.GetType().Name}.{name}");
                }

                if (value is Array array)
                {
                    property.arraySize = array.Length;

                    for (int i = 0; i < array.Length; i++)
                    {
                        property.GetArrayElementAtIndex(i).objectReferenceValue = (UnityEngine.Object)array.GetValue(i);
                    }
                }
                else if (value is int number)
                {
                    property.intValue = number;
                }
                else if (value is bool boolean)
                {
                    property.boolValue = boolean;
                }
                else
                {
                    UnityEngine.Object reference = (UnityEngine.Object)value;

                    if (reference == null)
                    {
                        throw new InvalidOperationException($"Cannot wire a missing object: {target.GetType().Name}.{name}");
                    }

                    property.objectReferenceValue = reference;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

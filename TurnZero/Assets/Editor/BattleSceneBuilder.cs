using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TurnZero.Editor
{
    // Explicit authoring tool only. These factories are never included in a player build.
    public static class BattleSceneBuilder
    {
        [MenuItem("TurnZero/Rebuild Prototype Scene and Prefabs")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Rebuild prototype assets",
                "This replaces the prototype prefabs and the Battle scene layout. Save custom prefab changes separately before rebuilding.", "Rebuild", "Cancel")) return;
            BuildBatch();
        }

        public static void BuildBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            new BattlePrefabBuilder().Build();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("TurnZero: authored scene and independent prefabs saved.");
        }
    }

    internal sealed partial class BattlePrefabBuilder
    {
        private readonly BattleRules rules = new BattleRules();
        private BattleRules activeRules => rules;
        private GameObject session, worldRoot, mapRoot;
        private Material surface;
        private Mesh facetedMesh;
        private const string Prefabs = "Assets/Prefabs/";
        private const string Art = "Assets/Art/Prototype/";
        private static readonly string[] ModelNames = { "Guardian", "Scout", "Ranger", "Support", "Base" };

        internal void Build()
        {
            Directory.CreateDirectory(Prefabs + "Map");
            Directory.CreateDirectory(Prefabs + "Characters");
            Directory.CreateDirectory(Prefabs + "UI");
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            surface = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PrototypeSurface.mat");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            facetedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "FacetedMesh.asset");
            if (facetedMesh == null) { facetedMesh = FacetedMesh(); AssetDatabase.CreateAsset(facetedMesh, Art + "FacetedMesh.asset"); }
            BakePortraits();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var oldController = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Battle Prototype" || g.name == "Battle Session");
            // Capture configured combat values when rebuilding an already authored session.
            var existing = oldController == null ? null : oldController.GetComponent<BattleController>();
            if (existing != null)
            {
                var json = JsonUtility.ToJson(existing);
                var data = JsonUtility.FromJson<RuleContainer>(json);
                if (data?.rules != null) CopyRules(data.rules);
            }
            var camera = Camera.main;
            if (camera == null) camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.transform.SetParent(null);
            if (oldController != null) UnityEngine.Object.DestroyImmediate(oldController);
            session = new GameObject("Battle Session");
            var controller = session.AddComponent<BattleController>();
            worldRoot = new GameObject("Battle World");
            worldRoot.transform.SetParent(session.transform, false);
            mapRoot = new GameObject("Fixed Battle Map");
            mapRoot.transform.SetParent(worldRoot.transform, false);
            var map = BuildMap();
            var world = worldRoot.AddComponent<BattleWorldView>();
            var actors = BuildActors(map);
            var markers = BuildMarkers();
            var line = new GameObject("Command Path", typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(worldRoot.transform, false);
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = 0.075f;
            line.positionCount = 5;
            line.gameObject.SetActive(false);
            Wire(world, ("map", map), ("actors", actors), ("lastSeen", markers), ("commandPath", line),
                ("movePathMaterial", Surface(WorldCyan)), ("attackPathMaterial", Surface(Red)));
            Save(worldRoot, "BattleWorld.prefab");

            BuildUI();
            var hud = WireUI();
            var rigObject = new GameObject("Battle Camera Rig");
            rigObject.transform.SetParent(session.transform, false);
            camera.transform.SetParent(rigObject.transform, false);
            camera.tag = "MainCamera";
            camera.orthographic = false;
            camera.fieldOfView = 36;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
            if (camera.GetComponent<PhysicsRaycaster>() == null) camera.gameObject.AddComponent<PhysicsRaycaster>();
            var rig = rigObject.AddComponent<BattleCameraRig>();
            Wire(rig, ("cameraView", camera));
            Save(rigObject, "BattleCameraRig.prefab");
            // Composition references are saved as overrides on the session, not external scene references in child assets.
            Wire(rig, ("map", map), ("layout", canvasRoot.GetComponent<BattleHudLayout>()));
            var events = new GameObject("Battle EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(session.transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Save(events, "BattleInput.prefab");
            JsonUtility.FromJsonOverwrite("{\"rules\":" + JsonUtility.ToJson(rules) + "}", controller);
            Wire(controller, ("world", world), ("hud", hud), ("cameraRig", rig));
            Preview(hud, map);
            Canvas.ForceUpdateCanvases();
            rig.Reframe();
            Save(session, "BattleSession.prefab");
            Selection.activeGameObject = session;
        }

        [Serializable] private sealed class RuleContainer { public BattleRules rules; }
        private void CopyRules(BattleRules source)
        {
            foreach (var field in typeof(BattleRules).GetFields()) field.SetValue(rules, field.GetValue(source));
        }

        private BattleMapView BuildMap()
        {
            BuildSurroundings();
            var tiles = new BattleTileView[rules.Width * rules.Height];
            var tileRoot = new GameObject("Tile", typeof(BattleWorldTarget), typeof(BattleTileView));
            var ground = Shape("Surface", tileRoot.transform, PrimitiveType.Cube, Vector3.down * 0.08f,
                new Vector3(1, 0.16f, 1), Color.white, true);
            var border = Shape("Selection Border", tileRoot.transform, PrimitiveType.Cube, Vector3.down * 0.06f,
                new Vector3(1.07f, 0.08f, 1.07f), WorldCyan, false);
            border.SetActive(false);
            Wire(tileRoot.GetComponent<BattleTileView>(), ("surface", ground.GetComponent<Renderer>()),
                ("border", border), ("target", tileRoot.GetComponent<BattleWorldTarget>()), ("terrainColor", new Color(0.62f, 0.58f, 0.49f)));
            var tilePrefab = PrefabUtility.SaveAsPrefabAsset(tileRoot, Prefabs + "Map/BattleTile.prefab");
            UnityEngine.Object.DestroyImmediate(tileRoot);
            for (int y = 0; y < rules.Height; y++)
                for (int x = 0; x < rules.Width; x++)
                {
                    var cell = new Cell(x, y);
                    var tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, mapRoot.transform);
                    tile.name = $"Tile {x},{y}";
                    tile.transform.localPosition = MapPosition(cell);
                    var target = tile.GetComponent<BattleWorldTarget>();
                    target.Cell = cell;
                    EditorUtility.SetDirty(target);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    Color terrain = (x * 13 + y * 7) % 5 < 2 ? new Color(0.44f, 0.53f, 0.25f) :
                        (x + y) % 2 == 0 ? new Color(0.62f, 0.58f, 0.49f) : new Color(0.53f, 0.52f, 0.45f);
                    tiles[y * rules.Width + x] = tile.GetComponent<BattleTileView>();
                    Wire(tiles[y * rules.Width + x], ("terrainColor", terrain));
                    ground = tile.transform.Find("Surface").gameObject;
                    ground.GetComponent<Renderer>().sharedMaterial = Surface(terrain);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(ground.GetComponent<Renderer>());
                }
            var map = mapRoot.AddComponent<BattleMapView>();
            Wire(map, ("width", rules.Width), ("height", rules.Height), ("spacing", Spacing), ("tiles", tiles));
            Save(mapRoot, "Map/FixedBattleMap.prefab");
            return map;
        }

        private BattleActorView[] BuildActors(BattleMapView map)
        {
            var result = new BattleActorView[10];
            for (int owner = 0; owner < 2; owner++)
                for (int slot = 0; slot < 5; slot++)
                {
                    int id = slot == 4 ? 101 + owner : owner * 4 + slot + 1;
                    EntityKind kind = slot == 4 ? EntityKind.Base : EntityKind.Unit;
                    var entity = new EntityState { Id = id, Owner = owner, Kind = kind };
                    GameObject root;
                    if (owner == 0)
                    {
                        var built = CreateActor(entity, false);
                        root = built.Root;
                        var view = root.AddComponent<BattleActorView>();
                        Wire(view, ("entityId", id), ("target", built.Target), ("label", built.Label));
                        Save(root, "Characters/" + ModelNames[slot] + ".prefab");
                    }
                    else
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Characters/" + ModelNames[slot] + ".prefab");
                        root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, worldRoot.transform);
                        root.name = "Entity " + id;
                        root.transform.localRotation = Quaternion.Euler(0, kind == EntityKind.Base ? 180 : -30, 0);
                        Wire(root.GetComponent<BattleActorView>(), ("entityId", id));
                        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                            if (renderer.sharedMaterial == Surface(TeamBlue)) { renderer.sharedMaterial = Surface(Red); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
                        foreach (var badge in root.GetComponentsInChildren<SpriteRenderer>()) { badge.color = Red; PrefabUtility.RecordPrefabInstancePropertyModifications(badge); }
                    }
                    result[owner * 5 + slot] = root.GetComponent<BattleActorView>();
                }
            return result;
        }

        private BattleLastSeenView[] BuildMarkers()
        {
            var root = new GameObject("Last Seen Marker");
            var marker = root.AddComponent<BattleLastSeenView>();
            var textObject = new GameObject("World HP Label", typeof(TextMesh));
            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = Vector3.up * 0.4f;
            var label = textObject.GetComponent<TextMesh>();
            label.font = font; label.fontSize = 40; label.characterSize = 0.065f;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.color = new Color(0.65f, 0.68f, 0.73f);
            label.GetComponent<Renderer>().sharedMaterial = font.material;
            var outline = new GameObject("Memory Outline", typeof(LineRenderer)).GetComponent<LineRenderer>();
            outline.transform.SetParent(root.transform, false);
            outline.useWorldSpace = false; outline.loop = true; outline.positionCount = 4;
            outline.startWidth = outline.endWidth = 0.035f; outline.sharedMaterial = Surface(Ghost);
            outline.SetPositions(new[] { new Vector3(-0.4f,0.08f,-0.4f),new Vector3(-0.4f,0.08f,0.4f),new Vector3(0.4f,0.08f,0.4f),new Vector3(0.4f,0.08f,-0.4f) });
            Wire(marker, ("label", label));
            root.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "LastSeenMarker.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            var result = new BattleLastSeenView[10];
            int[] ids = { 1, 2, 3, 4, 5, 6, 7, 8, 101, 102 };
            for (int i = 0; i < ids.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, worldRoot.transform);
                instance.name = "Last Seen " + ids[i];
                result[i] = instance.GetComponent<BattleLastSeenView>();
                Wire(result[i], ("entityId", ids[i]));
            }
            return result;
        }

        private BattleHudView WireUI()
        {
            var top = headerPanel.gameObject.AddComponent<BattleTopBarView>();
            Matching(top, "phaseText", "timerText", "ownBaseText", "enemyBaseText", "ownBaseBar", "enemyBaseBar");
            Save(headerPanel.gameObject, "UI/BattleTopBar.prefab");
            var commands = controlsPanel.gameObject.AddComponent<BattleCommandPanelView>();
            Matching(commands, "selectionText", "selectedHealthText", "equipmentText", "detailsText", "orderTargetText", "lockText",
                "selectedPortrait", "selectedHealth", "equipmentIcon", "previewArrow", "previewCells", "moveButton", "attackButton", "waitButton", "lockButton");
            Save(controlsPanel.gameObject, "UI/BattleCommandPanel.prefab");
            var cards = new BattleUnitCardView[4];
            for (int i = 0; i < 4; i++)
            {
                cards[i] = unitButtons[i].gameObject.AddComponent<BattleUnitCardView>();
                Wire(cards[i], ("slot", i), ("roleName", Roles[i]), ("weaponName", Weapons[i]), ("portrait", portraits[i]),
                    ("weaponIcon", Resources.Load<Sprite>("PrototypeUI/" + (i == 2 ? "bow" : i == 3 ? "item" : "blade"))),
                    ("moveIcon", Resources.Load<Sprite>("PrototypeUI/move")), ("attackIcon", Resources.Load<Sprite>("PrototypeUI/attack")),
                    ("waitIcon", Resources.Load<Sprite>("PrototypeUI/wait")), ("unitButton", unitButtons[i]), ("unitLabel", unitLabels[i]),
                    ("unitHealthText", unitHealthText[i]), ("unitOrderText", unitOrderText[i]), ("unitHealth", unitHealth[i]),
                    ("unitOrderPanel", unitOrderPanels[i]), ("unitOrderIcon", unitOrderIcons[i]));
                Save(unitButtons[i].gameObject, "UI/" + ModelNames[i] + "Card.prefab");
            }
            Save(teamPanel.gameObject, "UI/BattleRoster.prefab");
            var ap = apPanel.gameObject.AddComponent<BattleApPanelView>();
            Matching(ap, "apText", "reservedText", "apPips");
            Save(apPanel.gameObject, "UI/BattleApPanel.prefab");
            var bar = toolbar.gameObject.AddComponent<BattleToolbarView>();
            Wire(bar, ("switchText", switchText), ("resolveButton", resolveButton),
                ("switchButton", toolbar.Find("Switch Player").GetComponent<Button>()), ("rotateButton", toolbar.Find("Rotate Camera").GetComponent<Button>()),
                ("zoomInButton", toolbar.Find("Zoom In").GetComponent<Button>()), ("zoomOutButton", toolbar.Find("Zoom Out").GetComponent<Button>()),
                ("exportButton", toolbar.Find("Export Recording").GetComponent<Button>()), ("restartButton", toolbar.Find("Restart").GetComponent<Button>()));
            Save(toolbar.gameObject, "UI/BattleToolbar.prefab");
            var hud = canvasRoot.AddComponent<BattleHudView>();
            Wire(hud, ("topBar", top), ("commands", commands), ("toolbar", bar), ("apPanel", ap), ("cards", cards),
                ("messageText", messageText), ("logText", logText));
            var layout = canvasRoot.AddComponent<BattleHudLayout>();
            Matching(layout, "safeArea", "headerPanel", "boardPanel", "controlsPanel", "teamPanel", "apPanel", "toolbar", "helpPanel");
            Wire(layout, ("canvasRoot", canvasRoot.GetComponent<Canvas>()), ("cards", unitButtons.Select(b => b.GetComponent<RectTransform>()).ToArray()));
            Save(canvasRoot, "UI/BattleHud.prefab");
            return hud;
        }

        private void Preview(BattleHudView hud, BattleMapView map)
        {
            var view = new PlayerView { Player = 0, Turn = 1, Phase = MatchPhase.Planning, AP = rules.InitialAP, SecondsLeft = rules.PlanningSeconds };
            var cells = new[] { new Cell(3,3),new Cell(4,2),new Cell(3,1),new Cell(2,2),new Cell(3,2) };
            foreach (var actor in worldRoot.GetComponentsInChildren<BattleActorView>())
            {
                int id = actor.EntityId, owner = id == 102 || id >= 5 && id <= 8 ? 1 : 0;
                bool isBase = id > 100; int slot = isBase ? 4 : id - owner * 4 - 1;
                var cell = cells[slot]; if (owner == 1) cell = new Cell(rules.Width - 1 - cell.X, rules.Height - 1 - cell.Y);
                var entity = new EntityState { Id = id, Owner = owner, Kind = isBase ? EntityKind.Base : EntityKind.Unit,
                    Position = cell, Health = isBase ? rules.BaseHealth : rules.UnitHealth, MaximumHealth = isBase ? rules.BaseHealth : rules.UnitHealth };
                actor.Render(entity, map);
                view.Entities.Add(entity);
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor.GetComponent<BattleWorldTarget>());
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor.GetComponentInChildren<TextMesh>());
            }
            hud.Render(view, rules, 2, ActionKind.Move);
            messageText.text = "Scene preview. Play to choose bases and begin a match.";
            phaseText.text = "EDITOR PREVIEW";
        }

        private void BakePortraits()
        {
            var atlas = Resources.Load<Texture2D>("PrototypeUI/portrait-atlas");
            for (int i = 0; i < 4; i++)
            {
                portraits[i] = PortraitAsset(atlas, i, false);
                cardPortraits[i] = PortraitAsset(atlas, i, true);
            }
        }
        private static Sprite PortraitAsset(Texture2D atlas, int slot, bool cropped)
        {
            string path = Art + ModelNames[slot] + (cropped ? "CardPortrait" : "Portrait") + ".asset";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            var rect = new Rect(((slot % 2) + (cropped ? 0.18f : 0)) * atlas.width / 2, (1 - slot / 2) * atlas.height / 2,
                atlas.width * (cropped ? 0.32f : 0.5f), atlas.height / 2);
            sprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), 100);
            sprite.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(sprite, path);
            return sprite;
        }

        private Material Surface(Color color)
        {
            string path = Art + "Surface-" + ColorUtility.ToHtmlStringRGBA(color) + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(surface) { color = color, enableInstancing = true };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static void Save(GameObject root, string relativePath) =>
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, Prefabs + relativePath, InteractionMode.AutomatedAction);

        private void Matching(Component target, params string[] fields)
        {
            foreach (string field in fields)
                Wire(target, (field, GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this)));
        }

        private static void Wire(Component target, params (string name, object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var pair in values) Assign(serialized.FindProperty(pair.name), pair.value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }
        private static void Assign(SerializedProperty property, object value)
        {
            if (property == null) throw new InvalidOperationException("Missing serialized authoring field.");
            if (value is Array array)
            {
                property.arraySize = array.Length;
                for (int i = 0; i < array.Length; i++) Assign(property.GetArrayElementAtIndex(i), array.GetValue(i));
            }
            else if (value is UnityEngine.Object asset) property.objectReferenceValue = asset;
            else if (value is int integer) property.intValue = integer;
            else if (value is float number) property.floatValue = number;
            else if (value is string text) property.stringValue = text;
            else if (value is Color color) property.colorValue = color;
            else throw new ArgumentException("Unsupported authoring field value.");
        }
    }
}

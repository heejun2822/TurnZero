using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
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
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Rebuild prototype assets",
                "This replaces the prototype prefabs and the Battle scene layout. Save custom prefab changes separately before rebuilding.",
                "Rebuild",
                "Cancel"))
            {
                return;
            }

            BuildBatch();
        }

        public static void BuildBatch()
        {
            EditorSceneManager.OpenScene("Assets/ProjectFiles/Prototype/Scenes/Battle.unity");
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

        private GameObject session;
        private GameObject worldRoot;
        private GameObject mapRoot;
        private Material surface;
        private Mesh facetedMesh;
        private const string Prefabs = "Assets/ProjectFiles/Prototype/Prefabs/";
        private const string Art = "Assets/ProjectFiles/Art/Prototype/";
        private static readonly string[] ModelNames = { "Guardian", "Scout", "Ranger", "Support", "Base" };

        internal void Build()
        {
            Directory.CreateDirectory(Prefabs + "Map");
            Directory.CreateDirectory(Prefabs + "Characters");
            Directory.CreateDirectory(Prefabs + "UI");
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            surface = AssetDatabase.LoadAssetAtPath<Material>("Assets/ProjectFiles/Prototype/Materials/PrototypeSurface.mat");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/External/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            facetedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "FacetedMesh.asset");

            if (facetedMesh == null)
            {
                facetedMesh = FacetedMesh();
                AssetDatabase.CreateAsset(facetedMesh, Art + "FacetedMesh.asset");
            }

            BakePortraits();
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject oldController = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Battle Prototype" || g.name == "Battle Session");
            // Capture configured combat values when rebuilding an already authored session.
            BattleController existing = oldController == null ? null : oldController.GetComponent<BattleController>();

            if (existing != null)
            {
                string json = JsonUtility.ToJson(existing);
                RuleContainer data = JsonUtility.FromJson<RuleContainer>(json);

                if (data?.rules != null)
                {
                    CopyRules(data.rules);
                }
            }

            Camera camera = Camera.main;

            if (camera == null)
            {
                camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            }

            camera.transform.SetParent(null);

            if (oldController != null)
            {
                UnityEngine.Object.DestroyImmediate(oldController);
            }

            session = new GameObject("Battle Session");
            BattleController controller = session.AddComponent<BattleController>();

            worldRoot = new GameObject("Battle World");
            worldRoot.transform.SetParent(session.transform, false);
            mapRoot = new GameObject("Fixed Battle Map");
            mapRoot.transform.SetParent(worldRoot.transform, false);
            BattleMapView map = BuildMap();
            BattleWorldView world = worldRoot.AddComponent<BattleWorldView>();
            BattleActorView[] actors = BuildActors(map);
            BattleLastSeenView[] markers = BuildMarkers();
            LineRenderer line = new GameObject("Command Path", typeof(LineRenderer)).GetComponent<LineRenderer>();

            line.transform.SetParent(worldRoot.transform, false);
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = 0.075f;
            line.positionCount = 5;
            line.gameObject.SetActive(false);
            Wire(
                world,
                ("map", map),
                ("actors", actors),
                ("lastSeen", markers),
                ("commandPath", line),
                ("movePathMaterial", Surface(WorldCyan)),
                ("attackPathMaterial", Surface(Red)));
            Save(worldRoot, "BattleWorld.prefab");

            BuildUI();
            BattleHudView hud = WireUI();
            GameObject rigObject = new GameObject("Battle Camera Rig");

            rigObject.transform.SetParent(session.transform, false);
            camera.transform.SetParent(rigObject.transform, false);
            camera.tag = "MainCamera";
            camera.orthographic = false;
            camera.fieldOfView = 36;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            if (camera.GetComponent<PhysicsRaycaster>() == null)
            {
                camera.gameObject.AddComponent<PhysicsRaycaster>();
            }

            BattleCameraRig rig = rigObject.AddComponent<BattleCameraRig>();

            Wire(rig, ("cameraView", camera));
            Save(rigObject, "BattleCameraRig.prefab");
            // Composition references are saved as overrides on the session, not external scene references in child assets.
            Wire(rig, ("map", map), ("layout", canvasRoot.GetComponent<BattleHudLayout>()));
            GameObject events = new GameObject("Battle EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

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

        [Serializable]
        private sealed class RuleContainer { public BattleRules rules; }

        private void CopyRules(BattleRules source)
        {
            foreach (FieldInfo field in typeof(BattleRules).GetFields())
            {
                field.SetValue(rules, field.GetValue(source));
            }
        }

        private BattleMapView BuildMap()
        {
            GameObject tileRoot = new GameObject("Tile", typeof(BattleWorldTarget), typeof(BattleTileView));
            GameObject ground = Shape(
                "Surface",
                tileRoot.transform,
                PrimitiveType.Cube,
                Vector3.down * 0.08f,
                new Vector3(2, 0.16f, 2),
                Color.white,
                true);
            GameObject border = Shape(
                "Selection Border",
                tileRoot.transform,
                PrimitiveType.Cube,
                Vector3.down * 0.06f,
                new Vector3(2.14f, 0.08f, 2.14f),
                WorldCyan,
                false);

            border.SetActive(false);
            Wire(
                tileRoot.GetComponent<BattleTileView>(),
                ("surface", ground.GetComponent<Renderer>()),
                ("border", border),
                ("target", tileRoot.GetComponent<BattleWorldTarget>()));
            PrefabUtility.SaveAsPrefabAsset(tileRoot, Prefabs + "Map/BattleTile.prefab");
            UnityEngine.Object.DestroyImmediate(tileRoot);
            BattleMapView map = mapRoot.AddComponent<BattleMapView>();

            BattleTilemapAssets.BuildMap(map);
            rules.Width = map.Width;
            rules.Height = map.Height;
            rules.NeutralHeight = map.NeutralHeight;
            Save(mapRoot, "Map/FixedBattleMap.prefab");

            return map;
        }

        private BattleActorView[] BuildActors(BattleMapView map)
        {
            BattleActorView[] result = new BattleActorView[10];

            for (int owner = 0; owner < 2; owner++)
            {
                for (int slot = 0; slot < 5; slot++)
                {
                    int id = slot == 4 ? 101 + owner : owner * 4 + slot + 1;
                    EntityKind kind = slot == 4 ? EntityKind.Base : EntityKind.Unit;
                    EntityState entity = new EntityState
                    {
                        Id = id,
                        Owner = owner,
                        Kind = kind
                    };
                    GameObject root;

                    if (owner == 0)
                    {
                        Actor built = CreateActor(entity, false);

                        root = built.Root;
                        BattleActorView view = root.AddComponent<BattleActorView>();

                        Wire(view, ("entityId", id), ("target", built.Target), ("label", built.Label));
                        Save(root, "Characters/" + ModelNames[slot] + ".prefab");
                    }
                    else
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Characters/" + ModelNames[slot] + ".prefab");

                        root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, worldRoot.transform);
                        root.name = "Entity " + id;
                        root.transform.localRotation = Quaternion.Euler(0, kind == EntityKind.Base ? 180 : -30, 0);
                        Wire(root.GetComponent<BattleActorView>(), ("entityId", id));

                        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
                        {
                            if (renderer.sharedMaterial == Surface(TeamBlue))
                            {
                                renderer.sharedMaterial = Surface(Red);
                                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                            }
                        }

                        foreach (SpriteRenderer badge in root.GetComponentsInChildren<SpriteRenderer>())
                        {
                            badge.color = Red;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(badge);
                        }
                    }

                    result[owner * 5 + slot] = root.GetComponent<BattleActorView>();
                }
            }

            return result;
        }

        private BattleLastSeenView[] BuildMarkers()
        {
            GameObject root = new GameObject("Last Seen Marker");
            BattleLastSeenView marker = root.AddComponent<BattleLastSeenView>();
            GameObject textObject = new GameObject("World HP Label", typeof(TextMeshPro));

            textObject.transform.SetParent(root.transform, false);
            textObject.transform.localPosition = Vector3.up * 0.4f;
            TextMeshPro label = textObject.GetComponent<TextMeshPro>();

            label.font = font;
            label.fontSize = 2.6f;

            label.rectTransform.sizeDelta = new Vector2(2, 1);
            label.alignment = TextAlignmentOptions.Center;
            label.isOrthographic = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.color = new Color(0.65f, 0.68f, 0.73f);
            LineRenderer outline = new GameObject("Memory Outline", typeof(LineRenderer)).GetComponent<LineRenderer>();

            outline.transform.SetParent(root.transform, false);

            outline.useWorldSpace = false;
            outline.loop = true;
            outline.positionCount = 4;

            outline.startWidth = outline.endWidth = 0.035f;
            outline.sharedMaterial = Surface(Ghost);

            outline.SetPositions(new[]
            {
                new Vector3(-0.4f,0.08f,-0.4f),
                new Vector3(-0.4f,0.08f,0.4f),
                new Vector3(0.4f,0.08f,0.4f),
                new Vector3(0.4f,0.08f,-0.4f)
            });
            Wire(marker, ("label", label));
            root.SetActive(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "LastSeenMarker.prefab");

            UnityEngine.Object.DestroyImmediate(root);
            BattleLastSeenView[] result = new BattleLastSeenView[10];
            int[] ids = { 1, 2, 3, 4, 5, 6, 7, 8, 101, 102 };

            for (int i = 0; i < ids.Length; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, worldRoot.transform);

                instance.name = "Last Seen " + ids[i];
                result[i] = instance.GetComponent<BattleLastSeenView>();
                Wire(result[i], ("entityId", ids[i]));
            }

            return result;
        }

        private BattleHudView WireUI()
        {
            BattleTopBarView top = headerPanel.gameObject.AddComponent<BattleTopBarView>();

            Matching(top, "phaseText", "timerText", "ownBaseText", "enemyBaseText", "ownBaseBar", "enemyBaseBar");
            Save(headerPanel.gameObject, "UI/BattleTopBar.prefab");
            BattleCommandPanelView commands = controlsPanel.gameObject.AddComponent<BattleCommandPanelView>();

            Matching(
                commands,
                "selectionText",
                "selectedHealthText",
                "equipmentText",
                "detailsText",
                "orderTargetText",
                "lockText",
                "selectedPortrait",
                "selectedHealth",
                "equipmentIcon",
                "previewArrow",
                "previewCells",
                "moveButton",
                "attackButton",
                "waitButton",
                "lockButton");
            Save(controlsPanel.gameObject, "UI/BattleCommandPanel.prefab");
            BattleUnitCardView[] cards = new BattleUnitCardView[4];

            for (int i = 0; i < 4; i++)
            {
                cards[i] = unitButtons[i].gameObject.AddComponent<BattleUnitCardView>();
                Wire(
                    cards[i],
                    ("slot", i),
                    ("roleName", Roles[i]),
                    ("weaponName", Weapons[i]),
                    ("portrait", portraits[i]),
                    ("weaponIcon", Resources.Load<Sprite>("PrototypeUI/" + (i == 2 ? "bow" : i == 3 ? "item" : "blade"))),
                    ("moveIcon", Resources.Load<Sprite>("PrototypeUI/move")),
                    ("attackIcon", Resources.Load<Sprite>("PrototypeUI/attack")),
                    ("waitIcon", Resources.Load<Sprite>("PrototypeUI/wait")),
                    ("unitButton", unitButtons[i]),
                    ("unitLabel", unitLabels[i]),
                    ("unitHealthText", unitHealthText[i]),
                    ("unitOrderText", unitOrderText[i]),
                    ("unitHealth", unitHealth[i]),
                    ("unitOrderPanel", unitOrderPanels[i]),
                    ("unitOrderIcon", unitOrderIcons[i]));
                Save(unitButtons[i].gameObject, "UI/" + ModelNames[i] + "Card.prefab");
            }

            Save(teamPanel.gameObject, "UI/BattleRoster.prefab");
            BattleApPanelView ap = apPanel.gameObject.AddComponent<BattleApPanelView>();

            Matching(ap, "apText", "reservedText", "apPips");
            Save(apPanel.gameObject, "UI/BattleApPanel.prefab");
            BattleToolbarView bar = toolbar.gameObject.AddComponent<BattleToolbarView>();

            Wire(
                bar,
                ("switchText", switchText),
                ("resolveButton", resolveButton),
                ("switchButton", toolbar.Find("Switch Player").GetComponent<Button>()),
                ("rotateButton", toolbar.Find("Rotate Camera").GetComponent<Button>()),
                ("zoomInButton", toolbar.Find("Zoom In").GetComponent<Button>()),
                ("zoomOutButton", toolbar.Find("Zoom Out").GetComponent<Button>()),
                ("exportButton", toolbar.Find("Export Recording").GetComponent<Button>()),
                ("restartButton", toolbar.Find("Restart").GetComponent<Button>()));
            Save(toolbar.gameObject, "UI/BattleToolbar.prefab");
            BattleHudView hud = canvasRoot.AddComponent<BattleHudView>();

            Wire(
                hud,
                ("topBar", top),
                ("commands", commands),
                ("toolbar", bar),
                ("apPanel", ap),
                ("cards", cards),
                ("messageText", messageText),
                ("logText", logText));
            BattleHudLayout layout = canvasRoot.AddComponent<BattleHudLayout>();

            Matching(
                layout,
                "safeArea",
                "headerPanel",
                "boardPanel",
                "controlsPanel",
                "teamPanel",
                "apPanel",
                "toolbar",
                "helpPanel");
            Wire(
                layout,
                ("canvasRoot", canvasRoot.GetComponent<Canvas>()),
                ("cards", unitButtons.Select(b => b.GetComponent<RectTransform>()).ToArray()));
            Save(canvasRoot, "UI/BattleHud.prefab");

            return hud;
        }

        private void Preview(BattleHudView hud, BattleMapView map)
        {
            PlayerView view = new PlayerView
            {
                Player = 0,
                Turn = 1,
                Phase = MatchPhase.Planning,
                AP = Math.Min(rules.MaximumAP, rules.InitialAP + rules.TurnIncome),
                SecondsLeft = rules.PlanningSeconds
            };
            Cell[] cells = new[]
            {
                new Cell(3,3),
                new Cell(4,2),
                new Cell(3,1),
                new Cell(2,2),
                new Cell(3,2)
            };

            foreach (BattleActorView actor in worldRoot.GetComponentsInChildren<BattleActorView>())
            {
                int id = actor.EntityId;
                int owner = id == 102 || id >= 5 && id <= 8 ? 1 : 0;

                bool isBase = id > 100;
                int slot = isBase ? 4 : id - owner * 4 - 1;

                Cell cell = cells[slot];

                if (owner == 1)
                {
                    cell = new Cell(rules.Width - 1 - cell.X, rules.Height - 1 - cell.Y);
                }

                EntityState entity = new EntityState
                {
                    Id = id,
                    Owner = owner,
                    Kind = isBase ? EntityKind.Base : EntityKind.Unit,
                    Position = cell,
                    Health = isBase ? rules.BaseHealth : rules.UnitHealth,
                    MaximumHealth = isBase ? rules.BaseHealth : rules.UnitHealth
                };

                actor.Render(entity, map);
                view.Entities.Add(entity);
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor.GetComponent<BattleWorldTarget>());
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor.GetComponentInChildren<TMP_Text>());
            }

            hud.Render(view, rules, 2, ActionKind.Move);
            messageText.text = "Scene preview. Play to choose bases and begin a match.";
            phaseText.text = "EDITOR PREVIEW";
        }

        private void BakePortraits()
        {
            Texture2D atlas = Resources.Load<Texture2D>("PrototypeUI/portrait-atlas");

            for (int i = 0; i < 4; i++)
            {
                portraits[i] = PortraitAsset(atlas, i, false);
                cardPortraits[i] = PortraitAsset(atlas, i, true);
            }
        }

        private static Sprite PortraitAsset(Texture2D atlas, int slot, bool cropped)
        {
            string path = Art + ModelNames[slot] + (cropped ? "CardPortrait" : "Portrait") + ".asset";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
            {
                return sprite;
            }

            Rect rect = new Rect(
                ((slot % 2) + (cropped ? 0.18f : 0)) * atlas.width / 2,
                (1 - slot / 2) * atlas.height / 2,
                atlas.width * (cropped ? 0.32f : 0.5f),
                atlas.height / 2);

            sprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), 100);
            sprite.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(sprite, path);

            return sprite;
        }

        private Material Surface(Color color)
        {
            string path = Art + "Surface-" + ColorUtility.ToHtmlStringRGBA(color) + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                return material;
            }

            material = new Material(surface)
            {
                color = color,
                enableInstancing = true
            };
            AssetDatabase.CreateAsset(material, path);

            return material;
        }

        private static void Save(GameObject root, string relativePath)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, Prefabs + relativePath, InteractionMode.AutomatedAction);
        }

        private void Matching(Component target, params string[] fields)
        {
            foreach (string field in fields)
            {
                Wire(target, (field, GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this)));
            }
        }

        private static void Wire(Component target, params (string name, object value)[] values)
        {
            SerializedObject serialized = new SerializedObject(target);

            foreach ((string name, object value) pair in values)
            {
                Assign(serialized.FindProperty(pair.name), pair.value);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }

            EditorUtility.SetDirty(target);
        }

        private static void Assign(SerializedProperty property, object value)
        {
            if (property == null)
            {
                throw new InvalidOperationException("Missing serialized authoring field.");
            }

            if (value is Array array)
            {
                property.arraySize = array.Length;

                for (int i = 0; i < array.Length; i++)
                {
                    Assign(property.GetArrayElementAtIndex(i), array.GetValue(i));
                }
            }
            else if (value is UnityEngine.Object asset)
            {
                property.objectReferenceValue = asset;
            }
            else if (value is int integer)
            {
                property.intValue = integer;
            }
            else if (value is float number)
            {
                property.floatValue = number;
            }
            else if (value is string text)
            {
                property.stringValue = text;
            }
            else if (value is Color color)
            {
                property.colorValue = color;
            }
            else
            {
                throw new ArgumentException("Unsupported authoring field value.");
            }
        }
    }
}

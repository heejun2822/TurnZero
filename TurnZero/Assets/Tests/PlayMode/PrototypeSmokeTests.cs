using TMPro;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TurnZero.Tests
{
    public sealed class PrototypeSmokeTests
    {
        private GameObject root;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            root = GameObject.Find("Battle Session");
            Assert.That(root.GetComponent<BattleController>(), Is.Not.Null);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return Preview("placement.png");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
#endif
            yield return null;
        }

        private static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        private static TMP_Text Text(string name) => GameObject.Find(name).GetComponent<TMP_Text>();
        private static GameObject Floor(int x, int y) => Object.FindObjectsByType<BattleTileView>(FindObjectsSortMode.None)
            .Single(t => t.Cell == new Cell(x,y)).gameObject;
        private static void Tile(int x, int y) => Floor(x,y).GetComponent<BattleWorldTarget>()
            .OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });

        private static IEnumerator ResolveTurn()
        {
            Button("Resolve Turn").onClick.Invoke();
            float timeout = Time.realtimeSinceStartup + 3;
            while (Text("Phase").text.Contains("RESOLVING") && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(Text("Phase").text, Does.Not.Contain("RESOLVING"));
        }

        [UnityTest]
        public IEnumerator FogShowsUniformDarkUnexploredTilesAndDimsExploredTerrainIncludingProps()
        {
            void AssertUnexplored(BattleTileView tile)
            {
                var surface = tile.transform.Find("Surface").GetComponent<Renderer>();
                Assert.That(surface.enabled, Is.True);
                Assert.That(tile.GetComponentsInChildren<Renderer>(true).Where(r => r != surface).All(r => !r.enabled), Is.True);
                var tint = new MaterialPropertyBlock();
                surface.GetPropertyBlock(tint);
                Assert.That(Vector4.Distance(tint.GetColor("_BaseColor"), new Color(0.045f, 0.055f, 0.07f)), Is.LessThan(0.001f));
            }
            Assert.That(Camera.main.backgroundColor, Is.EqualTo(Color.black));
            var tree = Floor(3,10).GetComponent<BattleTileView>();
            var wall = Floor(4,10).GetComponent<BattleTileView>();
            AssertUnexplored(tree);
            AssertUnexplored(wall);
            AssertUnexplored(Floor(15,23).GetComponent<BattleTileView>());
            var home = Floor(15,9).GetComponent<BattleTileView>();
            var property = new MaterialPropertyBlock();
            home.transform.Find("Surface").GetComponent<Renderer>().GetPropertyBlock(property);
            var dim = property.GetColor("_BaseColor");
            Assert.That(dim.r, Is.EqualTo(home.TerrainColor.r * 0.3f).Within(0.001f));
            var view = new PlayerView { Player = 0, Phase = MatchPhase.Planning };
            view.ExploredCells.Add(tree.Cell);
            view.ExploredCells.Add(wall.Cell);
            var world = root.GetComponentInChildren<BattleWorldView>();
            world.Refresh(view, 0, ActionKind.Move);
            var trunk = tree.transform.Find("Tree").GetComponentInChildren<Renderer>();
            Assert.That(trunk.enabled, Is.True);
            trunk.GetPropertyBlock(property);
            Assert.That(property.GetColor("_BaseColor").r, Is.EqualTo(trunk.sharedMaterial.GetColor("_BaseColor").r * 0.3f).Within(0.001f));
            Assert.That(wall.transform.Find("RuinedWall").GetComponentsInChildren<Renderer>().All(r => r.enabled), Is.True);
            view.VisibleCells.Add(tree.Cell);
            world.Refresh(view, 0, ActionKind.Move);
            trunk.GetPropertyBlock(property);
            Assert.That(Vector4.Distance(property.GetColor("_BaseColor"), trunk.sharedMaterial.GetColor("_BaseColor")), Is.LessThan(0.001f));
            tree.transform.Find("Surface").GetComponent<Renderer>().GetPropertyBlock(property);
            Assert.That(Vector4.Distance(property.GetColor("_BaseColor"), tree.TerrainColor), Is.LessThan(0.001f));
            world.Refresh(new PlayerView(), 0, ActionKind.Move);
            AssertUnexplored(tree);
            tree.Render(TileVisibility.Unexplored, true, true, true);
            AssertUnexplored(tree);
            Assert.That(tree.transform.Find("Selection Border").gameObject.activeSelf, Is.False);
            Button("Restart").onClick.Invoke();
            Tile(7,8);
            Tile(8,20);
            yield return Preview("fog-states.png");
            Button("Switch Player").onClick.Invoke();
            AssertUnexplored(Floor(3,1).GetComponent<BattleTileView>());
            yield return Preview("fog-player-two.png");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ResolutionKeepsItsTurnVisibleBeforeStartingTheNextPlanningWindow()
        {
            Tile(7,8);
            Tile(8,20);
            Button("Resolve Turn").onClick.Invoke();
            Assert.That(Text("Phase").text, Does.Contain("TURN 01").And.Contain("RESOLVING"));
            Assert.That(Button("Resolve Turn").interactable, Is.False);
            Assert.That(Button("Move").interactable, Is.False);
            float timeout = Time.realtimeSinceStartup + 3;
            while (Text("Phase").text.Contains("RESOLVING") && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(Text("Phase").text, Does.Contain("TURN 02").And.Contain("PLAN ORDERS"));
            Assert.That(Text("Timer").text, Is.EqualTo("00:30"));
            Assert.That(Button("Resolve Turn").interactable, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PlacementPlanningResolutionAndRestartAreConnected()
        {
            Assert.That(Object.FindFirstObjectByType<InputSystemUIInputModule>(), Is.Not.Null);
            Tile(0, 0);
            Assert.That(Text("Phase").text, Does.Contain("P1"));
            Tile(2, 2);
            Assert.That(Text("Phase").text, Does.Contain("P2"));
            Tile(5, 20);
            Assert.That(Text("Phase").text, Does.Contain("TURN 01"));
            Button("Unit 1").onClick.Invoke();
            Button("Move").onClick.Invoke();
            Tile(2, 4);
            Assert.That(Text("Reserved AP").text, Is.EqualTo("RESERVED 1"));
            Button("Switch Player").onClick.Invoke();
            Assert.That(Text("Reserved AP").text, Is.EqualTo("RESERVED 0"));
            yield return ResolveTurn();
            Assert.That(Text("Phase").text, Does.Contain("TURN 02"));
            Button("Switch Player").onClick.Invoke();
            Assert.That(GameObject.Find("Entity 1").GetComponent<BattleWorldTarget>().Cell, Is.EqualTo(new Cell(2, 4)));
            yield return Preview("planning.png");
            Button("Restart").onClick.Invoke();
            Assert.That(Text("Phase").text, Does.Contain("PLACE YOUR BASE"));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator Preview(string filename, int width = 1600, int height = 900)
        {
            string folder = System.Environment.GetEnvironmentVariable("TURNZERO_PREVIEW_DIR");
            if (string.IsNullOrEmpty(folder) || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            yield return null;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var camera = Camera.main;
            var texture = new RenderTexture(width, height, 24);
            var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var previousTarget = camera.targetTexture;
            var previousAspect = camera.aspect;
            var scaler = canvas.GetComponent<CanvasScaler>();
            var previousScale = canvas.scaleFactor;
            var previousScalerEnabled = scaler.enabled;
            var previousActive = RenderTexture.active;
            try
            {
                texture.Create();
                camera.targetTexture = texture;
                camera.aspect = (float)width / height;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                // Simulate the target display rather than the Editor's fixed Game View size.
                scaler.enabled = false;
                canvas.scaleFactor = Mathf.Sqrt(width / 1600f * height / 900f);
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, filename), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                canvas.scaleFactor = previousScale;
                scaler.enabled = previousScalerEnabled;
                texture.Release();
                Object.Destroy(texture);
                Object.Destroy(pixels);
                Canvas.ForceUpdateCanvases();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator BaseDestructionShowsVictoryAndRestartRestoresPlacement()
        {
            Tile(2, 3);
            Tile(2, 15);
            for (int y = 5; y <= 13; y++)
            {
                Button("Unit 1").onClick.Invoke();
                Button("Move").onClick.Invoke();
                Tile(2,y);
                yield return ResolveTurn();
            }
            // Clear the enemy in front of the base, then advance into its vacated tile.
            for (int hit = 0; hit < 3; hit++)
            {
                Button("Unit 1").onClick.Invoke();
                Button("Attack").onClick.Invoke();
                Tile(2, 14);
                yield return ResolveTurn();
            }
            Button("Unit 1").onClick.Invoke();
            Button("Move").onClick.Invoke();
            Tile(2, 14);
            yield return ResolveTurn();
            for (int hit = 0; hit < 8; hit++)
            {
                Button("Unit 1").onClick.Invoke();
                Button("Attack").onClick.Invoke();
                Tile(2, 15);
                yield return ResolveTurn();
            }
            Assert.That(Text("Phase").text, Is.EqualTo("PLAYER 1 WINS"));
            Assert.That(Button("Resolve Turn").interactable, Is.False);
            Button("Restart").onClick.Invoke();
            Assert.That(Text("Phase").text, Does.Contain("PLACE YOUR BASE"));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RecordingJsonPreservesReplayInputsAndResults()
        {
            var match = new BattleMatch(new BattleRules());
            match.PlaceBase(0, new Cell(2, 2), 0, out _);
            match.PlaceBase(1, new Cell(5, 7), 0, out _);
            match.QueueOrder(0, 1, ActionOrder.Move(1, new Cell(2, 4)), out _);
            match.ResolveNow(1);
            var recording = JsonUtility.FromJson<BattleRecording>(JsonUtility.ToJson(match.ExportRecording()));
            Assert.That(recording.BasePlacements.Count, Is.EqualTo(2));
            Assert.That(recording.Turns.Count, Is.EqualTo(1));
            var turn = recording.Turns[0];
            var replay = BattleResolver.Resolve(turn.Before, recording.Rules, turn.PlayerOneOrders, turn.PlayerTwoOrders).State;
            Assert.That(JsonUtility.ToJson(replay), Is.EqualTo(JsonUtility.ToJson(turn.After)));
        }

        [UnityTest]
        public IEnumerator RestartReusesAuthoredObjectsAndAppliesCombatRules()
        {
            var objects = root.GetComponentsInChildren<Transform>(true);
            var canvas = root.GetComponentInChildren<Canvas>();
            var map = root.GetComponentInChildren<BattleMapView>();
            var actor = root.GetComponentsInChildren<BattleActorView>(true)[0];
            JsonUtility.FromJsonOverwrite("{\"rules\":" + JsonUtility.ToJson(new BattleRules { UnitHealth = 5 }) + "}", root.GetComponent<BattleController>());
            Tile(2, 2);
            Button("Restart").onClick.Invoke();
            Button("Restart").onClick.Invoke();
            yield return null;
            CollectionAssert.AreEquivalent(objects, root.GetComponentsInChildren<Transform>(true));
            Assert.That(root.GetComponentInChildren<Canvas>(), Is.SameAs(canvas));
            Assert.That(root.GetComponentInChildren<BattleMapView>(), Is.SameAs(map));
            Assert.That(root.GetComponentsInChildren<BattleActorView>(true), Does.Contain(actor));
            Assert.That(map.Width, Is.EqualTo(16));
            Assert.That(map.Height, Is.EqualTo(24));
            Assert.That(root.GetComponentsInChildren<Canvas>().Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<EventSystem>().Length, Is.EqualTo(1));
            Tile(2, 2);
            Tile(5, 20);
            Assert.That(Text("Selected HP").text, Is.EqualTo("HP  5 / 5"));
            yield return ResolveTurn();
            Assert.That(Text("Phase").text, Does.Contain("TURN 02"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ReferenceHudTracksOrdersAPCancelAndReadiness()
        {
            Tile(3, 2);
            Tile(4, 20);
            Assert.That(Text("Selected Unit").text, Is.EqualTo("02 SCOUT"));
            Assert.That(Text("Enemy Base HP").text, Is.EqualTo("? / 8"));
            Assert.That(Button("Skill").interactable, Is.False);
            Assert.That(Button("Item").interactable, Is.False);
            Button("Unit 2").onClick.Invoke();
            Tile(4, 3);
            Assert.That(Text("AP").text, Is.EqualTo("3 / 8"));
            Assert.That(Text("Reserved AP").text, Is.EqualTo("RESERVED 1"));
            Assert.That(Text("Order Details").text, Is.EqualTo("MOVE  /  1 AP"));
            Assert.That(GameObject.Find("Command Path").GetComponent<LineRenderer>().positionCount, Is.EqualTo(5));
            Assert.That(Button("Unit 2").transform.Find("Card Order/Card Order Text").GetComponent<TMP_Text>().text, Is.EqualTo("MOVE / 1 AP"));
            yield return Preview("reference-hud.png");
            yield return Preview("reference-hud-portrait.png", 900, 1600);
            Button("Wait").onClick.Invoke();
            Assert.That(Text("AP").text, Is.EqualTo("4 / 8"));
            Assert.That(Text("Reserved AP").text, Is.EqualTo("RESERVED 0"));
            Assert.That(GameObject.Find("Command Path"), Is.Null);
            Tile(4, 3);
            Button("Lock Orders").onClick.Invoke();
            Assert.That(Button("Move").interactable, Is.False);
            Assert.That(Button("Lock Orders").transform.Find("Label").GetComponent<TMP_Text>().text, Is.EqualTo("READY / LOCKED"));
            Button("Switch Player").onClick.Invoke();
            Assert.That(Text("AP").text, Is.EqualTo("4 / 8"));
            Assert.That(Text("Reserved AP").text, Is.EqualTo("RESERVED 0"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WorldUsesPerspectiveMeshesAndCameraControls()
        {
            var tile = Floor(2,2);
            Assert.That(tile.GetComponentInChildren<MeshRenderer>(), Is.Not.Null);
            Assert.That(tile.GetComponentInChildren<BoxCollider>(), Is.Not.Null);
            Assert.That(tile.GetComponent<Button>(), Is.Null);
            Assert.That(Camera.main.orthographic, Is.False);
            Assert.That(Camera.main.GetComponent<PhysicsRaycaster>(), Is.Not.Null);
            var rotation = Camera.main.transform.rotation;
            Button("Rotate Camera").onClick.Invoke();
            Assert.That(Camera.main.transform.rotation, Is.Not.EqualTo(rotation));
            var position = Camera.main.transform.position;
            Button("Zoom In").onClick.Invoke();
            Assert.That(Camera.main.transform.position, Is.Not.EqualTo(position));
            Tile(2, 3);
            Tile(2, 15);
            yield return Preview("combat-3d.png");
            LogAssert.NoUnexpectedReceived();
        }

        private static void CameraMouse(BattleCameraRig rig, Mouse mouse, Vector2 pointer, float wheel = 0, bool held = false)
        {
            var state = new MouseState { position = pointer, scroll = new Vector2(0, wheel) };
            if (held) state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, state);
            InputSystem.Update();
            typeof(BattleCameraRig).GetMethod("ReadMouse", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(rig, new object[] { 0.1f });
        }

        [UnityTest]
        public IEnumerator MouseWheelAndButtonsRespectZoomLimitsAndHud()
        {
            var rig = root.GetComponentInChildren<BattleCameraRig>();
            rig.enabled = false;
            var mouse = InputSystem.AddDevice<Mouse>();
            var area = root.GetComponentInChildren<BattleHudLayout>().FieldViewport;
            var center = Vector2.Scale(area.center, new Vector2(Screen.width, Screen.height));
            var full = rig.Camera.transform.position;
            try
            {
                CameraMouse(rig, mouse, center, 1);
                Assert.That(Vector3.Distance(full, rig.Camera.transform.position), Is.GreaterThan(0.1f));
                var zoomed = rig.Camera.transform.position;
                CameraMouse(rig, mouse, new Vector2(-1, Screen.height * 0.5f), 1);
                Assert.That(rig.Camera.transform.position, Is.EqualTo(zoomed));

                var overlay = new GameObject("Camera Test HUD", typeof(RectTransform), typeof(Image));
                try
                {
                    var rect = overlay.GetComponent<RectTransform>();
                    rect.SetParent(root.GetComponentInChildren<Canvas>().transform, false);
                    rect.anchorMin = rect.anchorMax = area.center;
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(100, 100);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    CameraMouse(rig, mouse, RectTransformUtility.WorldToScreenPoint(null, rect.position), 1);
                    Assert.That(rig.Camera.transform.position, Is.EqualTo(zoomed), "Wheel over HUD must not zoom the field.");
                }
                finally { Object.DestroyImmediate(overlay); }

                rig.Zoom(-100);
                var nearest = rig.Camera.transform.position;
                Button("Zoom In").onClick.Invoke();
                Assert.That(rig.Camera.transform.position, Is.EqualTo(nearest));
                rig.Zoom(100);
                Assert.That(Vector3.Distance(full, rig.Camera.transform.position), Is.LessThan(0.001f));
                Button("Zoom Out").onClick.Invoke();
                Assert.That(Vector3.Distance(full, rig.Camera.transform.position), Is.LessThan(0.001f));
            }
            finally { InputSystem.RemoveDevice(mouse); }
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EdgeScrollingFollowsScreenDirectionsAndIgnoresDragging()
        {
            var rig = root.GetComponentInChildren<BattleCameraRig>();
            rig.enabled = false;
            var mouse = InputSystem.AddDevice<Mouse>();
            var center = new Vector2(Screen.width, Screen.height) * 0.5f;
            try
            {
                for (int rotation = 0; rotation < 3; rotation++)
                {
                    foreach (var edge in new[] { new Vector2(1, center.y), new Vector2(Screen.width - 1, center.y),
                        new Vector2(center.x, 1), new Vector2(center.x, Screen.height - 1) })
                    {
                        rig.Zoom(100);
                        rig.Zoom(-100);
                        var before = rig.Camera.transform.position;
                        CameraMouse(rig, mouse, center);
                        CameraMouse(rig, mouse, edge, held: true);
                        CameraMouse(rig, mouse, new Vector2(-1, center.y));
                        Assert.That(rig.Camera.transform.position, Is.EqualTo(before));
                        CameraMouse(rig, mouse, edge);
                        var motion = rig.Camera.transform.position - before;
                        var expected = edge.x < center.x ? -rig.Camera.transform.right :
                            edge.x > center.x ? rig.Camera.transform.right :
                            edge.y < center.y ? -rig.Camera.transform.forward : rig.Camera.transform.forward;
                        expected.y = 0;
                        Assert.That(Vector3.Dot(motion, expected.normalized), Is.GreaterThan(0.01f), "Scroll direction after rotation.");
                    }
                    rig.Rotate();
                }
                var beforeTouch = rig.Camera.transform.position;
                var touch = InputSystem.AddDevice<Touchscreen>();
                try
                {
                    InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = center });
                    InputSystem.Update();
                    CameraMouse(rig, mouse, new Vector2(1, 1));
                    Assert.That(rig.Camera.transform.position, Is.EqualTo(beforeTouch));
                }
                finally { InputSystem.RemoveDevice(touch); }
            }
            finally { InputSystem.RemoveDevice(mouse); }
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertCameraBounds(BattleCameraRig rig, BattleMapView map, BattleHudLayout layout)
        {
            var field = layout.FieldViewport;
            var ground = new Plane(map.transform.up, map.transform.position);
            var points = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                var ray = rig.Camera.ViewportPointToRay(new Vector3(i % 2 == 0 ? field.xMin : field.xMax,
                    i / 2 == 0 ? field.yMin : field.yMax, 0));
                Assert.That(ground.Raycast(ray, out float distance), Is.True);
                var point = map.transform.InverseTransformPoint(ray.GetPoint(distance));
                points[i] = new Vector2(point.x, point.z);
            }
            var half = new Vector2(map.Width * 0.5f + 0.65f, map.Height * 0.5f + 0.65f) * map.Spacing;
            for (int axis = 0; axis < 2; axis++)
            {
                float minimum = points.Min(p => p[axis]);
                float maximum = points.Max(p => p[axis]);
                if (maximum - minimum > half[axis] * 2) continue;
                Assert.That(minimum, Is.GreaterThanOrEqualTo(-half[axis] - 0.001f));
                Assert.That(maximum, Is.LessThanOrEqualTo(half[axis] + 0.001f));
            }
        }

        [UnityTest]
        public IEnumerator CameraStaysWithinFieldWhenPanningRotatingAndResizing()
        {
            var rig = root.GetComponentInChildren<BattleCameraRig>();
            var map = root.GetComponentInChildren<BattleMapView>();
            var layout = root.GetComponentInChildren<BattleHudLayout>();
            rig.enabled = false;
            var mouse = InputSystem.AddDevice<Mouse>();
            float previousAspect = rig.Camera.aspect;
            var full = rig.Camera.transform.position;
            try
            {
                rig.Zoom(-100);
                foreach (var edge in new[] { new Vector2(1, 1), new Vector2(Screen.width - 1, Screen.height - 1) })
                {
                    for (int i = 0; i < 150; i++) CameraMouse(rig, mouse, edge);
                    var limit = rig.Camera.transform.position;
                    CameraMouse(rig, mouse, edge);
                    Assert.That(Vector3.Distance(limit, rig.Camera.transform.position), Is.LessThan(0.001f));
                    AssertCameraBounds(rig, map, layout);
                    rig.Rotate();
                    AssertCameraBounds(rig, map, layout);
                }
                CameraMouse(rig, mouse, new Vector2(Screen.width, Screen.height) * 0.5f);
                rig.enabled = true;
                yield return Preview("camera-zoomed.png");
                rig.enabled = false;
                foreach (float aspect in new[] { 9f / 16, 16f / 9 })
                {
                    rig.Camera.aspect = aspect;
                    rig.Zoom(100);
                    foreach (int x in new[] { 0, map.Width - 1 })
                        foreach (int y in new[] { 0, map.Height - 1 })
                        {
                            var point = rig.Camera.WorldToViewportPoint(map.Position(new Cell(x,y)) + Vector3.up * 1.9f);
                            Assert.That(layout.FieldViewport.Contains(new Vector2(point.x, point.y)), Is.True, "Full board must fit the available HUD area.");
                            Assert.That(point.z, Is.GreaterThan(rig.Camera.nearClipPlane));
                            Assert.That(point.z, Is.LessThan(rig.Camera.farClipPlane), "Expanded field must not be clipped.");
                        }
                    rig.Zoom(-100);
                    rig.Reframe();
                    AssertCameraBounds(rig, map, layout);
                }
                rig.Camera.aspect = previousAspect;
                // Restore the initial rotation and verify zooming out returns to the full board.
                for (int i = 0; i < 6; i++) rig.Rotate();
                rig.Zoom(100);
                Assert.That(Vector3.Distance(full, rig.Camera.transform.position), Is.LessThan(0.001f));
                CameraMouse(rig, mouse, new Vector2(1, 1));
                Assert.That(Vector3.Distance(full, rig.Camera.transform.position), Is.LessThan(0.001f));
            }
            finally
            {
                rig.Camera.aspect = previousAspect;
                InputSystem.RemoveDevice(mouse);
            }
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WorldHidesUnobservedEnemiesAndRendersOnlyLastSeenSnapshot()
        {
            Tile(2, 3);
            Tile(2, 15);
            Assert.That(GameObject.Find("Entity 7"), Is.Null);
            Assert.That(GameObject.Find("Entity 5"), Is.Null);
            Button("Switch Player").onClick.Invoke();
            Assert.That(GameObject.Find("Entity 5"), Is.Not.Null);
            Assert.That(GameObject.Find("Entity 7"), Is.Not.Null);
            Button("Switch Player").onClick.Invoke();
            Assert.That(GameObject.Find("Entity 5"), Is.Null);

            var observation = new Observation { Entity = new EntityState { Id = 7, Owner = 1, Kind = EntityKind.Unit,
                Position = new Cell(2, 5), Health = 3, MaximumHealth = 3 }, SeenOnTurn = 1 };
            var hidden = new PlayerView { Player = 0, Phase = MatchPhase.Planning };
            hidden.LastSeen.Add(observation.Copy());
            hidden.ExploredCells.Add(observation.Entity.Position);
            observation.Entity.Position = new Cell(7, 9);
            observation.Entity.Health = 1;
            var world = root.GetComponentInChildren<BattleWorldView>();
            world.Refresh(hidden, 0, ActionKind.Move);
            Assert.That(GameObject.Find("Entity 7"), Is.Null);
            var ghost = GameObject.Find("Last Seen 7");
            Assert.That(ghost, Is.Not.Null);
            Assert.That(ghost.transform.position, Is.EqualTo(world.Position(new Cell(2, 5))));
            Assert.That(ghost.GetComponentInChildren<TMP_Text>().text, Does.Contain("3 HP / T1"));
            Assert.That(ghost.GetComponent<BattleWorldTarget>(), Is.Null);
            yield return null;
            Assert.That(ghost.GetComponentsInChildren<Collider>(), Is.Empty);
            world.Refresh(new PlayerView(), 0, ActionKind.Move);
            Assert.That(GameObject.Find("Last Seen 7"), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RealMouseAndTouchEventsCanPlaceBothBases()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var touch = InputSystem.AddDevice<Touchscreen>();
            try
            {
                Physics.SyncTransforms();
                Vector2 first = Camera.main.WorldToScreenPoint(Floor(2,2).transform.position + Vector3.up * 0.08f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = first });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = first }.WithButton(MouseButton.Left));
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = first });
                yield return null;
                Assert.That(Text("Phase").text, Does.Contain("P2"));
                Vector2 second = Camera.main.WorldToScreenPoint(Floor(5,20).transform.position + Vector3.up * 0.08f);
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = second, pressure = 1 });
                yield return null;
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = second });
                yield return null;
                Assert.That(Text("Phase").text, Does.Contain("TURN 01"));
                Physics.SyncTransforms();
                Vector2 unit = Camera.main.WorldToScreenPoint(GameObject.Find("Entity 1").transform.position + Vector3.up * 0.7f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = unit });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = unit }.WithButton(MouseButton.Left));
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = unit });
                yield return null;
                Assert.That(Text("Selected Unit").text, Is.EqualTo("01 GUARDIAN"));
                Vector2 card = RectTransformUtility.WorldToScreenPoint(null, Button("Unit 2").transform.position);
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Began, position = card, pressure = 1 });
                yield return null;
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = card });
                yield return null;
                Assert.That(Text("Selected Unit").text, Is.EqualTo("02 SCOUT"));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(touch);
            }
        }
    }
}

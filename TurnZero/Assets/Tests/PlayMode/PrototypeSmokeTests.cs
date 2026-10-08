using System.Collections;
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
            root = GameObject.Find("Battle Prototype");
            Assert.That(root.GetComponent<BattlePrototype>(), Is.Not.Null);
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
        private static Text Text(string name) => GameObject.Find(name).GetComponent<Text>();
        private static void Tile(int x, int y) => GameObject.Find($"Tile {x},{y}").GetComponent<BattleWorldTarget>()
            .OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });

        [UnityTest]
        public IEnumerator PlacementPlanningResolutionAndRestartAreConnected()
        {
            Assert.That(Object.FindFirstObjectByType<InputSystemUIInputModule>(), Is.Not.Null);
            Tile(0, 0);
            Assert.That(Text("Phase").text, Does.Contain("P1"));
            Tile(2, 2);
            Assert.That(Text("Phase").text, Does.Contain("P2"));
            Tile(5, 7);
            Assert.That(Text("Phase").text, Does.Contain("TURN 01"));
            Button("Unit 1").onClick.Invoke();
            Button("Move").onClick.Invoke();
            Tile(2, 4);
            Assert.That(Text("Reserved AP").text, Is.EqualTo("RESERVED 1"));
            Button("Switch Player").onClick.Invoke();
            Assert.That(Text("Reserved AP").text, Is.EqualTo("RESERVED 0"));
            Button("Resolve Turn").onClick.Invoke();
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
            Tile(2, 6);
            // Clear the enemy in front of the base, then advance into its vacated tile.
            for (int hit = 0; hit < 3; hit++)
            {
                Button("Unit 1").onClick.Invoke();
                Button("Attack").onClick.Invoke();
                Tile(2, 5);
                Button("Resolve Turn").onClick.Invoke();
            }
            Button("Unit 1").onClick.Invoke();
            Button("Move").onClick.Invoke();
            Tile(2, 5);
            Button("Resolve Turn").onClick.Invoke();
            for (int hit = 0; hit < 8; hit++)
            {
                Button("Unit 1").onClick.Invoke();
                Button("Attack").onClick.Invoke();
                Tile(2, 6);
                Button("Resolve Turn").onClick.Invoke();
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
        public IEnumerator RestartAppliesChangedMapRulesWithoutDuplicatingUI()
        {
            var configuration = JsonUtility.ToJson(new BattleRules { Width = 10, Height = 12 });
            JsonUtility.FromJsonOverwrite("{\"rules\":" + configuration + "}", root.GetComponent<BattlePrototype>());
            // The running UI still uses its original match settings until restart.
            Tile(2, 2);
            Button("Restart").onClick.Invoke();
            yield return null;
            Assert.That(GameObject.Find("Tile 9,11"), Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<Canvas>().Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<EventSystem>().Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<BattleWorldView>().Length, Is.EqualTo(1));
            Assert.That(Text("Phase").text, Does.Contain("PLACE YOUR BASE"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ReferenceHudTracksOrdersAPCancelAndReadiness()
        {
            Tile(3, 2);
            Tile(4, 7);
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
            Assert.That(Button("Unit 2").transform.Find("Card Order/Card Order Text").GetComponent<Text>().text, Is.EqualTo("MOVE / 1 AP"));
            yield return Preview("reference-hud.png");
            yield return Preview("reference-hud-portrait.png", 900, 1600);
            Button("Wait").onClick.Invoke();
            Assert.That(Text("AP").text, Is.EqualTo("4 / 8"));
            Assert.That(Text("Reserved AP").text, Is.EqualTo("RESERVED 0"));
            Assert.That(GameObject.Find("Command Path"), Is.Null);
            Tile(4, 3);
            Button("Lock Orders").onClick.Invoke();
            Assert.That(Button("Move").interactable, Is.False);
            Assert.That(Button("Lock Orders").transform.Find("Label").GetComponent<Text>().text, Is.EqualTo("READY / LOCKED"));
            Button("Switch Player").onClick.Invoke();
            Assert.That(Text("AP").text, Is.EqualTo("4 / 8"));
            Assert.That(Text("Reserved AP").text, Is.EqualTo("RESERVED 0"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WorldUsesPerspectiveMeshesAndCameraControls()
        {
            var tile = GameObject.Find("Tile 2,2");
            Assert.That(tile.GetComponent<MeshRenderer>(), Is.Not.Null);
            Assert.That(tile.GetComponent<BoxCollider>(), Is.Not.Null);
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
            Tile(2, 6);
            yield return Preview("combat-3d.png");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WorldHidesUnobservedEnemiesAndRendersOnlyLastSeenSnapshot()
        {
            Tile(2, 3);
            Tile(2, 6);
            Assert.That(GameObject.Find("Entity 7"), Is.Not.Null);
            Assert.That(GameObject.Find("Entity 5"), Is.Null);
            Button("Switch Player").onClick.Invoke();
            Assert.That(GameObject.Find("Entity 5"), Is.Not.Null);
            Button("Switch Player").onClick.Invoke();
            Assert.That(GameObject.Find("Entity 5"), Is.Null);

            var observation = new Observation { Entity = new EntityState { Id = 7, Owner = 1, Kind = EntityKind.Unit,
                Position = new Cell(2, 5), Health = 3, MaximumHealth = 3 }, SeenOnTurn = 1 };
            var hidden = new PlayerView { Player = 0, Phase = MatchPhase.Planning };
            hidden.LastSeen.Add(observation.Copy());
            observation.Entity.Position = new Cell(7, 9);
            observation.Entity.Health = 1;
            var world = root.GetComponentInChildren<BattleWorldView>();
            world.Refresh(hidden, 0, ActionKind.Move);
            Assert.That(GameObject.Find("Entity 7"), Is.Null);
            var ghost = GameObject.Find("Last Seen 7");
            Assert.That(ghost.transform.position, Is.EqualTo(world.Position(new Cell(2, 5))));
            Assert.That(ghost.GetComponentInChildren<TextMesh>().text, Does.Contain("3 HP / T1"));
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
                Vector2 first = Camera.main.WorldToScreenPoint(GameObject.Find("Tile 2,2").transform.position + Vector3.up * 0.08f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = first });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = first }.WithButton(MouseButton.Left));
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = first });
                yield return null;
                Assert.That(Text("Phase").text, Does.Contain("P2"));
                Vector2 second = Camera.main.WorldToScreenPoint(GameObject.Find("Tile 5,7").transform.position + Vector3.up * 0.08f);
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

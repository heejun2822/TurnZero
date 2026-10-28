using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TurnZero.Tests
{
    public sealed class LocalPlaytestSmokeTests
    {
        private BattleSession session;
        private GameObject root;
        private BattleConfiguration configuration;
        private InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        private static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        private static TMP_Text Text(string name) => GameObject.Find(name).GetComponent<TMP_Text>();
        [UnitySetUp] public IEnumerator SetUp()
        {
            background = InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1600,900,"TurnZero QA");
#endif
            yield return SceneManager.LoadSceneAsync("LocalBattle",LoadSceneMode.Single); yield return null;
            root = GameObject.Find("Local Battle"); session = root.GetComponent<BattleSession>();
            configuration = Object.Instantiate(session.Configuration); configuration.TurnStartSeconds = 0; configuration.PlanningSeconds = 30; configuration.MoveSeconds = 0.01f; configuration.ActionSeconds = 0.01f;
            session.Initialize(configuration); yield return null; yield return null; Canvas.ForceUpdateCanvases();
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Object.Destroy(root); Object.Destroy(configuration); InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1600,900,"TurnZero QA");
#endif
            yield return null;
        }
        private IEnumerator Planning()
        {
            float deadline = Time.realtimeSinceStartup+5;
            while (session.Match.Phase != BattlePhase.Planning && session.Match.Phase != BattlePhase.Finished && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(session.Match.Phase == BattlePhase.Planning || session.Match.Phase == BattlePhase.Finished,Is.True);
        }
        [UnityTest] public IEnumerator SlicedHealthBarsResizeForFullPartialEmptyAndUnknownHealth()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
            var card = GameObject.Find("Own card 1").GetComponent<LocalRosterCard>();
            var track = card.transform.Find("Health track").GetComponent<RectTransform>();
            var fill = track.GetComponentsInChildren<Image>(true).Single(i => i.name == "Health fill");
            var view = session.View; var unit = view.Units.Single(u => u.Id == 1);
            int maximum = session.Rules.Characters[0].Health;
            Assert.That(fill.type,Is.EqualTo(Image.Type.Sliced));
            foreach (int health in new[] { maximum,maximum/2,1,0 })
            {
                unit.Health = health; card.Render(view); Canvas.ForceUpdateCanvases();
                Assert.That(fill.rectTransform.rect.width / track.rect.width,Is.EqualTo((float)health/maximum).Within(0.001f));
                Assert.That(fill.enabled,Is.EqualTo(health > 0));
            }
            var enemy = GameObject.Find("Enemy card 1").GetComponent<LocalRosterCard>(); enemy.Render(view);
            var unknown = enemy.GetComponentsInChildren<Image>(true).Single(i => i.name == "Health fill");
            Assert.That(unknown.type,Is.EqualTo(Image.Type.Sliced)); Assert.That(unknown.enabled,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SetupClosesAndBattleRendersFullScreenUnderOverlayHud()
        {
            var setup = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Match setup").gameObject;
            var camera = root.GetComponentInChildren<LocalBattleCamera>().Camera;
            Assert.That(setup.activeInHierarchy,Is.True);
            Button("Start deployment").onClick.Invoke(); yield return null;
            Assert.That(setup.activeInHierarchy,Is.False);
            Assert.That(camera.rect,Is.EqualTo(new Rect(0,0,1,1)));
            session.Events.AutoDeploy(); yield return Planning(); yield return null;
            Assert.That(setup.activeInHierarchy,Is.False);
            Assert.That(camera.pixelRect,Is.EqualTo(new Rect(0,0,Screen.width,Screen.height)));
            Assert.That(camera.clearFlags,Is.EqualTo(CameraClearFlags.SolidColor));
            Assert.That(root.GetComponentInChildren<Canvas>().renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            session.Events.Switch(); yield return null;
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(900,1600,"TurnZero QA");
#endif
            yield return null; yield return null;
            Assert.That(camera.pixelRect,Is.EqualTo(new Rect(0,0,Screen.width,Screen.height)));
            Assert.That(setup.GetComponentsInChildren<TMP_Text>(true).All(t => !t.isActiveAndEnabled),Is.True);
            session.Events.Restart(); Button("Quick deploy").onClick.Invoke(); yield return Planning(); yield return null;
            var world = root.GetComponentInChildren<LocalBattleWorld>();
            foreach (var unit in session.View.Units.Where(u => u.Owner == session.Player && u.Alive))
            {
                Vector2 point = camera.WorldToScreenPoint(world.Position(unit.Cell)+Vector3.up*0.7f);
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point },hits);
                Assert.That(hits,Is.Empty,"Initial allies should be visible between the portrait HUD panels.");
            }
            session.Events.Restart(); yield return null;
            Assert.That(setup.activeInHierarchy,Is.True);
            Button("Quick deploy").onClick.Invoke(); yield return Planning(); yield return null;
            Assert.That(setup.activeInHierarchy,Is.False); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ManualDeploymentCraftingOrdersAndBothReadyWorkThroughTheHud()
        {
            yield return Capture("setup-landscape.png");
            Button("Start deployment").onClick.Invoke(); Assert.That(session.Match.Phase,Is.EqualTo(BattlePhase.BasePlacement));
            foreach (int player in new[] { 0,1 })
            {
                session.Events.Tile(new GridCell(6,player == 0 ? 5 : 14));
                for (int i = 0; i < 5; i++) session.Events.Tile(new GridCell(6,player == 0 ? 6 : 13));
            }
            yield return Planning(); Assert.That(session.View.Units.Count(u => u.Owner == 0 && u.Alive),Is.EqualTo(5));
            session.Events.Select(2); Button("Action Skill").onClick.Invoke(); session.Events.Tile(new GridCell(5,6));
            Assert.That(session.View.ReservedAP,Is.EqualTo(3)); Assert.That(Text("AP available").text,Is.EqualTo("07"));
            Button("Action Wait").onClick.Invoke(); Assert.That(Text("AP available").text,Is.EqualTo("10"));
            Button("Craft 2").onClick.Invoke(); Assert.That(session.View.Materials,Is.EqualTo(1));
            Button("Action Equipment").onClick.Invoke(); session.Events.Tile(new GridCell(5,6));
            Assert.That(session.View.ReservedAP,Is.EqualTo(4));
            yield return Capture("planning-landscape.png");
            Button("Ready").onClick.Invoke(); Assert.That(session.View.Ready,Is.True);
            Button("Switch perspective").onClick.Invoke(); Assert.That(session.Player,Is.EqualTo(1)); Button("Ready").onClick.Invoke();
            Assert.That(root.GetComponentInChildren<LocalBattleCamera>().ManualEnabled,Is.False);
            yield return Planning(); Assert.That(session.Match.Turn,Is.EqualTo(2));
            Assert.That(session.Match.DebugSnapshot().Structures.Count,Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { 16,20 },session.Match.DebugSnapshot().AP);
            Button("Switch perspective").onClick.Invoke(); Assert.That(session.Player,Is.Zero,"Replacing configuration must not duplicate event subscriptions.");
            session.Events.Select(1); session.Events.Craft(1,false); session.Events.SetAction(BattleAction.Equipment);
            Assert.That(Text("Action information").text,Does.Contain("RANGE GLOBAL"));
            yield return Capture("turn-two-landscape.png"); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AllTurnsCanReachVictoryAndRestartReturnsToSetup()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
            for (int turn = 0; turn < 12 && session.Match.Phase != BattlePhase.Finished; turn++)
            {
                session.Events.Select(5); var unit = session.Selected;
                session.Events.SetAction(unit.ReadyOn[(int)BattleAction.Skill] <= session.Match.Turn ? BattleAction.Skill : BattleAction.Attack);
                session.Events.Tile(session.Match.DebugSnapshot().Bases[1].Cell);
                Assert.That(session.View.Orders.Any(o => o.UnitId == 5),Is.True,session.Message);
                Button("Resolve").onClick.Invoke(); yield return Planning();
            }
            Assert.That(session.Match.Phase,Is.EqualTo(BattlePhase.Finished)); Assert.That(session.View.Winner,Is.EqualTo(BattleWinner.PlayerOne));
            Assert.That(Text("Result").text,Does.Contain("PLAYER 1 WINS")); yield return Capture("victory-landscape.png");
            Button("New local match").onClick.Invoke(); Assert.That(session.Match.Phase,Is.EqualTo(BattlePhase.Setup)); Assert.That(session.View.AP,Is.Zero);
            Assert.That(session.View.Materials,Is.EqualTo(2)); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator KeyboardMousePathDragZoomPanAndUiBlockingUseRealInputEvents()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
            var mouse = InputSystem.AddDevice<Mouse>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Digit5)); yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                Assert.That(session.SelectedId,Is.EqualTo(5));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Digit5)); yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                var rig = root.GetComponentInChildren<LocalBattleCamera>(); var world = root.GetComponentInChildren<LocalBattleWorld>();
                Assert.That(Vector3.Distance(rig.FocusPoint,world.Position(session.Selected.Cell)),Is.LessThan(0.01f));
                yield return null; yield return null; Physics.SyncTransforms();
                var target = new GridCell(session.Selected.Cell.X+1,session.Selected.Cell.Y);
                Vector2 screen = rig.Camera.WorldToScreenPoint(world.Position(target)+Vector3.up*0.05f);
                InputSystem.QueueStateEvent(mouse,new MouseState { position = screen }.WithButton(MouseButton.Right)); yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState { position = screen }); yield return null;
                Assert.That(session.View.Orders.Single().Action,Is.EqualTo(BattleAction.Move));
                float zoom = rig.Zoom;
                InputSystem.QueueStateEvent(mouse,new MouseState { position = screen, scroll = new Vector2(0,120) }); yield return null;
                Assert.That(rig.Zoom,Is.LessThan(zoom));
                Vector2 ui = RectTransformUtility.WorldToScreenPoint(null,Button("Ready").transform.position); zoom = rig.Zoom;
                InputSystem.QueueStateEvent(mouse,new MouseState { position = ui, scroll = new Vector2(0,120) }); yield return null;
                Assert.That(rig.Zoom,Is.EqualTo(zoom));
                session.Events.SetAction(BattleAction.Wait); yield return null;
                Vector2 start = rig.Camera.WorldToScreenPoint(world.Position(session.Selected.Cell)+Vector3.up*0.75f);
                Vector2 end = rig.Camera.WorldToScreenPoint(world.Position(target)+Vector3.up*0.05f);
                InputSystem.QueueStateEvent(mouse,new MouseState { position = start }.WithButton(MouseButton.Left)); yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState { position = end }.WithButton(MouseButton.Left)); yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState { position = end }); yield return null;
                Assert.That(session.View.Orders.Single().Path.Last(),Is.EqualTo(target));
                var before = rig.FocusPoint; Vector2 empty = rig.Camera.WorldToScreenPoint(world.Position(new GridCell(3,9)));
                InputSystem.QueueStateEvent(mouse,new MouseState { position = empty }.WithButton(MouseButton.Left)); yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState { position = empty+new Vector2(40,20) }.WithButton(MouseButton.Left)); yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState { position = empty+new Vector2(40,20) }); yield return null;
                Assert.That(Vector3.Distance(before,rig.FocusPoint),Is.GreaterThan(0.05f));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab)); yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
                Assert.That(session.Player,Is.EqualTo(1)); Assert.That(rig.Camera.transform.eulerAngles.y,Is.EqualTo(180).Within(0.1f));
                LogAssert.NoUnexpectedReceived();
            }
            finally { InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard); }
        }
        [UnityTest] public IEnumerator PortraitLayoutAndHelpKeepCommandsAccessible()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(900,1600,"TurnZero QA");
#endif
            yield return null; yield return null; yield return null; Canvas.ForceUpdateCanvases();
            session.Events.Select(3); session.Events.SetAction(BattleAction.Skill); yield return Capture("planning-portrait.png");
            Assert.That(Button("Ready").gameObject.activeInHierarchy,Is.True);
            Assert.That(Button("Action Skill").interactable,Is.True);
            Button("Controls").onClick.Invoke(); Assert.That(session.ShowHelp,Is.True); yield return Capture("controls-portrait.png");
            Button("Close guide").onClick.Invoke(); Assert.That(session.ShowHelp,Is.False); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TouchPanPinchAndDoubleTapCardsUseTheSameInputRoutes()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
            var touch = InputSystem.AddDevice<Touchscreen>();
            try
            {
                var rig = root.GetComponentInChildren<LocalBattleCamera>(); var world = root.GetComponentInChildren<LocalBattleWorld>();
                Vector2 center = rig.Camera.pixelRect.center; var focus = rig.FocusPoint;
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = center, pressure = 1 }); yield return null;
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = center+new Vector2(55,0), pressure = 1 }); yield return null;
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = center+new Vector2(55,0) }); yield return null;
                Assert.That(Vector3.Distance(focus,rig.FocusPoint),Is.GreaterThan(0.05f));
                float zoom = rig.Zoom;
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Began, position = center-new Vector2(30,0), pressure = 1 });
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 3, phase = UnityEngine.InputSystem.TouchPhase.Began, position = center+new Vector2(30,0), pressure = 1 }); yield return null;
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = center-new Vector2(80,0), pressure = 1 });
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 3, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = center+new Vector2(80,0), pressure = 1 }); yield return null;
                Assert.That(rig.Zoom,Is.LessThan(zoom));
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = center-new Vector2(80,0) });
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 3, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = center+new Vector2(80,0) }); yield return null;
                Vector2 card = RectTransformUtility.WorldToScreenPoint(null,GameObject.Find("Own card 5").transform.position);
                for (int i = 0; i < 2; i++)
                {
                    InputSystem.QueueStateEvent(touch,new TouchState { touchId = 4+i, phase = UnityEngine.InputSystem.TouchPhase.Began, position = card, pressure = 1 }); yield return null;
                    InputSystem.QueueStateEvent(touch,new TouchState { touchId = 4+i, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = card }); yield return null;
                }
                Assert.That(session.SelectedId,Is.EqualTo(5)); Assert.That(Vector3.Distance(rig.FocusPoint,world.Position(session.Selected.Cell)),Is.LessThan(0.01f));
                LogAssert.NoUnexpectedReceived();
            }
            finally { InputSystem.RemoveDevice(touch); }
        }
        [UnityTest] public IEnumerator ClickingAnAlreadyDeployedModelPlacesTheNextCharacterOnItsTile()
        {
            Button("Start deployment").onClick.Invoke();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var camera = root.GetComponentInChildren<LocalBattleCamera>().Camera; var world = root.GetComponentInChildren<LocalBattleWorld>();
                foreach (var target in new[] { new GridCell(6,5),new GridCell(6,6),new GridCell(6,6) })
                {
                    Physics.SyncTransforms(); Vector2 point = camera.WorldToScreenPoint(world.Position(target)+Vector3.up*0.7f);
                    InputSystem.QueueStateEvent(mouse,new MouseState { position = point }); yield return null;
                    InputSystem.QueueStateEvent(mouse,new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null;
                    InputSystem.QueueStateEvent(mouse,new MouseState { position = point }); yield return null;
                }
                Assert.That(session.View.PlacementSlot,Is.EqualTo(2));
                Assert.That(session.View.Units.Count(u => u.Owner == 0 && u.Alive && u.Cell == new GridCell(6,6)),Is.EqualTo(2));
                session.Events.AutoDeploy(); yield return Planning(); LogAssert.NoUnexpectedReceived();
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }
        [UnityTest] public IEnumerator DragPathsStopAtMovementRangeAllowBacktrackingAndUseStealthSkillRange()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
            var rig = root.GetComponentInChildren<LocalBattleCamera>(); var world = root.GetComponentInChildren<LocalBattleWorld>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                session.Events.Select(5,true); session.Events.SetAction(BattleAction.Wait); yield return null; yield return null;
                Physics.SyncTransforms(); var start = session.Selected.Cell;
                Vector2 point = rig.Camera.WorldToScreenPoint(world.Position(start)+Vector3.up*0.75f);
                InputSystem.QueueStateEvent(mouse,new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null;
                int maximum = session.Rules.Characters[4].Move.Range;
                for (int step = 1; step <= maximum+1; step++)
                {
                    point = rig.Camera.WorldToScreenPoint(world.Position(new GridCell(start.X,start.Y+step))+Vector3.up*0.05f);
                    InputSystem.QueueStateEvent(mouse,new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null;
                    Assert.That(session.PathPreview.Count,Is.EqualTo(Mathf.Min(step,maximum)));
                }
                point = rig.Camera.WorldToScreenPoint(world.Position(new GridCell(start.X,start.Y+1))+Vector3.up*0.05f);
                InputSystem.QueueStateEvent(mouse,new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null;
                Assert.That(session.PathPreview.Count,Is.EqualTo(1));
                var end = new GridCell(start.X+1,start.Y+1);
                point = rig.Camera.WorldToScreenPoint(world.Position(end)+Vector3.up*0.05f);
                InputSystem.QueueStateEvent(mouse,new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null;
                Assert.That(session.PathPreview.Last(),Is.EqualTo(end));
                InputSystem.QueueStateEvent(mouse,new MouseState { position = point }); yield return null;
                var order = session.View.Orders.Single(o => o.UnitId == 5);
                Assert.That(order.Path.Count,Is.EqualTo(maximum)); Assert.That(order.Target,Is.EqualTo(end));
                Assert.That(session.PathPreview,Is.Empty);
            }
            finally { InputSystem.RemoveDevice(mouse); }
            var touch = InputSystem.AddDevice<Touchscreen>();
            try
            {
                session.Events.Select(2,true); session.Events.SetAction(BattleAction.Skill); yield return null; yield return null;
                session.Events.Zoom(-0.25f); yield return null;
                Physics.SyncTransforms(); var start = session.Selected.Cell;
                Vector2 point = rig.Camera.WorldToScreenPoint(world.Position(start)+Vector3.up*0.75f);
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 1,phase = UnityEngine.InputSystem.TouchPhase.Began,position = point,pressure = 1 }); yield return null;
                int maximum = session.Rules.Characters[1].Skill.Range;
                Assert.That(maximum,Is.LessThan(session.Rules.Characters[1].Move.Range));
                for (int step = 1; step <= maximum+1; step++)
                {
                    point = rig.Camera.WorldToScreenPoint(world.Position(new GridCell(start.X,start.Y+step))+Vector3.up*0.05f);
                    InputSystem.QueueStateEvent(touch,new TouchState { touchId = 1,phase = UnityEngine.InputSystem.TouchPhase.Moved,position = point,pressure = 1 }); yield return null;
                    Assert.That(session.PathPreview.Count,Is.EqualTo(Mathf.Min(step,maximum)));
                }
                InputSystem.QueueStateEvent(touch,new TouchState { touchId = 1,phase = UnityEngine.InputSystem.TouchPhase.Ended,position = point }); yield return null;
                var order = session.View.Orders.Single(o => o.UnitId == 2);
                Assert.That(order.Action,Is.EqualTo(BattleAction.Skill)); Assert.That(order.Path.Count,Is.EqualTo(maximum));
                Assert.That(order.Target,Is.EqualTo(new GridCell(start.X,start.Y+maximum))); LogAssert.NoUnexpectedReceived();
            }
            finally { InputSystem.RemoveDevice(touch); }
        }
        [UnityTest] public IEnumerator CloseInitialZoomAndTenfoldSensitivityApplyWithinLimitsAndResetForRematch()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning(); yield return null;
            var rig = root.GetComponentInChildren<LocalBattleCamera>(); var world = root.GetComponentInChildren<LocalBattleWorld>();
            Assert.That(rig.Zoom,Is.EqualTo(0.45f).Within(0.0001f));
            foreach (var cell in session.View.Units.Where(u => u.Owner == session.Player && u.Alive).Select(u => u.Cell).Append(session.View.OwnBase.Cell))
            {
                var point = rig.Camera.WorldToViewportPoint(world.Position(cell)+Vector3.up*0.7f);
                Assert.That(point.z,Is.GreaterThan(0)); Assert.That(point.x,Is.InRange(0f,1f)); Assert.That(point.y,Is.InRange(0f,1f));
            }
            session.Events.Zoom(0.1f);
            Assert.That(rig.Zoom,Is.EqualTo(0.45f*Mathf.Exp(-0.12f)).Within(0.0001f));
            session.Events.Zoom(100); Assert.That(rig.Zoom,Is.EqualTo(configuration.MinimumZoom));
            session.Events.Zoom(-100); Assert.That(rig.Zoom,Is.EqualTo(configuration.MaximumZoom));
            session.Events.Restart(); Assert.That(rig.Zoom,Is.EqualTo(configuration.InitialZoom));
            configuration.InitialZoom = 0.6f; session.Initialize(configuration);
            Assert.That(rig.Zoom,Is.EqualTo(0.6f)); LogAssert.NoUnexpectedReceived();
        }
        private IEnumerator Capture(string filename)
        {
            string folder = Environment.GetEnvironmentVariable("TURNZERO_LOCAL_PREVIEWS"); if (string.IsNullOrEmpty(folder)) yield break;
            Directory.CreateDirectory(folder); yield return null; yield return null; Canvas.ForceUpdateCanvases();
            var canvas = root.GetComponentInChildren<Canvas>(); var main = root.GetComponentInChildren<LocalBattleCamera>().Camera;
            int width = Screen.width, height = Screen.height;
            var viewport = main.rect; float aspect = main.aspect;
            var worldTexture = new RenderTexture(Mathf.Max(1,Mathf.RoundToInt(width*viewport.width)),Mathf.Max(1,Mathf.RoundToInt(height*viewport.height)),24);
            var finalTexture = new RenderTexture(width,height,24); var pixels = new Texture2D(width,height,TextureFormat.RGBA32,false);
            var captureObject = new GameObject("QA UI camera",typeof(Camera)); var captureCamera = captureObject.GetComponent<Camera>();
            var previousTarget = main.targetTexture; var previousActive = RenderTexture.active;
            var rawObject = new GameObject("QA board image",typeof(RectTransform),typeof(RawImage));
            try
            {
                worldTexture.Create(); finalTexture.Create();
                main.rect = new Rect(0,0,1,1); main.targetTexture = worldTexture; main.aspect = aspect;
                RenderPipeline.SubmitRenderRequest(main,new UniversalRenderPipeline.SingleCameraRequest { destination = worldTexture });
                main.targetTexture = previousTarget; main.rect = viewport; main.aspect = aspect;
                captureCamera.clearFlags = CameraClearFlags.SolidColor; captureCamera.backgroundColor = new Color(0.015f,0.025f,0.04f); captureCamera.orthographic = true;
                captureCamera.cullingMask = 1 << LayerMask.NameToLayer("UI"); captureCamera.targetTexture = finalTexture;
                rawObject.layer = LayerMask.NameToLayer("UI"); rawObject.transform.SetParent(canvas.transform,false); rawObject.transform.SetAsFirstSibling();
                var rect = rawObject.GetComponent<RectTransform>(); rect.anchorMin = viewport.min; rect.anchorMax = viewport.max; rect.offsetMin = rect.offsetMax = Vector2.zero;
                rawObject.GetComponent<RawImage>().texture = worldTexture; rawObject.GetComponent<RawImage>().raycastTarget = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = captureCamera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(captureCamera,new UniversalRenderPipeline.SingleCameraRequest { destination = finalTexture });
                RenderTexture.active = finalTexture; pixels.ReadPixels(new Rect(0,0,width,height),0,0); pixels.Apply(); File.WriteAllBytes(Path.Combine(folder,filename),pixels.EncodeToPNG());
            }
            finally
            {
                main.targetTexture = previousTarget; main.rect = viewport; main.aspect = aspect; RenderTexture.active = previousActive;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
                Object.Destroy(rawObject); Object.Destroy(captureObject); Object.Destroy(pixels); worldTexture.Release(); finalTexture.Release(); Object.Destroy(worldTexture); Object.Destroy(finalTexture);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator ReadyCanBeUnlockedThroughTheHudWithoutRestartingPlanning()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
            session.Events.Select(1); session.Events.SetAction(BattleAction.Move);
            var target = new GridCell(session.Selected.Cell.X,session.Selected.Cell.Y+1); session.Events.Tile(target);
            int reserved = session.View.ReservedAP; double seconds = session.View.SecondsLeft;
            Button("Ready").onClick.Invoke(); Assert.That(session.View.Ready,Is.True);
            Assert.That(Button("Ready").interactable,Is.True);
            Button("Ready").onClick.Invoke(); Assert.That(session.View.Ready,Is.False);
            Assert.That(session.View.ReservedAP,Is.EqualTo(reserved));
            Assert.That(session.View.SecondsLeft,Is.LessThanOrEqualTo(seconds));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator SimultaneousMovesStartOnTheSameFrameAndRestoreTheManualCamera()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
            configuration.MoveSeconds = 0.15f;
            foreach (int id in new[] { 1,2 })
            {
                session.Events.Select(id); session.Events.SetAction(BattleAction.Move);
                session.Events.Tile(new GridCell(session.Selected.Cell.X,session.Selected.Cell.Y+1));
            }
            var frames = new System.Collections.Generic.List<int>();
            Action<BattleCue> observe = cue => { if (cue.Kind == CueKind.Move) frames.Add(Time.frameCount); };
            var rig = root.GetComponentInChildren<LocalBattleCamera>(); var focus = rig.FocusPoint; float zoom = rig.Zoom;
            session.Events.CueRequested += observe;
            Button("Resolve").onClick.Invoke(); yield return Planning();
            session.Events.CueRequested -= observe;
            Assert.That(frames.Count,Is.EqualTo(2)); Assert.That(frames.Distinct().Count(),Is.EqualTo(1));
            Assert.That(rig.FocusPoint,Is.EqualTo(focus)); Assert.That(rig.Zoom,Is.EqualTo(zoom));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator KillRewardFlashesTheActualApIncreaseThroughTheReplay()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
            int rewards = 0; bool flash = false;
            Action<BattleCue> observe = cue => {
                if (cue.Kind != CueKind.Reward || cue.Owner != 0) return;
                rewards++; flash = Text("AP heading").text == $"+{cue.APDelta} AP";
            };
            session.Events.CueRequested += observe;
            for (int i = 0; i < 4 && rewards == 0; i++)
            {
                session.Events.Select(5);
                var action = session.Selected.ReadyOn[(int)BattleAction.Skill] <= session.Match.Turn ? BattleAction.Skill : BattleAction.Attack;
                session.Events.SetAction(action);
                session.Events.Tile(session.Match.DebugSnapshot().Units.Single(u => u.Id == 6).Cell);
                Assert.That(session.View.Orders.Any(o => o.UnitId == 5),Is.True,session.Message);
                Button("Resolve").onClick.Invoke(); yield return Planning();
            }
            session.Events.CueRequested -= observe;
            Assert.That(rewards,Is.EqualTo(1)); Assert.That(flash,Is.True);
            session.Events.Restart(); yield return null;
            Assert.That(Text("AP heading").text,Is.EqualTo("ACTION POINTS"));
            LogAssert.NoUnexpectedReceived();
        }


        [UnityTest] public IEnumerator CompletelyHiddenMovementDoesNotStartACameraTransition()
        {
            Button("Quick deploy").onClick.Invoke(); yield return Planning();
            var before = session.Match.DebugSnapshot(); var after = before.Copy();
            var actor = after.Units.Single(u => u.Id == 6); var source = actor.Cell;
            actor.Cell = new GridCell(source.X,source.Y+1);
            var step = new ResolutionStep { Before = before, After = after, SimultaneousMovement = true, Phase = 2 };
            step.Cues.Add(new BattleCue { Kind = CueKind.Move, Owner = 1, ActorId = 6, Source = source, Target = actor.Cell });
            Assert.That(step.VisibleCues(session.Rules,0,false),Is.Empty);
            var timeline = new BattleTimeline { Final = after }; timeline.Steps.Add(step);
            int moves = 0; Action observe = () => moves++; session.Events.MovementRequested += observe;
            var playback = new BattlePlayback(configuration,session.Events);
            yield return playback.Play(timeline,session.Rules,0,false,_ => { });
            session.Events.MovementRequested -= observe;
            Assert.That(moves,Is.Zero); LogAssert.NoUnexpectedReceived();
        }

    }
}

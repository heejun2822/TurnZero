using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed partial class BattlePrototype
    {
        private static readonly string[] Roles = { "GUARDIAN", "SCOUT", "RANGER", "SUPPORT" };
        private static readonly Color Ink = new Color(0.035f, 0.065f, 0.09f, 0.97f);
        private static readonly Color Edge = new Color(0.18f, 0.29f, 0.36f);
        private readonly Sprite[] portraits = new Sprite[4];
        private readonly Sprite[] cardPortraits = new Sprite[4];
        private Text reservedText;
        private void LoadPortraits() { }

        private void BuildUI()
        {
            canvasRoot = new GameObject("Battle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);
            canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            safeArea = Rect("Safe Area", canvasRoot.transform, Vector2.zero, Vector2.one);
            var header = Box("Header", safeArea, new Vector2(0, 0.94f), Vector2.one, Panel);
            phaseText = Label("Phase", header, Vector2.zero, new Vector2(0.52f, 1), "DEPLOYMENT", 25, Color.white, TextAnchor.MiddleLeft);
            timerText = Label("Timer", header, new Vector2(0.55f, 0), new Vector2(0.7f, 1), "--:--", 25, Gold, TextAnchor.MiddleCenter);
            apText = Label("AP", header, new Vector2(0.73f, 0), new Vector2(0.98f, 1), "", 25, Blue, TextAnchor.MiddleRight);
            var toolbar = Rect("Field Toolbar", safeArea, new Vector2(0.01f, 0.887f), new Vector2(0.755f, 0.932f));
            MakeButton("Switch Player", toolbar, Vector2.zero, new Vector2(0.26f, 1), "", SwitchPlayer, out switchText);
            MakeButton("Rotate Camera", toolbar, new Vector2(0.28f, 0), new Vector2(0.46f, 1), "ROTATE", () => world.Rotate(), out _);
            MakeButton("Zoom In", toolbar, new Vector2(0.48f, 0), new Vector2(0.55f, 1), "+", () => world.Zoom(-0.1f), out _);
            MakeButton("Zoom Out", toolbar, new Vector2(0.57f, 0), new Vector2(0.64f, 1), "-", () => world.Zoom(0.1f), out _);
            MakeButton("Export Recording", toolbar, new Vector2(0.66f, 0), new Vector2(0.83f, 1), "SAVE REPLAY", SaveRecording, out _);
            MakeButton("Restart", toolbar, new Vector2(0.85f, 0), Vector2.one, "RESTART", Restart, out _);
            boardPanel = Rect("Field", safeArea, new Vector2(0.01f, 0.14f), new Vector2(0.755f, 0.875f));
            controlsPanel = Box("Planning", safeArea, new Vector2(0.77f, 0.14f), new Vector2(0.99f, 0.875f), Panel);
            selectionText = Label("Selected Unit", controlsPanel, new Vector2(0.04f, 0.9f), new Vector2(0.96f, 0.99f), "SELECT UNIT", 24, Color.white, TextAnchor.MiddleCenter);
            unitButtons = new Button[4];
            unitLabels = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                unitButtons[i] = MakeButton($"Unit {i + 1}", controlsPanel, new Vector2(0.05f, 0.79f - i * 0.09f),
                    new Vector2(0.95f, 0.87f - i * 0.09f), "", () => SelectUnit(player * 4 + slot + 1), out unitLabels[i]);
            }
            moveButton = MakeButton("Move", controlsPanel, new Vector2(0.05f, 0.38f), new Vector2(0.47f, 0.46f), "MOVE", () => SetMode(ActionKind.Move), out _);
            attackButton = MakeButton("Attack", controlsPanel, new Vector2(0.53f, 0.38f), new Vector2(0.95f, 0.46f), "ATTACK", () => SetMode(ActionKind.Attack), out _);
            waitButton = MakeButton("Wait", controlsPanel, new Vector2(0.05f, 0.29f), new Vector2(0.47f, 0.37f), "WAIT", Wait, out _);
            lockButton = MakeButton("Lock Orders", controlsPanel, new Vector2(0.53f, 0.29f), new Vector2(0.95f, 0.37f), "READY", LockOrders, out lockText);
            detailsText = Label("Order Details", controlsPanel, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.28f), "", 20, Color.white, TextAnchor.MiddleLeft);
            reservedText = Label("Reserved AP", controlsPanel, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.2f), "", 20, Blue, TextAnchor.MiddleLeft);
            resolveButton = MakeButton("Resolve Turn", controlsPanel, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.12f), "ADVANCE TURN", ResolveNow, out _);
            var status = Box("Field Status", safeArea, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.125f), Panel, false);
            messageText = Label("Help", status, new Vector2(0.01f, 0.48f), new Vector2(0.99f, 0.99f), "", 18, Color.white, TextAnchor.MiddleLeft);
            logText = Label("Observed Results", status, new Vector2(0.01f, 0), new Vector2(0.99f, 0.48f), "", 17, Gold, TextAnchor.MiddleLeft);
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Battle EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
        }

        private void RefreshHud()
        {
            phaseText.text = view.Phase == MatchPhase.Placement ? $"P{player + 1} / PLACE YOUR BASE" : view.Phase == MatchPhase.Finished ? Outcome(view.Outcome) : $"TURN {view.Turn:00}  |  PLAN ORDERS";
            switchText.text = $"VIEW P{player + 1} / SWITCH";
            apText.text = $"{view.AP - view.ReservedAP} / {activeRules.MaximumAP}";
            reservedText.text = $"RESERVED {view.ReservedAP}";
            var selected = view.Entities.Find(e => e.Id == selectedId && e.Health > 0);
            int slot = selected == null ? 0 : selected.Id - player * 4 - 1;
            selectionText.text = selected == null ? "SELECT A UNIT" : $"{slot + 1:00} {Roles[slot]}";
            var order = view.Orders.Find(o => o.UnitId == selectedId);
            detailsText.text = order == null ? "WAIT / 0 AP" : $"{order.Kind.ToString().ToUpperInvariant()} / {activeRules.Cost(order.Kind)} AP";
            bool canPlan = view.Phase == MatchPhase.Planning && !view.Locked;
            moveButton.interactable = attackButton.interactable = waitButton.interactable = canPlan && selected != null;
            lockButton.interactable = canPlan;
            lockText.text = view.Locked ? "LOCKED" : "READY";
            resolveButton.interactable = view.Phase == MatchPhase.Planning;
            for (int i = 0; i < 4; i++)
            {
                var unit = view.Entities.Find(e => e.Id == player * 4 + i + 1);
                unitLabels[i].text = $"{i + 1:00} {Roles[i]} / " + (unit == null ? "HP --" : $"{unit.Health} HP");
                unitButtons[i].interactable = view.Phase == MatchPhase.Planning && unit != null && unit.Health > 0;
                unitButtons[i].image.color = unit != null && unit.Id == selectedId ? new Color(0.02f, 0.3f, 0.33f) : Panel;
            }
            logText.text = view.Messages.LastOrDefault() ?? "";
            if (view.Phase == MatchPhase.Finished) messageText.text = Outcome(view.Outcome) + ". Restart to play again.";
        }

        private void UpdateLayout()
        {
            var canvas = canvasRoot.GetComponent<Canvas>();
            var pixels = canvas.pixelRect;
            if (pixels.size != lastSize || Screen.safeArea != lastSafeArea)
            {
                lastSize = pixels.size;
                lastSafeArea = Screen.safeArea;
                safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
                Canvas.ForceUpdateCanvases();
            }
            boardPanel.GetWorldCorners(fieldCorners);
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var lower = RectTransformUtility.WorldToScreenPoint(uiCamera, fieldCorners[0]);
            var upper = RectTransformUtility.WorldToScreenPoint(uiCamera, fieldCorners[2]);
            world.SetViewport(new Rect((lower.x - pixels.x) / pixels.width, (lower.y - pixels.y) / pixels.height,
                (upper.x - lower.x) / pixels.width, (upper.y - lower.y) / pixels.height));
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.gameObject.layer = 5;
            result.SetParent(parent, false);
            SetAnchors(result, min, max);
            return result;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Box(string name, Transform parent, Vector2 min, Vector2 max, Color color, bool blocks = true)
        {
            var rect = Rect(name, parent, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = blocks;
            return rect;
        }

        private static void Frame(GameObject target, Color color)
        {
            var outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.effectColor = color;
        }

        private static Image Bar(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var background = Box(name, parent, min, max, new Color(0.1f, 0.16f, 0.2f), false);
            return Box("Fill", background, Vector2.zero, Vector2.one, color, false).GetComponent<Image>();
        }

        private static void Fill(Image image, float amount) => image.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(amount), 1);

        private static Image Icon(string name, Transform parent, Vector2 min, Vector2 max, string spriteName, Color color)
        {
            var image = Box(name, parent, min, max, color, false).GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>("PrototypeUI/" + spriteName);
            image.preserveAspect = true;
            return image;
        }

        private Text Label(string name, Transform parent, Vector2 min, Vector2 max, string value, int size, Color color, TextAnchor alignment)
        {
            var rect = Rect(name, parent, min, max);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontStyle = FontStyle.Bold;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 7;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max, string caption, Action action, out Text label)
        {
            var rect = Box(name, parent, min, max, Ink);
            Frame(rect.gameObject, Edge);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => action());
            label = Label("Label", rect, new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.975f), caption, 17, Color.white, TextAnchor.MiddleCenter);
            return button;
        }

    }
}

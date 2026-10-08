using System;
using UnityEngine;
using UnityEngine.UI;
namespace TurnZero.Editor
{
    internal sealed partial class BattlePrefabBuilder
    {
        private static readonly Color Panel = new Color(0.075f, 0.105f, 0.16f);
        private static readonly Color Blue = new Color(0.03f, 0.91f, 0.93f);
        private static readonly Color Gold = new Color(1f, 0.77f, 0.34f);
        private static readonly Color Ink = new Color(0.035f, 0.065f, 0.09f, 0.97f);
        private static readonly Color Edge = new Color(0.18f, 0.29f, 0.36f);
        private static readonly Color Muted = new Color(0.64f, 0.75f, 0.82f);
        private static readonly Color Enemy = new Color(1f, 0.32f, 0.37f);
        private static readonly Color Health = new Color(0.24f, 0.95f, 0.64f);
        private static readonly string[] Roles = { "GUARDIAN", "SCOUT", "RANGER", "SUPPORT" };
        private static readonly string[] Weapons = { "Practice Blade", "Light Dagger", "Field Bow", "Focus Staff" };
        private readonly Sprite[] portraits = new Sprite[4];
        private readonly Sprite[] cardPortraits = new Sprite[4];
        private RectTransform headerPanel;
        private RectTransform teamPanel;
        private RectTransform apPanel;
        private RectTransform toolbar;
        private RectTransform helpPanel;
        private Image selectedPortrait;
        private Image selectedHealth;
        private Text selectedHealthText;
        private Text equipmentText;
        private Image equipmentIcon;
        private Text ownBaseText;
        private Text enemyBaseText;
        private Image ownBaseBar;
        private Image enemyBaseBar;
        private Text reservedText;
        private Text orderTargetText;
        private Image[] apPips;
        private Image[] unitHealth;
        private Text[] unitHealthText;
        private Text[] unitOrderText;
        private Image[] unitOrderPanels;
        private Image[] unitOrderIcons;
        private Image[] previewCells;
        private Image previewArrow;

        private GameObject canvasRoot;
        private Font font;
        private RectTransform safeArea;
        private RectTransform boardPanel;
        private RectTransform controlsPanel;
        private Button[] unitButtons;
        private Text[] unitLabels;
        private Text phaseText;
        private Text timerText;
        private Text apText;
        private Text selectionText;
        private Text detailsText;
        private Text messageText;
        private Text logText;
        private Text switchText;
        private Button moveButton;
        private Button attackButton;
        private Button waitButton;
        private Button lockButton;
        private Button resolveButton;
        private Text lockText;
        private Vector2 lastSize;
        private Rect lastSafeArea;
        private readonly Vector3[] fieldCorners = new Vector3[4];

        private void BuildUI()
        {
            canvasRoot = new GameObject("Battle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(session.transform, false);
            canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            safeArea = Rect("Safe Area", canvasRoot.transform, Vector2.zero, Vector2.one);

            headerPanel = Box("Header", safeArea, new Vector2(0, 0.947f), Vector2.one, Ink);
            Icon("Ally Base Icon", headerPanel, new Vector2(0.014f, 0.15f), new Vector2(0.04f, 0.85f), "castle", Blue);
            Label("Ally Base", headerPanel, new Vector2(0.05f, 0), new Vector2(0.135f, 1), "ALLY BASE", 21, Blue, TextAnchor.MiddleLeft);
            ownBaseText = Label("Ally Base HP", headerPanel, new Vector2(0.137f, 0), new Vector2(0.198f, 1), "-- / --", 21, Blue, TextAnchor.MiddleLeft);
            ownBaseBar = Bar("Ally Base Health", headerPanel, new Vector2(0.205f, 0.35f), new Vector2(0.333f, 0.62f), Health);
            phaseText = Label("Phase", headerPanel, new Vector2(0.365f, 0), new Vector2(0.55f, 1), "DEPLOYMENT", 22, Muted, TextAnchor.MiddleCenter);
            timerText = Label("Timer", headerPanel, new Vector2(0.553f, 0), new Vector2(0.618f, 1), "--:--", 29, Gold, TextAnchor.MiddleCenter);
            enemyBaseBar = Bar("Enemy Base Health", headerPanel, new Vector2(0.675f, 0.35f), new Vector2(0.815f, 0.62f), Enemy);
            Icon("Enemy Base Icon", headerPanel, new Vector2(0.831f, 0.15f), new Vector2(0.856f, 0.85f), "castle", Enemy);
            Label("Enemy Base", headerPanel, new Vector2(0.862f, 0), new Vector2(0.955f, 1), "ENEMY BASE", 18, Enemy, TextAnchor.MiddleLeft);
            enemyBaseText = Label("Enemy Base HP", headerPanel, new Vector2(0.958f, 0), new Vector2(0.995f, 1), "? / ?", 18, Muted, TextAnchor.MiddleRight);

            boardPanel = Rect("Field", safeArea, new Vector2(0.008f, 0.21f), new Vector2(0.762f, 0.888f));
            toolbar = Rect("Field Toolbar", safeArea, new Vector2(0.012f, 0.895f), new Vector2(0.758f, 0.933f));
            MakeButton("Switch Player", toolbar, Vector2.zero, new Vector2(0.19f, 1), "VIEW P1", null, out switchText);
            MakeButton("Rotate Camera", toolbar, new Vector2(0.205f, 0), new Vector2(0.295f, 1), "ROTATE", null, out _);
            MakeButton("Zoom In", toolbar, new Vector2(0.306f, 0), new Vector2(0.362f, 1), "+", null, out _);
            MakeButton("Zoom Out", toolbar, new Vector2(0.372f, 0), new Vector2(0.428f, 1), "-", null, out _);
            resolveButton = MakeButton("Resolve Turn", toolbar, new Vector2(0.442f, 0), new Vector2(0.644f, 1), "ADVANCE TURN", null, out _);
            MakeButton("Export Recording", toolbar, new Vector2(0.657f, 0), new Vector2(0.815f, 1), "SAVE REPLAY", null, out _);
            MakeButton("Restart", toolbar, new Vector2(0.829f, 0), Vector2.one, "RESTART", null, out _);

            controlsPanel = Box("Planning", safeArea, new Vector2(0.774f, 0.187f), new Vector2(0.991f, 0.928f), Ink);
            Frame(controlsPanel.gameObject, Edge);
            var portraitRect = Rect("Selected Portrait", controlsPanel, new Vector2(0.48f, 0.77f), new Vector2(0.995f, 0.995f));
            selectedPortrait = portraitRect.gameObject.AddComponent<Image>();
            selectedPortrait.raycastTarget = false;
            selectionText = Label("Selected Unit", controlsPanel, new Vector2(0.055f, 0.91f), new Vector2(0.7f, 0.98f), "SELECT UNIT", 25, Color.white, TextAnchor.MiddleLeft);
            selectedHealthText = Label("Selected HP", controlsPanel, new Vector2(0.055f, 0.842f), new Vector2(0.46f, 0.9f), "HP -- / --", 21, Muted, TextAnchor.MiddleLeft);
            selectedHealth = Bar("Selected Health", controlsPanel, new Vector2(0.055f, 0.82f), new Vector2(0.46f, 0.836f), Health);
            var actionButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                string name = new[] { "Move", "Attack", "Skill", "Item" }[i];
                float left = 0.045f + i * 0.239f;
                int action = i;
                actionButtons[i] = MakeButton(name, controlsPanel, new Vector2(left, 0.64f), new Vector2(left + 0.208f, 0.754f), "",
                    null, out _);
                Icon(name + " Icon", actionButtons[i].transform, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), name.ToLowerInvariant(), i == 0 ? Blue : i == 1 ? Enemy : Muted);
                Label(name + " Caption", controlsPanel, new Vector2(left, 0.595f), new Vector2(left + 0.208f, 0.635f), name.ToUpperInvariant(), 18,
                    i == 0 ? Blue : Muted, TextAnchor.MiddleCenter);
                if (i > 1) actionButtons[i].interactable = false;
            }
            moveButton = actionButtons[0];
            attackButton = actionButtons[1];

            var orderPanel = Box("Selected Order Panel", controlsPanel, new Vector2(0.045f, 0.355f), new Vector2(0.955f, 0.57f), new Color(0.04f, 0.075f, 0.1f));
            Frame(orderPanel.gameObject, Edge);
            Label("Order Heading", orderPanel, new Vector2(0.045f, 0.74f), new Vector2(0.6f, 0.95f), "SELECTED ORDER", 16, Muted, TextAnchor.MiddleLeft);
            detailsText = Label("Order Details", orderPanel, new Vector2(0.045f, 0.39f), new Vector2(0.605f, 0.7f), "WAIT  /  0 AP", 24, Color.white, TextAnchor.MiddleLeft);
            orderTargetText = Label("Order Target", orderPanel, new Vector2(0.045f, 0.13f), new Vector2(0.61f, 0.35f), "Choose a target tile", 15, Muted, TextAnchor.MiddleLeft);
            var mini = Rect("Order Preview", orderPanel, new Vector2(0.65f, 0.09f), new Vector2(0.97f, 0.88f));
            previewCells = new Image[25];
            for (int i = 0; i < 25; i++)
                previewCells[i] = Box($"Preview Cell {i}", mini, new Vector2((i % 5) * 0.2f, (i / 5) * 0.2f),
                    new Vector2((i % 5) * 0.2f + 0.175f, (i / 5) * 0.2f + 0.175f), Edge, false).GetComponent<Image>();
            previewArrow = Icon("Preview Arrow", mini, new Vector2(0.43f, 0.43f), new Vector2(0.57f, 0.57f), "move", Blue);

            var equipment = Box("Equipment", controlsPanel, new Vector2(0.045f, 0.17f), new Vector2(0.955f, 0.332f), new Color(0.04f, 0.075f, 0.1f));
            Frame(equipment.gameObject, Edge);
            Label("Equipment Heading", equipment, new Vector2(0.045f, 0.73f), new Vector2(0.95f, 0.96f), "EQUIPMENT", 16, Muted, TextAnchor.MiddleLeft);
            var weaponBox = Box("Weapon Slot", equipment, new Vector2(0.035f, 0.12f), new Vector2(0.25f, 0.64f), Panel);
            Frame(weaponBox.gameObject, Edge);
            equipmentIcon = Icon("Weapon Icon", weaponBox, new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f), "bow", Gold);
            equipmentText = Label("Weapon Description", equipment, new Vector2(0.31f, 0.09f), new Vector2(0.965f, 0.65f), "", 20, Color.white, TextAnchor.MiddleLeft);
            waitButton = MakeButton("Wait", controlsPanel, new Vector2(0.045f, 0.033f), new Vector2(0.22f, 0.137f), "", null, out _);
            Icon("Cancel Icon", waitButton.transform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), "undo", Color.white);
            lockButton = MakeButton("Lock Orders", controlsPanel, new Vector2(0.257f, 0.033f), new Vector2(0.955f, 0.137f), "READY", null, out lockText);
            lockText.fontSize = 26;
            lockText.resizeTextMaxSize = 26;
            lockButton.image.color = new Color(0.015f, 0.48f, 0.53f);
            Frame(lockButton.gameObject, Blue);

            teamPanel = Box("Team Roster", safeArea, Vector2.zero, new Vector2(0.763f, 0.174f), Ink);
            Frame(teamPanel.gameObject, Edge);
            unitButtons = new Button[4];
            unitLabels = new Text[4];
            unitHealth = new Image[4];
            unitHealthText = new Text[4];
            unitOrderText = new Text[4];
            unitOrderPanels = new Image[4];
            unitOrderIcons = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                float left = 0.012f + i * 0.247f;
                unitButtons[i] = MakeButton($"Unit {i + 1}", teamPanel, new Vector2(left, 0.1f), new Vector2(left + 0.235f, 0.9f), "", null, out unitLabels[i]);
                var card = unitButtons[i].transform;
                var image = Box("Portrait", card, new Vector2(0.006f, 0.01f), new Vector2(0.295f, 0.99f), Color.white, false).GetComponent<Image>();
                image.sprite = cardPortraits[i];
                SetAnchors(unitLabels[i].rectTransform, new Vector2(0.33f, 0.7f), new Vector2(0.985f, 0.97f));
                unitLabels[i].alignment = TextAnchor.MiddleLeft;
                unitLabels[i].fontSize = 20;
                unitLabels[i].resizeTextMaxSize = 20;
                unitHealthText[i] = Label("Card HP", card, new Vector2(0.33f, 0.54f), new Vector2(0.975f, 0.73f), "", 16, Muted, TextAnchor.MiddleLeft);
                unitHealth[i] = Bar("Card Health", card, new Vector2(0.33f, 0.45f), new Vector2(0.965f, 0.52f), Health);
                var order = Box("Card Order", card, new Vector2(0.32f, 0.035f), new Vector2(0.972f, 0.38f), Panel, false);
                Frame(order.gameObject, Edge);
                unitOrderPanels[i] = order.GetComponent<Image>();
                unitOrderIcons[i] = Icon("Card Order Icon", order, new Vector2(0.04f, 0.12f), new Vector2(0.21f, 0.88f), "wait", Muted);
                unitOrderText[i] = Label("Card Order Text", order, new Vector2(0.26f, 0.1f), new Vector2(0.98f, 0.9f), "WAIT", 18, Muted, TextAnchor.MiddleCenter);
            }

            apPanel = Box("AP Panel", safeArea, new Vector2(0.769f, 0), new Vector2(1, 0.174f), Ink);
            Frame(apPanel.gameObject, Edge);
            Label("AP Heading", apPanel, new Vector2(0.08f, 0.57f), new Vector2(0.27f, 0.92f), "AP", 37, Blue, TextAnchor.MiddleLeft);
            apText = Label("AP", apPanel, new Vector2(0.32f, 0.54f), new Vector2(0.94f, 0.96f), "0 / 8", 44, Color.white, TextAnchor.MiddleLeft);
            reservedText = Label("Reserved AP", apPanel, new Vector2(0.08f, 0.02f), new Vector2(0.93f, 0.19f), "RESERVED 0", 16, Muted, TextAnchor.MiddleRight);
            apPips = new Image[activeRules.MaximumAP];
            for (int i = 0; i < apPips.Length; i++)
            {
                float width = 0.84f / apPips.Length;
                apPips[i] = Box($"AP Pip {i}", apPanel, new Vector2(0.08f + i * width, 0.25f), new Vector2(0.08f + i * width + width * 0.86f, 0.46f), Edge, false).GetComponent<Image>();
            }
            helpPanel = Box("Field Status", safeArea, new Vector2(0.014f, 0.178f), new Vector2(0.755f, 0.214f), new Color(0.025f, 0.05f, 0.075f, 0.88f), false);
            messageText = Label("Help", helpPanel, new Vector2(0.01f, 0), new Vector2(0.65f, 1), "", 15, Muted, TextAnchor.MiddleLeft);
            logText = Label("Observed Results", helpPanel, new Vector2(0.665f, 0), new Vector2(0.99f, 1), "", 14, Gold, TextAnchor.MiddleRight);
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

            label = Label("Label", rect, new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.975f), caption, 17, Color.white, TextAnchor.MiddleCenter);
            return button;
        }

    }
}

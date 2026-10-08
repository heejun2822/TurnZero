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

        private void LoadPortraits()
        {
            var atlas = Resources.Load<Texture2D>("PrototypeUI/portrait-atlas");
            for (int i = 0; i < portraits.Length; i++)
            {
                portraits[i] = Sprite.Create(atlas, new Rect((i % 2) * atlas.width / 2, (1 - i / 2) * atlas.height / 2,
                    atlas.width / 2, atlas.height / 2), new Vector2(0.5f, 0.5f), 100);
                cardPortraits[i] = Sprite.Create(atlas, new Rect(((i % 2) + 0.18f) * atlas.width / 2, (1 - i / 2) * atlas.height / 2,
                    atlas.width * 0.32f, atlas.height / 2), new Vector2(0.5f, 0.5f), 100);
            }
        }

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
            MakeButton("Switch Player", toolbar, Vector2.zero, new Vector2(0.19f, 1), "VIEW P1", SwitchPlayer, out switchText);
            MakeButton("Rotate Camera", toolbar, new Vector2(0.205f, 0), new Vector2(0.295f, 1), "ROTATE", () => world.Rotate(), out _);
            MakeButton("Zoom In", toolbar, new Vector2(0.306f, 0), new Vector2(0.362f, 1), "+", () => world.Zoom(-0.1f), out _);
            MakeButton("Zoom Out", toolbar, new Vector2(0.372f, 0), new Vector2(0.428f, 1), "-", () => world.Zoom(0.1f), out _);
            resolveButton = MakeButton("Resolve Turn", toolbar, new Vector2(0.442f, 0), new Vector2(0.644f, 1), "ADVANCE TURN", ResolveNow, out _);
            MakeButton("Export Recording", toolbar, new Vector2(0.657f, 0), new Vector2(0.815f, 1), "SAVE REPLAY", SaveRecording, out _);
            MakeButton("Restart", toolbar, new Vector2(0.829f, 0), Vector2.one, "RESTART", Restart, out _);

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
                    () => SetMode(action == 0 ? ActionKind.Move : ActionKind.Attack), out _);
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
            waitButton = MakeButton("Wait", controlsPanel, new Vector2(0.045f, 0.033f), new Vector2(0.22f, 0.137f), "", Wait, out _);
            Icon("Cancel Icon", waitButton.transform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), "undo", Color.white);
            lockButton = MakeButton("Lock Orders", controlsPanel, new Vector2(0.257f, 0.033f), new Vector2(0.955f, 0.137f), "READY", LockOrders, out lockText);
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
                unitButtons[i] = MakeButton($"Unit {i + 1}", teamPanel, new Vector2(left, 0.1f), new Vector2(left + 0.235f, 0.9f), "", () => SelectUnit(player * 4 + slot + 1), out unitLabels[i]);
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
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Battle EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
        }

        private void UpdateLayout()
        {
            var canvas = canvasRoot.GetComponent<Canvas>();
            var pixels = canvas.pixelRect;
            var size = pixels.size;
            if (size != lastSize || Screen.safeArea != lastSafeArea)
            {
                lastSize = size;
                lastSafeArea = Screen.safeArea;
                safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
                bool portrait = size.y > size.x;
                SetAnchors(headerPanel, new Vector2(0, portrait ? 0.94f : 0.947f), Vector2.one);
                SetAnchors(boardPanel, new Vector2(0.008f, portrait ? 0.655f : 0.21f), new Vector2(portrait ? 0.992f : 0.762f, 0.888f));
                SetAnchors(toolbar, new Vector2(0.012f, 0.895f), new Vector2(portrait ? 0.988f : 0.758f, 0.933f));
                SetAnchors(controlsPanel, new Vector2(portrait ? 0.55f : 0.774f, portrait ? 0.145f : 0.187f), new Vector2(0.991f, portrait ? 0.645f : 0.928f));
                SetAnchors(teamPanel, Vector2.zero, portrait ? new Vector2(0.537f, 0.645f) : new Vector2(0.763f, 0.174f));
                SetAnchors(apPanel, new Vector2(portrait ? 0.55f : 0.769f, 0), new Vector2(1, portrait ? 0.135f : 0.174f));
                SetAnchors(helpPanel, new Vector2(0.014f, portrait ? 0.649f : 0.178f), new Vector2(portrait ? 0.99f : 0.755f, portrait ? 0.685f : 0.214f));
                for (int i = 0; i < 4; i++)
                    SetAnchors(unitButtons[i].GetComponent<RectTransform>(), portrait ? new Vector2(0.02f, 0.02f + (3 - i) * 0.245f) : new Vector2(0.012f + i * 0.247f, 0.1f),
                        portrait ? new Vector2(0.98f, 0.25f + (3 - i) * 0.245f) : new Vector2(0.247f + i * 0.247f, 0.9f));
                Canvas.ForceUpdateCanvases();
            }
            boardPanel.GetWorldCorners(fieldCorners);
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var lower = RectTransformUtility.WorldToScreenPoint(uiCamera, fieldCorners[0]);
            var upper = RectTransformUtility.WorldToScreenPoint(uiCamera, fieldCorners[2]);
            world.SetViewport(new Rect((lower.x - pixels.x) / pixels.width, (lower.y - pixels.y) / pixels.height,
                (upper.x - lower.x) / pixels.width, (upper.y - lower.y) / pixels.height));
        }

        private void RefreshHud()
        {
            var selected = view.Entities.Find(e => e.Id == selectedId && e.Health > 0);
            int slot = selected == null ? 1 : selected.Id - player * 4 - 1;
            timerText.text = view.Phase == MatchPhase.Planning ? $"00:{Math.Ceiling(match.SecondsLeft(Now)):00}" : "--:--";
            phaseText.text = view.Phase == MatchPhase.Placement ? $"P{player + 1} / PLACE YOUR BASE" : view.Phase == MatchPhase.Finished ? Outcome(view.Outcome) : $"TURN {view.Turn:00}  |  PLAN ORDERS";
            phaseText.color = view.Phase == MatchPhase.Finished ? Gold : Muted;
            switchText.text = $"VIEW P{player + 1} / SWITCH";
            selectionText.text = selected == null ? "SELECT A UNIT" : $"{slot + 1:00} {Roles[slot]}";
            selectedPortrait.sprite = portraits[slot];
            selectedPortrait.color = selected == null ? new Color(1, 1, 1, 0.45f) : Color.white;
            selectedHealthText.text = selected == null ? "HP -- / --" : $"HP  {selected.Health} / {selected.MaximumHealth}";
            Fill(selectedHealth, selected == null ? 0 : (float)selected.Health / selected.MaximumHealth);
            equipmentText.text = $"{Weapons[slot]}\nDMG {activeRules.AttackDamage}  /  RANGE {activeRules.AttackRange}";
            equipmentIcon.sprite = Resources.Load<Sprite>("PrototypeUI/" + (slot == 2 ? "bow" : slot == 3 ? "item" : "blade"));
            var order = view.Orders.Find(o => o.UnitId == selectedId);
            detailsText.text = order == null ? "WAIT  /  0 AP" : $"{order.Kind.ToString().ToUpperInvariant()}  /  {activeRules.Cost(order.Kind)} AP";
            orderTargetText.text = selected == null ? "Select an allied unit" : order == null ? "Choose a target to queue" : order.Kind == ActionKind.Move ? $"TO TILE {order.Destination}" : $"TARGET #{order.TargetId}";
            bool canPlan = view.Phase == MatchPhase.Planning && !view.Locked;
            moveButton.interactable = attackButton.interactable = waitButton.interactable = canPlan && selected != null;
            Frame(moveButton.gameObject, mode == ActionKind.Move ? Blue : Edge);
            Frame(attackButton.gameObject, mode == ActionKind.Attack ? Enemy : Edge);
            moveButton.image.color = mode == ActionKind.Move ? new Color(0.02f, 0.22f, 0.25f) : Ink;
            attackButton.image.color = mode == ActionKind.Attack ? new Color(0.25f, 0.075f, 0.1f) : Ink;
            lockButton.interactable = canPlan;
            lockText.text = view.Locked ? "READY / LOCKED" : view.Phase == MatchPhase.Placement ? "PLACE BASE" : "READY";
            resolveButton.interactable = view.Phase == MatchPhase.Planning;
            logText.text = view.Messages.LastOrDefault() ?? "";
            if (view.Phase == MatchPhase.Finished) messageText.text = Outcome(view.Outcome) + ". Restart to play again.";
            apText.text = $"{view.AP - view.ReservedAP} / {activeRules.MaximumAP}";
            reservedText.text = $"RESERVED {view.ReservedAP}";
            for (int i = 0; i < apPips.Length; i++) apPips[i].color = i < view.AP - view.ReservedAP ? Blue : i < view.AP ? new Color(0.23f, 0.32f, 0.37f) : new Color(0.11f, 0.16f, 0.2f);
            UpdateBaseGauge(player, ownBaseText, ownBaseBar, Health);
            UpdateBaseGauge(1 - player, enemyBaseText, enemyBaseBar, Enemy);

            for (int i = 0; i < 4; i++)
            {
                var unit = view.Entities.Find(e => e.Id == player * 4 + i + 1);
                var unitOrder = view.Orders.Find(o => o.UnitId == player * 4 + i + 1);
                var kind = unitOrder == null ? ActionKind.Wait : unitOrder.Kind;
                Color accent = kind == ActionKind.Move ? Blue : kind == ActionKind.Attack ? Enemy : Muted;
                unitLabels[i].text = $"{i + 1:00} {Roles[i]}";
                unitHealthText[i].text = unit == null ? "HP -- / --" : unit.Health == 0 ? "DEFEATED" : $"HP  {unit.Health} / {unit.MaximumHealth}";
                Fill(unitHealth[i], unit == null ? 0 : (float)unit.Health / unit.MaximumHealth);
                unitButtons[i].interactable = view.Phase == MatchPhase.Planning && unit != null && unit.Health > 0;
                Frame(unitButtons[i].gameObject, unit != null && unit.Id == selectedId ? Blue : Edge);
                unitOrderText[i].text = kind == ActionKind.Wait ? "WAIT" : $"{kind.ToString().ToUpperInvariant()} / {activeRules.Cost(kind)} AP";
                unitOrderText[i].color = accent;
                unitOrderIcons[i].sprite = Resources.Load<Sprite>("PrototypeUI/" + kind.ToString().ToLowerInvariant());
                unitOrderIcons[i].color = accent;
                Frame(unitOrderPanels[i].gameObject, kind == ActionKind.Wait ? Edge : accent);
                unitOrderPanels[i].color = kind == ActionKind.Move ? new Color(0.025f, 0.18f, 0.23f) : kind == ActionKind.Attack ? new Color(0.2f, 0.055f, 0.08f) : Panel;
            }
            foreach (var cell in previewCells) cell.color = Edge;
            previewCells[12].color = new Color(0.04f, 0.47f, 0.51f);
            previewArrow.enabled = order != null && order.Kind == ActionKind.Move && selected != null;
            if (previewArrow.enabled)
            {
                int dx = order.Destination.X - selected.Position.X;
                int dy = order.Destination.Y - selected.Position.Y;
                previewCells[12 + dx + dy * 5].color = Blue;
                previewArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                SetAnchors(previewArrow.rectTransform, new Vector2(0.39f + dx * 0.1f, 0.39f + dy * 0.1f), new Vector2(0.61f + dx * 0.1f, 0.61f + dy * 0.1f));
            }
        }

        private void UpdateBaseGauge(int owner, Text text, Image bar, Color color)
        {
            var entity = view.Entities.Find(e => e.Owner == owner && e.Kind == EntityKind.Base);
            var memory = view.LastSeen.Find(o => o.Entity.Owner == owner && o.Entity.Kind == EntityKind.Base);
            var known = entity ?? memory?.Entity;
            text.text = known == null ? $"? / {activeRules.BaseHealth}" : $"{known.Health}{(entity == null ? "*" : "")} / {known.MaximumHealth}";
            bar.color = entity == null ? Muted : color;
            Fill(bar, known == null ? 0 : (float)known.Health / known.MaximumHealth);
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

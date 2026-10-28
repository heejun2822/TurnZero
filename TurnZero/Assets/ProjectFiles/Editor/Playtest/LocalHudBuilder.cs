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
    internal sealed class LocalHudBuilder
    {
        private readonly GameObject root;
        private readonly BattleConfiguration configuration;
        private readonly GameRules rules;
        private readonly TMP_FontAsset font;
        private readonly Color ink = new Color(0.028f,0.045f,0.066f), panel = new Color(0.045f,0.066f,0.09f), edge = new Color(0.12f,0.18f,0.23f);
        private readonly Color muted = new Color(0.52f,0.62f,0.69f), cyan = new Color(0.15f,0.82f,0.75f), amber = new Color(0.96f,0.7f,0.32f);
        public LocalHudBuilder(GameObject root, BattleConfiguration configuration, GameRules rules, TMP_FontAsset font)
        { this.root = root; this.configuration = configuration; this.rules = rules; this.font = font; }
        public LocalBattleHud Build()
        {
            var canvasObject = new GameObject("Game HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(LocalBattleHud),typeof(LocalHudLayout));
            canvasObject.transform.SetParent(root.transform,false);
            canvasObject.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = 0.5f;
            var safe = Rect("Safe area",canvasObject.transform,0,0,1,1);
            var header = Box("Match header",safe,0.018f,0.918f,0.982f,0.985f,panel);
            Box("Accent",header,0,0.95f,1,1,cyan,false);
            Text("Brand",header,0.015f,0.35f,0.17f,0.91f,"TURNZERO",27,Color.white);
            Text("Build label",header,0.016f,0.08f,0.22f,0.39f,"LOCAL PLAYTEST   /   v0.2",10,muted);
            var phase = Text("Phase",header,0.25f,0.35f,0.62f,0.89f,"MATCH SETUP",19,Color.white,TextAlignmentOptions.Center);
            var timer = Text("Timer",header,0.63f,0.28f,0.71f,0.9f,"--",27,amber,TextAlignmentOptions.Center);
            var bases = Text("Base health",header,0.73f,0.35f,0.985f,0.91f,"P1  30/30   VS   P2  ?/?",14,Color.white,TextAlignmentOptions.Right);
            var perspective = Text("Perspective",header,0.66f,0.03f,0.985f,0.35f,"P1 PERSPECTIVE",10,cyan,TextAlignmentOptions.Right);
            var intel = Box("Enemy intel",safe,0.018f,0.29f,0.19f,0.895f,panel);
            var intelHeading = Text("Intel heading",intel,0.06f,0.915f,0.94f,0.98f,"OPPONENT / INTEL",15,Color.white);
            var intelRule = Text("Intel rule",intel,0.06f,0.85f,0.94f,0.913f,"LIVE WHEN SEEN  /  * LAST OBSERVED",9,muted);
            var intelFooter = Text("Intel footer",intel,0.06f,0.02f,0.94f,0.10f,"Double-click an observed card to focus.\nUnknown information stays hidden.",10,muted);
            var cards = new List<LocalRosterCard>(); var ownRects = new List<RectTransform>(); var enemyRects = new List<RectTransform>();
            for (int i = 0; i < GameDefinitions.CharactersPerTeam; i++) { var card = Card(intel,i,true); cards.Add(card); enemyRects.Add(card.GetComponent<RectTransform>()); }
            var roster = Rect("Your roster",safe,0.205f,0.038f,0.982f,0.212f);
            for (int i = 0; i < GameDefinitions.CharactersPerTeam; i++) { var card = Card(roster,i,false); cards.Add(card); ownRects.Add(card.GetComponent<RectTransform>()); }
            var ap = Box("AP and inventory",safe,0.018f,0.038f,0.19f,0.265f,panel);
            var apComponent = BuildAP(ap);
            var commands = Box("Command panel",safe,0.785f,0.29f,0.982f,0.895f,panel);
            Box("Command accent",commands,0,0.994f,1,1,cyan,false);
            var selection = Rect("Selection header",commands,0.045f,0.77f,0.955f,0.97f);
            var selectedPortrait = Image("Selected portrait",selection,0,0.17f,0.26f,1,null,Color.white); selectedPortrait.preserveAspect = true;
            var selectedName = Text("Selected name",selection,0.31f,0.63f,1,0.97f,"SELECT A CHARACTER",19,Color.white);
            var selectedHealth = Text("Selected HP",selection,0.31f,0.35f,1,0.63f,"-- / -- HP",16,cyan);
            var selectedStatus = Text("Selected status",selection,0,0,1,0.17f,"CLICK A MODEL OR CARD",9,muted);
            var reserved = Text("Reserved order",selection,0.31f,0.08f,1,0.33f,"RESERVED / WAIT",9,amber);
            var actions = Rect("Action buttons",commands,0.045f,0.51f,0.955f,0.75f);
            var actionButtons = new List<Button>(); var actionLabels = new List<TMP_Text>();
            for (int i = 0; i <= (int)BattleAction.Wait; i++)
            {
                float x0 = i == 4 ? 0 : (i%2)*0.51f, x1 = i == 4 ? 1 : (i%2)*0.51f+0.49f;
                float y0 = i < 2 ? 0.68f : i < 4 ? 0.34f : 0, y1 = i < 2 ? 1 : i < 4 ? 0.66f : 0.31f;
                var button = Button("Action "+(BattleAction)i,actions,x0,y0,x1,y1,"",13,out var label); actionButtons.Add(button); actionLabels.Add(label);
            }
            var detail = Box("Action detail",commands,0.045f,0.30f,0.955f,0.49f,ink,false);
            var actionInfo = Text("Action information",detail,0.05f,0.04f,0.95f,0.96f,"Choose an action to inspect range, cost and priority.",11,muted);
            var craft = Rect("Equipment crafting",commands,0.045f,0.10f,0.955f,0.285f);
            var craftInfo = Text("Craft information",craft,0,0.64f,1,1,"INVENTORY / 2 CRAFT MATERIALS",10,muted);
            var craftButtons = new List<Button>(); var craftLabels = new List<TMP_Text>();
            for (int i = 0; i < 3; i++) { var button = Button("Craft "+i,craft,i/3f+0.005f,0.02f,(i+1)/3f-0.005f,0.59f,"",10,out var label); craftButtons.Add(button); craftLabels.Add(label); }
            var ready = Button("Ready",commands,0.045f,0.02f,0.955f,0.085f,"ENTER / READY",15,out var readyLabel,cyan);
            var toolbar = Rect("Local test toolbar",safe,0.205f,0.235f,0.765f,0.278f);
            var toolbarButtons = new List<Button>(); var toolbarLabels = new List<TMP_Text>();
            string[] toolbarNames = { "Switch perspective", "Pause timer", "Resolve", "Debug vision", "Controls", "Restart" };
            string[] toolbarTitles = { "TAB / VIEW P2", "P / PAUSE", "SPACE / RESOLVE", "VISION / PLAYER", "F1 / GUIDE", "REMATCH" };
            for (int i = 0; i < 6; i++) { var button = Button(toolbarNames[i],toolbar,i/6f+0.003f,0,(i+1)/6f-0.003f,1,toolbarTitles[i],10,out var label); toolbarButtons.Add(button); toolbarLabels.Add(label); }
            var hint = Rect("Message and log",safe,0.21f,0.29f,0.76f,0.335f);
            var message = Text("Message",hint,0,0.45f,1,1,"Deploy your teams to begin.",11,muted,TextAlignmentOptions.Center);
            var log = Text("Combat log",safe,0.215f,0.845f,0.76f,0.9f,"",10,muted,TextAlignmentOptions.Center);
            var banner = Box("Turn banner",safe,0.30f,0.68f,0.70f,0.80f,new Color(0.025f,0.055f,0.075f,0.97f));
            var bannerText = Text("Turn banner text",banner,0.04f,0.08f,0.96f,0.92f,"TURN 01\n<size=35%>+10 AP / PLAN YOUR NEXT MOVE</size>",38,Color.white,TextAlignmentOptions.Center); banner.gameObject.SetActive(false);
            var setup = BuildSetup(safe,out var start,out var auto,out var fields,out var fieldLabels,out var decks,out var deckLabels);
            var help = Modal("Controls guide",safe,out var helpBody);
            Text("Controls title",helpBody,0.06f,0.82f,0.94f,0.95f,"COMMAND FIELD GUIDE",28,Color.white);
            Text("Controls copy",helpBody,0.07f,0.17f,0.93f,0.80f,
                "<color=#39D1BE>SELECT & FOCUS</color>\nClick a model or card. 1-5 selects your roster; repeat a key quickly or double-click a card to focus. Click a stacked tile again to cycle observed characters.\n\n<color=#39D1BE>PLAN ONE ACTION</color>\nQ move / W attack / E skill / R equipment / T wait-cancel. Choose an action, then click a tile. Right-click moves immediately. Drag the selected character to draw a manual path. REVIVE appears on defeated ally cards.\n\n<color=#39D1BE>CAMERA & TOUCH</color>\nDrag empty battlefield space to pan. Scroll to zoom. On mobile: drag to pan, pinch to zoom, drag a selected character to draw a path, double-tap a card to focus. Camera controls lock during resolution.\n\n<color=#E7B768>SOLO TESTING</color>\nTAB switches teams; each sees their own field below. ENTER locks your team; both ready resolves immediately. P pauses the planning clock. SPACE resolves or fast-forwards. VISION toggles clearly marked debug sight. REMATCH returns to setup.\n\nAmber AP is reserved; teal is available. Dark slots are empty. Enemy * values are last observed. Local rule assumptions are recorded in DevelopmentPlan.md and the configuration asset.",14,muted);
            var closeHelp = Button("Close guide",helpBody,0.32f,0.04f,0.68f,0.12f,"BACK TO BATTLE",15,out _,cyan); help.SetActive(false);
            var result = Modal("Match result",safe,out var resultBody);
            var resultText = Text("Result",resultBody,0.08f,0.43f,0.92f,0.76f,"BATTLE COMPLETE",48,Color.white,TextAlignmentOptions.Center);
            Text("Result copy",resultBody,0.13f,0.25f,0.87f,0.40f,"The final simulation result is shared by both perspectives.\nRestart to adjust a field or equipment deck and test again.",16,muted,TextAlignmentOptions.Center);
            var rematch = Button("New local match",resultBody,0.28f,0.10f,0.72f,0.21f,"SET UP A REMATCH",17,out _,cyan); result.SetActive(false);
            var confirm = Modal("Replace equipment",safe,out var confirmBody);
            Text("Confirm title",confirmBody,0.08f,0.59f,0.92f,0.75f,"REPLACE EQUIPMENT?",30,Color.white,TextAlignmentOptions.Center);
            Text("Confirm copy",confirmBody,0.13f,0.36f,0.87f,0.56f,"The current item is destroyed.\nOne crafting material will be consumed.",18,muted,TextAlignmentOptions.Center);
            var confirmButton = Button("Confirm replacement",confirmBody,0.18f,0.16f,0.49f,0.28f,"REPLACE",17,out _,cyan);
            var cancelConfirm = Button("Keep equipment",confirmBody,0.51f,0.16f,0.82f,0.28f,"KEEP CURRENT",17,out _); confirm.SetActive(false);
            var hud = canvasObject.GetComponent<LocalBattleHud>();
            LocalPlaytestBuilder.Wire(hud,("phaseText",phase),("timerText",timer),("baseText",bases),("perspectiveText",perspective),("messageText",message),("logText",log),
                ("selectedName",selectedName),("selectedHealth",selectedHealth),("selectedStatus",selectedStatus),("selectedPortrait",selectedPortrait),("actionInfo",actionInfo),("reservedInfo",reserved),("craftInfo",craftInfo),("phaseBannerText",bannerText),("resultText",resultText),
                ("actionButtons",actionButtons.ToArray()),("actionLabels",actionLabels.ToArray()),("craftButtons",craftButtons.ToArray()),("craftLabels",craftLabels.ToArray()),("fieldButtons",fields),("fieldLabels",fieldLabels),("deckButtons",decks),("deckLabels",deckLabels),
                ("readyButton",ready),("readyLabel",readyLabel),("switchButton",toolbarButtons[0]),("switchLabel",toolbarLabels[0]),("pauseButton",toolbarButtons[1]),("pauseLabel",toolbarLabels[1]),("resolveButton",toolbarButtons[2]),("resolveLabel",toolbarLabels[2]),
                ("revealButton",toolbarButtons[3]),("revealLabel",toolbarLabels[3]),("helpButton",toolbarButtons[4]),("restartButton",toolbarButtons[5]),("startButton",start),("autoDeployButton",auto),("rematchButton",rematch),("closeHelpButton",closeHelp),
                ("confirmButton",confirmButton),("cancelConfirmButton",cancelConfirm),("setupPanel",setup),("helpPanel",help),("resultPanel",result),("phaseBanner",banner.gameObject),("confirmPanel",confirm),("cards",cards.ToArray()),("apPanel",apComponent));
            LocalPlaytestBuilder.Wire(canvasObject.GetComponent<LocalHudLayout>(),("safeArea",safe),("header",header),("intel",intel),("commands",commands),("roster",roster),("ap",ap),("toolbar",toolbar),("hint",hint),
                ("selectionHeader",selection),("actions",actions),("details",detail),("craft",craft),("ready",ready.GetComponent<RectTransform>()),("ownCards",ownRects.ToArray()),("enemyCards",enemyRects.ToArray()),("desktopIntelLabels",new[] { intelHeading.gameObject,intelRule.gameObject,intelFooter.gameObject }));
            return hud;
        }
        private LocalRosterCard Card(Transform parent, int slot, bool enemy)
        {
            var rect = Box($"{(enemy ? "Enemy" : "Own")} card {slot+1}",parent,0,0,1,1,new Color(0.055f,0.077f,0.10f));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.navigation = new Navigation { mode = Navigation.Mode.None };
            var accent = Image("Team accent",rect,0,0,0.014f,1,null,cyan);
            var portrait = Image("Portrait",rect,0.04f,0.24f,0.31f,0.92f,configuration.Characters[slot].Portrait,Color.white); portrait.preserveAspect = true;
            var name = Text("Character name",rect,0.35f,0.73f,0.96f,0.96f,configuration.Characters[slot].Values.Name.ToUpperInvariant(),enemy ? 12 : 14,Color.white);
            var hp = Text("Health",rect,0.35f,0.51f,0.96f,0.74f,"14 / 14",12,cyan);
            var hpTrack = Box("Health track",rect,0.35f,0.43f,0.94f,0.47f,edge,false);
            var hpFill = Image("Health fill",hpTrack,0,0,1,1,AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),cyan); hpFill.type = UnityEngine.UI.Image.Type.Sliced;
            var status = Text("Status",rect,0.35f,0.24f,0.96f,0.42f,"READY",9,muted);
            var equipment = Text("Equipment",rect,0.04f,0.12f,0.96f,0.25f,"NO EQUIPMENT",9,muted);
            var order = Text("Order",rect,0.04f,0.01f,0.96f,0.14f,enemy ? "" : "WAIT / 0 AP",10,amber);
            var view = rect.gameObject.AddComponent<LocalRosterCard>();
            LocalPlaytestBuilder.Wire(view,("slot",slot),("enemy",enemy),("button",button),("portrait",portrait),("accent",accent),("background",rect.GetComponent<Image>()),("healthFill",hpFill),
                ("nameText",name),("healthText",hp),("statusText",status),("equipmentText",equipment),("orderText",order));
            return view;
        }
        private LocalApPanel BuildAP(RectTransform panelRoot)
        {
            var heading = Text("AP heading",panelRoot,0.07f,0.86f,0.94f,0.97f,"ACTION POINTS",13,Color.white);
            var available = Text("AP available",panelRoot,0.61f,0.41f,0.95f,0.73f,"10",38,cyan);
            var maximum = Text("AP maximum",panelRoot,0.61f,0.26f,0.95f,0.43f,"/ 25 AP",12,muted);
            var reserved = Text("AP reserved",panelRoot,0.07f,0.72f,0.94f,0.85f,"0 RESERVED",11,amber);
            var inventory = Text("Inventory",panelRoot,0.05f,0.02f,0.95f,0.15f,"CRAFT 2 / UPGRADE -- / CONSUMABLE --",8,muted);
            var gridRect = Rect("25 AP slots",panelRoot,0.07f,0.20f,0.54f,0.67f); var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>(); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 5; grid.spacing = new Vector2(3,3); grid.cellSize = new Vector2(20,13);
            var pips = new List<Image>();
            for (int i = 0; i < rules.MaximumAP; i++) pips.Add(Image("AP slot "+(i+1),gridRect,0,0,1,1,null,i < 10 ? cyan : edge));
            var template = new GameObject("AP pip",typeof(RectTransform),typeof(Image)); template.layer = LayerMask.NameToLayer("UI"); var image = template.GetComponent<Image>(); image.color = cyan; image.raycastTarget = false;
            var pipPrefab = PrefabUtility.SaveAsPrefabAsset(template,GameDefinitions.AssetRoot+"Prefabs/UI/ApPip.prefab").GetComponent<Image>(); UnityEngine.Object.DestroyImmediate(template);
            var component = panelRoot.gameObject.AddComponent<LocalApPanel>();
            LocalPlaytestBuilder.Wire(component,("availableText",available),("reservedText",reserved),("maximumText",maximum),("inventoryText",inventory),("authoredPips",pips.ToArray()),("grid",grid),("pipPrefab",pipPrefab));
            LocalPlaytestBuilder.Wire(component,("headingText",heading));
            return component;
        }
        private GameObject BuildSetup(Transform parent, out Button start, out Button auto, out Button[] fieldButtons, out TMP_Text[] fieldLabels, out Button[] deckButtons, out TMP_Text[] deckLabels)
        {
            var overlay = Modal("Match setup",parent,out var body);
            Text("Setup eyebrow",body,0.05f,0.91f,0.95f,0.97f,"TWO TEAMS. ONE BATTLEFIELD. YOUR TEST BENCH.",12,cyan);
            Text("Setup title",body,0.05f,0.79f,0.95f,0.91f,"LOCAL MATCH LAB",38,Color.white);
            Text("Setup copy",body,0.05f,0.72f,0.95f,0.79f,"12 x 20 FIELD  /  FIVE CHARACTERS PER TEAM  /  SIMULTANEOUS TURNS",12,muted);
            var fields = new List<Button>(); var labels = new List<TMP_Text>(); var decks = new List<Button>(); var deckTexts = new List<TMP_Text>();
            for (int owner = 0; owner < GameDefinitions.PlayerCount; owner++)
            {
                var team = Box("Player "+(owner+1)+" setup",body,0.05f+owner*0.46f,0.24f,0.49f+owner*0.46f,0.69f,ink,false);
                Text("Team heading",team,0.04f,0.84f,0.96f,0.96f,$"PLAYER 0{owner+1}",23,LocalBattleWorld.Team(owner));
                Text("Field label",team,0.04f,0.73f,0.96f,0.84f,"SELECT HOME FIELD",11,muted);
                for (int i = 0; i < 3; i++) { var button = Button($"P{owner+1} field {i+1}",team,0.04f+i*0.31f,0.59f,0.33f+i*0.31f,0.73f,configuration.Fields[i].DisplayName,11,out var label); fields.Add(button); labels.Add(label); }
                Text("Roster list",team,0.04f,0.35f,0.96f,0.56f,"STARTER ROSTER / 5 DISTINCT CHARACTERS\nVANGUARD  /  SHADE  /  PYRE  /  BINDER  /  DEADEYE",11,muted);
                Text("Equipment heading",team,0.04f,0.24f,0.96f,0.35f,"EQUIPMENT DECK / CLICK A SLOT TO CYCLE",10,muted);
                for (int i = 0; i < GameDefinitions.EquipmentDeckSlots; i++) { var button = Button($"P{owner+1} equipment slot {i+1}",team,0.04f+i*0.185f,0.04f,0.215f+i*0.185f,0.23f,"",10,out var label); decks.Add(button); deckTexts.Add(label); }
            }
            Text("Local profile",body,0.06f,0.13f,0.94f,0.22f,"LOCAL PROFILE v1 / provisional rules are documented, not final design decisions.\nThree defined equipment types may repeat across five deck slots. Upgrade and consumable content is undefined.\nDeploy manually to test placement, or quick-deploy both teams. TAB switches perspective during the match.",11,muted,TextAlignmentOptions.Center);
            start = Button("Start deployment",body,0.18f,0.035f,0.49f,0.115f,"DEPLOY TEAMS",17,out _,cyan);
            auto = Button("Quick deploy",body,0.51f,0.035f,0.82f,0.115f,"QUICK DEPLOY",17,out _);
            fieldButtons = fields.ToArray(); fieldLabels = labels.ToArray(); deckButtons = decks.ToArray(); deckLabels = deckTexts.ToArray(); return overlay;
        }
        private GameObject Modal(string name, Transform parent, out RectTransform body)
        {
            var overlay = Box(name,parent,0,0,1,1,new Color(0.008f,0.016f,0.027f,0.91f));
            body = Box(name+" body",overlay,0.14f,0.12f,0.86f,0.88f,panel);
            Box("Modal accent",body,0,0.992f,1,1,cyan,false); return overlay.gameObject;
        }
        private RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.gameObject.layer = LayerMask.NameToLayer("UI"); rect.SetParent(parent,false); rect.anchorMin = new Vector2(x0,y0); rect.anchorMax = new Vector2(x1,y1); rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private RectTransform Box(string name, Transform parent, float x0, float y0, float x1, float y1, Color color, bool raycast = true)
        { var rect = Rect(name,parent,x0,y0,x1,y1); var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast; return rect; }
        private Image Image(string name, Transform parent, float x0, float y0, float x1, float y1, Sprite sprite, Color color)
        { var rect = Rect(name,parent,x0,y0,x1,y1); var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = false; return image; }
        private TMP_Text Text(string name, Transform parent, float x0, float y0, float x1, float y1, string text, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var rect = Rect(name,parent,x0,y0,x1,y1); var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.fontSize = size; label.enableAutoSizing = true; label.fontSizeMin = Mathf.Min(9,size); label.fontSizeMax = size;
            label.color = color; label.alignment = alignment; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Ellipsis; label.margin = new Vector4(1,0,1,0); return label;
        }
        private Button Button(string name, Transform parent, float x0, float y0, float x1, float y1, string text, float size, out TMP_Text label, Color? fill = null)
        {
            var rect = Box(name,parent,x0,y0,x1,y1,fill ?? edge); var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.highlightedColor = new Color(1.2f,1.2f,1.2f); colors.pressedColor = new Color(0.72f,0.84f,0.87f); colors.disabledColor = new Color(0.34f,0.4f,0.45f); button.colors = colors;
            label = Text("Label",rect,0.035f,0.05f,0.965f,0.95f,text,size,fill.HasValue ? ink : Color.white,TextAlignmentOptions.Center); return button;
        }

    }
}

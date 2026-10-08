using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed partial class BattlePrototype : MonoBehaviour
    {
        [SerializeField] private BattleRules rules = new BattleRules();
        [SerializeField] private Material surfaceMaterial;
        private BattleRules activeRules;
        private GameObject canvasRoot;
        private BattleWorldView world;
        private BattleMatch match;
        private PlayerView view;
        private int player;
        private int selectedId;
        private ActionKind mode = ActionKind.Move;
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

        private static readonly Color Panel = new Color(0.075f, 0.105f, 0.16f);
        private static readonly Color Blue = new Color(0.03f, 0.91f, 0.93f);
        private static readonly Color Gold = new Color(1f, 0.77f, 0.34f);
        private double Now => Time.realtimeSinceStartupAsDouble;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            LoadPortraits();
            Restart();
        }

        private void Update()
        {
            if (match == null) return;
            if (match.Tick(Now))
            {
                messageText.text = "Turn resolved. Review the log, then plan your next actions.";
                Refresh();
            }
            timerText.text = match.Phase == MatchPhase.Planning ? $"00:{Math.Ceiling(match.SecondsLeft(Now)):00}" : "--:--";
        }

        private void LateUpdate() => UpdateLayout();

        public void Restart()
        {
            rules.Validate();
            activeRules = rules.Copy();
            if (canvasRoot != null) { canvasRoot.SetActive(false); Destroy(canvasRoot); }
            if (world != null) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            lastSize = Vector2.zero;
            BuildUI();
            var worldObject = new GameObject("Battle World");
            worldObject.transform.SetParent(transform, false);
            world = worldObject.AddComponent<BattleWorldView>();
            world.Initialize(activeRules, surfaceMaterial, font, Camera.main, ClickCell);
            match = new BattleMatch(activeRules);
            player = 0;
            selectedId = 0;
            mode = ActionKind.Move;
            messageText.text = "P1: choose a highlighted base tile. Four units spawn around it.";
            Refresh();
        }

        private void SwitchPlayer()
        {
            if (match.Phase == MatchPhase.Placement)
            {
                messageText.text = "Finish the current base placement first.";
                return;
            }
            player = 1 - player;
            selectedId = player * 4 + 2;
            messageText.text = $"Viewing P{player + 1}. Opponent orders and AP are hidden.";
            Refresh();
        }

        private void SelectUnit(int id)
        {
            var entity = view.Entities.Find(e => e.Id == id && e.Owner == player && e.Kind == EntityKind.Unit && e.Health > 0);
            if (entity == null || view.Phase != MatchPhase.Planning) return;
            selectedId = id;
            messageText.text = view.Locked ? "Your orders are locked until the turn ends." : "Choose MOVE or ATTACK, then choose a highlighted tile.";
            Refresh();
        }

        private void SetMode(ActionKind action)
        {
            mode = action;
            messageText.text = action == ActionKind.Move ? "Choose an adjacent tile. Occupied tiles and contested moves fail." : "Choose a visible enemy in the highlighted attack range.";
            Refresh();
        }

        private void ClickCell(Cell cell)
        {
            // Tick before accepting input: a UI click arriving at the deadline belongs to the next turn.
            if (match.Tick(Now)) { selectedId = 0; Refresh(); return; }
            if (match.Phase == MatchPhase.Placement)
            {
                if (!match.PlaceBase(player, cell, Now, out string error)) messageText.text = error;
                else
                {
                    if (match.Phase == MatchPhase.Placement) player = match.PlacementPlayer;
                    else { player = 0; selectedId = 2; }
                    messageText.text = match.Phase == MatchPhase.Placement ? "P2: place your base in your highlighted field." : "Select an allied unit to plan. Switch views to enter P2 orders.";
                    Refresh();
                }
                return;
            }
            if (match.Phase != MatchPhase.Planning) return;
            var clicked = view.Entities.Find(e => e.Position == cell && e.Health > 0);
            if (clicked != null && clicked.Owner == player && clicked.Kind == EntityKind.Unit) { SelectUnit(clicked.Id); return; }
            if (selectedId == 0) { messageText.text = "Select a living allied unit first."; return; }
            ActionOrder order;
            if (mode == ActionKind.Move) order = ActionOrder.Move(selectedId, cell);
            else if (clicked != null && clicked.Owner != player) order = ActionOrder.Attack(selectedId, clicked.Id);
            else { messageText.text = "Attack requires a currently visible enemy."; return; }
            Queue(order);
        }

        private void Wait()
        {
            if (selectedId != 0) Queue(ActionOrder.Wait(selectedId));
        }

        private void Queue(ActionOrder order)
        {
            if (match.Tick(Now)) { selectedId = 0; Refresh(); return; }
            messageText.text = match.QueueOrder(player, view.Turn, order, out string error) ? "Order updated. Unassigned units wait for free." : error;
            Refresh();
        }

        private void LockOrders()
        {
            if (match.Tick(Now)) { selectedId = 0; Refresh(); return; }
            if (match.LockOrders(player, view.Turn)) messageText.text = "Orders locked. The shared timer continues; switch to the other player.";
            Refresh();
        }

        private void ResolveNow()
        {
            if (!match.ResolveNow(Now)) return;
            messageText.text = "Both players' current orders resolved. Review the observed results.";
            Refresh();
        }

        private void Refresh()
        {
            view = match.View(player, Now);
            if (!view.Entities.Exists(e => e.Id == selectedId && e.Health > 0)) selectedId = 0;
            RefreshHud();
            world.Refresh(view, selectedId, mode);
        }

        private void SaveRecording()
        {
            try
            {
                string folder = Path.Combine(Application.persistentDataPath, "Replays");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, $"turnzero-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.json");
                File.WriteAllText(path, JsonUtility.ToJson(match.ExportRecording(), true));
                messageText.text = "Debug replay saved. File location is in the Unity Console.";
                Debug.Log($"TurnZero debug replay: {path}");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                messageText.text = "Could not save the debug replay.";
                Debug.LogException(exception);
            }
        }

        private static string Outcome(BattleOutcome result) => result == BattleOutcome.Draw ? "DRAW" : result == BattleOutcome.PlayerOne ? "PLAYER 1 WINS" : "PLAYER 2 WINS";

        private void OnDestroy()
        {
            foreach (var portrait in portraits) if (portrait != null) Destroy(portrait);
            foreach (var portrait in cardPortraits) if (portrait != null) Destroy(portrait);
        }
    }
}

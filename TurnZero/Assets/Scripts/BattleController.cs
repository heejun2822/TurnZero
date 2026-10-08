using System;
using System.IO;
using UnityEngine;

namespace TurnZero
{
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] private BattleRules rules = new BattleRules();
        [SerializeField] private BattleWorldView world;
        [SerializeField] private BattleHudView hud;
        [SerializeField] private BattleCameraRig cameraRig;
        private BattleRules activeRules;
        private BattleMatch match;
        private PlayerView view;
        private int player;
        private int selectedId;
        private ActionKind mode = ActionKind.Move;
        private double Now => Time.realtimeSinceStartupAsDouble;

        private void Start()
        {
            hud.Bind(slot => SelectUnit(player * 4 + slot + 1), SetMode, Wait, LockOrders, SwitchPlayer, cameraRig, ResolveNow, SaveRecording, Restart);
            Restart();
        }
        private void Update()
        {
            if (match == null) return;
            if (match.Tick(Now))
            {
                hud.Message = "Turn resolved. Review the log, then plan your next actions.";
                Refresh();
            }
            hud.Timer(match.Phase, match.SecondsLeft(Now));
        }
        public void Restart()
        {
            activeRules = rules.Copy();
            // The authored map is the authority for board dimensions.
            activeRules.Width = world.Map.Width;
            activeRules.Height = world.Map.Height;
            activeRules.Validate();
            world.Bind(activeRules, cameraRig.Camera, ClickCell);
            match = new BattleMatch(activeRules);
            player = 0;
            selectedId = 0;
            mode = ActionKind.Move;
            hud.Message = "P1: choose a highlighted base tile. Four units spawn around it.";
            Refresh();
        }

        private void SwitchPlayer()
        {
            if (match.Phase == MatchPhase.Placement)
            {
                hud.Message = "Finish the current base placement first.";
                return;
            }
            player = 1 - player;
            selectedId = player * 4 + 2;
            hud.Message = $"Viewing P{player + 1}. Opponent orders and AP are hidden.";
            Refresh();
        }

        private void SelectUnit(int id)
        {
            var entity = view.Entities.Find(e => e.Id == id && e.Owner == player && e.Kind == EntityKind.Unit && e.Health > 0);
            if (entity == null || view.Phase != MatchPhase.Planning) return;
            selectedId = id;
            hud.Message = view.Locked ? "Your orders are locked until the turn ends." : "Choose MOVE or ATTACK, then choose a highlighted tile.";
            Refresh();
        }

        private void SetMode(ActionKind action)
        {
            mode = action;
            hud.Message = action == ActionKind.Move ? "Choose an adjacent tile. Occupied tiles and contested moves fail." : "Choose a visible enemy in the highlighted attack range.";
            Refresh();
        }

        private void ClickCell(Cell cell)
        {
            // Tick before accepting input: a UI click arriving at the deadline belongs to the next turn.
            if (match.Tick(Now)) { selectedId = 0; Refresh(); return; }
            if (match.Phase == MatchPhase.Placement)
            {
                if (!match.PlaceBase(player, cell, Now, out string error)) hud.Message = error;
                else
                {
                    if (match.Phase == MatchPhase.Placement) player = match.PlacementPlayer;
                    else { player = 0; selectedId = 2; }
                    hud.Message = match.Phase == MatchPhase.Placement ? "P2: place your base in your highlighted field." : "Select an allied unit to plan. Switch views to enter P2 orders.";
                    Refresh();
                }
                return;
            }
            if (match.Phase != MatchPhase.Planning) return;
            var clicked = view.Entities.Find(e => e.Position == cell && e.Health > 0);
            if (clicked != null && clicked.Owner == player && clicked.Kind == EntityKind.Unit) { SelectUnit(clicked.Id); return; }
            if (selectedId == 0) { hud.Message = "Select a living allied unit first."; return; }
            ActionOrder order;
            if (mode == ActionKind.Move) order = ActionOrder.Move(selectedId, cell);
            else if (clicked != null && clicked.Owner != player) order = ActionOrder.Attack(selectedId, clicked.Id);
            else { hud.Message = "Attack requires a currently visible enemy."; return; }
            Queue(order);
        }

        private void Wait()
        {
            if (selectedId != 0) Queue(ActionOrder.Wait(selectedId));
        }

        private void Queue(ActionOrder order)
        {
            if (match.Tick(Now)) { selectedId = 0; Refresh(); return; }
            hud.Message = match.QueueOrder(player, view.Turn, order, out string error) ? "Order updated. Unassigned units wait for free." : error;
            Refresh();
        }

        private void LockOrders()
        {
            if (match.Tick(Now)) { selectedId = 0; Refresh(); return; }
            if (match.LockOrders(player, view.Turn)) hud.Message = "Orders locked. The shared timer continues; switch to the other player.";
            Refresh();
        }

        private void ResolveNow()
        {
            if (!match.ResolveNow(Now)) return;
            hud.Message = "Both players' current orders resolved. Review the observed results.";
            Refresh();
        }

        private void Refresh()
        {
            view = match.View(player, Now);
            if (!view.Entities.Exists(e => e.Id == selectedId && e.Health > 0)) selectedId = 0;
            hud.Render(view, activeRules, selectedId, mode);
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
                hud.Message = "Debug replay saved. File location is in the Unity Console.";
                Debug.Log($"TurnZero debug replay: {path}");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                hud.Message = "Could not save the debug replay.";
                Debug.LogException(exception);
            }
        }

    }
}

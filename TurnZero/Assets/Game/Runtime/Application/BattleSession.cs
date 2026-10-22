using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TurnZero.Game
{
    public sealed class BattleSession : MonoBehaviour
    {
        public BattleEvents Events { get; } = new BattleEvents();
        public BattleConfiguration Configuration { get; private set; }
        public LocalBattleMatch Match { get; private set; }
        public GameRules Rules => Match.Rules;
        public BattleView View => display == null ? Match.View(Player, clock, RevealAll) : BattleVisibility.View(display, Rules, Player, clock, RevealAll);
        public int Player { get; private set; }
        public int SelectedId { get; private set; }
        public BattleAction Action { get; private set; } = BattleAction.Move;
        public bool Armed { get; private set; }
        public bool Paused { get; private set; }
        public bool RevealAll { get; private set; }
        public bool ShowHelp { get; private set; }
        public bool Running { get; private set; }
        public string Message { get; private set; }
        public GridCell? HoverCell { get; private set; }
        public GridCell? PortalEntrance { get; private set; }
        public int[] Fields { get; } = new int[2];
        public int[][] EquipmentDecks { get; } = new[] { new[] { 0,1,2,0,1 },new[] { 0,1,2,0,1 } };
        public List<string> History { get; } = new List<string>();
        public List<GridCell> PathPreview { get; private set; } = new List<GridCell>();
        private MatchState display;
        private double clock;
        private bool fastForward;
        private Coroutine playback;
        public BattleUnit Selected => View.Units.FirstOrDefault(u => u.Id == SelectedId) ?? View.KnownEnemies.FirstOrDefault(o => o.Unit.Id == SelectedId)?.Unit;
        public bool CanUseCamera => !Running && (Match.Phase == BattlePhase.Planning || Match.Phase == BattlePhase.BasePlacement || Match.Phase == BattlePhase.UnitPlacement);

        public void Initialize(BattleConfiguration configuration)
        {
            Configuration = configuration;
            if (Match != null) { Restart(); return; }
            Events.CharacterRequested += Select;
            Events.ActionRequested += SelectAction;
            Events.TileRequested += Tile;
            Events.PathRequested += SetPath;
            Events.PathPreviewRequested += PreviewPath;
            Events.HoverRequested += Hover;
            Events.ReadyRequested += Ready;
            Events.SwitchRequested += Switch;
            Events.ResolveRequested += Resolve;
            Events.RestartRequested += Restart;
            Events.DeployRequested += Deploy;
            Events.AutoDeployRequested += AutoDeploy;
            Events.PauseRequested += TogglePause;
            Events.RevealRequested += ToggleReveal;
            Events.CraftRequested += Craft;
            Events.FieldRequested += SetField;
            Events.DeckRequested += CycleDeck;
            Events.HelpRequested += Help;
            Restart();
        }
        private void Update()
        {
            if (Match == null) return;
            if (!(Paused && Match.Phase == BattlePhase.Planning)) clock += Time.unscaledDeltaTime;
            if (Match.Tick(clock))
            {
                if (SelectedId == 0) SelectedId = Player * 5 + 1;
                Message = "Plan one action per character. TAB switches player. ENTER readies your team.";
                Events.Refresh();
            }
            if (Match.ShouldResolve(clock)) Resolve();
        }
        public void Restart()
        {
            if (playback != null) StopCoroutine(playback);
            Match = new LocalBattleMatch(Configuration.CreateRules()); Player = 0; SelectedId = 0; Action = BattleAction.Move;
            for (int owner = 0; owner < 2; owner++)
            {
                Fields[owner] %= Configuration.Fields.Length;
                for (int slot = 0; slot < 5; slot++) EquipmentDecks[owner][slot] %= Rules.Equipment.Length;
            }
            Armed = false; Running = false; Paused = false; RevealAll = false; ShowHelp = false; display = null;
            clock = 0; PortalEntrance = null; HoverCell = null; History.Clear();
            PathPreview.Clear();
            Message = "LOCAL PLAYTEST / Pick each side's field, then deploy a base and five characters.";
            Events.Refresh();
        }
        private void Deploy()
        {
            if (Match.Phase != BattlePhase.Setup) return;
            Match.BeginPlacement(); Message = "P1 / Place your base on your field. Then deploy five characters inside base vision."; Events.Refresh();
        }
        private void AutoDeploy()
        {
            if (Running || Match.Phase == BattlePhase.Planning || Match.Phase == BattlePhase.TurnStart || Match.Phase == BattlePhase.Finished) return;
            Match.AutoDeploy(clock); Player = 0; SelectedId = 1; Message = "Both teams deployed. The first turn starts with 10 AP."; Events.Refresh();
        }
        private void SetField(int player, int index)
        {
            if (Match.Phase != BattlePhase.Setup || player < 0 || player > 1 || index < 0 || index >= Configuration.Fields.Length) return;
            Fields[player] = index; Events.Refresh();
        }
        private void Select(int id, bool focus)
        {
            if (Running || Match.Phase == BattlePhase.Setup || Match.Phase == BattlePhase.BasePlacement || Match.Phase == BattlePhase.UnitPlacement) return;
            var view = View;
            var unit = view.Units.Find(u => u.Id == id) ?? view.KnownEnemies.Find(o => o.Unit.Id == id)?.Unit;
            if (unit == null) { Message = "This enemy has not been observed."; Events.Refresh(); return; }
            SelectedId = id; Armed = false; PortalEntrance = null; HoverCell = null;
            Action = unit.Alive ? BattleAction.Move : BattleAction.Revive;
            if (focus && (unit.Owner == Player || unit.Alive)) Events.Focus(unit.Owner == Player && !unit.Alive ? view.OwnBase.Cell : unit.Cell);
            Message = unit.Owner == Player ? "Q move / W attack / E skill / R equipment / T wait. Double-click a card to focus." : "Enemy information is limited to the latest observation.";
            Events.Refresh();
        }
        private void CycleDeck(int player, int slot)
        {
            if (Match.Phase != BattlePhase.Setup || player < 0 || player > 1 || slot < 0 || slot >= 5) return;
            EquipmentDecks[player][slot] = (EquipmentDecks[player][slot] + 1) % Rules.Equipment.Length; Events.Refresh();
        }
        private void SelectAction(BattleAction action)
        {
            if (Running || Match.Phase != BattlePhase.Planning || View.Ready || Selected?.Owner != Player) return;
            if (action == BattleAction.Wait)
            {
                Submit(new BattleOrder { UnitId = SelectedId, Action = BattleAction.Wait }); Armed = false; PortalEntrance = null; Events.Refresh(); return;
            }
            if (!Selected.Alive && action != BattleAction.Revive) action = BattleAction.Revive;
            if ((int)action < 4 && Selected.ReadyOn[(int)action] > Match.Turn)
            { Message = $"Available on turn {Selected.ReadyOn[(int)action]}."; Events.Refresh(); return; }
            bool rootBlocked = Selected.Rooted(Match.Turn) && (action == BattleAction.Move || action == BattleAction.Skill && Rules.Characters[Selected.Slot].SkillEffect == SkillEffect.StealthMove ||
                action == BattleAction.Equipment && Selected.Equipment >= 0 && Rules.Equipment[Selected.Equipment].Effect == EquipmentEffect.Swap);
            if (rootBlocked) { Message = "ROOT prevents this movement action this turn."; Events.Refresh(); return; }
            Action = action; Armed = true; PortalEntrance = null; HoverCell = null;
            Message = action == BattleAction.Equipment && Selected.Equipment >= 0 && Rules.Equipment[Selected.Equipment].Effect == EquipmentEffect.Portal ?
                "Choose an explored entrance, then an explored exit." : "Click a target tile. Right-click moves immediately; drag the selected character to draw a path.";
            Events.Refresh();
        }
        private void Tile(GridCell cell, bool quickMove)
        {
            if (Running || !Rules.Contains(cell)) return;
            if (Match.Phase == BattlePhase.BasePlacement || Match.Phase == BattlePhase.UnitPlacement)
            {
                if (!Match.Place(Player, cell, clock, out string error)) Message = error;
                else
                {
                    if (Match.Phase == BattlePhase.BasePlacement || Match.Phase == BattlePhase.UnitPlacement) Player = Match.PlacementPlayer;
                    else { Player = 0; SelectedId = 1; }
                    Message = Match.Phase == BattlePhase.BasePlacement ? $"P{Player+1} / Place your base." : Match.Phase == BattlePhase.UnitPlacement ?
                        $"P{Player+1} / Deploy {Rules.Characters[View.PlacementSlot].Name} within base vision." : "Deployment complete. Turn 01 begins.";
                }
                Events.Refresh(); return;
            }
            if (Match.Phase != BattlePhase.Planning) return;
            if (quickMove)
            {
                if (Selected == null || Selected.Owner != Player || !Selected.Alive) { Message = "Select a living ally first."; Events.Refresh(); return; }
                Action = BattleAction.Move; Armed = true; PortalEntrance = null;
            }
            if (!Armed)
            {
                var candidates = View.Units.Where(u => u.Alive && u.Cell == cell).OrderBy(u => u.Owner == Player ? 0 : 1).ThenBy(u => u.Id).ToList();
                if (candidates.Count > 0) Select(candidates[(candidates.FindIndex(u => u.Id == SelectedId) + 1) % candidates.Count].Id, false);
                return;
            }
            if (Selected == null || Selected.Owner != Player) return;
            var order = new BattleOrder { UnitId = SelectedId, Action = Action, Target = cell };
            if (Action == BattleAction.Move || Action == BattleAction.Skill && Rules.Characters[Selected.Slot].SkillEffect == SkillEffect.StealthMove)
                order.Path = Match.Path(SelectedId, cell, Action == BattleAction.Skill);
            if (Action == BattleAction.Equipment && Selected.Equipment >= 0)
            {
                var equipment = Rules.Equipment[Selected.Equipment];
                if (equipment.Effect == EquipmentEffect.Portal)
                {
                    if (!PortalEntrance.HasValue)
                    {
                        if (!View.Explored.Contains(cell) || Selected.Cell.Distance(cell) > equipment.Action.Range || cell == View.OwnBase.Cell)
                        { Message = "Choose an explored entrance within range."; Events.Refresh(); return; }
                        PortalEntrance = cell; Message = "Entrance set. Choose the portal exit (up to 10 tiles away)."; Events.Refresh(); return;
                    }
                    order.Target = PortalEntrance.Value; order.Exit = cell;
                }
                if (equipment.Effect == EquipmentEffect.Swap) order.TargetUnit = View.Units.FirstOrDefault(u => u.Owner == Player && u.Id != SelectedId && u.Alive && u.Cell == cell)?.Id ?? 0;
            }
            if (Submit(order)) PortalEntrance = null;
        }
        private void SetPath(List<GridCell> path)
        {
            if (Running || Selected == null || Selected.Owner != Player || path == null || path.Count == 0) return;
            var action = Action == BattleAction.Skill && Rules.Characters[Selected.Slot].SkillEffect == SkillEffect.StealthMove ? BattleAction.Skill : BattleAction.Move;
            Action = action; Armed = true; Submit(new BattleOrder { UnitId = SelectedId, Action = action, Target = path.Last(), Path = path });
        }
        private void PreviewPath(List<GridCell> path) { PathPreview = new List<GridCell>(path); Events.Refresh(); }
        private bool Submit(BattleOrder order)
        {
            bool accepted = Match.Queue(Player, View.Turn, order, out string error);
            Message = accepted ? order.Action == BattleAction.Wait ? "Order canceled. Reserved AP released." : $"{order.Action.ToString().ToUpperInvariant()} reserved. AP is spent when it executes." : error;
            Events.Refresh(); return accepted;
        }
        private void Hover(GridCell cell)
        {
            if (HoverCell == cell) return;
            HoverCell = cell; if (Armed) Events.Refresh();
        }
        private void Ready()
        {
            if (!Match.Ready(Player)) return;
            Armed = false; Message = "Team ready. Switch player with TAB. Both ready ends planning immediately.";
            Events.Refresh(); if (Match.BothReady) Resolve();
        }
        private void Switch()
        {
            if (Running || Match.Phase == BattlePhase.BasePlacement || Match.Phase == BattlePhase.UnitPlacement || Match.Phase == BattlePhase.Setup) return;
            Player = 1 - Player; SelectedId = Player * 5 + 1; Armed = false; PortalEntrance = null; HoverCell = null;
            Message = $"P{Player+1} perspective. Enemy orders and AP stay private. Your field is below."; Events.Refresh();
        }
        private void TogglePause()
        {
            if (Running) return;
            Paused = !Paused; Message = Paused ? "LOCAL TEST / Planning clock paused. Both ready and Resolve still work." : "Planning clock resumed."; Events.Refresh();
        }
        private void ToggleReveal() { RevealAll = !RevealAll; Message = RevealAll ? "DEBUG VISION / Both teams are revealed for local testing." : "Player-limited vision restored."; Events.Refresh(); }
        private void Help() { ShowHelp = !ShowHelp; Events.Refresh(); }
        private void Craft(int index, bool replace)
        {
            if (!EquipmentDecks[Player].Contains(index)) { Message = "This equipment is not in your chosen deck."; Events.Refresh(); return; }
            Message = Match.Craft(Player, SelectedId, index, replace, out string error) ? $"{Rules.Equipment[index].Name} equipped. One crafting material used." : error;
            Events.Refresh();
        }
        private void Resolve()
        {
            if (Running) { fastForward = true; Message = "Fast-forwarding this resolution."; return; }
            var timeline = Match.Resolve(); if (timeline == null) return;
            Running = true; fastForward = false; Armed = false; PortalEntrance = null;
            playback = StartCoroutine(Playback(timeline));
        }
        private IEnumerator Playback(BattleTimeline timeline)
        {
            foreach (var step in timeline.Steps)
            {
                display = step.Before.Copy(); display.Phase = BattlePhase.Resolving; display.Winner = BattleWinner.None; Events.Refresh();
                foreach (var cue in step.VisibleCues(Rules, Player, RevealAll))
                {
                    Message = $"P{cue.Owner+1} / {cue.Label}"; History.Add($"T{Match.Turn:00}  {Message}");
                    if (History.Count > 32) History.RemoveAt(0);
                    Events.Focus(cue.Target); Events.Cue(cue);
                    float duration = cue.Kind == CueKind.Move || cue.Kind == CueKind.Portal ? Configuration.MoveSeconds : Configuration.ActionSeconds;
                    float elapsed = 0;
                    while (!fastForward && elapsed < duration) { elapsed += Time.unscaledDeltaTime; yield return null; }
                    yield return null;
                }
                display = step.After.Copy(); display.Phase = BattlePhase.Resolving; display.Winner = BattleWinner.None; Events.Refresh();
            }
            display = null; Match.CompleteResolution(clock); Running = false; playback = null;
            Message = Match.Phase == BattlePhase.Finished ? "Match complete. Review the result or start a rematch." : "Resolution complete. AP carries over into the next turn.";
            Events.Refresh();
        }
    }
}

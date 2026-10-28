using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TurnZero
{
    public sealed class BattleSession : MonoBehaviour
    {
        public BattleEvents Events { get; } = new BattleEvents();
        public BattleConfiguration Configuration { get; private set; }
        public LocalBattleMatch Match { get; private set; }
        public GameRules Rules => Match.Rules;

        public BattleView View
        {
            get
            {
                if (cachedView == null ||
                    viewMatchRevision != Match.Revision ||
                    viewPlaybackRevision != replay.Revision ||
                    cachedView.Player != Player ||
                    viewRevealAll != RevealAll)
                {
                    cachedView = display == null
                        ? Match.View(Player, clock, RevealAll)
                        : BattleVisibility.View(display, Rules, Player, clock, RevealAll);
                    viewMatchRevision = Match.Revision;
                    viewPlaybackRevision = replay.Revision;
                    viewRevealAll = RevealAll;
                }

                cachedView.SecondsLeft = SecondsLeft;

                return cachedView;
            }
        }

        public double SecondsLeft => Math.Max(0, (display == null ? Match.Deadline : display.Deadline) - clock);

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
        public int[] Fields { get; } = new int[GameDefinitions.PlayerCount];
        public int[][] EquipmentDecks { get; } = new[] { new[] { 0, 1, 2, -1, -1 }, new[] { 0, 1, 2, -1, -1 } };
        public List<string> History { get; } = new List<string>();
        public List<GridCell> PathPreview { get; } = new List<GridCell>();

        private BattlePlayback replay;
        private BattleView cachedView;
        private int viewMatchRevision;
        private int viewPlaybackRevision;
        private bool viewRevealAll;

        private MatchState display => replay?.Display;

        private double clock;
        private Coroutine playback;

        public BattleUnit Selected
        {
            get
            {
                BattleView view = View;

                return view.FindUnit(SelectedId) ?? view.FindKnownEnemy(SelectedId)?.Unit;
            }
        }

        public bool CanUseCamera
        {
            get
            {
                return !Running &&
                    (Match.Phase == BattlePhase.Planning ||
                    Match.Phase == BattlePhase.BasePlacement ||
                    Match.Phase == BattlePhase.UnitPlacement);
            }
        }

        public void Initialize(BattleConfiguration configuration)
        {
            Configuration = configuration;
            replay = new BattlePlayback(configuration, Events);

            if (Match != null)
            {
                Restart();

                return;
            }

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
            if (Match == null)
            {
                return;
            }

            if (!(Paused && Match.Phase == BattlePhase.Planning))
            {
                clock += Time.unscaledDeltaTime;
            }

            if (Match.Tick(clock))
            {
                if (SelectedId == 0)
                {
                    SelectedId = Player * GameDefinitions.CharactersPerTeam + 1;
                }

                Message = "Plan one action per character. TAB switches player. ENTER readies your team.";
                Events.Refresh();
            }

            if (Match.ShouldResolve(clock))
            {
                Resolve();
            }
        }

        public void Restart()
        {
            if (playback != null)
            {
                StopCoroutine(playback);
            }

            replay.Reset();
            playback = null;

            Match = new LocalBattleMatch(Configuration.CreateRules());
            cachedView = null;
            Player = 0;
            SelectedId = 0;
            Action = BattleAction.Move;

            for (int owner = 0; owner < GameDefinitions.PlayerCount; owner++)
            {
                Fields[owner] %= Configuration.Fields.Length;

                for (int slot = 0; slot < GameDefinitions.EquipmentDeckSlots; slot++)
                {
                    if (EquipmentDecks[owner][slot] >= Rules.Equipment.Length)
                    {
                        EquipmentDecks[owner][slot] = -1;
                    }
                }
            }

            Armed = false;
            Running = false;
            Paused = false;
            RevealAll = false;
            ShowHelp = false;

            clock = 0;
            PortalEntrance = null;
            HoverCell = null;
            History.Clear();

            PathPreview.Clear();
            Message = "LOCAL PLAYTEST / Pick each side's field, then deploy a base and five characters.";
            Events.Refresh();
        }

        private void Deploy()
        {
            if (Match.Phase != BattlePhase.Setup)
            {
                return;
            }

            if (!ApplyDecks())
            {
                return;
            }

            Match.BeginPlacement();
            Message = "P1 / Place your base on your field. Then deploy five characters inside base vision.";
            Events.Refresh();
        }

        private void AutoDeploy()
        {
            if (Running ||
                Match.Phase == BattlePhase.Planning ||
                Match.Phase == BattlePhase.TurnStart ||
                Match.Phase == BattlePhase.Finished)
            {
                return;
            }

            if (Match.Phase == BattlePhase.Setup && !ApplyDecks())
            {
                return;
            }

            Match.AutoDeploy(clock);
            Player = 0;
            SelectedId = 1;
            Message = "Both teams deployed. The first turn starts with 10 AP.";
            Events.Refresh();
        }

        private void SetField(int player, int index)
        {
            if (Match.Phase != BattlePhase.Setup ||
                player < 0 ||
                player >= GameDefinitions.PlayerCount ||
                index < 0 ||
                index >= Configuration.Fields.Length)
            {
                return;
            }

            Fields[player] = index;
            Events.Refresh();
        }

        private void Select(int id, bool focus)
        {
            if (Running ||
                Match.Phase == BattlePhase.Setup ||
                Match.Phase == BattlePhase.BasePlacement ||
                Match.Phase == BattlePhase.UnitPlacement)
            {
                return;
            }

            BattleView view = View;
            BattleUnit unit = view.FindUnit(id) ?? view.FindKnownEnemy(id)?.Unit;

            if (unit == null)
            {
                Message = "This enemy has not been observed.";
                Events.Refresh();

                return;
            }

            SelectedId = id;
            Armed = false;
            PortalEntrance = null;
            HoverCell = null;

            Action = unit.Alive ? BattleAction.Move : BattleAction.Revive;

            if (focus && (unit.Owner == Player || unit.Alive))
            {
                Events.Focus(unit.Owner == Player && !unit.Alive ? view.OwnBase.Cell : unit.Cell);
            }

            Message = unit.Owner == Player
                ? "Q move / W attack / E skill / R equipment / T wait. Double-click a card to focus."
                : "Enemy information is limited to the latest observation.";
            Events.Refresh();
        }

        private bool ApplyDecks()
        {
            for (int player = 0; player < GameDefinitions.PlayerCount; player++)
            {
                if (!Match.SetEquipmentDeck(player, EquipmentDecks[player].Where(i => i >= 0).ToArray()))
                {
                    Message = "Choose 1 to 5 distinct equipment types for each team.";
                    Events.Refresh();

                    return false;
                }
            }

            return true;
        }

        private void CycleDeck(int player, int slot)
        {
            if (Match.Phase != BattlePhase.Setup ||
                player < 0 ||
                player >= GameDefinitions.PlayerCount ||
                slot < 0 ||
                slot >= GameDefinitions.EquipmentDeckSlots)
            {
                return;
            }

            int candidate = EquipmentDecks[player][slot];

            do
            {
                candidate = (candidate + 2) % (Rules.Equipment.Length + 1) - 1;
            }
            while (candidate >= 0 && EquipmentDecks[player].Where((_, i) => i != slot).Contains(candidate));

            EquipmentDecks[player][slot] = candidate;
            Events.Refresh();
        }

        private void SelectAction(BattleAction action)
        {
            if (Running || Match.Phase != BattlePhase.Planning || View.Ready || Selected?.Owner != Player)
            {
                return;
            }

            if (action == BattleAction.Wait)
            {
                Submit(new BattleOrder
                {
                    UnitId = SelectedId,
                    Action = BattleAction.Wait
                });
                Armed = false;
                PortalEntrance = null;
                Events.Refresh();

                return;
            }

            if (!Selected.Alive && action != BattleAction.Revive)
            {
                action = BattleAction.Revive;
            }

            if ((int)action < GameDefinitions.CooldownActionCount && Selected.ReadyOn[(int)action] > Match.Turn)
            {
                Message = $"Available on turn {Selected.ReadyOn[(int)action]}.";
                Events.Refresh();

                return;
            }

            bool rootBlocked = Selected.Rooted(Match.Turn) &&
                (action == BattleAction.Move ||
                action == BattleAction.Skill &&
                Rules.Characters[Selected.Slot].SkillEffect == SkillEffect.StealthMove ||
                action == BattleAction.Equipment &&
                Selected.Equipment >= 0 &&
                Rules.Equipment[Selected.Equipment].Effect == EquipmentEffect.Swap);

            if (rootBlocked)
            {
                Message = "ROOT prevents this movement action this turn.";
                Events.Refresh();

                return;
            }

            Action = action;
            Armed = true;
            PortalEntrance = null;
            HoverCell = null;

            Message = action == BattleAction.Equipment &&
                Selected.Equipment >= 0 &&
                Rules.Equipment[Selected.Equipment].Effect == EquipmentEffect.Portal
                ? "Choose an explored entrance, then an explored exit."
                : "Click a target tile. Right-click moves immediately; drag the selected character to draw a path.";
            Events.Refresh();
        }

        private void Tile(GridCell cell, bool quickMove)
        {
            if (Running || !Rules.Contains(cell))
            {
                return;
            }

            if (Match.Phase == BattlePhase.BasePlacement || Match.Phase == BattlePhase.UnitPlacement)
            {
                if (!Match.Place(Player, cell, clock, out string error))
                {
                    Message = error;
                }
                else
                {
                    if (Match.Phase == BattlePhase.BasePlacement || Match.Phase == BattlePhase.UnitPlacement)
                    {
                        Player = Match.PlacementPlayer;
                    }
                    else
                    {
                        Player = 0;
                        SelectedId = 1;
                    }

                    Message = Match.Phase == BattlePhase.BasePlacement
                        ? $"P{Player + 1} / Place your base."
                        : Match.Phase == BattlePhase.UnitPlacement
                        ? $"P{Player + 1} / Deploy {Rules.Characters[View.PlacementSlot].Name} within base vision."
                        : "Deployment complete. Turn 01 begins.";
                }

                Events.Refresh();

                return;
            }

            if (Match.Phase != BattlePhase.Planning)
            {
                return;
            }

            if (quickMove)
            {
                if (Selected == null || Selected.Owner != Player || !Selected.Alive)
                {
                    Message = "Select a living ally first.";
                    Events.Refresh();

                    return;
                }

                Action = BattleAction.Move;
                Armed = true;
                PortalEntrance = null;
            }

            if (!Armed)
            {
                BattleUnit next = View.FindNextUnitAt(cell, SelectedId);

                if (next != null)
                {
                    Select(next.Id, false);
                }

                return;
            }

            if (Selected == null || Selected.Owner != Player)
            {
                return;
            }

            BattleOrder order = new BattleOrder
            {
                UnitId = SelectedId,
                Action = Action,
                Target = cell
            };

            if (Action == BattleAction.Move ||
                Action == BattleAction.Skill &&
                Rules.Characters[Selected.Slot].SkillEffect == SkillEffect.StealthMove)
            {
                order.Path = Match.Path(SelectedId, cell, Action == BattleAction.Skill);
            }

            if (Action == BattleAction.Equipment && Selected.Equipment >= 0)
            {
                EquipmentSpec equipment = Rules.Equipment[Selected.Equipment];

                if (equipment.Effect == EquipmentEffect.Portal)
                {
                    if (!PortalEntrance.HasValue)
                    {
                        if (!View.Explored.Contains(cell) ||
                            Selected.Cell.Distance(cell) > equipment.Action.Range ||
                            cell == View.OwnBase.Cell)
                        {
                            Message = "Choose an explored entrance within range.";
                            Events.Refresh();

                            return;
                        }

                        PortalEntrance = cell;
                        Message = "Entrance set. Choose the portal exit (up to 10 tiles away).";
                        Events.Refresh();

                        return;
                    }

                    order.Target = PortalEntrance.Value;
                    order.Exit = cell;
                }

                if (equipment.Effect == EquipmentEffect.Swap)
                {
                    order.TargetUnit = View.Units.FirstOrDefault(u => u.Owner == Player && u.Id != SelectedId && u.Alive && u.Cell == cell)?.Id ?? 0;
                }
            }

            if (Submit(order))
            {
                PortalEntrance = null;
            }
        }

        private void SetPath(List<GridCell> path)
        {
            if (Running || Selected == null || Selected.Owner != Player || path == null || path.Count == 0)
            {
                return;
            }

            BattleAction action = Action == BattleAction.Skill && Rules.Characters[Selected.Slot].SkillEffect == SkillEffect.StealthMove
                ? BattleAction.Skill
                : BattleAction.Move;

            Action = action;
            Armed = true;
            Submit(new BattleOrder
            {
                UnitId = SelectedId,
                Action = action,
                Target = path[path.Count - 1],
                Path = path
            });
        }

        private void PreviewPath(List<GridCell> path)
        {
            bool unchanged = PathPreview.Count == path.Count;

            for (int i = 0; unchanged && i < path.Count; i++)
            {
                unchanged = PathPreview[i] == path[i];
            }

            if (unchanged)
            {
                return;
            }

            PathPreview.Clear();
            PathPreview.AddRange(path);
            Events.Refresh();
        }

        private bool Submit(BattleOrder order)
        {
            bool accepted = Match.Queue(Player, View.Turn, order, out string error, clock);

            Message = accepted
                ? order.Action == BattleAction.Wait
                ? "Order canceled. Reserved AP released."
                : $"{order.Action.ToString().ToUpperInvariant()} reserved. AP is spent when it executes."
                : error;

            Events.Refresh();

            return accepted;
        }

        private void Hover(GridCell cell)
        {
            if (HoverCell == cell)
            {
                return;
            }

            HoverCell = cell;

            if (Armed)
            {
                Events.Refresh();
            }
        }

        private void Ready()
        {
            if (!Match.Ready(Player, clock))
            {
                return;
            }

            Armed = false;
            Message = View.Ready
                ? "Team ready. Press READY again to unlock while time remains."
                : "Ready canceled. Planning time is unchanged.";

            Events.Refresh();

            if (Match.BothReady)
            {
                Resolve();
            }
        }

        private void Switch()
        {
            if (Running ||
                Match.Phase == BattlePhase.BasePlacement ||
                Match.Phase == BattlePhase.UnitPlacement ||
                Match.Phase == BattlePhase.Setup)
            {
                return;
            }

            Player = 1 - Player;
            SelectedId = Player * GameDefinitions.CharactersPerTeam + 1;
            Armed = false;
            PortalEntrance = null;
            HoverCell = null;

            Message = $"P{Player + 1} perspective. Enemy orders and AP stay private. Your field is below.";
            Events.Refresh();
        }

        private void TogglePause()
        {
            if (Running)
            {
                return;
            }

            Paused = !Paused;
            Message = Paused ? "LOCAL TEST / Planning clock paused. Both ready and Resolve still work." : "Planning clock resumed.";
            Events.Refresh();
        }

        private void ToggleReveal()
        {
            RevealAll = !RevealAll;
            Message = RevealAll ? "DEBUG VISION / Both teams are revealed for local testing." : "Player-limited vision restored.";
            Events.Refresh();
        }

        private void Help()
        {
            ShowHelp = !ShowHelp;
            Events.Refresh();
        }

        private void Craft(int index, bool replace)
        {
            if (!EquipmentDecks[Player].Contains(index))
            {
                Message = "This equipment is not in your chosen deck.";
                Events.Refresh();

                return;
            }

            Message = Match.Craft(Player, SelectedId, index, replace, out string error, clock)
                ? $"{Rules.Equipment[index].Name} equipped. One crafting material used."
                : error;
            Events.Refresh();
        }

        private void Resolve()
        {
            if (Running)
            {
                replay.FastForward = true;
                Message = "Fast-forwarding this resolution.";

                return;
            }

            BattleTimeline timeline = Match.Resolve();

            if (timeline == null)
            {
                return;
            }

            Running = true;
            replay.FastForward = false;
            Armed = false;
            PortalEntrance = null;

            playback = StartCoroutine(Playback(timeline));
        }

        private IEnumerator Playback(BattleTimeline timeline)
        {
            yield return replay.Play(
                timeline,
                Rules,
                Player,
                RevealAll,
                message =>
                {
                    Message = message;
                    History.Add($"T{Match.Turn:00}  {message}");

                    if (History.Count > 32)
                    {
                        History.RemoveAt(0);
                    }
                });

            Match.CompleteResolution(clock);
            Running = false;
            playback = null;

            Message = Match.Phase == BattlePhase.Finished
                ? "Match complete. Review the result or start a rematch."
                : "Resolution complete. AP carries over into the next turn.";
            Events.Refresh();
        }

        private void OnDestroy()
        {
            if (playback != null)
            {
                StopCoroutine(playback);
            }

            Events.CharacterRequested -= Select;
            Events.ActionRequested -= SelectAction;
            Events.TileRequested -= Tile;
            Events.PathRequested -= SetPath;
            Events.PathPreviewRequested -= PreviewPath;
            Events.HoverRequested -= Hover;
            Events.ReadyRequested -= Ready;
            Events.SwitchRequested -= Switch;
            Events.ResolveRequested -= Resolve;
            Events.RestartRequested -= Restart;
            Events.DeployRequested -= Deploy;
            Events.AutoDeployRequested -= AutoDeploy;
            Events.PauseRequested -= TogglePause;
            Events.RevealRequested -= ToggleReveal;
            Events.CraftRequested -= Craft;
            Events.FieldRequested -= SetField;
            Events.DeckRequested -= CycleDeck;
            Events.HelpRequested -= Help;
        }
    }
}

using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalBattleHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text phaseText, timerText, baseText, perspectiveText, messageText, logText, selectedName, selectedHealth, selectedStatus, actionInfo, reservedInfo, craftInfo, phaseBannerText, resultText;
        [SerializeField] private Image selectedPortrait;
        [SerializeField] private Button[] actionButtons, craftButtons, fieldButtons, deckButtons;
        [SerializeField] private TMP_Text[] actionLabels, craftLabels, fieldLabels, deckLabels;
        [SerializeField] private Button readyButton, switchButton, resolveButton, restartButton, pauseButton, revealButton, helpButton, startButton, autoDeployButton, rematchButton, closeHelpButton, confirmButton, cancelConfirmButton;
        [SerializeField] private TMP_Text readyLabel, switchLabel, pauseLabel, revealLabel, resolveLabel;
        [SerializeField] private GameObject setupPanel, helpPanel, resultPanel, phaseBanner, confirmPanel;
        [SerializeField] private LocalRosterCard[] cards;
        [SerializeField] private LocalApPanel apPanel;
        private BattleSession session;
        private TMP_Text[] setupRosters;
        public void Bind(BattleSession value)
        {
            session = value; session.Events.Changed += Render; session.Events.CueRequested += Cue;
            setupRosters = setupPanel.GetComponentsInChildren<TMP_Text>(true).Where(t => t.name == "Roster list").ToArray();
            foreach (var card in cards) card.Bind(value);
            for (int i = 0; i < actionButtons.Length; i++) { int index = i; actionButtons[i].onClick.AddListener(() => session.Events.SetAction((BattleAction)index)); }
            for (int i = 0; i < craftButtons.Length; i++) { int index = i; craftButtons[i].onClick.AddListener(() => Craft(index)); }
            for (int i = 0; i < fieldButtons.Length; i++) { int index = i; fieldButtons[i].onClick.AddListener(() => session.Events.Field(index/3,index%3)); }
            for (int i = 0; i < deckButtons.Length; i++) { int index = i; deckButtons[i].onClick.AddListener(() => session.Events.Deck(index/GameDefinitions.EquipmentDeckSlots,index%GameDefinitions.EquipmentDeckSlots)); }
            readyButton.onClick.AddListener(session.Events.Ready); switchButton.onClick.AddListener(session.Events.Switch);
            resolveButton.onClick.AddListener(session.Events.Resolve); restartButton.onClick.AddListener(session.Events.Restart);
            pauseButton.onClick.AddListener(session.Events.Pause); revealButton.onClick.AddListener(session.Events.Reveal); helpButton.onClick.AddListener(session.Events.Help);
            startButton.onClick.AddListener(session.Events.Deploy); autoDeployButton.onClick.AddListener(session.Events.AutoDeploy);
            rematchButton.onClick.AddListener(session.Events.Restart); closeHelpButton.onClick.AddListener(session.Events.Help);
            cancelConfirmButton.onClick.AddListener(() => confirmPanel.SetActive(false));
        }
        private void Update()
        {
            if (session == null) return;
            var view = session.View;
            timerText.text = view.Phase == BattlePhase.Planning ? $"{Mathf.CeilToInt((float)view.SecondsLeft):00}s" : view.Phase == BattlePhase.TurnStart ? "GET READY" : "--";
            if (view.Phase == BattlePhase.TurnStart) phaseBannerText.text = $"TURN {view.Turn:00}\n<size=35%>+{session.Rules.TurnIncome} AP  /  PLAN YOUR NEXT MOVE</size>";
        }
        private void Render()
        {
            var view = session.View; var rules = session.Rules;
            phaseText.text = view.Phase == BattlePhase.Setup ? "MATCH SETUP" : view.Phase == BattlePhase.BasePlacement ? "DEPLOY BASE" : view.Phase == BattlePhase.UnitPlacement ? $"DEPLOY {view.PlacementSlot+1} / 5" : $"TURN {view.Turn:00}  /  {view.Phase.ToString().ToUpperInvariant()}";
            perspectiveText.text = $"P{view.Player+1} PERSPECTIVE" + (session.RevealAll ? "  /  DEBUG VISION" : "");
            baseText.text = $"P{view.Player+1}  {view.OwnBase.Health}/{rules.BaseHealth}     VS     P{2-view.Player}  " + (view.EnemyBase == null ? "?/?" : $"{view.EnemyBase.Health}/{rules.BaseHealth}{(view.EnemyBaseVisible ? "" : " *")}");
            messageText.text = session.Message;
            logText.text = string.Join("\n",session.History.Skip(Mathf.Max(0,session.History.Count-3)));
            foreach (var card in cards) card.Render(view);
            apPanel.Render(view,rules.MaximumAP);
            setupPanel.SetActive(view.Phase == BattlePhase.Setup);
            helpPanel.SetActive(session.ShowHelp); resultPanel.SetActive(view.Phase == BattlePhase.Finished);
            phaseBanner.SetActive(view.Phase == BattlePhase.TurnStart);
            if (view.Phase == BattlePhase.Setup) confirmPanel.SetActive(false);
            if (view.Phase == BattlePhase.Finished) resultText.text = view.Winner == BattleWinner.Draw ? "DRAW\n<size=28%>THE BATTLE IS OVER</size>" : $"PLAYER {(view.Winner == BattleWinner.PlayerOne ? 1 : 2)} WINS\n<size=28%>BATTLE COMPLETE / TURN {view.Turn:00}</size>";
            readyLabel.text = view.Ready ? "READY / CLICK TO UNLOCK" : "ENTER / READY";
            readyButton.interactable = view.Phase == BattlePhase.Planning && !session.Running;
            switchLabel.text = $"TAB / VIEW P{2-view.Player}";
            switchButton.interactable = !session.Running && view.Phase != BattlePhase.Setup && view.Phase != BattlePhase.BasePlacement && view.Phase != BattlePhase.UnitPlacement;
            pauseLabel.text = session.Paused ? "P / RESUME" : "P / PAUSE";
            revealLabel.text = session.RevealAll ? "VISION / DEBUG" : "VISION / PLAYER";
            resolveLabel.text = session.Running ? "SPACE / FAST" : "SPACE / RESOLVE";
            resolveButton.interactable = view.Phase == BattlePhase.Planning || session.Running;
            pauseButton.interactable = !session.Running;
            for (int i = 0; i < fieldButtons.Length; i++)
            {
                int field = i%3; fieldButtons[i].interactable = field < session.Configuration.Fields.Length;
                fieldLabels[i].text = (session.Fields[i/3] == field ? "> " : "") + (field < session.Configuration.Fields.Length ? session.Configuration.Fields[field].DisplayName.ToUpperInvariant() : "EMPTY");
            }
            for (int i = 0; i < deckButtons.Length; i++) { int type = session.EquipmentDecks[i/GameDefinitions.EquipmentDeckSlots][i%GameDefinitions.EquipmentDeckSlots]; deckLabels[i].text = $"{i%GameDefinitions.EquipmentDeckSlots+1}\n" + (type < 0 ? "EMPTY" : rules.Equipment[type].Name.ToUpperInvariant()); }
            foreach (var text in setupRosters) text.text = "STARTER ROSTER / FIVE DISTINCT CHARACTERS\n" + string.Join(" / ",rules.Characters.Select(c => $"{c.Name.ToUpperInvariant()} {c.Health}HP"));
            var unit = session.Selected;
            var definition = unit == null ? null : session.Configuration.Characters[unit.Slot];
            selectedName.text = definition == null ? "SELECT A CHARACTER" : definition.Values.Name.ToUpperInvariant();
            selectedName.color = definition == null ? Color.white : definition.Accent;
            selectedHealth.text = unit == null ? "-- / -- HP" : $"{unit.Health} / {definition.Values.Health} HP";
            selectedStatus.text = unit == null ? "CLICK A MODEL OR CARD" : unit.Owner != view.Player && !view.Units.Any(u => u.Id == unit.Id) ? "LAST OBSERVED / READ ONLY" : !unit.Alive ? "DEFEATED / REVIVE NEXT TURN" : unit.Rooted(view.Turn) ? "ROOT / CANNOT MOVE" : BattleStatusEffects.Has(unit,StatusEffectKind.Stealth,view.Turn) ? "STEALTH / ENEMY CANNOT SEE" : "NO STATUS EFFECTS";
            selectedPortrait.sprite = definition?.Portrait; selectedPortrait.color = definition == null ? Color.clear : Color.white;
            bool own = unit != null && unit.Owner == view.Player;
            bool edit = own && view.Phase == BattlePhase.Planning && !session.Running;
            var names = new[] { "Q  MOVE", "W  ATTACK", "E  SKILL", "R  EQUIPMENT", "T  WAIT / CANCEL" };
            for (int i = 0; i < actionButtons.Length; i++)
            {
                var spec = unit == null || i == 4 ? null : BattleCommands.Spec(rules,unit,(BattleAction)i);
                bool revive = own && !unit.Alive && i == 0;
                bool rooted = unit != null && unit.Rooted(view.Turn) && (i == 0 || i == 2 && rules.Characters[unit.Slot].SkillEffect == SkillEffect.StealthMove || i == 3 && unit.Equipment >= 0 && rules.Equipment[unit.Equipment].Effect == EquipmentEffect.Swap);
                actionLabels[i].text = revive ? $"REVIVE / {BattleCommands.Cost(rules,unit,BattleAction.Revive,view.Turn)} AP" : names[i] + (i < 4 ? $"\n{(spec == null ? "--" : spec.Cost.ToString())} AP" : "");
                actionButtons[i].interactable = edit && !rooted && (revive || unit.Alive && (i == 4 || spec != null && unit.ReadyOn[i] <= view.Turn));
                if (rooted) actionLabels[i].text += " / ROOT";
                if (unit != null && i < 4 && unit.ReadyOn[i] > view.Turn && !revive) actionLabels[i].text += $" / {unit.ReadyOn[i]-view.Turn}T";
            }
            var selectedOrder = unit == null ? null : view.Orders.Find(o => o.UnitId == unit.Id);
            reservedInfo.text = selectedOrder == null ? "RESERVED  /  WAIT" : $"RESERVED  /  {selectedOrder.Action.ToString().ToUpperInvariant()}  /  {BattleCommands.Cost(rules,unit,selectedOrder.Action,view.Turn)} AP";
            if (unit == null) actionInfo.text = "Select an ally to inspect actions and reserve a command.";
            else if (!unit.Alive) actionInfo.text = "REVIVE / Target your field inside base vision. Requires another living ally. Full HP; no second action this turn.";
            else
            {
                var spec = BattleCommands.Spec(rules,unit,session.Action);
                bool movement = session.Action == BattleAction.Move || session.Action == BattleAction.Skill && rules.Characters[unit.Slot].SkillEffect == SkillEffect.StealthMove;
                bool globalSwap = session.Action == BattleAction.Equipment && unit.Equipment >= 0 && rules.Equipment[unit.Equipment].Effect == EquipmentEffect.Swap;
                string scope = spec == null ? "" : globalSwap ? "RANGE GLOBAL / 1 ALLY" : movement ? $"PATH {spec.Range}" : $"RANGE {spec.Range} / AREA {spec.Area}";
                actionInfo.text = spec == null ? "Equip an item using a crafting material below. QWER chooses actions; T waits for free." :
                    $"<color=#E7B768>{spec.Name.ToUpperInvariant()}</color>\n{spec.Cost} AP  /  {scope}\nPHASE {spec.Priority}  /  COOLDOWN {spec.Cooldown} / READY T{((int)session.Action < 4 ? unit.ReadyOn[(int)session.Action] : view.Turn)}\n{spec.Description}";
            }
            craftInfo.text = $"INVENTORY / {view.Materials} CRAFT MATERIALS\nChoose equipment to craft. Replacement destroys the old item.";
            for (int i = 0; i < craftButtons.Length; i++)
            {
                craftLabels[i].text = i < rules.Equipment.Length ? rules.Equipment[i].Name.ToUpperInvariant() + $"\n{rules.Equipment[i].Action.Cost} AP" : "--";
                craftButtons[i].interactable = edit && unit.Alive && view.Materials > 0 && i < rules.Equipment.Length && session.EquipmentDecks[view.Player].Contains(i);
            }
        }
        private void Cue(BattleCue cue) { if (cue.Kind == CueKind.Reward && cue.Owner == session.Player) apPanel.FlashReward(cue.APDelta); messageText.text = session.Message; logText.text = string.Join("\n",session.History.Skip(Mathf.Max(0,session.History.Count-3))); }
        private void Craft(int index)
        {
            if (session.Selected?.Equipment >= 0)
            {
                confirmPanel.SetActive(true); confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() => { confirmPanel.SetActive(false); session.Events.Craft(index,true); });
            }
            else session.Events.Craft(index,false);
        }
        private void OnDestroy() { if (session != null) { session.Events.Changed -= Render; session.Events.CueRequested -= Cue; } }
    }
}

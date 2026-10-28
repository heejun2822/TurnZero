using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalActorsView : MonoBehaviour
    {
        [SerializeField]
        private LocalFieldMap map;
        [SerializeField]
        private LocalActorView[] actors;
        [SerializeField]
        private LocalActorView[] ghosts;

        private BattleSession session;

        public void Bind(BattleSession value, Camera camera)
        {
            session = value;

            foreach (LocalActorView actor in actors)
            {
                actor.Bind(camera);
            }

            foreach (LocalActorView ghost in ghosts)
            {
                ghost.Bind(camera);
            }
        }

        public void Render(BattleView view)
        {
            GameRules rules = session.Rules;

            if (view.Phase == BattlePhase.Setup)
            {
                foreach (LocalActorView actor in actors.Concat(ghosts))
                {
                    actor.EnsureModel(actor.EntityId >= GameDefinitions.FirstBaseId
                        ? session.Configuration.BaseModel
                        : session.Configuration.Characters[(actor.EntityId - 1) % GameDefinitions.CharactersPerTeam].Model);
                }
            }

            foreach (LocalActorView actor in actors)
            {
                if (actor.EntityId >= GameDefinitions.FirstBaseId)
                {
                    int owner = actor.EntityId - GameDefinitions.FirstBaseId;
                    BattleBase home = owner == view.Player ? view.OwnBase : view.EnemyBase;

                    bool visible = home != null && home.Placed && home.Health > 0;

                    actor.Render(
                        visible,
                        home?.Cell ?? default,
                        visible ? map.Position(home.Cell) : Vector3.zero,
                        home == null ? "" : $"P{owner + 1} BASE\n{home.Health} / {rules.BaseHealth}",
                        LocalBattleWorld.Team(owner),
                        false,
                        owner != view.Player && !view.EnemyBaseVisible);
                    continue;
                }

                BattleUnit unit = view.FindUnit(actor.EntityId);

                if (unit != null && !unit.Alive)
                {
                    unit = null;
                }
                Vector3 offset = Vector3.zero;

                int stackCount = 1;
                bool showLabel = true;

                if (unit != null)
                {
                    int index = 0;
                    bool selectedInStack = false;

                    stackCount = 0;

                    foreach (BattleUnit other in view.Units)
                    {
                        if (!other.Alive || other.Cell != unit.Cell)
                        {
                            continue;
                        }

                        stackCount++;

                        if (other.Id < unit.Id)
                        {
                            index++;
                        }

                        selectedInStack |= other.Id == session.SelectedId;
                    }

                    if (stackCount > 1)
                    {
                        offset = new Vector3(Mathf.Cos(index * Mathf.PI * 2 / stackCount), 0, Mathf.Sin(index * Mathf.PI * 2 / stackCount)) * map.Spacing * 0.31f;
                        showLabel = unit.Id == session.SelectedId || !selectedInStack && index == 0;
                    }
                }

                string text = unit == null
                    ? ""
                    : $"{unit.Slot + 1}  {unit.Health}/{rules.Characters[unit.Slot].Health}" + (unit.Rooted(view.Turn)
                    ? " ROOT"
                    : BattleStatusEffects.Has(unit, StatusEffectKind.Stealth, view.Turn) ? " HIDDEN" : "");

                if (stackCount > 1)
                {
                    text += $"\nSTACK {stackCount}";
                }

                actor.Render(
                    unit != null,
                    unit?.Cell ?? default,
                    unit == null ? Vector3.zero : map.Position(unit.Cell) + offset,
                    text,
                    LocalBattleWorld.Team(unit?.Owner ?? 0),
                    unit?.Id == session.SelectedId,
                    false,
                    showLabel,
                    stackCount > 1 ? 0.62f : 1);
            }

            foreach (LocalActorView ghost in ghosts)
            {
                UnitObservation seen = view.FindLastSeen(ghost.EntityId);

                ghost.Render(
                    seen != null,
                    seen?.Unit.Cell ?? default,
                    seen == null ? Vector3.zero : map.Position(seen.Unit.Cell),
                    seen == null
                    ? ""
                    : $"{rules.Characters[seen.Unit.Slot].Name}\n{view.Turn - seen.Turn} T AGO / {seen.Unit.Health} HP",
                    Color.gray,
                    false,
                    true);
            }

        }

        public bool Play(BattleCue cue)
        {
            if (cue.Kind == CueKind.Swap)
            {
                foreach (KeyValuePair<int, GridCell> position in cue.Positions)
                {
                    LocalActorView swapped = FindActor(position.Key);

                    if (swapped != null && swapped.gameObject.activeSelf)
                    {
                        swapped.MoveTo(map.Position(position.Value), session.Configuration.ActionSeconds);
                    }
                }

                return true;
            }

            if (cue.Kind == CueKind.Move || cue.Kind == CueKind.Portal)
            {
                LocalActorView actor = FindActor(cue.ActorId);

                if (actor != null && actor.gameObject.activeSelf)
                {
                    actor.MoveTo(map.Position(cue.Target), session.Configuration.MoveSeconds);
                }

                return true;
            }

            return false;
        }

        private LocalActorView FindActor(int id)
        {
            foreach (LocalActorView actor in actors)
            {
                if (actor.EntityId == id)
                {
                    return actor;
                }
            }

            return null;
        }
    }
}

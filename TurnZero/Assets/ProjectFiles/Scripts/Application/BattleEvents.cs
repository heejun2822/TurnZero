using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnZero
{
    // Owned by one scene session. No static listeners survive restart or scene reload.
    public sealed class BattleEvents
    {
        public event Action Changed;
        public event Action<int, bool> CharacterRequested;
        public event Action<BattleAction> ActionRequested;
        public event Action<GridCell, bool> TileRequested;
        public event Action<List<GridCell>> PathRequested;
        public event Action<List<GridCell>> PathPreviewRequested;
        public event Action<GridCell> HoverRequested;
        public event Action<Vector2> PanRequested;
        public event Action<float> ZoomRequested;
        public event Action<GridCell, bool> FocusRequested;
        public event Action ReadyRequested;
        public event Action SwitchRequested;
        public event Action ResolveRequested;
        public event Action RestartRequested;
        public event Action DeployRequested;
        public event Action AutoDeployRequested;
        public event Action PauseRequested;
        public event Action RevealRequested;
        public event Action<int, bool> CraftRequested;
        public event Action<int, int> FieldRequested;
        public event Action<int, int> DeckRequested;
        public event Action<BattleCue> CueRequested;
        public event Action HelpRequested;
        public event Action MovementRequested;

        public void Refresh()
        {
            Changed?.Invoke();
        }

        public void Select(int id, bool focus = false)
        {
            CharacterRequested?.Invoke(id, focus);
        }

        public void SetAction(BattleAction action)
        {
            ActionRequested?.Invoke(action);
        }

        public void Tile(GridCell cell, bool quickMove = false)
        {
            TileRequested?.Invoke(cell, quickMove);
        }

        public void Path(List<GridCell> path)
        {
            PathRequested?.Invoke(path);
        }

        public void PreviewPath(List<GridCell> path)
        {
            PathPreviewRequested?.Invoke(path);
        }

        public void Hover(GridCell cell)
        {
            HoverRequested?.Invoke(cell);
        }

        public void Pan(Vector2 delta)
        {
            PanRequested?.Invoke(delta);
        }

        public void Zoom(float amount)
        {
            ZoomRequested?.Invoke(amount);
        }

        public void Focus(GridCell cell, bool immediate = false)
        {
            FocusRequested?.Invoke(cell, immediate);
        }

        public void Ready()
        {
            ReadyRequested?.Invoke();
        }

        public void Switch()
        {
            SwitchRequested?.Invoke();
        }

        public void Resolve()
        {
            ResolveRequested?.Invoke();
        }

        public void Restart()
        {
            RestartRequested?.Invoke();
        }

        public void Deploy()
        {
            DeployRequested?.Invoke();
        }

        public void AutoDeploy()
        {
            AutoDeployRequested?.Invoke();
        }

        public void Pause()
        {
            PauseRequested?.Invoke();
        }

        public void Reveal()
        {
            RevealRequested?.Invoke();
        }

        public void Craft(int index, bool replace)
        {
            CraftRequested?.Invoke(index, replace);
        }

        public void Field(int player, int index)
        {
            FieldRequested?.Invoke(player, index);
        }

        public void Deck(int player, int slot)
        {
            DeckRequested?.Invoke(player, slot);
        }

        public void Cue(BattleCue cue)
        {
            CueRequested?.Invoke(cue);
        }

        public void Movement()
        {
            MovementRequested?.Invoke();
        }

        public void Help()
        {
            HelpRequested?.Invoke();
        }
    }
}

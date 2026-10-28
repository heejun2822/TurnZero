using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace TurnZero.Game
{
    public sealed class BattleInput : MonoBehaviour
    {
        private BattleSession session;
        private LocalBattleWorld world;
        private LocalBattleCamera cameraRig;
        private bool pressed, dragging, unitDrag, blocked, touching;
        private Vector2 pressPoint, lastPoint;
        private GridCell startCell;
        private readonly List<GridCell> path = new List<GridCell>();
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private int lastKey = -1;
        private double lastKeyTime;
        private float lastPinch;
        public void Bind(BattleSession value, LocalBattleWorld board, LocalBattleCamera camera) { session = value; world = board; cameraRig = camera; }
        private void Update()
        {
            if (session == null || !Application.isFocused && !Application.isBatchMode) { pressed = false; return; }
            Keys();
            var touch = Touchscreen.current;
            if (touch != null && touch.touches.Count(t => t.press.isPressed) >= 2)
            {
                var points = touch.touches.Where(t => t.press.isPressed).Take(2).Select(t => t.position.ReadValue()).ToArray();
                float distance = Vector2.Distance(points[0],points[1]);
                if (lastPinch > 0 && !OverUI(points[0]) && !OverUI(points[1])) session.Events.Zoom((distance - lastPinch) / 80);
                lastPinch = distance; pressed = false; session.Events.PreviewPath(new List<GridCell>()); return;
            }
            lastPinch = 0;
            if (touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame))
            {
                touching = true; var primary = touch.primaryTouch;
                Pointer(primary.position.ReadValue(),primary.press.wasPressedThisFrame,primary.press.isPressed,primary.press.wasReleasedThisFrame,false);
                return;
            }
            if (touching) { touching = false; pressed = false; }
            var mouse = Mouse.current; if (mouse == null) return;
            var point = mouse.position.ReadValue();
            Pointer(point,mouse.leftButton.wasPressedThisFrame,mouse.leftButton.isPressed,mouse.leftButton.wasReleasedThisFrame,mouse.rightButton.wasPressedThisFrame);
            if (cameraRig.OnBoard(point) && !OverUI(point) && !mouse.leftButton.isPressed)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f) session.Events.Zoom(Mathf.Clamp(wheel / 120,-4,4));
            }
        }
        private void Keys()
        {
            var keyboard = Keyboard.current; if (keyboard == null) return;
            if (session.ShowHelp)
            {
                if (keyboard.f1Key.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame) session.Events.Help();
                return;
            }
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null) return;
            var digits = new[] { Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5 };
            for (int i = 0; i < digits.Length; i++) if (keyboard[digits[i]].wasPressedThisFrame)
            {
                bool twice = lastKey == i && Time.unscaledTimeAsDouble - lastKeyTime <= 0.32;
                session.Events.Select(session.Player * 5 + i + 1,twice); lastKey = i; lastKeyTime = Time.unscaledTimeAsDouble;
            }
            var actions = new[] { Key.Q,Key.W,Key.E,Key.R,Key.T };
            for (int i = 0; i < actions.Length; i++) if (keyboard[actions[i]].wasPressedThisFrame) session.Events.SetAction((BattleAction)i);
            if (keyboard.tabKey.wasPressedThisFrame) session.Events.Switch();
            if (keyboard.enterKey.wasPressedThisFrame) session.Events.Ready();
            if (keyboard.escapeKey.wasPressedThisFrame) { path.Clear(); session.Events.PreviewPath(path); session.Events.SetAction(BattleAction.Wait); }
            if (keyboard.f1Key.wasPressedThisFrame) session.Events.Help();
            if (keyboard.spaceKey.wasPressedThisFrame) session.Events.Resolve();
            if (keyboard.pKey.wasPressedThisFrame) session.Events.Pause();
        }
        private void Pointer(Vector2 point, bool down, bool held, bool up, bool right)
        {
            if (session.ShowHelp) return;
            bool onBoard = cameraRig.OnBoard(point) && !OverUI(point);
            if (onBoard && world.TryCell(cameraRig.Camera,point,out var hover)) session.Events.Hover(hover);
            if (right && onBoard && world.TryCell(cameraRig.Camera,point,out var quick)) { session.Events.Tile(quick,true); return; }
            if (down)
            {
                pressed = true; blocked = !onBoard || !session.CanUseCamera; dragging = false; pressPoint = lastPoint = point; path.Clear();
                bool found = world.TryCell(cameraRig.Camera,point,out startCell);
                var selected = session.Selected;
                unitDrag = found && selected != null && selected.Owner == session.Player && selected.Alive && selected.Cell == startCell && session.Match.Phase == BattlePhase.Planning && !session.View.Ready;
            }
            if (!pressed || blocked) { if (up) pressed = false; return; }
            if (held)
            {
                if (Vector2.Distance(point,pressPoint) > 8) dragging = true;
                if (dragging && unitDrag)
                {
                    if (onBoard && world.TryCell(cameraRig.Camera,point,out var cell)) Append(cell);
                }
                else if (dragging) session.Events.Pan(point - lastPoint);
                lastPoint = point;
            }
            if (up)
            {
                if (dragging && unitDrag && path.Count > 0) session.Events.Path(new List<GridCell>(path));
                else if (!dragging && onBoard && world.TryCell(cameraRig.Camera,point,out var cell))
                {
                    if (session.Match.Phase == BattlePhase.Planning && world.TryActor(cameraRig.Camera,point,out int id) && !session.Armed) session.Events.Select(id);
                    else session.Events.Tile(cell);
                }
                path.Clear(); session.Events.PreviewPath(path); pressed = false;
            }
        }
        private void Append(GridCell cell)
        {
            var unit = session.Selected; if (unit == null) return;
            var character = session.Rules.Characters[unit.Slot];
            int maximum = session.Action == BattleAction.Skill && character.SkillEffect == SkillEffect.StealthMove ? character.Skill.Range : character.Move.Range;
            var previous = path.Count == 0 ? startCell : path[path.Count - 1];
            if (cell == previous) return;
            if (cell == startCell) path.Clear();
            else if (path.Contains(cell)) path.RemoveRange(path.IndexOf(cell)+1,path.Count-path.IndexOf(cell)-1);
            else if (previous.Distance(cell) == 1 && path.Count < maximum) path.Add(cell);
            session.Events.PreviewPath(path);
        }
        private bool OverUI(Vector2 point)
        {
            if (EventSystem.current == null) return false;
            hits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point },hits);
            return hits.Any(h => h.gameObject.GetComponentInParent<Canvas>() != null);
        }
    }
}

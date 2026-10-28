using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace TurnZero
{
    public sealed class BattleInput : MonoBehaviour
    {
        private static readonly Key[] CharacterKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
        private static readonly Key[] ActionKeys = { Key.Q, Key.W, Key.E, Key.R, Key.T };
        private BattleSession session;
        private LocalBattleWorld world;
        private LocalBattleCamera cameraRig;
        private bool pressed;
        private bool dragging;
        private bool unitDrag;
        private bool blocked;
        private bool touching;
        private Vector2 pressPoint;
        private Vector2 lastPoint;
        private GridCell startCell;
        private readonly List<GridCell> path = new List<GridCell>();
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private int lastKey = -1;
        private double lastKeyTime;
        private float lastPinch;
        private EventSystem pointerEventSystem;
        private PointerEventData pointerData;

        public void Bind(BattleSession value, LocalBattleWorld board, LocalBattleCamera camera)
        {
            session = value;
            world = board;
            cameraRig = camera;
        }

        private void Update()
        {
            if (session == null || !Application.isFocused && !Application.isBatchMode)
            {
                pressed = false;

                return;
            }

            Keys();
            Touchscreen touch = Touchscreen.current;

            if (TryGetPinchPoints(touch, out Vector2 first, out Vector2 second))
            {
                float distance = Vector2.Distance(first, second);

                if (lastPinch > 0 && !OverUI(first) && !OverUI(second))
                {
                    session.Events.Zoom((distance - lastPinch) / 80);
                }

                lastPinch = distance;
                pressed = false;
                path.Clear();
                session.Events.PreviewPath(path);

                return;
            }

            lastPinch = 0;

            if (touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame))
            {
                touching = true;
                UnityEngine.InputSystem.Controls.TouchControl primary = touch.primaryTouch;

                Pointer(
                    primary.position.ReadValue(),
                    primary.press.wasPressedThisFrame,
                    primary.press.isPressed,
                    primary.press.wasReleasedThisFrame,
                    false);

                return;
            }

            if (touching)
            {
                touching = false;
                pressed = false;
            }

            Mouse mouse = Mouse.current;

            if (mouse == null)
            {
                return;
            }

            Vector2 point = mouse.position.ReadValue();

            Pointer(
                point,
                mouse.leftButton.wasPressedThisFrame,
                mouse.leftButton.isPressed,
                mouse.leftButton.wasReleasedThisFrame,
                mouse.rightButton.wasPressedThisFrame);

            float wheel = mouse.scroll.ReadValue().y;

            if (Mathf.Abs(wheel) > 0.01f && !mouse.leftButton.isPressed && cameraRig.OnBoard(point) && !OverUI(point))
            {
                session.Events.Zoom(Mathf.Clamp(wheel / 120, -4, 4));
            }
        }

        private static bool TryGetPinchPoints(Touchscreen touch, out Vector2 first, out Vector2 second)
        {
            first = default;
            second = default;

            if (touch == null)
            {
                return false;
            }

            int count = 0;

            for (int i = 0; i < touch.touches.Count; i++)
            {
                UnityEngine.InputSystem.Controls.TouchControl point = touch.touches[i];

                if (!point.press.isPressed)
                {
                    continue;
                }

                if (count == 0)
                {
                    first = point.position.ReadValue();
                    count++;
                }
                else
                {
                    second = point.position.ReadValue();

                    return true;
                }
            }

            return false;
        }

        private void Keys()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (session.ShowHelp)
            {
                if (keyboard.f1Key.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                {
                    session.Events.Help();
                }

                return;
            }

            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
            {
                return;
            }

            for (int i = 0; i < CharacterKeys.Length; i++)
            {
                if (keyboard[CharacterKeys[i]].wasPressedThisFrame)
                {
                    bool twice = lastKey == i && Time.unscaledTimeAsDouble - lastKeyTime <= 0.32;

                    session.Events.Select(session.Player * GameDefinitions.CharactersPerTeam + i + 1, twice);
                    lastKey = i;
                    lastKeyTime = Time.unscaledTimeAsDouble;
                }
            }

            for (int i = 0; i < ActionKeys.Length; i++)
            {
                if (keyboard[ActionKeys[i]].wasPressedThisFrame)
                {
                    session.Events.SetAction((BattleAction)i);
                }
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                session.Events.Switch();
            }

            if (keyboard.enterKey.wasPressedThisFrame)
            {
                session.Events.Ready();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                path.Clear();
                session.Events.PreviewPath(path);
                session.Events.SetAction(BattleAction.Wait);
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                session.Events.Help();
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                session.Events.Resolve();
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                session.Events.Pause();
            }
        }

        private void Pointer(Vector2 point, bool down, bool held, bool up, bool right)
        {
            if (session.ShowHelp)
            {
                return;
            }

            bool onBoard = cameraRig.OnBoard(point) && !OverUI(point);

            if (onBoard && world.TryCell(cameraRig.Camera, point, out GridCell hover))
            {
                session.Events.Hover(hover);
            }

            if (right && onBoard && world.TryCell(cameraRig.Camera, point, out GridCell quick))
            {
                session.Events.Tile(quick, true);

                return;
            }

            if (down)
            {
                pressed = true;
                blocked = !onBoard || !session.CanUseCamera;
                dragging = false;
                pressPoint = lastPoint = point;
                path.Clear();

                bool found = world.TryCell(cameraRig.Camera, point, out startCell);
                BattleUnit selected = session.Selected;

                unitDrag = found &&
                    selected != null &&
                    selected.Owner == session.Player &&
                    selected.Alive &&
                    selected.Cell == startCell &&
                    session.Match.Phase == BattlePhase.Planning &&
                    !session.View.Ready;
            }

            if (!pressed || blocked)
            {
                if (up)
                {
                    pressed = false;
                }

                return;
            }

            if (held)
            {
                if (Vector2.Distance(point, pressPoint) > 8)
                {
                    dragging = true;
                }

                if (dragging && unitDrag)
                {
                    if (onBoard && world.TryCell(cameraRig.Camera, point, out GridCell cell))
                    {
                        Append(cell);
                    }
                }
                else if (dragging)
                {
                    session.Events.Pan(point - lastPoint);
                }

                lastPoint = point;
            }

            if (up)
            {
                if (dragging && unitDrag && path.Count > 0)
                {
                    session.Events.Path(new List<GridCell>(path));
                }
                else if (!dragging && onBoard && world.TryCell(cameraRig.Camera, point, out GridCell cell))
                {
                    if (session.Match.Phase == BattlePhase.Planning &&
                        world.TryActor(cameraRig.Camera, point, out int id) &&
                        !session.Armed)
                    {
                        session.Events.Select(id);
                    }
                    else
                    {
                        session.Events.Tile(cell);
                    }
                }

                path.Clear();
                session.Events.PreviewPath(path);
                pressed = false;
            }
        }

        private void Append(GridCell cell)
        {
            BattleUnit unit = session.Selected;

            if (unit == null)
            {
                return;
            }

            CharacterSpec character = session.Rules.Characters[unit.Slot];
            int maximum = session.Action == BattleAction.Skill && character.SkillEffect == SkillEffect.StealthMove
                ? character.Skill.Range
                : character.Move.Range;
            GridCell previous = path.Count == 0 ? startCell : path[path.Count - 1];

            if (cell == previous)
            {
                return;
            }

            if (cell == startCell)
            {
                path.Clear();
            }
            else if (path.Contains(cell))
            {
                path.RemoveRange(path.IndexOf(cell) + 1, path.Count - path.IndexOf(cell) - 1);
            }
            else if (previous.Distance(cell) == 1 && path.Count < maximum)
            {
                path.Add(cell);
            }

            session.Events.PreviewPath(path);
        }

        private bool OverUI(Vector2 point)
        {
            EventSystem current = EventSystem.current;

            if (current == null)
            {
                return false;
            }

            if (pointerEventSystem != current || pointerData == null)
            {
                pointerEventSystem = current;
                pointerData = new PointerEventData(current);
            }

            hits.Clear();
            pointerData.Reset();
            pointerData.position = point;
            current.RaycastAll(pointerData, hits);

            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject.GetComponentInParent<Canvas>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

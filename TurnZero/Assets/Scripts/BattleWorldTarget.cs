using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TurnZero
{
    // The Input System UI module delivers mouse and touch clicks through PhysicsRaycaster.
    public sealed class BattleWorldTarget : MonoBehaviour, IPointerClickHandler
    {
        public Cell Cell { get; set; }
        public Action<Cell> Click { get; set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Click?.Invoke(Cell);
        }
    }
}

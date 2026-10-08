using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TurnZero
{
    // The Input System UI module delivers mouse and touch clicks through PhysicsRaycaster.
    public sealed class BattleWorldTarget : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Cell cell;
        public Cell Cell { get => cell; set => cell = value; }
        public Action<Cell> Click { get; set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Click?.Invoke(Cell);
        }
    }
}

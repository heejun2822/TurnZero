using UnityEngine;
using UnityEngine.UI;

namespace TurnZero
{
    public sealed class LocalEquipmentConfirmationView : MonoBehaviour
    {
        [SerializeField]
        private Button confirmButton;
        [SerializeField]
        private Button cancelConfirmButton;

        private BattleSession session;
        private int equipment;

        public void Bind(BattleSession value)
        {
            Unbind();
            session = value;
            confirmButton.onClick.AddListener(Confirm);
            cancelConfirmButton.onClick.AddListener(Cancel);
        }

        public void Show(int index)
        {
            equipment = index;
            gameObject.SetActive(true);
        }

        public void Render(BattleView view)
        {
            if (view.Phase == BattlePhase.Setup)
            {
                Cancel();
            }
        }

        private void Confirm()
        {
            gameObject.SetActive(false);
            session.Events.Craft(equipment, true);
        }

        private void Cancel()
        {
            gameObject.SetActive(false);
        }

        private void Unbind()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
            }

            if (cancelConfirmButton != null)
            {
                cancelConfirmButton.onClick.RemoveListener(Cancel);
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}

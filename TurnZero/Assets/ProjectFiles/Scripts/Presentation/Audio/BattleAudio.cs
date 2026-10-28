using UnityEngine;

namespace TurnZero
{
    public sealed class BattleAudio : MonoBehaviour
    {
        [SerializeField]
        private AudioSource source;
        [SerializeField]
        private AudioClip turnStart;
        private BattleSession session;
        private int lastTurn;

        public void Bind(BattleSession value)
        {
            session = value;
            session.Events.Changed += Refresh;
        }

        private void Refresh()
        {
            if (session.Match.Phase == BattlePhase.Setup)
            {
                lastTurn = 0;
            }

            if (session.Match.Phase == BattlePhase.TurnStart && session.Match.Turn != lastTurn)
            {
                lastTurn = session.Match.Turn;

                if (turnStart != null)
                {
                    source.PlayOneShot(turnStart, 0.35f);
                }
            }
        }

        private void OnDestroy()
        {
            if (session != null)
            {
                session.Events.Changed -= Refresh;
            }
        }
    }
}

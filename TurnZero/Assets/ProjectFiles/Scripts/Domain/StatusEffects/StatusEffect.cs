using System;

namespace TurnZero
{
    [Serializable]
    public sealed class StatusEffect
    {
        public StatusEffectKind Kind;
        public int SourceId;
        public int StartTurn;
        public int ExpiresTurn;

        public StatusEffect Copy()
        {
            return (StatusEffect)MemberwiseClone();
        }
    }
}

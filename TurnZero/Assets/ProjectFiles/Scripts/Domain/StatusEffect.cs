using System;
namespace TurnZero.Game
{
    [Serializable]
    public sealed class StatusEffect
    {
        public StatusEffectKind Kind;
        public int SourceId, StartTurn, ExpiresTurn;
        public StatusEffect Copy() => (StatusEffect)MemberwiseClone();
    }
}

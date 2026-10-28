namespace TurnZero
{
    public sealed class StatusEffectDefinition
    {
        public readonly StatusEffectKind Kind;
        public readonly string Id;
        public readonly int Delay, Duration;
        public StatusEffectDefinition(StatusEffectKind kind, string id, int delay, int duration)
        { Kind = kind; Id = id; Delay = delay; Duration = duration; }
    }
}

using System;

namespace TurnZero
{
    [Serializable]
    public sealed class ActionSpec
    {
        public string Name;
        public string Description;
        public int Range, Area, Damage, Cost, Priority, Cooldown;
        public ActionSpec Copy() => (ActionSpec)MemberwiseClone();
    }
}

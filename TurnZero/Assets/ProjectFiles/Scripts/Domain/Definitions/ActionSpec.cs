using System;

namespace TurnZero
{
    [Serializable]
    public sealed class ActionSpec
    {
        public string Name;
        public string Description;
        public int Range;
        public int Area;
        public int Damage;
        public int Cost;
        public int Priority;
        public int Cooldown;

        public ActionSpec Copy()
        {
            return (ActionSpec)MemberwiseClone();
        }
    }
}

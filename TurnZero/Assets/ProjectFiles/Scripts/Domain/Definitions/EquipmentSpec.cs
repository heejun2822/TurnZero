using System;

namespace TurnZero
{
    [Serializable]
    public sealed class EquipmentSpec
    {
        public string Id;
        public string Name;
        public EquipmentEffect Effect;
        public ActionSpec Action;
        public int Health;
        public int Duration;
        public int ExitRange;

        public EquipmentSpec Copy()
        {
            return new EquipmentSpec
            {
                Id = Id,
                Name = Name,
                Effect = Effect,
                Action = Action.Copy(),
                Health = Health,
                Duration = Duration,
                ExitRange = ExitRange
            };
        }
    }
}

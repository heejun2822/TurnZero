using System;

namespace TurnZero
{
    [Serializable]
    public sealed class EquipmentSpec
    {
        public string Id, Name;
        public EquipmentEffect Effect;
        public ActionSpec Action;
        public int Health, Duration, ExitRange;
        public EquipmentSpec Copy() => new EquipmentSpec { Id = Id, Name = Name, Effect = Effect, Action = Action.Copy(),
            Health = Health, Duration = Duration, ExitRange = ExitRange };
    }
}

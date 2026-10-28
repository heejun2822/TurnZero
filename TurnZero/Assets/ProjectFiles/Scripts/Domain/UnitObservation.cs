using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    [Serializable]
    public sealed class UnitObservation
    {
        public BattleUnit Unit;
        public int Turn;
        public bool HasPosition = true;
        public UnitObservation Copy() => new UnitObservation { Unit = Unit.Copy(), Turn = Turn, HasPosition = HasPosition };
    }
}

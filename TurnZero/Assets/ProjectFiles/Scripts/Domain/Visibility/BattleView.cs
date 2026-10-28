using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public sealed class BattleView
    {
        public int Player;
        public int Turn;
        public int PlacementPlayer;
        public int PlacementSlot;
        public int AP;
        public int ReservedAP;
        public int Materials;
        public BattlePhase Phase;
        public BattleWinner Winner;
        public bool Ready;
        public double SecondsLeft;
        public HashSet<GridCell> Visible;
        public HashSet<GridCell> Explored;
        public List<BattleUnit> Units;
        public List<UnitObservation> LastSeen;
        public List<UnitObservation> KnownEnemies;
        public BattleBase OwnBase;
        public BattleBase EnemyBase;
        public bool EnemyBaseVisible;
        public List<BattleOrder> Orders;
        public List<BattleStructure> Structures;
        public List<FireZone> Fires;
        public List<GroundItem> Items;

        public Sight Sight(GridCell cell)
        {
            return Visible.Contains(cell)
                ? global::TurnZero.Sight.Visible
                : Explored.Contains(cell) ? global::TurnZero.Sight.Explored : global::TurnZero.Sight.Unknown;
        }
    }
}

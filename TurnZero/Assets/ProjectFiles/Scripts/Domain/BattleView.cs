using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    public sealed class BattleView
    {
        public int Player, Turn, PlacementPlayer, PlacementSlot, AP, ReservedAP, Materials;
        public BattlePhase Phase;
        public BattleWinner Winner;
        public bool Ready;
        public double SecondsLeft;
        public HashSet<GridCell> Visible, Explored;
        public List<BattleUnit> Units;
        public List<UnitObservation> LastSeen;
        public List<UnitObservation> KnownEnemies;
        public BattleBase OwnBase, EnemyBase;
        public bool EnemyBaseVisible;
        public List<BattleOrder> Orders;
        public List<BattleStructure> Structures;
        public List<FireZone> Fires;
        public List<GroundItem> Items;
        public Sight Sight(GridCell cell) => Visible.Contains(cell) ? Game.Sight.Visible : Explored.Contains(cell) ? Game.Sight.Explored : Game.Sight.Unknown;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    [Serializable]
    public sealed class BattleUnit
    {
        public int Id, Owner, Slot, Health, DiedTurn;
        public bool Placed;
        public GridCell Cell;
        public int Equipment = -1;
        public int[] ReadyOn = new int[4];
        public int HiddenThrough, RootFrom, RootThrough;
        public bool Alive => Placed && Health > 0;
        public bool Rooted(int turn) => RootFrom > 0 && turn >= RootFrom && turn <= RootThrough;
        public BattleUnit Copy()
        {
            var copy = (BattleUnit)MemberwiseClone();
            copy.ReadyOn = (int[])ReadyOn.Clone();
            return copy;
        }
    }

    [Serializable]
    public sealed class BattleBase
    {
        public int Owner, Health;
        public bool Placed;
        public GridCell Cell;
        public BattleBase Copy() => (BattleBase)MemberwiseClone();
    }

    [Serializable]
    public sealed class BattleOrder
    {
        public int UnitId;
        public BattleAction Action;
        public GridCell Target, Exit;
        public int TargetUnit;
        public List<GridCell> Path = new List<GridCell>();
        public BattleOrder Copy() => new BattleOrder { UnitId = UnitId, Action = Action, Target = Target, Exit = Exit,
            TargetUnit = TargetUnit, Path = new List<GridCell>(Path) };
    }

    [Serializable]
    public sealed class BattleStructure
    {
        public int Id, Owner, Link, Health, ReadyTurn, Vision;
        public StructureKind Kind;
        public GridCell Cell, Destination;
        public BattleStructure Copy() => (BattleStructure)MemberwiseClone();
    }

    [Serializable]
    public sealed class FireZone
    {
        public int Id, Owner, Damage, Area, ThroughTurn;
        public GridCell Cell;
        public FireZone Copy() => (FireZone)MemberwiseClone();
    }

    [Serializable]
    public sealed class GroundItem
    {
        public int Id;
        public GridCell Cell;
        public bool[] Collected = new bool[2];
        public GroundItem Copy() => new GroundItem { Id = Id, Cell = Cell, Collected = (bool[])Collected.Clone() };
    }

    [Serializable]
    public sealed class UnitObservation
    {
        public BattleUnit Unit;
        public int Turn;
        public UnitObservation Copy() => new UnitObservation { Unit = Unit.Copy(), Turn = Turn };
    }

    [Serializable]
    public sealed class MatchState
    {
        public int Turn, PlacementPlayer, PlacementSlot, NextId = 201;
        public BattlePhase Phase = BattlePhase.Setup;
        public BattleWinner Winner;
        public double Deadline;
        public int[] AP = new int[2], Materials = new int[2];
        public bool[] Ready = new bool[2];
        public List<BattleUnit> Units = new List<BattleUnit>();
        public BattleBase[] Bases = { new BattleBase { Owner = 0 }, new BattleBase { Owner = 1 } };
        public List<BattleStructure> Structures = new List<BattleStructure>();
        public List<FireZone> Fires = new List<FireZone>();
        public List<GroundItem> Items = new List<GroundItem>();
        public List<BattleOrder> Orders = new List<BattleOrder>();
        public HashSet<GridCell>[] Explored = { new HashSet<GridCell>(), new HashSet<GridCell>() };
        public HashSet<GridCell>[] Revealed = { new HashSet<GridCell>(), new HashSet<GridCell>() };
        public Dictionary<int, UnitObservation>[] Memory = { new Dictionary<int, UnitObservation>(), new Dictionary<int, UnitObservation>() };
        public BattleBase[] KnownBases = new BattleBase[2];
        public HashSet<string> FireHits = new HashSet<string>();
        public MatchState Copy()
        {
            var copy = (MatchState)MemberwiseClone();
            copy.AP = (int[])AP.Clone(); copy.Materials = (int[])Materials.Clone(); copy.Ready = (bool[])Ready.Clone();
            copy.Units = Units.Select(u => u.Copy()).ToList(); copy.Bases = Array.ConvertAll(Bases, b => b.Copy());
            copy.Structures = Structures.Select(s => s.Copy()).ToList(); copy.Fires = Fires.Select(f => f.Copy()).ToList();
            copy.Items = Items.Select(i => i.Copy()).ToList(); copy.Orders = Orders.Select(o => o.Copy()).ToList();
            copy.Explored = Array.ConvertAll(Explored, e => new HashSet<GridCell>(e));
            copy.Revealed = Array.ConvertAll(Revealed, e => new HashSet<GridCell>(e));
            copy.Memory = Array.ConvertAll(Memory, m => m.ToDictionary(p => p.Key, p => p.Value.Copy()));
            copy.KnownBases = Array.ConvertAll(KnownBases, b => b?.Copy()); copy.FireHits = new HashSet<string>(FireHits);
            return copy;
        }
    }

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

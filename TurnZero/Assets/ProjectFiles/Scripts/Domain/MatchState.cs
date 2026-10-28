using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnZero.Game
{
    [Serializable]
    public sealed class MatchState
    {
        public int Turn, PlacementPlayer, PlacementSlot, NextId = 201;
        public BattlePhase Phase = BattlePhase.Setup;
        public BattleWinner Winner;
        public double Deadline;
        public int[] AP = new int[2], Materials = new int[2], PendingKillAP = new int[2];
        public bool[] Ready = new bool[2];
        public int[][] EquipmentDecks = { new int[0], new int[0] };
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
        public Dictionary<int, GroundItem>[] ItemMemory = { new Dictionary<int, GroundItem>(), new Dictionary<int, GroundItem>() };
        public Dictionary<int, BattleStructure>[] StructureMemory = { new Dictionary<int, BattleStructure>(), new Dictionary<int, BattleStructure>() };
        public MatchState Copy()
        {
            var copy = (MatchState)MemberwiseClone();
            copy.EquipmentDecks = Array.ConvertAll(EquipmentDecks, d => (int[])d.Clone());
            copy.PendingKillAP = (int[])PendingKillAP.Clone(); copy.AP = (int[])AP.Clone(); copy.Materials = (int[])Materials.Clone(); copy.Ready = (bool[])Ready.Clone();
            copy.Units = Units.Select(u => u.Copy()).ToList(); copy.Bases = Array.ConvertAll(Bases, b => b.Copy());
            copy.Structures = Structures.Select(s => s.Copy()).ToList(); copy.Fires = Fires.Select(f => f.Copy()).ToList();
            copy.Items = Items.Select(i => i.Copy()).ToList(); copy.Orders = Orders.Select(o => o.Copy()).ToList();
            copy.Explored = Array.ConvertAll(Explored, e => new HashSet<GridCell>(e));
            copy.Revealed = Array.ConvertAll(Revealed, e => new HashSet<GridCell>(e));
            copy.Memory = Array.ConvertAll(Memory, m => m.ToDictionary(p => p.Key, p => p.Value.Copy()));
            copy.KnownBases = Array.ConvertAll(KnownBases, b => b?.Copy());
            copy.ItemMemory = Array.ConvertAll(ItemMemory, m => m.ToDictionary(p => p.Key,p => p.Value.Copy()));
            copy.StructureMemory = Array.ConvertAll(StructureMemory, m => m.ToDictionary(p => p.Key,p => p.Value.Copy()));
            return copy;
        }
    }
}

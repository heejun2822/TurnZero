using System.Collections.Generic;
using System.Linq;

namespace TurnZero
{
    public sealed class BattleCue
    {
        public CueKind Kind;
        public int ActorId, Owner, Area, Damage;
        public GridCell Source, Target;
        public string Label;
        public bool SourceVisible;
        public bool Persistent;
        public int CreatedTurn, EffectId, APDelta;
        public Dictionary<int,int> UnitDamage = new Dictionary<int,int>(), StructureDamage = new Dictionary<int,int>();
        public int[] BaseDamage = new int[GameDefinitions.PlayerCount];
        public Dictionary<int,GridCell> Positions = new Dictionary<int,GridCell>();
        public List<GridCell> VisibleCells;
        public BattleCue Copy()
        {
            var copy = (BattleCue)MemberwiseClone();
            copy.UnitDamage = new Dictionary<int,int>(UnitDamage); copy.StructureDamage = new Dictionary<int,int>(StructureDamage);
            copy.BaseDamage = (int[])BaseDamage.Clone(); copy.Positions = new Dictionary<int,GridCell>(Positions);
            copy.VisibleCells = VisibleCells == null ? null : new List<GridCell>(VisibleCells); return copy;
        }
    }
}

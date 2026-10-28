using System;

namespace TurnZero.Game
{
    [Serializable]
    public struct GridCell : IEquatable<GridCell>
    {
        public int X, Y;
        public GridCell(int x, int y) { X = x; Y = y; }
        public int Distance(GridCell other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        public bool Equals(GridCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCell cell && Equals(cell);
        public override int GetHashCode() => X * 397 ^ Y;
        public static bool operator ==(GridCell a, GridCell b) => a.Equals(b);
        public static bool operator !=(GridCell a, GridCell b) => !a.Equals(b);
        public override string ToString() => $"{X + 1},{Y + 1}";
    }
}

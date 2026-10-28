using System;

namespace TurnZero
{
    [Serializable]
    public struct GridCell : IEquatable<GridCell>
    {
        public int X;
        public int Y;

        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int Distance(GridCell other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public bool Equals(GridCell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCell cell && Equals(cell);
        }

        public override int GetHashCode()
        {
            return X * 397 ^ Y;
        }

        public static bool operator ==(GridCell a, GridCell b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(GridCell a, GridCell b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return $"{X + 1},{Y + 1}";
        }
    }
}

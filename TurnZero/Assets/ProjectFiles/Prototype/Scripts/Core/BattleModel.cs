using System;
using System.Collections.Generic;

namespace TurnZero
{
    [Serializable]
    public sealed class BattleRules
    {
        public int Width = 8;
        public int Height = 10;
        public int NeutralHeight;
        public int UnitHealth = 3;
        public int BaseHealth = 8;
        public int AttackDamage = 1;
        public int AttackRange = 2;
        public int VisionRange = 2;
        public int InitialAP = 0;
        public int TurnIncome = 10;
        public int MaximumAP = 25;
        public int MoveCost = 1;
        public int AttackCost = 2;
        public float PlanningSeconds = 30;

        public BattleRules Copy()
        {
            return (BattleRules)MemberwiseClone();
        }

        public void Validate()
        {
            if (Width < 4 ||
                Height < 6 ||
                NeutralHeight < 0 ||
                NeutralHeight > Height - 6 ||
                (Height - NeutralHeight) % 2 != 0 ||
                UnitHealth < 1 ||
                BaseHealth < 1 ||
                AttackDamage < 1 ||
                AttackRange < 1 ||
                VisionRange < 1 ||
                InitialAP < 0 ||
                TurnIncome < 0 ||
                MaximumAP < 1 ||
                InitialAP > MaximumAP ||
                MoveCost < 1 ||
                AttackCost < 1 ||
                MoveCost > MaximumAP ||
                AttackCost > MaximumAP ||
                float.IsNaN(PlanningSeconds) ||
                float.IsInfinity(PlanningSeconds) ||
                PlanningSeconds <= 0)
            {
                throw new ArgumentException("Invalid prototype rules. Use equal home fields of at least three rows and positive combat values.");
            }
        }

        public bool Contains(Cell cell)
        {
            return cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;
        }

        public bool CanPlaceBase(int player, Cell cell)
        {
            if (player < 0 || player > 1 || cell.X < 1 || cell.X >= Width - 1)
            {
                return false;
            }

            int homeHeight = (Height - NeutralHeight) / 2;
            int localY = cell.Y - player * (homeHeight + NeutralHeight);

            return localY >= 1 && localY < homeHeight - 1;
        }

        public int Cost(ActionKind action)
        {
            return action == ActionKind.Move ? MoveCost : action == ActionKind.Attack ? AttackCost : 0;
        }
    }

    [Serializable]
    public struct Cell : IEquatable<Cell>
    {
        public int X;
        public int Y;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int Distance(Cell other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public bool Equals(Cell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object other)
        {
            return other is Cell cell && Equals(cell);
        }

        public override int GetHashCode()
        {
            return X * 397 ^ Y;
        }

        public override string ToString()
        {
            return $"({X + 1},{Y + 1})";
        }

        public static bool operator ==(Cell left, Cell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Cell left, Cell right)
        {
            return !left.Equals(right);
        }
    }

    public enum MatchPhase
    {
        Placement,
        Planning,
        Finished,
        Resolving
    }
    public enum TileVisibility
    {
        Unexplored,
        Explored,
        Visible
    }
    public enum ActionKind
    {
        Wait,
        Move,
        Attack
    }
    public enum EntityKind
    {
        Unit,
        Base
    }
    public enum BattleOutcome
    {
        None,
        PlayerOne,
        PlayerTwo,
        Draw
    }

    [Serializable]
    public sealed class EntityState
    {
        public int Id;
        public int Owner;
        public EntityKind Kind;
        public Cell Position;
        public int Health;
        public int MaximumHealth;

        public EntityState Copy()
        {
            return (EntityState)MemberwiseClone();
        }

        public string Label => Kind == EntityKind.Base ? $"P{Owner + 1} Base" : $"P{Owner + 1} Unit {Id - Owner * 4}";
    }

    [Serializable]
    public sealed class ActionOrder
    {
        public int UnitId;
        public ActionKind Kind;
        public Cell Destination;
        public int TargetId;

        public ActionOrder Copy()
        {
            return (ActionOrder)MemberwiseClone();
        }

        public static ActionOrder Move(int id, Cell to)
        {
            return new ActionOrder
            {
                UnitId = id,
                Kind = ActionKind.Move,
                Destination = to
            };
        }

        public static ActionOrder Attack(int id, int target)
        {
            return new ActionOrder
            {
                UnitId = id,
                Kind = ActionKind.Attack,
                TargetId = target
            };
        }

        public static ActionOrder Wait(int id)
        {
            return new ActionOrder
            {
                UnitId = id,
                Kind = ActionKind.Wait
            };
        }
    }

    [Serializable]
    public sealed class BattleState
    {
        public int Turn;
        public MatchPhase Phase = MatchPhase.Placement;
        public BattleOutcome Outcome;
        public int[] AP = new int[2];
        public List<EntityState> Entities = new List<EntityState>();
        // Reconstructed from actions during replay; these cells belong only to their stamped turn.
        [NonSerialized]
        public List<VisionReveal> VisionReveals = new List<VisionReveal>();

        public void RevealCell(int player, Cell position)
        {
            if (player < 0 || player > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(player));
            }

            VisionReveals.Add(new VisionReveal
            {
                Player = player,
                Position = position,
                Turn = Turn
            });
        }

        public BattleState Copy()
        {
            BattleState copy = new BattleState
            {
                Turn = Turn,
                Phase = Phase,
                Outcome = Outcome,
                AP = (int[])AP.Clone()
            };

            foreach (EntityState entity in Entities)
            {
                copy.Entities.Add(entity.Copy());
            }

            copy.VisionReveals.AddRange(VisionReveals);

            return copy;
        }
    }

    public struct VisionReveal
    {
        public int Player;
        public Cell Position;
        public int Turn;
    }

    [Serializable]
    public sealed class Observation
    {
        public EntityState Entity;
        public int SeenOnTurn;

        public Observation Copy()
        {
            return new Observation
            {
                Entity = Entity.Copy(),
                SeenOnTurn = SeenOnTurn
            };
        }
    }

    // This is the UI boundary. Opponent AP and orders are deliberately absent.
    public sealed class PlayerView
    {
        public int Player;
        public int Turn;
        public MatchPhase Phase;
        public BattleOutcome Outcome;
        public int AP;
        public int ReservedAP;
        public bool Locked;
        public double SecondsLeft;
        public HashSet<Cell> VisibleCells = new HashSet<Cell>();
        public HashSet<Cell> ExploredCells = new HashSet<Cell>();
        public List<EntityState> Entities = new List<EntityState>();
        public List<Observation> LastSeen = new List<Observation>();
        public List<ActionOrder> Orders = new List<ActionOrder>();
        public List<string> Messages = new List<string>();

        public TileVisibility Visibility(Cell cell)
        {
            return VisibleCells.Contains(cell)
                ? TileVisibility.Visible
                : ExploredCells.Contains(cell) ? TileVisibility.Explored : TileVisibility.Unexplored;
        }
    }

    public sealed class TurnResult
    {
        public BattleState State;
        public HashSet<Cell>[] SeenCells =
        {
            new HashSet<Cell>(),
            new HashSet<Cell>()
        };
        public List<Observation>[] Observations =
        {
            new List<Observation>(),
            new List<Observation>()
        };
        public List<string>[] Messages =
        {
            new List<string>(),
            new List<string>()
        };
    }

    [Serializable]
    public sealed class TurnRecord
    {
        public BattleState Before;
        public List<ActionOrder> PlayerOneOrders;
        public List<ActionOrder> PlayerTwoOrders;
        public BattleState After;
    }

    [Serializable]
    public sealed class BattleRecording
    {
        public int FormatVersion = 1;
        public BattleRules Rules;
        public List<Cell> BasePlacements = new List<Cell>();
        public List<TurnRecord> Turns = new List<TurnRecord>();
    }
}

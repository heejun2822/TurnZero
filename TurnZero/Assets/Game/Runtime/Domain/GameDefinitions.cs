using System;

namespace TurnZero.Game
{
    public enum BattlePhase { Setup, BasePlacement, UnitPlacement, TurnStart, Planning, Resolving, Finished }
    public enum BattleAction { Move, Attack, Skill, Equipment, Wait, Revive }
    public enum SkillEffect { Damage, StealthMove, BurningGround, RootDamage, Precision }
    public enum EquipmentEffect { Portal, Swap, Beacon }
    public enum StructureKind { PortalEntrance, PortalExit, Beacon }
    public enum BattleWinner { None, PlayerOne, PlayerTwo, Draw }
    public enum Sight { Unknown, Explored, Visible }

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

    [Serializable]
    public sealed class ActionSpec
    {
        public string Name;
        public string Description;
        public int Range, Area, Damage, Cost, Priority, Cooldown;
        public ActionSpec Copy() => (ActionSpec)MemberwiseClone();
    }

    [Serializable]
    public sealed class CharacterSpec
    {
        public string Id, Name;
        public int Health;
        public SkillEffect SkillEffect;
        public ActionSpec Move, Attack, Skill;
        public CharacterSpec Copy() => new CharacterSpec { Id = Id, Name = Name, Health = Health, SkillEffect = SkillEffect,
            Move = Move.Copy(), Attack = Attack.Copy(), Skill = Skill.Copy() };
    }

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

    [Serializable]
    public sealed class GameRules
    {
        public int Width = 12, HomeHeight = 8, NeutralHeight = 4;
        public int BaseHealth = 30, BaseVision = 3, UnitVision = 2;
        public int TurnIncome = 10, MaximumAP = 25, KillReward = 4;
        public int ReviveCost = 15, MinimumReviveCost = 10, InitialMaterials = 2;
        public float TurnStartSeconds = 2, PlanningSeconds = 30;
        public int Seed = 2471;
        public CharacterSpec[] Characters;
        public EquipmentSpec[] Equipment;
        public int Height => HomeHeight * 2 + NeutralHeight;
        public bool Contains(GridCell cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        public bool Home(int player, GridCell cell) => Contains(cell) && (player == 0 ? cell.Y < HomeHeight : cell.Y >= HomeHeight + NeutralHeight);
        public int BaseId(int player) => 101 + player;
        public GameRules Copy()
        {
            var copy = (GameRules)MemberwiseClone();
            copy.Characters = Array.ConvertAll(Characters, c => c.Copy());
            copy.Equipment = Array.ConvertAll(Equipment, e => e.Copy());
            return copy;
        }
        public void Validate()
        {
            if (Width < 4 || HomeHeight < 4 || NeutralHeight < 0 || BaseHealth < 1 || BaseVision < 1 || UnitVision < 1 ||
                MaximumAP < 1 || TurnIncome < 0 || KillReward < 0 || InitialMaterials < 0 || ReviveCost < MinimumReviveCost || MinimumReviveCost < 0 ||
                !Finite(TurnStartSeconds) || TurnStartSeconds < 0 || !Finite(PlanningSeconds) || PlanningSeconds <= 0 ||
                Characters == null || Characters.Length != 5 || Equipment == null || Equipment.Length < 1)
                throw new ArgumentException("Invalid local battle configuration.");
            foreach (var character in Characters)
            {
                if (character == null || string.IsNullOrWhiteSpace(character.Id) || character.Health < 1) throw new ArgumentException("Invalid character.");
                Check(character.Move); Check(character.Attack); Check(character.Skill);
                if (character.Move.Range < 1) throw new ArgumentException("Movement range must be positive.");
            }
            foreach (var equipment in Equipment)
            {
                if (equipment == null || string.IsNullOrWhiteSpace(equipment.Id) || equipment.Duration < 0 || equipment.Health < 0 || equipment.ExitRange < 0)
                    throw new ArgumentException("Invalid equipment.");
                Check(equipment.Action);
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void Check(ActionSpec action)
        {
            if (action == null || action.Range < 0 || action.Area < 0 || action.Damage < 0 || action.Cost < 0 ||
                action.Cost > MaximumAP || action.Priority < 1 || action.Priority > 4 || action.Cooldown < 0)
                throw new ArgumentException("Invalid action definition.");
        }
    }
}

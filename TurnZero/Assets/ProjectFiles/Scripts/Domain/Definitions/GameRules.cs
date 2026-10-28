using System;
using System.Linq;

namespace TurnZero
{
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
        public int BaseId(int player) => GameDefinitions.FirstBaseId + player;
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
                Characters == null || Characters.Length != GameDefinitions.CharactersPerTeam || Equipment == null || Equipment.Length < 1)
                throw new ArgumentException("Invalid local battle configuration.");
            if (Characters.Any(c => c == null) || Characters.Select(c => c.Id).Distinct().Count() != GameDefinitions.CharactersPerTeam) throw new ArgumentException("Five distinct character types are required.");
            if (Equipment.Any(e => e == null) || Equipment.Select(e => e.Id).Distinct().Count() != Equipment.Length) throw new ArgumentException("Equipment types must be distinct.");
            foreach (var character in Characters)
            {
                if (character == null || string.IsNullOrWhiteSpace(character.Id) || character.Health < 1 || character.SkillEffect == SkillEffect.BurningGround && character.SkillDuration < 1) throw new ArgumentException("Invalid character.");
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
                action.Cost > MaximumAP || action.Priority < GameDefinitions.FirstActionPhase || action.Priority > GameDefinitions.LastActionPhase || action.Cooldown < 0)
                throw new ArgumentException("Invalid action definition.");
        }
    }
}

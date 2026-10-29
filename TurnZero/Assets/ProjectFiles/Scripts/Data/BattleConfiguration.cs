using System;
using UnityEngine;

namespace TurnZero
{
    [CreateAssetMenu(menuName = "TurnZero/Battle configuration")]
    public sealed class BattleConfiguration : ScriptableObject
    {
        [Header("Content — order defines the five roster slots")]
        public CharacterDefinition[] Characters;
        public EquipmentDefinition[] Equipment;
        public FieldDefinition[] Fields;
        public GameObject BaseModel;
        [Header("Match")]
        public int BaseHealth = 30;
        [Header("Match")]
        public int BaseVision = 3;
        [Header("Match")]
        public int CharacterVision = 2;
        public int TurnIncome = 10;
        public int MaximumAP = 25;
        public int KillReward = 4;
        public int ReviveCost = 15;
        public int MinimumReviveCost = 10;
        public int InitialCraftMaterials = 2;
        public float TurnStartSeconds = 2;
        public float PlanningSeconds = 30;
        public int RandomSeed = 2471;
        public LocalBattleMode DefaultMode = LocalBattleMode.TwoPlayers;
        [Header("Presentation and camera")]
        public float MoveSeconds = 0.12f;
        [Header("Presentation and camera")]
        public float ActionSeconds = 0.45f;
        public float CameraPitch = 56;
        public float MinimumZoom = 0.35f;
        public float MaximumZoom = 1.1f;
        public float InitialZoom = 0.45f;
        public float ZoomSensitivity = 1.2f;
        [TextArea(6, 16)]
        public string LocalProfileNotes;

        public GameRules CreateRules()
        {
            if (Fields == null || Fields.Length == 0 || Fields[0] == null)
            {
                throw new InvalidOperationException("Assign a battlefield definition.");
            }

            FieldDefinition field = Fields[0];
            GameRules rules = new GameRules
            {
                Width = field.Width,
                HomeHeight = field.HomeHeight,
                NeutralHeight = field.NeutralHeight,
                BaseHealth = BaseHealth,
                BaseVision = BaseVision,
                UnitVision = CharacterVision,
                TurnIncome = TurnIncome,
                MaximumAP = MaximumAP,
                KillReward = KillReward,
                ReviveCost = ReviveCost,
                MinimumReviveCost = MinimumReviveCost,
                InitialMaterials = InitialCraftMaterials,
                TurnStartSeconds = TurnStartSeconds,
                PlanningSeconds = PlanningSeconds,
                Seed = RandomSeed,
                Characters = Array.ConvertAll(Characters ?? Array.Empty<CharacterDefinition>(), c => c == null ? null : c.Values?.Copy()),
                Equipment = Array.ConvertAll(Equipment ?? Array.Empty<EquipmentDefinition>(), e => e == null ? null : e.Values?.Copy())
            };

            rules.Validate();

            foreach (FieldDefinition preset in Fields)
            {
                if (preset == null ||
                    preset.Width != rules.Width ||
                    preset.HomeHeight != rules.HomeHeight ||
                    preset.NeutralHeight != rules.NeutralHeight ||
                    preset.TileSpacing <= 0)
                {
                    throw new InvalidOperationException("Field presets must share valid board dimensions and tile spacing.");
                }
            }

            if (MoveSeconds < 0 ||
                ActionSeconds < 0 ||
                MinimumZoom <= 0 ||
                MaximumZoom < MinimumZoom ||
                InitialZoom < MinimumZoom ||
                InitialZoom > MaximumZoom ||
                ZoomSensitivity <= 0 ||
                CameraPitch < 20 ||
                CameraPitch > 85)
            {
                throw new InvalidOperationException("Invalid battle presentation settings.");
            }

            return rules;
        }
    }
}

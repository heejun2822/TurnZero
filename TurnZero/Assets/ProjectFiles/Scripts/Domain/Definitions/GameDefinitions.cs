namespace TurnZero
{
    public static class GameDefinitions
    {
        // Fixed match structure; editable balance values remain in GameRules and data assets.
        public const int PlayerCount = 2;
        public const int CharactersPerTeam = 5;
        public const int EquipmentDeckSlots = 5;
        public const int CooldownActionCount = 4;
        public const int FirstBaseId = 101;
        public const int FirstDynamicEntityId = 201;

        public const int RevivePhase = 0;
        public const int FirstActionPhase = 1;
        public const int LastActionPhase = 4;
        public const int TurnWrapPhase = LastActionPhase + 1;
        public const int CraftMaterialSpawnPeriod = 5;
        public const string BotVersion = "local-bot-1";

        // Shared immutable status definitions.
        public static readonly StatusEffectDefinition Stealth = new StatusEffectDefinition(StatusEffectKind.Stealth, "SE-01", 0, 2);
        public static readonly StatusEffectDefinition Root = new StatusEffectDefinition(StatusEffectKind.Root, "SE-02", 1, 1);

        // Project asset identifiers are strings and have no Unity API dependency.
        public const string AssetRoot = "Assets/ProjectFiles/";
        public const string LocalBattleScenePath = AssetRoot + "Scenes/LocalBattle.unity";
        public const string BattleConfigurationPath = AssetRoot + "Data/LocalBattleConfiguration.asset";
        public const string FieldPalettePath = AssetRoot + "Prefabs/World/FieldPalette.prefab";
        public const string FieldBrushPath = AssetRoot + "Data/GameObjectBrush.asset";
    }

    // Explicit values preserve existing serialized enums and action indexes.
    public enum BattlePhase
    {
        Setup = 0,
        BasePlacement = 1,
        UnitPlacement = 2,
        TurnStart = 3,
        Planning = 4,
        Resolving = 5,
        Finished = 6
    }
    public enum LocalBattleMode
    {
        TwoPlayers = 0,
        HumanVsBot = 1
    }
    public enum BattleAction
    {
        Move = 0,
        Attack = 1,
        Skill = 2,
        Equipment = 3,
        Wait = 4,
        Revive = 5
    }
    public enum BattleWinner
    {
        None = 0,
        PlayerOne = 1,
        PlayerTwo = 2,
        Draw = 3
    }
    public enum SkillEffect
    {
        Damage = 0,
        StealthMove = 1,
        BurningGround = 2,
        RootDamage = 3,
        Precision = 4
    }
    public enum EquipmentEffect
    {
        Portal = 0,
        Swap = 1,
        Beacon = 2
    }
    public enum StructureKind
    {
        PortalEntrance = 0,
        PortalExit = 1,
        Beacon = 2
    }
    public enum Sight
    {
        Unknown = 0,
        Explored = 1,
        Visible = 2
    }
    public enum CueKind
    {
        Move = 0,
        Attack = 1,
        Skill = 2,
        Structure = 3,
        Revive = 4,
        Portal = 5,
        Swap = 6,
        Reward = 7
    }
    public enum StatusEffectKind
    {
        Stealth = 0,
        Root = 1
    }
}

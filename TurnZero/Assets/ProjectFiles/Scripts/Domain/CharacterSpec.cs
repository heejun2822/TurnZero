using System;

namespace TurnZero.Game
{
    [Serializable]
    public sealed class CharacterSpec
    {
        public string Id, Name;
        public int Health;
        public int SkillDuration = 3;
        public SkillEffect SkillEffect;
        public ActionSpec Move, Attack, Skill;
        public CharacterSpec Copy() => new CharacterSpec { Id = Id, Name = Name, Health = Health, SkillDuration = SkillDuration, SkillEffect = SkillEffect,
            Move = Move.Copy(), Attack = Attack.Copy(), Skill = Skill.Copy() };
    }
}

using System;

namespace TurnZero
{
    [Serializable]
    public sealed class CharacterSpec
    {
        public string Id;
        public string Name;
        public int Health;
        public int SkillDuration = 3;
        public SkillEffect SkillEffect;
        public ActionSpec Move;
        public ActionSpec Attack;
        public ActionSpec Skill;

        public CharacterSpec Copy()
        {
            return new CharacterSpec
            {
                Id = Id,
                Name = Name,
                Health = Health,
                SkillDuration = SkillDuration,
                SkillEffect = SkillEffect,
                Move = Move.Copy(),
                Attack = Attack.Copy(),
                Skill = Skill.Copy()
            };
        }
    }
}

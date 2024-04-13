using System;

namespace Dreamling.Interfaces
{
    public class CharacterTrait
    {
        public static CharacterTrait Timid = new CharacterTrait() { MatchedTraits = new[] { Timid, Aggressive, Playful } };

        public static CharacterTrait Aggressive = new CharacterTrait() { MatchedTraits = new[] { Timid, Aggressive, Curious } };

        public static CharacterTrait Playful = new CharacterTrait { MatchedTraits = new[] { Timid, Curious, Playful } };

        public static CharacterTrait Curious = new CharacterTrait { MatchedTraits = new[] { Playful, Timid, Curious } };

        public CharacterTrait[] MatchedTraits { get; set; } = Array.Empty<CharacterTrait>();
    }
}

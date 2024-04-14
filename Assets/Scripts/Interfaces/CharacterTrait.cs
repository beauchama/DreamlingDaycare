using System;

namespace Dreamlings.Interfaces
{
    public class CharacterTrait
    {
        public static CharacterTrait Timid = new() { MatchedTraits = new[] { Timid, Aggressive, Playful } };

        public static CharacterTrait Aggressive = new() { MatchedTraits = new[] { Timid, Aggressive, Curious } };

        public static CharacterTrait Playful = new() { MatchedTraits = new[] { Timid, Curious, Playful } };

        public static CharacterTrait Curious = new() { MatchedTraits = new[] { Playful, Timid, Curious } };

        public CharacterTrait[] MatchedTraits { get; set; } = Array.Empty<CharacterTrait>();
    }
}

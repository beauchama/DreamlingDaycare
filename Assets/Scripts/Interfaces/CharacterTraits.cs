using System;

namespace Dreamling.Interfaces
{
    public class CharacterTraits
    {
        public static CharacterTraits Timid = new CharacterTraits() { MatchedTraits = new[] { Timid, Aggressive, Playful } };

        public static CharacterTraits Aggressive = new CharacterTraits() { MatchedTraits = new[] { Timid, Aggressive, Curious } };

        public static CharacterTraits Playful = new CharacterTraits { MatchedTraits = new[] { Timid, Curious, Playful } };

        public static CharacterTraits Curious = new CharacterTraits { MatchedTraits = new[] { Playful, Timid, Curious } };

        public CharacterTraits[] MatchedTraits { get; set; } = Array.Empty<CharacterTraits>();
    }
}

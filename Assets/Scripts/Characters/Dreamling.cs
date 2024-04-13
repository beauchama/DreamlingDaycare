using System;
using System.Linq;

namespace Dreamling.Characters
{
    public class Dreamling
    {
        public string Name { get; set; }

        public int Quality { get; set; }

        public bool HasIllness { get; set; }

        public bool IsInjured { get; set; }

        public NeededFood NeededFood { get; set; }

        public CharacterTraits CharacterTraits { get; set; }

        public bool CanBreed(NeededFood neededFood, CharacterTraits characterTraits)
        {
            return !(HasIllness || IsInjured)
            && NeededFood == neededFood
            && CharacterTraits.MatchedTraits.Any(m => m == characterTraits);
        }

        public Dreamling Breed(Dreamling dreamling)
        {
            Random random = new Random();

            Dreamling inheritedParent = random.Next(0, 1) == 1 ? this : dreamling;

            return new Dreamling
            {
                Name = inheritedParent.Name,
                NeededFood = (NeededFood)Enum.GetValues(typeof(NeededFood)).GetValue(random.Next(0, 4)),
                CharacterTraits = inheritedParent.CharacterTraits.MatchedTraits[new Random().Next(0, 2)],
                Quality = CalculateQuality(dreamling.Quality),
            };
        }

        private int CalculateQuality(int quality)
        {
            Random random = new Random();

            if (Quality > quality || Quality < quality)
            {
                return random.Next(0, 10) <= 7 ? Quality : quality;
            }

            return random.Next(0, 1) == 1 ? Quality + 1 : Quality;
        }
    }

    public enum NeededFood
    {
        Plant,
        Meat,
        Fish,
        Flower,
        Legume
    }

    public class CharacterTraits
    {
        public static CharacterTraits Timid = new CharacterTraits() { MatchedTraits = new[] { Timid, Aggressive, Playful } };

        public static CharacterTraits Aggressive = new CharacterTraits() { MatchedTraits = new[] { Timid, Aggressive, Curious } };

        public static CharacterTraits Playful = new CharacterTraits { MatchedTraits = new[] { Timid, Curious, Playful } };

        public static CharacterTraits Curious = new CharacterTraits { MatchedTraits = new[] { Playful, Timid, Curious } };

        public CharacterTraits[] MatchedTraits { get; set; } = Array.Empty<CharacterTraits>();
    }
}

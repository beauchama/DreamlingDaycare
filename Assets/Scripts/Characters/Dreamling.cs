using Dreamlings.Interfaces;
using Dreamlings.Tools;
using UnityEngine;

namespace Dreamlings.Characters
{
    public class Dreamling
    {
        public string Name { get; set; } = "Rooo!";

        public int Quality { get; set; } = 1;

        public bool HasIllness { get; set; }

        public bool IsInjured { get; set; }

        public NeededFood NeededFood { get; set; }

        public bool CanBreed()
        {
            return !(HasIllness || IsInjured)
            && GameManager.Instance.Inventory.HasFood(NeededFood);
        }

        public Dreamling Breed(Dreamling dreamling)
        {
            GameManager.Instance.Inventory.RemoveFood(NeededFood);

            Dreamling inheritedParent = Random.Range(0, 1) == 1 ? this : dreamling;

            return new Dreamling
            {
                Name = inheritedParent.Name,
                NeededFood = NeededFoodGenerator.Generate(),
                Quality = CalculateQuality(dreamling.Quality),
            };
        }

        private int CalculateQuality(int quality)
        {
            if (Quality > quality || Quality < quality)
            {
                return Random.Range(0, 11) <= 7 ? Quality : quality;
            }

            return Random.Range(0, 2) == 1 ? Quality + 1 : Quality;
        }
    }
}

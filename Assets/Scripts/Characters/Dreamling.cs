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

        public DreamlingType DreamlingType { get; set; }

        public int Score => Quality * 100;

        public bool CanBreed()
        {
            return !(HasIllness || IsInjured)
            && GameManager.Instance.Inventory.HasFood(NeededFood);
        }

        public Dreamling Breed()
        {
            GameManager.Instance.Inventory.RemoveFood(NeededFood);

            return new Dreamling
            {
                Name = DreamlingNameGenerator.Generate(),
                NeededFood = NeededFoodGenerator.Generate(),
                Quality = Random.Range(1, 6),
            };
        }
    }
}

using System.Linq;
using Dreamlings.Interfaces;
using TMPro;
using UnityEngine;

namespace Dreamlings.Explorations
{
    public class Inventory
    {
        private int Plants { get; set; }

        private int Meats { get; set; }

        private int Veggies { get; set; }

        private int Fishes { get; set; }

        private int Flowers { get; set; }

        public bool HasFood(NeededFood neededFood)
        {
            return neededFood switch
            {
                NeededFood.Plant => Plants > 0,
                NeededFood.Meat => Meats > 0,
                NeededFood.Veggies => Veggies > 0,
                NeededFood.Fish => Fishes > 0,
                NeededFood.Flower => Flowers > 0,
                _ => false,
            };
        }

        public void RemoveFood(NeededFood neededFood)
        {
            switch (neededFood)
            {
                case NeededFood.Plant:
                    Plants--;
                    break;
                case NeededFood.Meat:
                    Meats--;
                    break;
                case NeededFood.Veggies:
                    Veggies--;
                    break;
                case NeededFood.Fish:
                    Fishes--;
                    break;
                case NeededFood.Flower:
                    Flowers--;
                    break;
            }

            UpdateUI();
        }

        public void AddFood(NeededFood neededFood, bool ignoreUI = false)
        {
            switch (neededFood)
            {
                case NeededFood.Plant:
                    Plants++;
                    break;
                case NeededFood.Meat:
                    Meats++;
                    break;
                case NeededFood.Veggies:
                    Veggies++;
                    break;
                case NeededFood.Fish:
                    Fishes++;
                    break;
                case NeededFood.Flower:
                    Flowers++;
                    break;
            }

            if (!ignoreUI)
            {
                UpdateUI();
            }
        }

        public void RemoveInventory(Inventory inventory)
        {
            Plants -= inventory.Plants;
            Meats -= inventory.Meats;
            Veggies -= inventory.Veggies;
            Fishes -= inventory.Fishes;
            Flowers -= inventory.Flowers;

            UpdateUI();
        }

        private void UpdateUI()
        {
            var uiObject = GameObject.FindGameObjectsWithTag("InventoryUI").FirstOrDefault();
            if (uiObject is null)
            {
                return;
            }

            var textBoxes = uiObject.GetComponentsInChildren<TextMeshProUGUI>();
            textBoxes.Single(x => x.name == "QtyPlants").text = Plants.ToString();
            textBoxes.Single(x => x.name == "QtyMeat").text = Meats.ToString();
            textBoxes.Single(x => x.name == "QtyVeggies").text = Veggies.ToString();
            textBoxes.Single(x => x.name == "QtyFish").text = Fishes.ToString();
            textBoxes.Single(x => x.name == "QtyFlowers").text = Flowers.ToString();
        }
    }
}

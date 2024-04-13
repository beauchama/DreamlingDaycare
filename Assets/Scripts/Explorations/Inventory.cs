using Dreamlings.Interfaces;

namespace Dreamlings.Explorations
{
    public class Inventory
    {
        private int Plants { get; set; } = 5;

        private int Meats { get; set; } = 5;

        private int Legumes { get; set; } = 5;

        private int Fishes { get; set; } = 5;

        private int Flowers { get; set; } = 5;

        public bool HasFood(NeededFood neededFood)
        {
            return neededFood switch
            {
                NeededFood.Plant => Plants > 0,
                NeededFood.Meat => Meats > 0,
                NeededFood.Legume => Legumes > 0,
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
                case NeededFood.Legume:
                    Legumes--;
                    break;
                case NeededFood.Fish:
                    Fishes--;
                    break;
                case NeededFood.Flower:
                    Flowers--;
                    break;
            }
        }

        public void AddFood(NeededFood neededFood)
        {
            switch (neededFood)
            {
                case NeededFood.Plant:
                    Plants++;
                    break;
                case NeededFood.Meat:
                    Meats++;
                    break;
                case NeededFood.Legume:
                    Legumes++;
                    break;
                case NeededFood.Fish:
                    Fishes++;
                    break;
                case NeededFood.Flower:
                    Flowers++;
                    break;
            }
        }
    }
}

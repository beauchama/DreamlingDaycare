using Dreamlings.Interfaces;

namespace Dreamlings.Explorations
{
    public class Inventory
    {
        private int Plants { get; set; }

        private int Meats { get; set; }

        private int Legumes { get; set; }

        private int Fishes { get; set; }

        private int Flowers { get; set; }

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

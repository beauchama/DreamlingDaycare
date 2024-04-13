using Dreamling.Interfaces;

namespace Dreamling.Explorations
{
    public class Inventory
    {
        public int Plants { get; set; }

        public int Meats { get; set; }

        public int Legumes { get; set; }

        public int Fishes { get; set; }

        public int Flowers { get; set; }

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
    }
}

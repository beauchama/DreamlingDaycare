using Dreamlings.Interfaces;
using UnityEngine;

namespace Dreamlings.Tools
{
    public static class NeededFoodGenerator
    {
        private readonly static NeededFood[] foods;

        static NeededFoodGenerator()
        {
            foods = (NeededFood[])System.Enum.GetValues(typeof(NeededFood));
        }

        public static NeededFood Generate()
        {
            return foods[Random.Range(0, foods.Length)];
        }
    }
}
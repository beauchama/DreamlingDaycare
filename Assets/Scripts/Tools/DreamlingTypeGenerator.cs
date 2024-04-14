using Dreamlings.Interfaces;
using UnityEngine;

namespace Dreamlings.Tools
{
    public static class DreamlingTypeGenerator
    {
        private readonly static DreamlingType[] types;

        static DreamlingTypeGenerator()
        {
            types = (DreamlingType[])System.Enum.GetValues(typeof(DreamlingType));
        }

        public static DreamlingType Generate()
        {
            return types[Random.Range(0, types.Length)];
        }
    }
}
using UnityEngine;

namespace Dreamlings.Tools
{
    public static class DreamlingNameGenerator
    {
        private readonly static string[] syllables = { "ba", "be", "bi", "bo", "bu", "da", "de", "di", "do", "du", "ga", "ge", "gi", "go", "gu" };

        public static string Generate()
        {
            string randomName = "";
            int nameLength = Random.Range(2, 4);

            for (int i = 0; i < nameLength; i++)
            {
                randomName += syllables[Random.Range(0, syllables.Length)];
            }

            char[] name = randomName.ToCharArray();
            name[0] = char.ToUpper(name[0]);

            return new string(name);
        }
    }
}
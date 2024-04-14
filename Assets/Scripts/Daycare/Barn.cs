using System.Collections.Generic;
using System.Linq;
using Dreamlings.Characters;

namespace Daycare
{
    public class Barn
    {
        private const int MaxDreamlings = 1;
        public readonly List<Dreamling> Dreamlings = new(MaxDreamlings);

        public string AddDreamling(Dreamling dreamling)
        {
            if (Dreamlings.Count >= MaxDreamlings)
            {
                return "The barn is full! Choose a different barn or sell a Dreamling.";
            }

            if (!CheckCompatibility(dreamling))
            {
                return "The Dreamling is not compatible with the other Dreamlings in the barn.";
            }

            Dreamlings.Add(dreamling);
            return null;
        }

        public void RemoveDreamling(Dreamling dreamling)
        {
            Dreamlings.Remove(dreamling);
        }

        private bool CheckCompatibility(Dreamling dreamling)
        {
            if (!Dreamlings.Any())
            {
                return true;
            }

            // TODO: Check traits?
            // foreach (var d in Dreamlings)
            // {
            // }
            //
            // return false;

            return true;
        }
    }
}

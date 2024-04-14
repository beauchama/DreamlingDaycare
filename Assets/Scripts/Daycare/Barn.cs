using System.Collections.Generic;
using System.Linq;
using Dreamlings.Characters;

namespace Daycare
{
    public class Barn
    {
        private const int MaxDreamlings = 6;
        public readonly List<Dreamling> Dreamlings = new(MaxDreamlings);

        public bool AddDreamling(Dreamling dreamling)
        {
            if (Dreamlings.Count < MaxDreamlings && CheckCompatibility(dreamling))
            {
                Dreamlings.Add(dreamling);
                return true;
            }

            return false;
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

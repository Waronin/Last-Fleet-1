using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;

namespace LastFleet.FleetModules
{
    /// <summary>
    /// The modules a found upgrade module can become (designer: choose 1 of 3): different kinds, none
    /// of them already installed, each equally likely.
    /// </summary>
    public static class FleetModuleOffer
    {
        public const int Size = 3;

        public static void Roll(DeterministicRandom random, FleetModuleSlots slots, List<FleetModuleKind> offer)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }
            if (offer == null)
            {
                throw new ArgumentNullException(nameof(offer));
            }

            offer.Clear();
            List<FleetModuleKind> candidates = new List<FleetModuleKind>();
            foreach (FleetModuleKind kind in FleetModuleCatalog.All)
            {
                if (!slots.IsInstalled(kind))
                {
                    candidates.Add(kind);
                }
            }
            while (offer.Count < Size && candidates.Count > 0)
            {
                int index = random.Range(0, candidates.Count);
                offer.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
        }
    }
}

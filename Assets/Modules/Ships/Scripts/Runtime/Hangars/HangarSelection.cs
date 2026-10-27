using System.Collections.Generic;

namespace LastFleet.Ships
{
    /// <summary>
    /// Picks where a small ship lands: its home hangar if there is room, otherwise any hangar with
    /// free space (GDD: every small ship can land in any hangar with a free slot).
    /// </summary>
    public static class HangarSelection
    {
        public static Hangar FindFor(ShipAgent ship, IReadOnlyList<Hangar> hangars)
        {
            Hangar home = ship.HomeHangar;
            if (home != null && home.HasSpace)
            {
                return home;
            }

            foreach (Hangar hangar in hangars)
            {
                if (hangar != null && hangar.HasSpace)
                {
                    return hangar;
                }
            }
            return null;
        }
    }
}

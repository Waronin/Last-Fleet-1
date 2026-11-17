using System.Collections.Generic;

namespace LastFleet.Ships
{
    /// <summary>
    /// Which sides fight each other. Every pair not listed is neutral, and a side is never hostile to
    /// itself. The Pronin hunt every human ship, the fleet's as well as stranded ones; the fleet leaves
    /// stranded ships alone. New factions, e.g. for quests, add their pairs here.
    /// </summary>
    public static class FactionRelations
    {
        static readonly HashSet<(Allegiance, Allegiance)> HostilePairs = new HashSet<(Allegiance, Allegiance)>
        {
            (Allegiance.HumanFleet, Allegiance.Pronin),
            (Allegiance.Stranded, Allegiance.Pronin)
        };

        public static bool AreHostile(Allegiance first, Allegiance second)
        {
            return first != second && (HostilePairs.Contains((first, second)) || HostilePairs.Contains((second, first)));
        }
    }
}

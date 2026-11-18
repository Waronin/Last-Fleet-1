using System;

namespace LastFleet.AbandonedShips
{
    /// <summary>
    /// Who can hand over a crew member to a small ship without crew (GDD: a ship nobody can fly is lost unless
    /// someone climbs aboard). The donor keeps at least one person, so it never becomes a derelict itself; the
    /// ship it saves flies with that one person until its hangar fills the posts. Values are balancing starting points.
    /// </summary>
    public static class CrewTransferRules
    {
        /// <summary>A donor must have this many aboard: one to give, one to keep flying.</summary>
        public const int MinimumDonorCrew = 2;

        /// <summary>Distance between the two ships at which the crew member crosses over, in units.</summary>
        public const float TransferRangeUnits = 6f;

        /// <summary>Where the donor waits beside the derelict, in the derelict's frame.</summary>
        public const float SideOffsetUnits = 3f;

        public static bool CanDonate(int crewCount)
        {
            return crewCount >= MinimumDonorCrew;
        }

        public static bool IsInRange(float distanceUnits)
        {
            if (distanceUnits < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceUnits), "A distance cannot be negative.");
            }
            return distanceUnits <= TransferRangeUnits;
        }
    }
}

namespace LastFleet.Repairs
{
    /// <summary>Whether a ship can be put into a shipyard's queue, and why not.</summary>
    public enum RepairEligibility
    {
        Eligible,
        AlreadyQueued,
        Undamaged,

        /// <summary>Small ships are repaired in a hangar only (GDD).</summary>
        NotInHangar,

        /// <summary>The ship has no hull that can be repaired.</summary>
        NoHull
    }
}

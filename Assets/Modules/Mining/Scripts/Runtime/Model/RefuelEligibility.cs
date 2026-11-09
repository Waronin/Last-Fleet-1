namespace LastFleet.Mining
{
    /// <summary>Whether a ship can be put into a tanker's refueling queue, and why not.</summary>
    public enum RefuelEligibility
    {
        Eligible,
        AlreadyQueued,
        TankFull,

        /// <summary>Only capital ships are refueled by a tanker; small ships refuel in a hangar.</summary>
        NotCapital,

        /// <summary>No tanker of the fleet is in the ship's zone.</summary>
        NoTankerInZone
    }
}

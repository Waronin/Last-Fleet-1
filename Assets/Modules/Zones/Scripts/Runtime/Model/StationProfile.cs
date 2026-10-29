using System;

namespace LastFleet.Zones
{
    public enum StationAtmosphere
    {
        Breathable,
        Thin,

        /// <summary>Hostile to life (GDD): the crew can come to harm aboard.</summary>
        Toxic
    }

    public enum StationAttitude
    {
        /// <summary>Nobody aboard to have an attitude.</summary>
        None,
        Friendly,
        Wary,
        Hostile
    }

    public enum StationCondition
    {
        Intact,
        Damaged,
        Ruined
    }

    public enum StationRadiation
    {
        None,
        Elevated
    }

    /// <summary>
    /// The truth about a station, fixed when its zone is generated (GDD: scanning shows contents,
    /// crew, condition, radiation and atmosphere; hailing tells whether the crew is hostile, though
    /// the answer can be a lie). The player only learns it through scans, hails and docking.
    /// </summary>
    public sealed class StationProfile
    {
        public StationProfile(StationAtmosphere atmosphere, int occupants, StationAttitude attitude, StationCondition condition,
            StationRadiation radiation)
        {
            if (occupants < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(occupants), "Occupants must not be negative.");
            }
            if ((occupants == 0) != (attitude == StationAttitude.None))
            {
                throw new ArgumentException("Only an inhabited station has an attitude, and every inhabited one has one.", nameof(attitude));
            }

            Atmosphere = atmosphere;
            Occupants = occupants;
            Attitude = attitude;
            Condition = condition;
            Radiation = radiation;
        }

        public StationAtmosphere Atmosphere { get; }
        public int Occupants { get; }
        public bool IsInhabited => Occupants > 0;
        public StationAttitude Attitude { get; }
        public StationCondition Condition { get; }
        public StationRadiation Radiation { get; }
    }
}

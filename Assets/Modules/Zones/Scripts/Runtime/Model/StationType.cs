using System;

namespace LastFleet.Zones
{
    /// <summary>What a station was built for (GDD: 3–4 station types for the prototype).</summary>
    public enum StationType
    {
        /// <summary>Small post, about 3 km, often inhabited by a handful of people.</summary>
        Outpost,

        /// <summary>About 5 km; data and scientists.</summary>
        Research,

        /// <summary>About 7 km; resources and upgrade modules, often abandoned.</summary>
        Industrial,

        /// <summary>About 9 km ring; many people to rescue.</summary>
        Habitat
    }

    /// <summary>
    /// Fixed size and docking ports per station type. Each type has one model whose size is set in
    /// its prefab (stations are 3–6 times the size of capital ships); the generator only needs the
    /// footprint to keep other objects clear, and a test checks that every prefab fits it.
    /// </summary>
    public static class StationTypes
    {
        /// <summary>Radius on the zone plane that the station model, docking arms included, stays within.</summary>
        public static float FootprintRadiusUnits(StationType type)
        {
            return type switch
            {
                StationType.Outpost => 16f,
                StationType.Research => 26f,
                StationType.Industrial => 36f,
                StationType.Habitat => 46f,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        /// <summary>Rough length of the model for players, e.g. "about 5 km"; the footprint adds a margin.</summary>
        public static float NominalLengthKlicks(StationType type)
        {
            return type switch
            {
                StationType.Outpost => 3f,
                StationType.Research => 5f,
                StationType.Industrial => 7f,
                StationType.Habitat => 9f,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        /// <summary>
        /// Scan time relative to a deposit (factor 1): a shuttle that scans a rock in 2 s needs 3 s for an
        /// outpost and 6 s for a habitat ring.
        /// </summary>
        public static float ScanDurationFactor(StationType type)
        {
            return type switch
            {
                StationType.Outpost => 1.5f,
                StationType.Research => 2f,
                StationType.Industrial => 2.5f,
                StationType.Habitat => 3f,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static int DockingPortCount(StationType type)
        {
            return type switch
            {
                StationType.Outpost => 2,
                StationType.Research => 3,
                StationType.Industrial => 4,
                StationType.Habitat => 6,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}

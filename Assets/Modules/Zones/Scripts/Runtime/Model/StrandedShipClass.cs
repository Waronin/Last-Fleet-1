using System;
using LastFleet.Shared.Randomness;

namespace LastFleet.Zones
{
    /// <summary>The capital ship classes that can be found stranded; the same classes the fleet flies.</summary>
    public enum StrandedShipClass
    {
        Liner,
        Refinery,
        Tanker,
        Cruiser
    }

    /// <summary>
    /// Size and odds per stranded ship class. The footprint holds the ship's model on the zone plane with
    /// a margin; a test checks every stranded ship model against it. Liners are found most often,
    /// cruisers least. All weights are balancing starting points.
    /// </summary>
    public static class StrandedShipClasses
    {
        /// <summary>Two airlocks, one per flank.</summary>
        public const int DockingPortCount = 2;

        /// <summary>Scan time relative to a deposit: between an outpost and a research station.</summary>
        public const float ScanDurationFactor = 2f;

        static readonly (StrandedShipClass Value, float Weight)[] Odds =
        {
            (StrandedShipClass.Liner, 0.4f), (StrandedShipClass.Refinery, 0.25f), (StrandedShipClass.Tanker, 0.25f), (StrandedShipClass.Cruiser, 0.1f)
        };

        public static StrandedShipClass Pick(DeterministicRandom random)
        {
            return random.PickWeighted(Odds);
        }

        /// <summary>The technical class name of the fleet ship it becomes, e.g. "Liner".</summary>
        public static string ShipClassName(StrandedShipClass shipClass)
        {
            return shipClass switch
            {
                StrandedShipClass.Liner => "Liner",
                StrandedShipClass.Refinery => "Refinery",
                StrandedShipClass.Tanker => "Tanker",
                StrandedShipClass.Cruiser => "Cruiser",
                _ => throw new ArgumentOutOfRangeException(nameof(shipClass), shipClass, null)
            };
        }

        public static float FootprintRadiusUnits(StrandedShipClass shipClass)
        {
            return shipClass switch
            {
                StrandedShipClass.Liner => 10f,
                StrandedShipClass.Refinery => 7f,
                StrandedShipClass.Tanker => 8f,
                StrandedShipClass.Cruiser => 9f,
                _ => throw new ArgumentOutOfRangeException(nameof(shipClass), shipClass, null)
            };
        }

        /// <summary>Rough hull length for players, e.g. "about 1.6 km".</summary>
        public static float NominalLengthKlicks(StrandedShipClass shipClass)
        {
            return shipClass switch
            {
                StrandedShipClass.Liner => 1.6f,
                StrandedShipClass.Refinery => 0.9f,
                StrandedShipClass.Tanker => 1.2f,
                StrandedShipClass.Cruiser => 1.3f,
                _ => throw new ArgumentOutOfRangeException(nameof(shipClass), shipClass, null)
            };
        }
    }
}

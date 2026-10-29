using System;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>
    /// A station inside a station zone. It lies on the zone plane, where ships fly, so its docking
    /// bays can be reached. What it really is (<see cref="Profile"/>) stays hidden; a scan reveals
    /// part of it (<see cref="ScanReport"/>), hails and docking the rest. Scan state is run state and
    /// stays when the fleet leaves the zone.
    /// </summary>
    public sealed class ZoneStation : IScanTarget
    {
        /// <summary>Scanning ships keep about 2 km from the station's outer hull.</summary>
        public const float ScanStandoffMarginUnits = 20f;

        public ZoneStation(string name, StationType type, Vector3 localPosition, float headingDegrees, StationProfile profile)
        {
            Name = name;
            Type = type;
            LocalPosition = localPosition;
            HeadingDegrees = headingDegrees;
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public string Name { get; }
        public StationType Type { get; }
        public Vector3 LocalPosition { get; }
        public float HeadingDegrees { get; }
        public StationProfile Profile { get; }
        public float FootprintRadiusUnits => StationTypes.FootprintRadiusUnits(Type);
        public int DockingBayCount => StationTypes.DockingBayCount(Type);

        /// <summary>Null until the station has been scanned.</summary>
        public StationScanReport ScanReport { get; private set; }

        public bool IsScanned => ScanReport != null;
        public float ScanStandoffUnits => FootprintRadiusUnits + ScanStandoffMarginUnits;
        public float ScanDurationFactor => StationTypes.ScanDurationFactor(Type);

        public void CompleteScan(float accuracyRoll)
        {
            ScanReport ??= StationScanReport.FromProfile(Profile, accuracyRoll);
        }
    }
}

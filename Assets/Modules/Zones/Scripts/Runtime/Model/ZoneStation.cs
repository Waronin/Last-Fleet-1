using System;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>
    /// A station inside a station zone. It lies on the zone plane, where ships fly, so its docking
    /// bays can be reached. What it really is (<see cref="Profile"/>) stays hidden until scans, hails
    /// and docking reveal it.
    /// </summary>
    public sealed class ZoneStation
    {
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
    }
}

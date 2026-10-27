using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>A station structure inside a station zone. Interactions (scan, hail, dock) come later.</summary>
    public sealed class ZoneStation
    {
        public ZoneStation(string name, Vector3 localPosition, float sizeUnits)
        {
            Name = name;
            LocalPosition = localPosition;
            SizeUnits = sizeUnits;
        }

        public string Name { get; }
        public Vector3 LocalPosition { get; }
        public float SizeUnits { get; }
    }
}

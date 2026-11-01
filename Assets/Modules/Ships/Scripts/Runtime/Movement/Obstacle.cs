using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Something ships fly around and hide behind, as a circle on the zone plane: a rock, a station or
    /// a capital ship. The source lets callers skip the obstacle that is the ship itself or its target.
    /// </summary>
    public readonly struct Obstacle
    {
        public Obstacle(Vector2 center, float radiusUnits, object source)
        {
            Center = center;
            RadiusUnits = radiusUnits;
            Source = source;
        }

        /// <summary>Center relative to the zone, on the plane (x, z).</summary>
        public Vector2 Center { get; }

        public float RadiusUnits { get; }
        public object Source { get; }
    }
}

using UnityEngine;

namespace LastFleet.StarSystem
{
    public static class OrbitMath
    {
        /// <summary>Position on a circular orbit in the system plane (x, z mapped to x, y).</summary>
        public static Vector2 PositionOnOrbit(float radius, float angleDegrees)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }
    }
}

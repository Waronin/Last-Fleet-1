using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Where a harvester sits when it clamps onto a rock: on the surface point that faces the ship,
    /// belly towards the rock, keeping its heading as far as the surface allows.
    /// </summary>
    public static class HullClampPose
    {
        public static void Compute(Vector3 rockCenter, float surfaceRadius, Vector3 shipPosition, Vector3 shipForward,
            float hullClearance, out Vector3 position, out Quaternion rotation)
        {
            Vector3 normal = shipPosition - rockCenter;
            normal = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
            position = rockCenter + normal * (surfaceRadius + hullClearance);

            Vector3 forward = Vector3.ProjectOnPlane(shipForward, normal);
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, normal);
            }
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.right, normal);
            }
            rotation = Quaternion.LookRotation(forward.normalized, normal);
        }
    }
}

using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Where a harvester sits when it clamps onto a deposit: on the upper side facing its mining slot,
    /// belly towards the surface, keeping its heading as far as the surface allows. The upper side
    /// stays in view of the camera above the zone plane, so the ship can still be seen and clicked
    /// even when the deposit floats above its slot. Deposits are ellipsoids, so flat gas clouds work too.
    /// </summary>
    public static class HullClampPose
    {
        /// <summary>How far above the horizon the clamp point lies, seen from the deposit center.</summary>
        public const float ClampElevationDegrees = 40f;

        public static void Compute(Vector3 depositCenter, float horizontalRadius, float verticalRadius, Vector3 shipPosition,
            Vector3 shipForward, float hullClearance, out Vector3 position, out Quaternion rotation)
        {
            Vector3 outward = Vector3.ProjectOnPlane(shipPosition - depositCenter, Vector3.up);
            outward = outward.sqrMagnitude > 1e-8f ? outward.normalized : Vector3.forward;
            float elevation = ClampElevationDegrees * Mathf.Deg2Rad;
            Vector3 direction = outward * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation);

            // Distance to the ellipsoid surface along the direction, and the surface normal there.
            float horizontalSquared = horizontalRadius * horizontalRadius;
            float verticalSquared = verticalRadius * verticalRadius;
            float distance = 1f / Mathf.Sqrt((direction.x * direction.x + direction.z * direction.z) / horizontalSquared +
                                             direction.y * direction.y / verticalSquared);
            Vector3 surfacePoint = direction * distance;
            Vector3 normal = new Vector3(surfacePoint.x / horizontalSquared, surfacePoint.y / verticalSquared,
                surfacePoint.z / horizontalSquared).normalized;
            position = depositCenter + surfacePoint + normal * hullClearance;

            Vector3 forward = Vector3.ProjectOnPlane(shipForward, normal);
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, normal);
            }
            rotation = Quaternion.LookRotation(forward.normalized, normal);
        }
    }
}

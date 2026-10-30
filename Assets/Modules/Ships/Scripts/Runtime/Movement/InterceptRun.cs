using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Steering of an attack run (see <see cref="ShipOrderKind.Intercept"/>): aim beside the target,
    /// fly past it, hold the course until the break-off distance is behind, then come about for the
    /// next pass. Without the hold the ship would turn on the spot right behind its target.
    /// </summary>
    public static class InterceptRun
    {
        /// <summary>Where to aim: beside the target, seen along the approach; the offset's sign picks the side.</summary>
        public static Vector2 AimPoint(Vector2 shipPosition, Vector2 targetPosition, float passOffsetUnits)
        {
            Vector2 approach = targetPosition - shipPosition;
            if (approach.sqrMagnitude < 1e-8f)
            {
                return targetPosition;
            }
            Vector2 direction = approach.normalized;
            Vector2 right = new Vector2(direction.y, -direction.x);
            return targetPosition + right * passOffsetUnits;
        }

        /// <summary>
        /// Whether the ship keeps its current course instead of steering at the target: from passing it
        /// (the target falls behind) until it is the break-off distance away.
        /// </summary>
        public static bool HoldsCourse(bool wasHolding, Vector2 forward, Vector2 toTarget, float breakOffUnits)
        {
            float distance = toTarget.magnitude;
            if (distance >= breakOffUnits)
            {
                return false;
            }
            return wasHolding || Vector2.Dot(forward, toTarget) < 0f;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Flying around obstacles and seeing past them, on the zone plane. Steering looks ahead along the
    /// intended direction and, if a circle (plus clearance) is in the way, turns towards the tangent
    /// past its nearer side. An obstacle that holds the destination is never avoided: the ship is
    /// meant to get there, e.g. to a mining slot, a docking port or a landing bay.
    /// </summary>
    public static class ObstacleAvoidance
    {
        /// <summary>Whether the segment between two points passes through a circle.</summary>
        public static bool SegmentHitsCircle(Vector2 from, Vector2 to, Vector2 center, float radius)
        {
            Vector2 segment = to - from;
            float lengthSquared = segment.sqrMagnitude;
            float t = lengthSquared > 1e-8f ? Mathf.Clamp01(Vector2.Dot(center - from, segment) / lengthSquared) : 0f;
            Vector2 closest = from + segment * t;
            return (center - closest).sqrMagnitude < radius * radius;
        }

        /// <summary>
        /// Whether a line of fire is blocked (GDD: cover in asteroid fields). Obstacles that are the
        /// shooter or the target themselves do not count.
        /// </summary>
        public static bool IsLineBlocked(Vector2 from, Vector2 to, IReadOnlyList<Obstacle> obstacles, object shooter, object target)
        {
            foreach (Obstacle obstacle in obstacles)
            {
                if (obstacle.Source == shooter || obstacle.Source == target)
                {
                    continue;
                }
                if (SegmentHitsCircle(from, to, obstacle.Center, obstacle.RadiusUnits))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The direction to fly instead of <paramref name="desiredDirection"/>, or the desired direction
        /// itself when nothing within the look-ahead distance (and before the destination) is in the way.
        /// </summary>
        public static Vector2 Steer(Vector2 position, Vector2 desiredDirection, Vector2 destination, float lookAheadUnits,
            float clearanceUnits, IReadOnlyList<Obstacle> obstacles, object self)
        {
            if (desiredDirection.sqrMagnitude < 1e-8f)
            {
                return desiredDirection;
            }
            Vector2 direction = desiredDirection.normalized;
            float reach = Mathf.Min(lookAheadUnits, (destination - position).magnitude);
            Vector2 probeEnd = position + direction * reach;

            bool found = false;
            Obstacle nearest = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (Obstacle obstacle in obstacles)
            {
                float radius = obstacle.RadiusUnits + clearanceUnits;
                if (obstacle.Source == self || (destination - obstacle.Center).sqrMagnitude <= radius * radius)
                {
                    continue;
                }
                float distance = (obstacle.Center - position).magnitude;
                if (distance < nearestDistance && SegmentHitsCircle(position, probeEnd, obstacle.Center, radius))
                {
                    found = true;
                    nearest = obstacle;
                    nearestDistance = distance;
                }
            }
            if (!found)
            {
                return direction;
            }
            return TangentPast(position, direction, nearest.Center, nearest.RadiusUnits + clearanceUnits);
        }

        /// <summary>Direction past a circle on the side the desired direction already leans to; straight out when inside it.</summary>
        static Vector2 TangentPast(Vector2 position, Vector2 direction, Vector2 center, float radius)
        {
            Vector2 toCenter = center - position;
            float distance = toCenter.magnitude;
            if (distance <= radius)
            {
                return distance > 1e-6f ? -toCenter / distance : direction;
            }

            // Cross product sign: positive when the center lies to the left of the course.
            float side = direction.x * toCenter.y - direction.y * toCenter.x >= 0f ? -1f : 1f;
            float angle = Mathf.Asin(radius / distance) * Mathf.Rad2Deg * side;
            return Rotate(toCenter / distance, angle);
        }

        static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }
    }
}

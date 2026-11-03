using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using UnityEngine;

namespace LastFleet.Pursuit
{
    /// <summary>
    /// Where the Pronin attack takes place: the carrier arrives in a random zone and later strikes
    /// the zone holding most of the fleet's capital ships.
    /// </summary>
    public static class ProninAttackRules
    {
        /// <summary>
        /// The zone with the most capital ships, given one zone index per capital ship in space; ties go
        /// to the lower index. -1 when there is none.
        /// </summary>
        public static int StrikeZone(IReadOnlyList<int> capitalShipZoneIndices)
        {
            Dictionary<int, int> countByZone = new Dictionary<int, int>();
            int bestZone = -1;
            int bestCount = 0;
            foreach (int zoneIndex in capitalShipZoneIndices)
            {
                countByZone.TryGetValue(zoneIndex, out int count);
                count++;
                countByZone[zoneIndex] = count;
                if (count > bestCount || (count == bestCount && zoneIndex < bestZone))
                {
                    bestCount = count;
                    bestZone = zoneIndex;
                }
            }
            return bestZone;
        }

        /// <summary>
        /// A random zone of the system for the carrier's arrival. It avoids the given zone (where the
        /// fleet's capital ships are), so the strike is a second move; with a single zone it has no choice.
        /// </summary>
        public static int ArrivalZone(int zoneCount, int avoidedZoneIndex, DeterministicRandom random)
        {
            if (zoneCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(zoneCount), "A system needs at least one zone.");
            }
            bool canAvoid = avoidedZoneIndex >= 0 && avoidedZoneIndex < zoneCount && zoneCount > 1;
            if (!canAvoid)
            {
                return random.Range(0, zoneCount);
            }

            int pick = random.Range(0, zoneCount - 1);
            return pick >= avoidedZoneIndex ? pick + 1 : pick;
        }

        /// <summary>
        /// Whether the hunting carrier must set a new course: its destination (the zone it is in or
        /// jumping to) is not the strike zone, or it idles there out of reach of the fleet's capital
        /// ships, e.g. because they moved within the zone. No strike zone means nothing to hunt.
        /// </summary>
        public static bool NeedsNewCourse(int strikeZoneIndex, int carrierDestinationZoneIndex, bool carrierIsIdle,
            float distanceToTargetsUnits, float engageRangeUnits)
        {
            if (strikeZoneIndex < 0)
            {
                return false;
            }
            if (carrierDestinationZoneIndex != strikeZoneIndex)
            {
                return true;
            }
            return carrierIsIdle && distanceToTargetsUnits > engageRangeUnits;
        }

        /// <summary>
        /// Where the carrier stops in the strike zone: short of the targets' center on the side it comes
        /// from, so its guns reach them, and inside the zone.
        /// </summary>
        public static Vector2 StrikePoint(Vector2 targetsCenter, Vector2 travelDirection, float standoffUnits, float maximumRadiusUnits)
        {
            Vector2 point = targetsCenter - travelDirection.normalized * standoffUnits;
            return Vector2.ClampMagnitude(point, maximumRadiusUnits);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Picks ships for automatic tasks such as scanning or mining: free ships first, then the
    /// nearest busy ones. Travel cost prefers ships in the target zone, then nearby zones.
    /// </summary>
    public static class ShipAvailability
    {
        /// <summary>Any ship outside the target zone costs more than every ship inside it.</summary>
        const float OtherZonePenaltyUnits = 100000f;
        const float UnitsPerAU = 1000f;

        static readonly List<IShipAutomation> AutomationBuffer = new List<IShipAutomation>();

        /// <summary>Idle in a zone or docked, with no queued orders and no automatic task running.</summary>
        public static bool IsFree(ShipAgent ship)
        {
            if ((ship.Activity != ShipActivity.Idle && ship.Activity != ShipActivity.Docked) || ship.QueuedOrderCount > 0)
            {
                return false;
            }

            ship.GetComponents(AutomationBuffer);
            foreach (IShipAutomation automation in AutomationBuffer)
            {
                if (automation.IsAutomationActive)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Rough cost for a ship to reach a point in a zone of the current system.</summary>
        public static float TravelCost(ShipAgent ship, int zoneIndex, Vector2 zonePositionAU, Vector3 zoneLocalPoint)
        {
            if (ship.Zone.Record.PointOfInterestIndex == zoneIndex)
            {
                Vector3 local = ship.Zone.transform.InverseTransformPoint(ship.WorldPosition);
                return new Vector2(local.x - zoneLocalPoint.x, local.z - zoneLocalPoint.z).magnitude;
            }

            float zoneDistanceAU = Vector2.Distance(ship.Zone.Record.PointOfInterest.PositionAU, zonePositionAU);
            return OtherZonePenaltyUnits + zoneDistanceAU * UnitsPerAU;
        }

        /// <summary>
        /// Fills <paramref name="result"/> with up to <paramref name="count"/> candidates: free ones by
        /// ascending cost, then busy ones by ascending cost.
        /// </summary>
        public static void PickFreeThenNearest<T>(IReadOnlyList<T> candidates, Func<T, bool> isFree, Func<T, float> cost,
            int count, List<T> result)
        {
            result.Clear();
            if (count <= 0)
            {
                return;
            }

            List<(T Candidate, bool Free, float Cost)> ranked = new List<(T Candidate, bool Free, float Cost)>(candidates.Count);
            foreach (T candidate in candidates)
            {
                ranked.Add((candidate, isFree(candidate), cost(candidate)));
            }
            ranked.Sort((a, b) => a.Free != b.Free ? (a.Free ? -1 : 1) : a.Cost.CompareTo(b.Cost));

            for (int i = 0; i < ranked.Count && result.Count < count; i++)
            {
                result.Add(ranked[i].Candidate);
            }
        }
    }
}

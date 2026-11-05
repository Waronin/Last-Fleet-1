using System;
using System.Collections.Generic;
using LastFleet.Galaxy;
using LastFleet.Shared.Randomness;

namespace LastFleet.Quests
{
    /// <summary>
    /// Chooses the star system for the next quest step when the previous one is done: an unvisited
    /// system within one jump if there is one, so the step is always reachable and fresh; otherwise the
    /// nearest unvisited system; otherwise any system in range. The choice is fixed by the seed.
    /// </summary>
    public static class QuestSystemPicker
    {
        public static int Pick(GalaxyLayout galaxy, int fromSystemIndex, Func<int, bool> isVisited, ulong seed)
        {
            if (galaxy == null)
            {
                throw new ArgumentNullException(nameof(galaxy));
            }
            if (isVisited == null)
            {
                throw new ArgumentNullException(nameof(isVisited));
            }

            DeterministicRandom random = new DeterministicRandom(seed);
            IReadOnlyList<int> neighbours = galaxy.GetNeighbours(fromSystemIndex);
            List<int> fresh = new List<int>();
            foreach (int neighbour in neighbours)
            {
                if (neighbour != fromSystemIndex && !isVisited(neighbour))
                {
                    fresh.Add(neighbour);
                }
            }
            // Neighbours come in a stable order, so the same seed always picks the same system.
            if (fresh.Count > 0)
            {
                return fresh[random.Range(0, fresh.Count)];
            }

            int nearest = NearestUnvisited(galaxy, fromSystemIndex, isVisited);
            if (nearest >= 0)
            {
                return nearest;
            }
            return neighbours.Count > 0 ? neighbours[random.Range(0, neighbours.Count)] : fromSystemIndex;
        }

        static int NearestUnvisited(GalaxyLayout galaxy, int fromSystemIndex, Func<int, bool> isVisited)
        {
            int nearest = -1;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < galaxy.Systems.Count; i++)
            {
                if (i == fromSystemIndex || isVisited(i))
                {
                    continue;
                }
                float distance = galaxy.DistanceLightYears(fromSystemIndex, i);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }
            }
            return nearest;
        }
    }
}

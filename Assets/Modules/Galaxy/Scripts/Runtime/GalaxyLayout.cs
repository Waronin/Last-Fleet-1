using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFleet.Galaxy
{
    /// <summary>Immutable result of galaxy generation: systems and their jump connections.</summary>
    public sealed class GalaxyLayout
    {
        readonly StarSystemNode[] _systems;
        readonly int[][] _neighbourIndices;

        public GalaxyLayout(ulong seed, float jumpRangeLightYears, IReadOnlyList<StarSystemNode> systems, int startSystemIndex)
        {
            if (systems == null || systems.Count == 0)
            {
                throw new ArgumentException("A galaxy needs at least one system.", nameof(systems));
            }
            if (startSystemIndex < 0 || startSystemIndex >= systems.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(startSystemIndex));
            }

            Seed = seed;
            JumpRangeLightYears = jumpRangeLightYears;
            StartSystemIndex = startSystemIndex;
            _systems = new StarSystemNode[systems.Count];
            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i].Index != i)
                {
                    throw new ArgumentException($"System at position {i} has index {systems[i].Index}.", nameof(systems));
                }
                _systems[i] = systems[i];
            }
            _neighbourIndices = BuildNeighbours(_systems, jumpRangeLightYears);
        }

        public ulong Seed { get; }
        public float JumpRangeLightYears { get; }
        public int StartSystemIndex { get; }
        public IReadOnlyList<StarSystemNode> Systems => _systems;

        /// <summary>Systems within jump range of the given system, ordered by index.</summary>
        public IReadOnlyList<int> GetNeighbours(int systemIndex)
        {
            return _neighbourIndices[systemIndex];
        }

        public float DistanceLightYears(int fromIndex, int toIndex)
        {
            return Vector3.Distance(_systems[fromIndex].PositionLightYears, _systems[toIndex].PositionLightYears);
        }

        static int[][] BuildNeighbours(StarSystemNode[] systems, float jumpRangeLightYears)
        {
            float jumpRangeSquared = jumpRangeLightYears * jumpRangeLightYears;
            List<int>[] neighbours = new List<int>[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                neighbours[i] = new List<int>();
            }

            for (int i = 0; i < systems.Length; i++)
            {
                for (int j = i + 1; j < systems.Length; j++)
                {
                    float distanceSquared = (systems[i].PositionLightYears - systems[j].PositionLightYears).sqrMagnitude;
                    if (distanceSquared <= jumpRangeSquared)
                    {
                        neighbours[i].Add(j);
                        neighbours[j].Add(i);
                    }
                }
            }

            int[][] result = new int[systems.Length][];
            for (int i = 0; i < systems.Length; i++)
            {
                neighbours[i].Sort();
                result[i] = neighbours[i].ToArray();
            }
            return result;
        }
    }
}

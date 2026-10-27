using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFleet.Shared.Randomness
{
    /// <summary>
    /// Seedable pseudo-random source based on SplitMix64. The algorithm is fixed in this file, so
    /// generated content does not depend on System.Random or UnityEngine.Random implementations.
    /// Integer output is identical everywhere; float helpers use single-precision math and are
    /// expected, but not guaranteed, to match across platforms for the same call order.
    /// </summary>
    public sealed class DeterministicRandom
    {
        const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
        const float FloatUnit = 1f / 16777216f;

        ulong _state;

        public DeterministicRandom(ulong seed)
        {
            _state = seed;
        }

        /// <summary>
        /// Derives an independent seed for a sub-generator, e.g. one per star system. Using a salt keeps
        /// each consumer's sequence stable when other consumers change how many values they draw.
        /// </summary>
        public static ulong DeriveSeed(ulong seed, ulong salt)
        {
            return Mix(seed + GoldenGamma * (salt + 1UL));
        }

        public ulong NextULong()
        {
            _state += GoldenGamma;
            return Mix(_state);
        }

        /// <summary>Returns a value in [0, 1).</summary>
        public float NextFloat()
        {
            return (NextULong() >> 40) * FloatUnit;
        }

        /// <summary>Returns a value in [minInclusive, maxExclusive).</summary>
        public float Range(float minInclusive, float maxExclusive)
        {
            return minInclusive + (maxExclusive - minInclusive) * NextFloat();
        }

        /// <summary>Returns a value in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive),
                    $"maxExclusive ({maxExclusive}) must be greater than minInclusive ({minInclusive}).");
            }

            ulong span = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextULong() % span));
        }

        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }

        /// <summary>Picks one option with a probability proportional to its weight.</summary>
        public T PickWeighted<T>(IReadOnlyList<(T Value, float Weight)> options)
        {
            if (options == null || options.Count == 0)
            {
                throw new ArgumentException("At least one option is required.", nameof(options));
            }

            float totalWeight = 0f;
            for (int i = 0; i < options.Count; i++)
            {
                totalWeight += options[i].Weight;
            }

            float roll = Range(0f, totalWeight);
            for (int i = 0; i < options.Count; i++)
            {
                roll -= options[i].Weight;
                if (roll < 0f)
                {
                    return options[i].Value;
                }
            }
            return options[options.Count - 1].Value;
        }

        public float AngleDegrees()
        {
            return Range(0f, 360f);
        }

        public Vector3 InsideUnitSphere()
        {
            while (true)
            {
                Vector3 candidate = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f));
                if (candidate.sqrMagnitude <= 1f)
                {
                    return candidate;
                }
            }
        }

        static ulong Mix(ulong value)
        {
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}

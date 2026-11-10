using System;

namespace LastFleet.TacticalWeapons
{
    /// <summary>
    /// Missiles a warship carries and the one it is building. Only one missile is built at a time, and
    /// only while a finished one would still fit.
    /// </summary>
    public sealed class MissileMagazine
    {
        public MissileMagazine(int capacity, int startingMissiles, float buildSeconds)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "A magazine holds at least one missile.");
            }
            if (startingMissiles < 0 || startingMissiles > capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(startingMissiles), "Starting missiles must fit the magazine.");
            }
            if (buildSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(buildSeconds), "Building takes time.");
            }
            Capacity = capacity;
            Count = startingMissiles;
            BuildSeconds = buildSeconds;
        }

        public int Capacity { get; }
        public int Count { get; private set; }
        public float BuildSeconds { get; }
        public bool IsBuilding { get; private set; }
        public float BuildSecondsLeft { get; private set; }
        public bool IsFull => Count >= Capacity;
        public bool CanStartBuild => !IsBuilding && !IsFull;

        /// <summary>Starts building a missile; false when one is already being built or the magazine is full.</summary>
        public bool StartBuild()
        {
            if (!CanStartBuild)
            {
                return false;
            }
            IsBuilding = true;
            BuildSecondsLeft = BuildSeconds;
            return true;
        }

        /// <summary>Advances the build; returns true for the step that finishes a missile.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (!IsBuilding || deltaSeconds <= 0f)
            {
                return false;
            }
            BuildSecondsLeft -= deltaSeconds;
            if (BuildSecondsLeft > 0f)
            {
                return false;
            }
            BuildSecondsLeft = 0f;
            IsBuilding = false;
            Count++;
            return true;
        }

        public bool TryTake()
        {
            if (Count <= 0)
            {
                return false;
            }
            Count--;
            return true;
        }
    }
}

using System;

namespace LastFleet.Ships
{
    /// <summary>
    /// The crew of a small ship as a head count (GDD: crews of small ships are people of the fleet).
    /// The ship works in proportion to its crew; without anyone aboard it cannot fly at all.
    /// </summary>
    public sealed class CrewComplement
    {
        public CrewComplement(int required, int count)
        {
            if (required < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(required), "A crewed ship needs at least one crew member.");
            }
            if (count < 0 || count > required)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Crew must be between 0 and {required}.");
            }
            Required = required;
            Count = count;
        }

        public int Required { get; }
        public int Count { get; private set; }
        public int Missing => Required - Count;
        public bool IsEmpty => Count == 0;
        public bool IsFull => Count == Required;

        /// <summary>Share of full performance, e.g. mining speed or scan speed: crew aboard divided by crew required.</summary>
        public float Efficiency => (float)Count / Required;

        /// <summary>Takes crew members aboard up to the required number and returns how many joined.</summary>
        public int Add(int people)
        {
            ValidateAmount(people);
            int added = Math.Min(people, Missing);
            Count += added;
            return added;
        }

        /// <summary>Removes up to the given number of crew members and returns how many left.</summary>
        public int Remove(int people)
        {
            ValidateAmount(people);
            int removed = Math.Min(people, Count);
            Count -= removed;
            return removed;
        }

        static void ValidateAmount(int people)
        {
            if (people < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(people), "The number of crew members must not be negative.");
            }
        }
    }
}

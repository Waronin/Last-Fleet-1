using System;

namespace LastFleet.People
{
    /// <summary>
    /// The passengers of one ship: a head count, not individuals (GDD: Passengers). The capacity is the
    /// comfortable occupancy; above it the ship is overcrowded, which will lower morale, up to a hard
    /// limit of <see cref="OvercrowdingLimitFactor"/> times the capacity.
    /// </summary>
    public sealed class PassengerQuarters
    {
        public const float OvercrowdingLimitFactor = 1.5f;

        public PassengerQuarters(int capacity, int count)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Passenger capacity must be positive.");
            }
            Capacity = capacity;
            HardLimit = (int)Math.Floor(capacity * OvercrowdingLimitFactor);
            if (count < 0 || count > HardLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Start count must be between 0 and {HardLimit}.");
            }
            Count = count;
        }

        public int Capacity { get; }
        public int HardLimit { get; }
        public int Count { get; private set; }
        public bool IsOvercrowded => Count > Capacity;
        public int FreeComfortableSpace => Math.Max(0, Capacity - Count);
        public int FreeSpace => HardLimit - Count;

        /// <summary>Takes people aboard up to the hard limit and returns how many found room.</summary>
        public int Add(int people)
        {
            ValidateAmount(people);
            int added = Math.Min(people, FreeSpace);
            Count += added;
            return added;
        }

        /// <summary>Removes up to the given number of people, e.g. casualties, and returns how many left.</summary>
        public int Remove(int people)
        {
            ValidateAmount(people);
            int removed = Math.Min(people, Count);
            Count -= removed;
            return removed;
        }

        /// <summary>Moves up to the given number of people into other quarters and returns how many moved.</summary>
        public int TransferTo(PassengerQuarters target, int people)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (target == this)
            {
                throw new ArgumentException("Quarters cannot transfer to themselves.", nameof(target));
            }
            ValidateAmount(people);

            int moved = Math.Min(Math.Min(people, Count), target.FreeSpace);
            Count -= moved;
            target.Add(moved);
            return moved;
        }

        static void ValidateAmount(int people)
        {
            if (people < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(people), "The number of people must not be negative.");
            }
        }
    }
}

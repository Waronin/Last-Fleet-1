using System;
using System.Collections.Generic;

namespace LastFleet.Mining
{
    /// <summary>
    /// Supplies stored on an industrial ship (GDD: industrial ships store resources). Each supply has
    /// its own capacity. Fuel is not kept here but in the ships' tanks, so its capacity is zero.
    /// </summary>
    public sealed class SupplyStock
    {
        readonly Dictionary<Supply, float> _amounts = new Dictionary<Supply, float>();
        readonly Dictionary<Supply, float> _capacities = new Dictionary<Supply, float>();

        public SupplyStock(float metalCapacity, float waterCapacity)
        {
            SetCapacity(Supply.Fuel, 0f);
            SetCapacity(Supply.Metal, metalCapacity);
            SetCapacity(Supply.Water, waterCapacity);
        }

        public float AmountOf(Supply supply)
        {
            return _amounts[supply];
        }

        public float CapacityOf(Supply supply)
        {
            return _capacities[supply];
        }

        public float FreeSpaceFor(Supply supply)
        {
            return _capacities[supply] - _amounts[supply];
        }

        /// <summary>Stores up to the given amount and returns how much fit.</summary>
        public float Store(Supply supply, float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Cannot store a negative amount.");
            }

            float stored = Math.Min(amount, FreeSpaceFor(supply));
            _amounts[supply] += stored;
            return stored;
        }

        void SetCapacity(Supply supply, float capacity)
        {
            if (capacity < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must not be negative.");
            }
            _capacities[supply] = capacity;
            _amounts[supply] = 0f;
        }
    }
}

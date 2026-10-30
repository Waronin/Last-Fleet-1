using System;
using System.Collections.Generic;
using LastFleet.Zones;

namespace LastFleet.Mining
{
    /// <summary>
    /// Raw materials stored on an industrial ship until they are processed (GDD: industrial ships
    /// store resources). Each material has its own capacity.
    /// </summary>
    public sealed class RawMaterialStock
    {
        readonly Dictionary<ResourceKind, float> _amounts = new Dictionary<ResourceKind, float>();
        readonly Dictionary<ResourceKind, float> _capacities = new Dictionary<ResourceKind, float>();

        public RawMaterialStock(float oreCapacity, float iceCapacity, float gasCapacity, float metalCapacity)
        {
            SetCapacity(ResourceKind.Ore, oreCapacity);
            SetCapacity(ResourceKind.Ice, iceCapacity);
            SetCapacity(ResourceKind.Gas, gasCapacity);
            SetCapacity(ResourceKind.Metal, metalCapacity);
        }

        public float AmountOf(ResourceKind resource)
        {
            return _amounts[resource];
        }

        public float CapacityOf(ResourceKind resource)
        {
            return _capacities[resource];
        }

        public float FreeSpaceFor(ResourceKind resource)
        {
            return _capacities[resource] - _amounts[resource];
        }

        /// <summary>Stores up to the given amount and returns how much fit.</summary>
        public float Store(ResourceKind resource, float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Cannot store a negative amount.");
            }

            float stored = Math.Min(amount, FreeSpaceFor(resource));
            _amounts[resource] += stored;
            return stored;
        }

        void SetCapacity(ResourceKind resource, float capacity)
        {
            if (capacity < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must not be negative.");
            }
            _capacities[resource] = capacity;
            _amounts[resource] = 0f;
        }
    }
}

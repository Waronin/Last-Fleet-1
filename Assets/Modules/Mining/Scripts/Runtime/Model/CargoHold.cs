using System;
using LastFleet.Zones;

namespace LastFleet.Mining
{
    /// <summary>A harvester's cargo hold. It carries one raw material at a time.</summary>
    public sealed class CargoHold
    {
        public CargoHold(float capacity)
        {
            if (capacity <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Cargo capacity must be positive.");
            }
            Capacity = capacity;
        }

        public float Capacity { get; }
        public float Amount { get; private set; }
        public ResourceKind Resource { get; private set; }
        public bool IsEmpty => Amount <= 0f;
        public bool IsFull => Amount >= Capacity;
        public float FreeSpace => Capacity - Amount;

        /// <summary>Loads up to the given amount; another material only fits into an empty hold.</summary>
        public float Load(ResourceKind resource, float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Cannot load a negative amount.");
            }
            if (!IsEmpty && resource != Resource)
            {
                return 0f;
            }

            Resource = resource;
            float loaded = Math.Min(amount, FreeSpace);
            Amount += loaded;
            return loaded;
        }

        public void Unload(float amount)
        {
            if (amount < 0f || amount > Amount + 1e-4f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), $"Cannot unload {amount} of {Amount}.");
            }
            Amount = Math.Max(0f, Amount - amount);
        }
    }
}

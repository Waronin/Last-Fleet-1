using System;

namespace LastFleet.Ships
{
    /// <summary>The fuel tank of one ship (GDD: what is left in the tanks of the individual ships).</summary>
    public sealed class FuelTank
    {
        public FuelTank(float capacity)
        {
            if (capacity <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Tank capacity must be positive.");
            }
            Capacity = capacity;
            Amount = capacity;
        }

        public float Capacity { get; }
        public float Amount { get; private set; }
        public float FreeSpace => Capacity - Amount;
        public float Fraction => Amount / Capacity;
        public bool IsEmpty => Amount <= 0f;

        /// <summary>Burns up to the given amount and returns how much was actually burnt.</summary>
        public float Drain(float amount)
        {
            ValidateAmount(amount);
            float drained = Math.Min(amount, Amount);
            Amount -= drained;
            return drained;
        }

        /// <summary>Burns the whole amount, or nothing if the tank holds less.</summary>
        public bool TryDrain(float amount)
        {
            ValidateAmount(amount);
            if (amount > Amount)
            {
                return false;
            }
            Amount -= amount;
            return true;
        }

        /// <summary>Fills up to the given amount and returns how much fit.</summary>
        public float Fill(float amount)
        {
            ValidateAmount(amount);
            float filled = Math.Min(amount, FreeSpace);
            Amount += filled;
            return filled;
        }

        /// <summary>Pumps up to the given amount into another tank and returns how much moved.</summary>
        public float TransferTo(FuelTank target, float maximumAmount)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (target == this)
            {
                throw new ArgumentException("A tank cannot refuel itself.", nameof(target));
            }
            ValidateAmount(maximumAmount);

            float moved = Math.Min(maximumAmount, Math.Min(Amount, target.FreeSpace));
            Amount -= moved;
            target.Fill(moved);
            return moved;
        }

        static void ValidateAmount(float amount)
        {
            if (amount < 0f || float.IsNaN(amount))
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Fuel amounts must not be negative.");
            }
        }
    }
}

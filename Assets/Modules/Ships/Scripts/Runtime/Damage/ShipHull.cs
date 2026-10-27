using System;

namespace LastFleet.Ships
{
    /// <summary>Hull points of a ship (GDD: when the hull reaches 0 the ship is destroyed).</summary>
    public sealed class ShipHull
    {
        public ShipHull(float maximum)
        {
            if (maximum <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), "Hull points must be positive.");
            }
            Maximum = maximum;
            Current = maximum;
        }

        public float Maximum { get; }
        public float Current { get; private set; }
        public float Fraction => Current / Maximum;
        public bool IsDestroyed => Current <= 0f;

        /// <summary>Applies damage and returns true only for the hit that destroys the ship.</summary>
        public bool TakeDamage(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Damage cannot be negative.");
            }
            if (IsDestroyed)
            {
                return false;
            }

            Current = Math.Max(0f, Current - amount);
            return IsDestroyed;
        }
    }
}

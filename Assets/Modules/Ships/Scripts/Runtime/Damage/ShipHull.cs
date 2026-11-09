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
        public float Missing => Maximum - Current;
        public bool IsDamaged => Current < Maximum;

        /// <summary>Restores up to the amount, never above the maximum, and returns how much was restored. A destroyed hull stays destroyed.</summary>
        public float Repair(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Repairs cannot be negative.");
            }
            if (IsDestroyed)
            {
                return 0f;
            }

            float restored = Math.Min(amount, Missing);
            // Snap to the maximum so rounding never leaves a hull a fraction of a point short.
            Current = restored >= Missing ? Maximum : Current + restored;
            return restored;
        }

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

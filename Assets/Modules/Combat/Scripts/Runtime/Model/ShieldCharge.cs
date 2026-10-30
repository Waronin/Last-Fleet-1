using System;

namespace LastFleet.Combat
{
    /// <summary>
    /// Shield of a ship (GDD: fighters have strong shields). It absorbs weapon hits before they reach
    /// the hull and recharges once the ship has not been hit for a while.
    /// </summary>
    public sealed class ShieldCharge
    {
        readonly float _rechargePerSecond;
        readonly float _rechargeDelaySeconds;
        float _secondsSinceHit = float.PositiveInfinity;

        public ShieldCharge(float maximum, float rechargePerSecond, float rechargeDelaySeconds)
        {
            if (maximum <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), "Shield strength must be positive.");
            }
            if (rechargePerSecond < 0f || rechargeDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rechargePerSecond), "Recharge values must not be negative.");
            }

            Maximum = maximum;
            Current = maximum;
            _rechargePerSecond = rechargePerSecond;
            _rechargeDelaySeconds = rechargeDelaySeconds;
        }

        public float Maximum { get; }
        public float Current { get; private set; }
        public float Fraction => Current / Maximum;
        public bool IsDown => Current <= 0f;

        /// <summary>Absorbs as much of a hit as the shield holds and returns the damage that gets through.</summary>
        public float Absorb(float damage)
        {
            if (damage < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Damage cannot be negative.");
            }

            _secondsSinceHit = 0f;
            float absorbed = Math.Min(Current, damage);
            Current -= absorbed;
            return damage - absorbed;
        }

        public void Tick(float deltaTime)
        {
            _secondsSinceHit += deltaTime;
            if (_secondsSinceHit < _rechargeDelaySeconds)
            {
                return;
            }
            Current = Math.Min(Maximum, Current + _rechargePerSecond * deltaTime);
        }
    }
}

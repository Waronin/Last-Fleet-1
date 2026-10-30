using System;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Balance values of one weapon system. Weapons fire in bursts at targets inside their firing arc
    /// and hit or miss per shot. Ranges reach across half a zone, but the hit chance falls with
    /// distance, so closing in is worth it. Values are balancing starting points.
    /// </summary>
    [Serializable]
    public sealed class WeaponSettings
    {
        [SerializeField, Min(0.1f)] float _rangeUnits = 250f;
        [SerializeField, Min(0f)] float _damagePerHit = 4f;
        [Tooltip("Time between two shots of a burst.")]
        [SerializeField, Min(0.05f)] float _secondsBetweenShots = 0.12f;
        [SerializeField, Min(1)] int _roundsPerBurst = 1;
        [Tooltip("Pause after a burst before the next one; with single rounds the shot interval applies.")]
        [SerializeField, Min(0f)] float _secondsBetweenBursts;
        [Tooltip("Chance to hit at point-blank range.")]
        [SerializeField, Range(0f, 1f)] float _closeHitChance = 0.85f;
        [Tooltip("Chance to hit at the edge of the range.")]
        [SerializeField, Range(0f, 1f)] float _longHitChance = 0.05f;
        [Tooltip("Full width of the cone in front of the ship the weapons can fire into; 360 for turrets.")]
        [SerializeField, Range(1f, 360f)] float _firingArcDegrees = 360f;
        [Tooltip("Speed of the rounds; a hit lands when its round arrives. 0 hits at once, like an energy beam.")]
        [SerializeField, Min(0f)] float _projectileSpeedUnitsPerSecond;

        public WeaponSettings()
        {
        }

        public WeaponSettings(float rangeUnits, float damagePerHit, float secondsBetweenShots, float closeHitChance, float longHitChance,
            int roundsPerBurst = 1, float secondsBetweenBursts = 0f, float firingArcDegrees = 360f, float projectileSpeedUnitsPerSecond = 0f)
        {
            if (rangeUnits <= 0f || secondsBetweenShots <= 0f || damagePerHit < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rangeUnits), "Range and rate of fire must be positive, damage must not be negative.");
            }
            if (roundsPerBurst < 1 || secondsBetweenBursts < 0f || projectileSpeedUnitsPerSecond < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(roundsPerBurst), "A burst needs a round; pauses and speeds must not be negative.");
            }
            _rangeUnits = rangeUnits;
            _damagePerHit = damagePerHit;
            _secondsBetweenShots = secondsBetweenShots;
            _closeHitChance = Mathf.Clamp01(closeHitChance);
            _longHitChance = Mathf.Clamp01(longHitChance);
            _roundsPerBurst = roundsPerBurst;
            _secondsBetweenBursts = secondsBetweenBursts;
            _firingArcDegrees = Mathf.Clamp(firingArcDegrees, 1f, 360f);
            _projectileSpeedUnitsPerSecond = projectileSpeedUnitsPerSecond;
        }

        public float RangeUnits => _rangeUnits;
        public float DamagePerHit => _damagePerHit;
        public float SecondsBetweenShots => _secondsBetweenShots;
        public int RoundsPerBurst => _roundsPerBurst;
        public float SecondsBetweenBursts => _secondsBetweenBursts;
        public float FiringArcDegrees => _firingArcDegrees;
        public float ProjectileSpeedUnitsPerSecond => _projectileSpeedUnitsPerSecond;

        /// <summary>Hit chance at a distance, falling linearly from close range to the edge; zero outside the range.</summary>
        public float HitChance(float distanceUnits)
        {
            if (distanceUnits > _rangeUnits)
            {
                return 0f;
            }
            return Mathf.Lerp(_closeHitChance, _longHitChance, distanceUnits / _rangeUnits);
        }

        /// <summary>Whether a target in this direction lies in the firing arc around the ship's forward direction.</summary>
        public bool IsInArc(Vector2 forward, Vector2 toTarget)
        {
            return IsInArc(forward, toTarget, _firingArcDegrees);
        }

        /// <summary>Whether a direction lies in a cone of the given full width around the forward direction.</summary>
        public static bool IsInArc(Vector2 forward, Vector2 toTarget, float arcDegrees)
        {
            if (arcDegrees >= 360f || toTarget.sqrMagnitude < 1e-8f)
            {
                return true;
            }
            return Vector2.Angle(forward, toTarget) <= arcDegrees * 0.5f;
        }

        /// <summary>How long a round needs to fly this far; 0 for weapons that hit at once.</summary>
        public float FlightSeconds(float distanceUnits)
        {
            return _projectileSpeedUnitsPerSecond > 0f ? distanceUnits / _projectileSpeedUnitsPerSecond : 0f;
        }
    }
}

using System;
using UnityEngine;

namespace LastFleet.FleetJump
{
    /// <summary>Timing and cost of the large jump drive (GDD: The jump). Values are balancing starting points.</summary>
    [Serializable]
    public sealed class FleetJumpSettings
    {
        [Tooltip("After an arrival the large jump drive needs this long before it can charge again (GDD: about 5 minutes).")]
        [SerializeField, Min(0f)] float _cooldownSeconds = 300f;
        [Tooltip("Charge time once the player confirmed the jump; small ships can still land meanwhile.")]
        [SerializeField, Min(0f)] float _chargeSeconds = 30f;
        [Tooltip("Share of a capital ship's own tank a jump over the full jump range costs; shorter jumps cost proportionally less.")]
        [SerializeField, Range(0.01f, 1f)] float _fullRangeTankFraction = 0.3f;

        public FleetJumpSettings()
        {
        }

        public FleetJumpSettings(float cooldownSeconds, float chargeSeconds, float fullRangeTankFraction)
        {
            _cooldownSeconds = cooldownSeconds;
            _chargeSeconds = chargeSeconds;
            _fullRangeTankFraction = fullRangeTankFraction;
        }

        public float CooldownSeconds => _cooldownSeconds;
        public float ChargeSeconds => _chargeSeconds;
        public float FullRangeTankFraction => _fullRangeTankFraction;

        public void Validate()
        {
            if (_cooldownSeconds < 0f || _chargeSeconds < 0f)
            {
                throw new ArgumentException("Cooldown and charge time must not be negative.");
            }
            if (_fullRangeTankFraction <= 0f || _fullRangeTankFraction > 1f)
            {
                throw new ArgumentException($"The tank share of a full jump must lie in (0, 1] (was {_fullRangeTankFraction}).");
            }
        }
    }
}

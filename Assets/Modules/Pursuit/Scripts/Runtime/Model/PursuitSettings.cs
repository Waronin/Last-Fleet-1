using System;
using UnityEngine;

namespace LastFleet.Pursuit
{
    /// <summary>
    /// When the Pronin find the fleet after it arrived in a system and how their attack unfolds.
    /// Defaults are the GDD's suggestion for Normal: 13 minutes until the attack.
    /// </summary>
    [Serializable]
    public sealed class PursuitSettings
    {
        [Tooltip("Time from the fleet's arrival in a system until the Pronin carrier arrives.")]
        [SerializeField, Min(1f)] float _arrivalSeconds = 780f;
        [Tooltip("The countdown turns into a warning this long before the arrival.")]
        [SerializeField, Min(0f)] float _warningLeadSeconds = 30f;
        [Tooltip("After the carrier's arrival it launches its fighters, then jumps to the fleet's capital ships after this long.")]
        [SerializeField, Min(0f)] float _carrierStrikeDelaySeconds = 30f;
        [Tooltip("The carrier launches another wave of fighters this often while it lives.")]
        [SerializeField, Min(1f)] float _fighterWaveIntervalSeconds = 60f;

        public PursuitSettings()
        {
        }

        public PursuitSettings(float arrivalSeconds, float warningLeadSeconds, float carrierStrikeDelaySeconds, float fighterWaveIntervalSeconds = 60f)
        {
            _arrivalSeconds = arrivalSeconds;
            _warningLeadSeconds = warningLeadSeconds;
            _carrierStrikeDelaySeconds = carrierStrikeDelaySeconds;
            _fighterWaveIntervalSeconds = fighterWaveIntervalSeconds;
        }

        public float ArrivalSeconds => _arrivalSeconds;
        public float WarningLeadSeconds => _warningLeadSeconds;
        public float CarrierStrikeDelaySeconds => _carrierStrikeDelaySeconds;
        public float FighterWaveIntervalSeconds => _fighterWaveIntervalSeconds;

        public void Validate()
        {
            if (_arrivalSeconds <= 0f)
            {
                throw new ArgumentException($"The arrival time must be positive (was {_arrivalSeconds} s).");
            }
            if (_warningLeadSeconds < 0f || _carrierStrikeDelaySeconds < 0f)
            {
                throw new ArgumentException("Warning lead and strike delay must not be negative.");
            }
            if (_fighterWaveIntervalSeconds <= 0f)
            {
                throw new ArgumentException($"The fighter wave interval must be positive (was {_fighterWaveIntervalSeconds} s).");
            }
        }
    }
}

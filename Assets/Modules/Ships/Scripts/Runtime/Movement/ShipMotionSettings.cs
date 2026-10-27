using System;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Movement feel of a ship class (GDD: Physics feel without physics). Values are tuned for game
    /// feel, not physical accuracy. Distances in Unity units (1 unit = 100 m), speeds in units per second.
    /// </summary>
    [Serializable]
    public sealed class ShipMotionSettings
    {
        [Tooltip("Top speed with the main drive.")]
        [SerializeField, Min(0.1f)] float _maximumSpeed = 40f;
        [SerializeField, Min(0.1f)] float _mainDriveAcceleration = 12f;
        [SerializeField, Min(1f)] float _turnRateDegreesPerSecond = 60f;
        [Tooltip("Acceleration of the maneuvering thrusters, used for short moves and drift correction.")]
        [SerializeField, Min(0.1f)] float _thrusterAcceleration = 4f;
        [SerializeField, Min(0.1f)] float _thrusterMaximumSpeed = 6f;
        [Tooltip("Below this distance the ship maneuvers with thrusters instead of flip and burn.")]
        [SerializeField, Min(0.1f)] float _maneuverRangeUnits = 25f;
        [SerializeField, Min(0.01f)] float _arrivalToleranceUnits = 0.5f;
        [SerializeField, Range(0.5f, 30f)] float _alignToleranceDegrees = 5f;

        public ShipMotionSettings()
        {
        }

        public ShipMotionSettings(float maximumSpeed, float mainDriveAcceleration, float turnRateDegreesPerSecond,
            float thrusterAcceleration, float thrusterMaximumSpeed, float maneuverRangeUnits,
            float arrivalToleranceUnits, float alignToleranceDegrees)
        {
            _maximumSpeed = maximumSpeed;
            _mainDriveAcceleration = mainDriveAcceleration;
            _turnRateDegreesPerSecond = turnRateDegreesPerSecond;
            _thrusterAcceleration = thrusterAcceleration;
            _thrusterMaximumSpeed = thrusterMaximumSpeed;
            _maneuverRangeUnits = maneuverRangeUnits;
            _arrivalToleranceUnits = arrivalToleranceUnits;
            _alignToleranceDegrees = alignToleranceDegrees;
        }

        public float MaximumSpeed => _maximumSpeed;
        public float MainDriveAcceleration => _mainDriveAcceleration;
        public float TurnRateDegreesPerSecond => _turnRateDegreesPerSecond;
        public float ThrusterAcceleration => _thrusterAcceleration;
        public float ThrusterMaximumSpeed => _thrusterMaximumSpeed;
        public float ManeuverRangeUnits => _maneuverRangeUnits;
        public float ArrivalToleranceUnits => _arrivalToleranceUnits;
        public float AlignToleranceDegrees => _alignToleranceDegrees;

        /// <summary>Time for a 180 degree flip.</summary>
        public float FlipSeconds => 180f / _turnRateDegreesPerSecond;
    }
}

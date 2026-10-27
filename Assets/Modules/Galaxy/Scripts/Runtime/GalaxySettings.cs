using System;
using UnityEngine;

namespace LastFleet.Galaxy
{
    /// <summary>Shape and density of a generated galaxy. Values are balancing starting points.</summary>
    [Serializable]
    public sealed class GalaxySettings
    {
        [SerializeField, Min(1)] int _systemCount = 120;
        [SerializeField, Min(10f)] float _radiusLightYears = 400f;
        [Tooltip("Vertical extent of the galactic disc.")]
        [SerializeField, Min(0f)] float _thicknessLightYears = 30f;
        [Tooltip("Maximum distance of one fleet jump (GDD: about 50 light years).")]
        [SerializeField, Min(1f)] float _jumpRangeLightYears = 50f;
        [SerializeField, Min(0f)] float _minimumSpacingLightYears = 15f;
        [Tooltip("Rejected placements allowed per requested system before generation stops early.")]
        [SerializeField, Min(1)] int _placementAttemptsPerSystem = 40;

        public GalaxySettings()
        {
        }

        public GalaxySettings(int systemCount, float radiusLightYears, float thicknessLightYears,
            float jumpRangeLightYears, float minimumSpacingLightYears, int placementAttemptsPerSystem)
        {
            _systemCount = systemCount;
            _radiusLightYears = radiusLightYears;
            _thicknessLightYears = thicknessLightYears;
            _jumpRangeLightYears = jumpRangeLightYears;
            _minimumSpacingLightYears = minimumSpacingLightYears;
            _placementAttemptsPerSystem = placementAttemptsPerSystem;
        }

        public int SystemCount => _systemCount;
        public float RadiusLightYears => _radiusLightYears;
        public float ThicknessLightYears => _thicknessLightYears;
        public float JumpRangeLightYears => _jumpRangeLightYears;
        public float MinimumSpacingLightYears => _minimumSpacingLightYears;
        public int PlacementAttemptsPerSystem => _placementAttemptsPerSystem;

        public void Validate()
        {
            if (_systemCount < 1)
            {
                throw new ArgumentException($"System count must be at least 1 (was {_systemCount}).");
            }
            if (_radiusLightYears <= 0f || _thicknessLightYears < 0f)
            {
                throw new ArgumentException("Galaxy radius must be positive and thickness must not be negative.");
            }
            if (_minimumSpacingLightYears >= _jumpRangeLightYears)
            {
                throw new ArgumentException(
                    $"Minimum spacing ({_minimumSpacingLightYears} ly) must be smaller than the jump range ({_jumpRangeLightYears} ly).");
            }
            if (_placementAttemptsPerSystem < 1)
            {
                throw new ArgumentException("At least one placement attempt per system is required.");
            }
        }
    }
}

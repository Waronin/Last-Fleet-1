using System;

namespace LastFleet.Zones
{
    /// <summary>
    /// What a scan from outside reveals about a station (GDD: scanning shows crew, condition,
    /// radiation and atmosphere). Atmosphere, condition and radiation are exact; the people aboard
    /// show only as a range of life signs. Whether they are hostile takes a hail.
    /// </summary>
    public sealed class StationScanReport
    {
        /// <summary>The range spans ±25 % around an estimate that is itself up to 15 % off.</summary>
        public const float RangeHalfWidth = 0.25f;
        public const float EstimateError = 0.15f;

        StationScanReport(StationAtmosphere atmosphere, StationCondition condition, StationRadiation radiation,
            int minimumLifeSigns, int maximumLifeSigns)
        {
            Atmosphere = atmosphere;
            Condition = condition;
            Radiation = radiation;
            MinimumLifeSigns = minimumLifeSigns;
            MaximumLifeSigns = maximumLifeSigns;
        }

        public StationAtmosphere Atmosphere { get; }
        public StationCondition Condition { get; }
        public StationRadiation Radiation { get; }
        public int MinimumLifeSigns { get; }
        public int MaximumLifeSigns { get; }
        public bool HasLifeSigns => MaximumLifeSigns > 0;

        /// <param name="roll">0 to 1: where the estimate lands within its error; the true count always lies in the range.</param>
        public static StationScanReport FromProfile(StationProfile profile, float roll)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            if (!profile.IsInhabited)
            {
                return new StationScanReport(profile.Atmosphere, profile.Condition, profile.Radiation, 0, 0);
            }

            float error = (Math.Clamp(roll, 0f, 1f) * 2f - 1f) * EstimateError;
            float estimate = profile.Occupants * (1f + error);
            int minimum = Math.Min(profile.Occupants, Math.Max(1, (int)Math.Floor(estimate * (1f - RangeHalfWidth))));
            int maximum = Math.Max(profile.Occupants, (int)Math.Ceiling(estimate * (1f + RangeHalfWidth)));
            return new StationScanReport(profile.Atmosphere, profile.Condition, profile.Radiation, minimum, maximum);
        }
    }
}

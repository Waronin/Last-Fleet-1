using System;

namespace LastFleet.People
{
    /// <summary>
    /// How many passengers die when their ship is hit (GDD: passengers die through damage to capital
    /// ships). Each percent of hull lost kills between 0.1 and 0.25 percent of the people aboard, so
    /// 10 % hull damage costs 1 to 2.5 % of the passengers. A destroyed ship loses everyone.
    /// </summary>
    public static class CasualtyRules
    {
        public const float MinimumDeathsPerHullFraction = 0.1f;
        public const float MaximumDeathsPerHullFraction = 0.25f;

        /// <param name="hullFractionLost">Damage of the hit divided by the maximum hull, 0 to 1.</param>
        /// <param name="roll">Random value from 0 to 1 that picks a rate within the range.</param>
        public static int DeathsFromHit(int passengers, float hullFractionLost, float roll)
        {
            if (passengers < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(passengers), "Passengers must not be negative.");
            }
            if (hullFractionLost < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hullFractionLost), "Hull loss must not be negative.");
            }

            float clampedRoll = Math.Clamp(roll, 0f, 1f);
            float rate = MinimumDeathsPerHullFraction + (MaximumDeathsPerHullFraction - MinimumDeathsPerHullFraction) * clampedRoll;
            float deaths = passengers * Math.Min(1f, hullFractionLost) * rate;
            return Math.Min(passengers, (int)Math.Round(deaths));
        }
    }
}

using System;

namespace LastFleet.Shared.Randomness
{
    /// <summary>
    /// Chance that an event with an exponential waiting time happens during a stretch of time, e.g. a
    /// mining hazard or a crew loss aboard a docked ship. Splitting the time into frames does not change
    /// the result, so hazards behave the same at any frame rate.
    /// </summary>
    public static class HazardTiming
    {
        /// <param name="meanSeconds">Average time until the event; infinity means it never happens.</param>
        public static float ChanceWithin(float meanSeconds, float seconds)
        {
            if (meanSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(meanSeconds), "The mean waiting time must be positive.");
            }
            if (seconds <= 0f || float.IsPositiveInfinity(meanSeconds))
            {
                return 0f;
            }
            return 1f - (float)Math.Exp(-seconds / meanSeconds);
        }
    }
}

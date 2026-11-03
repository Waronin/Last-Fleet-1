using System;

namespace LastFleet.Pursuit
{
    /// <summary>Countdown times as "m:ss", the same in every language.</summary>
    public static class ClockFormat
    {
        /// <summary>Rounds up, so a countdown shows 0:00 only once the time is over.</summary>
        public static string MinutesSeconds(float seconds)
        {
            int totalSeconds = (int)Math.Ceiling(Math.Max(0f, seconds));
            return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
        }
    }
}

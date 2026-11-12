using System;

namespace LastFleet.ShipControl
{
    /// <summary>
    /// The fuel levels the player can pick for automatic refueling: off, 15 %, 30 % or 50 % of the tank.
    /// A ship below its level flies back to a hangar; small ships start at 30 %.
    /// </summary>
    public static class RefuelThresholds
    {
        public const float Off = 0f;
        public const float Default = 0.3f;

        static readonly float[] Steps = { Off, 0.15f, Default, 0.5f };

        /// <summary>The step after the given one, wrapping from the highest back to off.</summary>
        public static float Next(float current)
        {
            for (int i = 0; i < Steps.Length; i++)
            {
                if (Math.Abs(Steps[i] - current) < 1e-4f)
                {
                    return Steps[(i + 1) % Steps.Length];
                }
            }
            // An unknown value, e.g. from older settings: start the cycle over.
            return Steps[0];
        }

        /// <summary>Whether a tank this full should send the ship home under the threshold.</summary>
        public static bool ShouldReturn(float fuelFraction, float threshold)
        {
            return threshold > Off && fuelFraction < threshold;
        }
    }
}

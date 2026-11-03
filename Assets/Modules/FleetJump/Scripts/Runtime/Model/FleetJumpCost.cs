using System;

namespace LastFleet.FleetJump
{
    /// <summary>
    /// Fuel a capital ship pays for a jump from its own tank: a share of its tank per jump range, so a
    /// full-range jump costs the same share for every ship and shorter jumps cost less.
    /// </summary>
    public static class FleetJumpCost
    {
        public static float ForShip(float tankCapacity, float distanceLightYears, float jumpRangeLightYears, float fullRangeTankFraction)
        {
            if (tankCapacity < 0f || distanceLightYears < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tankCapacity), "Tank capacity and distance must not be negative.");
            }
            if (jumpRangeLightYears <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(jumpRangeLightYears), "The jump range must be positive.");
            }
            return tankCapacity * fullRangeTankFraction * (distanceLightYears / jumpRangeLightYears);
        }

        /// <summary>How many more jumps of this cost the fuel allows.</summary>
        public static int JumpsLeft(float fuel, float costPerJump)
        {
            if (costPerJump <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(costPerJump), "A jump must cost fuel.");
            }
            return (int)Math.Floor(Math.Max(0f, fuel) / costPerJump + 1e-4f);
        }
    }
}

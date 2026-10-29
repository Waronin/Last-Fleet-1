using System;
using LastFleet.Zones;

namespace LastFleet.Mining
{
    public enum HazardEvent
    {
        None,
        Shatter,
        GasExplosion,

        /// <summary>One crew member of the mining ship dies; the deposit stays radioactive.</summary>
        Radiation
    }

    /// <summary>
    /// When mining hazards go off (GDD: brittle rocks break apart, gas pockets explode). Every mining
    /// ship drills, so the risk grows with the number of ships on a rock. Radiation takes the crew of
    /// each mining ship one by one (GDD: the crew suffers over time; a doctor will reduce it later).
    /// Values are balancing starting points.
    /// </summary>
    public static class MiningHazardRules
    {
        public const float BrittleMeanSeconds = 60f;
        public const float GasPocketsMeanSeconds = 45f;
        public const float RadiationMeanSeconds = 40f;
        public const float ShatterDamage = 40f;
        public const float GasExplosionDamage = 70f;
        public const float ShatterRangeUnits = 10f;
        public const float GasExplosionRangeUnits = 6f;

        /// <summary>Chance that the hazard goes off during this much mining by one ship.</summary>
        public static float Probability(DepositHazard hazard, float miningSeconds)
        {
            float meanSeconds = hazard switch
            {
                DepositHazard.Brittle => BrittleMeanSeconds,
                DepositHazard.GasPockets => GasPocketsMeanSeconds,
                DepositHazard.Radioactive => RadiationMeanSeconds,
                _ => float.PositiveInfinity
            };
            // Exponential waiting time, so the chance does not depend on the frame rate.
            return float.IsPositiveInfinity(meanSeconds) ? 0f : 1f - (float)Math.Exp(-miningSeconds / meanSeconds);
        }

        /// <summary>The event caused by this much mining, given a random roll in [0, 1).</summary>
        public static HazardEvent Roll(DepositHazard hazard, float miningSeconds, float roll)
        {
            if (roll >= Probability(hazard, miningSeconds))
            {
                return HazardEvent.None;
            }
            return hazard switch
            {
                DepositHazard.Brittle => HazardEvent.Shatter,
                DepositHazard.GasPockets => HazardEvent.GasExplosion,
                _ => HazardEvent.Radiation
            };
        }
    }
}

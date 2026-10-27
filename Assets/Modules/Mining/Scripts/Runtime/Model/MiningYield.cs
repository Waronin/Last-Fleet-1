using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Mining bonus (GDD: the longer an asteroid is mined, the richer the exposed veins, up to +50 %
    /// yield per load). Every unit taken from the deposit puts this multiple into the hold, so loads
    /// fill faster and bring more. Leaving early wastes the best part of a deposit.
    /// </summary>
    public static class MiningYield
    {
        public const float MaximumBonus = 0.5f;
        public const float SecondsToFullBonus = 120f;

        public static float Multiplier(float depositMiningSeconds)
        {
            return 1f + MaximumBonus * Mathf.Clamp01(depositMiningSeconds / SecondsToFullBonus);
        }
    }
}

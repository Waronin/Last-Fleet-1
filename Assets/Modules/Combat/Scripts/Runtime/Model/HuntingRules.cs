namespace LastFleet.Combat
{
    /// <summary>
    /// Which ship an autonomous hunter goes for (GDD: Pronin fighters hunt small ships and prefer
    /// isolated ones in remote zones). Lower cost wins: small ships before capital ships, then the
    /// ship with the fewest friends around it, then the nearest.
    /// </summary>
    public static class HuntingRules
    {
        /// <summary>Any small ship is preferred over every capital ship.</summary>
        public const float CapitalShipPenalty = 1000000f;

        /// <summary>Each friendly ship near the target counts like this much extra travel.</summary>
        public const float EscortPenaltyUnits = 40f;

        /// <summary>Friendly ships within this distance of a target protect it.</summary>
        public const float EscortRadiusUnits = 30f;

        public static float Cost(bool isCapital, int escortCount, float travelCostUnits)
        {
            float cost = travelCostUnits + escortCount * EscortPenaltyUnits;
            return isCapital ? cost + CapitalShipPenalty : cost;
        }
    }
}

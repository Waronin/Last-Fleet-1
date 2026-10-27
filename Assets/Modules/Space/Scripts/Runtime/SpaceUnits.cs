namespace LastFleet.Space
{
    /// <summary>
    /// Conversion between the game scale and Unity units (GDD: Scale and readability).
    /// One Unity unit is 100 m at game scale; one klick (1 km) is therefore 10 units.
    /// </summary>
    public static class SpaceUnits
    {
        public const float MetersPerUnit = 100f;
        public const float UnitsPerKlick = 1000f / MetersPerUnit;

        public static float KlicksToUnits(float klicks)
        {
            return klicks * UnitsPerKlick;
        }

        public static float UnitsToKlicks(float units)
        {
            return units / UnitsPerKlick;
        }

        public static float MetersToUnits(float meters)
        {
            return meters / MetersPerUnit;
        }

        public static float UnitsToMeters(float units)
        {
            return units * MetersPerUnit;
        }
    }
}

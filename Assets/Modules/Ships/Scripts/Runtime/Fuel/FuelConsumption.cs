using System;
using LastFleet.Space;

namespace LastFleet.Ships
{
    /// <summary>
    /// Fuel use of a ship class per drive (GDD: Fuel consumption). Main drive and thrusters burn per
    /// kilometre travelled under power, the small jump drive per AU between zones.
    /// </summary>
    public sealed class FuelConsumption
    {
        public FuelConsumption(float fuelPerKlick, float fuelPerAU)
        {
            if (fuelPerKlick < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fuelPerKlick), "Consumption must not be negative.");
            }
            if (fuelPerAU < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fuelPerAU), "Consumption must not be negative.");
            }
            FuelPerKlick = fuelPerKlick;
            FuelPerAU = fuelPerAU;
        }

        public float FuelPerKlick { get; }
        public float FuelPerAU { get; }

        public float ForTravel(float distanceUnits)
        {
            return SpaceUnits.UnitsToKlicks(Math.Abs(distanceUnits)) * FuelPerKlick;
        }

        public float ForJump(float distanceAU)
        {
            return Math.Abs(distanceAU) * FuelPerAU;
        }
    }
}

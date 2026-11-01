using System;
using LastFleet.Ships;

namespace LastFleet.Mining
{
    /// <summary>
    /// Moves fuel between a supply store and a ship's tank, never more than the source holds or the
    /// target can take. The hangar refuels small ships tank to tank the same way.
    /// </summary>
    public static class FuelPumping
    {
        /// <summary>Pumps up to the given amount from a store into a tank and returns how much moved.</summary>
        public static float Pump(SupplyStock source, FuelTank target, float maximumAmount)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            float moved = source.Take(Supply.Fuel, Math.Min(maximumAmount, target.FreeSpace));
            target.Fill(moved);
            return moved;
        }
    }
}

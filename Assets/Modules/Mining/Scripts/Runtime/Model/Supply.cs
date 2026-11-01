using System;
using LastFleet.Zones;

namespace LastFleet.Mining
{
    /// <summary>Usable products the fleet keeps in stock (GDD: Resources, products).</summary>
    public enum Supply
    {
        Fuel,
        Metal,
        Water
    }

    /// <summary>
    /// What a raw material becomes once it is delivered. Processing on the refinery is skipped for
    /// now: delivered ore is metal, ice is water and gas is fuel, one unit for one unit.
    /// </summary>
    public static class SupplyConversion
    {
        public static Supply From(ResourceKind resource)
        {
            return resource switch
            {
                ResourceKind.Ore => Supply.Metal,
                ResourceKind.Metal => Supply.Metal,
                ResourceKind.Ice => Supply.Water,
                ResourceKind.Gas => Supply.Fuel,
                _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, null)
            };
        }
    }
}

namespace LastFleet.Ships
{
    /// <summary>A number of the fleet that fleet modules raise or lower by a share, e.g. +0.2 for 20 % faster.</summary>
    public enum FleetStat
    {
        GasMiningRate,
        OreMiningRate,
        IceMiningRate,
        CargoCapacity,
        MaximumHull,
        RepairRate,
        CapitalShipSpeed,
        SmallShipSpeed,
        CapitalShipFuelUse,
        SmallShipFuelUse,

        /// <summary>Share of the countdown after which late signals come in; -0.5 halves it.</summary>
        LateSignalDelay
    }

    /// <summary>Something the fleet can do or is safe from only with a fleet module.</summary>
    public enum FleetAbility
    {
        /// <summary>No crew member dies of radiation while mining or docked.</summary>
        RadiationImmunity,

        /// <summary>Hostile station crews behave like wary ones: no attacks and no capture while docked.</summary>
        PacifyHostileStations
    }

    /// <summary>One change of a fleet stat, as a share of the base value.</summary>
    public readonly struct FleetStatBonus
    {
        public FleetStatBonus(FleetStat stat, float share)
        {
            Stat = stat;
            Share = share;
        }

        public FleetStat Stat { get; }

        /// <summary>+0.3 for 30 % more, -0.1 for 10 % less.</summary>
        public float Share { get; }
    }
}

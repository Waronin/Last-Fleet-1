namespace LastFleet.FleetModules
{
    /// <summary>
    /// The fleet modules a found upgrade module can become. Each works for the whole fleet while it sits
    /// in a module slot; most carry a small drawback (see <see cref="FleetModuleCatalog"/>).
    /// </summary>
    public enum FleetModuleKind
    {
        GasMining,
        OreMining,
        IceMining,
        CargoBays,
        HullPlating,
        RadiationShielding,
        StationDiplomacy,
        CapitalShipDrives,
        SmallShipDrives,
        SignalAmplifier
    }
}

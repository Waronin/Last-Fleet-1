namespace LastFleet.TacticalWeapons
{
    /// <summary>Whether a launcher can fire an EMP missile now, and why not.</summary>
    public enum LaunchReadiness
    {
        Ready,
        NoMissile,
        Stunned,
        NotInZone
    }

    /// <summary>Whether a launcher can start building a missile now, and why not.</summary>
    public enum BuildReadiness
    {
        Ready,
        MagazineFull,
        AlreadyBuilding,
        NotEnoughSupplies
    }
}

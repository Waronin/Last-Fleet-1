namespace LastFleet.Ships
{
    /// <summary>
    /// The side (faction) a ship belongs to. Each side keeps its own <see cref="ShipRegistry"/>; a ship that
    /// changes sides moves to the other side's registry (see <see cref="ShipRegistry.TransferTo"/>). Who
    /// fights whom is set in <see cref="FactionRelations"/>.
    /// </summary>
    public enum Allegiance
    {
        /// <summary>The player's fleet of human refugees.</summary>
        HumanFleet,

        /// <summary>The enemy (GDD: The Pronin, working name).</summary>
        Pronin,

        /// <summary>Stranded capital ships adrift in the system: helpless, but the Pronin hunt them like the fleet.</summary>
        Stranded
    }
}

namespace LastFleet.Ships
{
    /// <summary>
    /// The side a ship fights for. Each side keeps its own <see cref="ShipRegistry"/>; a ship that is
    /// captured moves to the other side's registry (see <see cref="ShipRegistry.TransferTo"/>).
    /// </summary>
    public enum Allegiance
    {
        /// <summary>The player's fleet of human refugees.</summary>
        HumanFleet,

        /// <summary>The enemy (GDD: The Pronin, working name).</summary>
        Pronin
    }
}

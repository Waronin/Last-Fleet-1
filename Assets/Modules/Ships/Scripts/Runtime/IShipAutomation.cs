namespace LastFleet.Ships
{
    /// <summary>
    /// A component that drives its ship on its own, such as mining or scanning. While active the ship
    /// counts as busy even when it is idle between orders, e.g. while attached to a rock.
    /// </summary>
    public interface IShipAutomation
    {
        bool IsAutomationActive { get; }
    }
}

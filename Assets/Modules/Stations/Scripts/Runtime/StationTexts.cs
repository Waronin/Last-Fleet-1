using LastFleet.Shared.Text;

namespace LastFleet.Stations
{
    /// <summary>Docking texts in the string table "Stations".</summary>
    public static class StationTexts
    {
        const string Table = "Stations";

        public static readonly EnumTextKeys<DockingState> DockingStates = new EnumTextKeys<DockingState>(Table, "docking-state");

        /// <summary>{ship}, {station}</summary>
        public static readonly GameTextKey DockingDispatched = new GameTextKey(Table, "notice.docking-dispatched");

        /// <summary>{ship}, {station}</summary>
        public static readonly GameTextKey Docked = new GameTextKey(Table, "notice.docked");

        /// <summary>{station}</summary>
        public static readonly GameTextKey NoFreePort = new GameTextKey(Table, "notice.no-free-port");

        public static readonly GameTextKey NoShipToDock = new GameTextKey(Table, "notice.no-ship-to-dock");

        /// <summary>{ship}, {station}, {count}</summary>
        public static readonly GameTextKey Captured = new GameTextKey(Table, "notice.captured");

        public static readonly GameTextKey ToxicAtmosphereCause = new GameTextKey(Table, "cause.toxic-atmosphere");
        public static readonly GameTextKey RadiationCause = new GameTextKey(Table, "cause.radiation");
        public static readonly GameTextKey HostileAttackCause = new GameTextKey(Table, "cause.hostile-attack");

        /// <summary>{state}, {station}</summary>
        public static readonly GameTextKey DockingTask = new GameTextKey(Table, "task.docking");

        /// <summary>{used}, {total}</summary>
        public static readonly GameTextKey PortsInUse = new GameTextKey(Table, "line.ports-in-use");

        /// <summary>{ship}, {state}</summary>
        public static readonly GameTextKey DockedShipLine = new GameTextKey(Table, "line.docked-ship");

        public static readonly GameTextKey DockAction = new GameTextKey(Table, "action.dock");
        public static readonly GameTextKey AllPortsTaken = new GameTextKey(Table, "action.dock.all-ports-taken");
    }
}

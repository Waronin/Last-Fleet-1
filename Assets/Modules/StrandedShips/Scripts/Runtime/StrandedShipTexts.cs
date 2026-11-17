using LastFleet.Shared.Text;

namespace LastFleet.StrandedShips
{
    /// <summary>Texts of stranded ships in the string table "StrandedShips".</summary>
    public static class StrandedShipTexts
    {
        const string Table = "StrandedShips";

        public static readonly GameTextKey RepairAction = new GameTextKey(Table, "action.repair");
        public static readonly GameTextKey CancelAction = new GameTextKey(Table, "action.cancel");

        /// <summary>{class} ship class, {length} km</summary>
        public static readonly GameTextKey TypeLine = new GameTextKey(Table, "line.type");

        /// <summary>{percent}</summary>
        public static readonly GameTextKey HullLine = new GameTextKey(Table, "line.hull");

        /// <summary>{metal}, {seconds}</summary>
        public static readonly GameTextKey CostLine = new GameTextKey(Table, "line.cost");

        public static readonly GameTextKey WaitingForCrewLine = new GameTextKey(Table, "line.waiting-crew");
        public static readonly GameTextKey WaitingForMetalLine = new GameTextKey(Table, "line.waiting-metal");

        /// <summary>{seconds}</summary>
        public static readonly GameTextKey RepairingLine = new GameTextKey(Table, "line.repairing");

        /// <summary>{station}</summary>
        public static readonly GameTextKey RepairOrdered = new GameTextKey(Table, "feedback.ordered");

        /// <summary>{station}</summary>
        public static readonly GameTextKey RepairCancelled = new GameTextKey(Table, "feedback.cancelled");

        /// <summary>{ship} new fleet ship, {count} survivors aboard</summary>
        public static readonly GameTextKey Recovered = new GameTextKey(Table, "notice.recovered");

        /// <summary>{station}</summary>
        public static readonly GameTextKey OutOfMetal = new GameTextKey(Table, "notice.no-metal");
    }
}

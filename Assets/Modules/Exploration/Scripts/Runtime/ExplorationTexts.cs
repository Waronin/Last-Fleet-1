using LastFleet.Shared.Text;

namespace LastFleet.Exploration
{
    /// <summary>Scouting texts in the string table "Exploration".</summary>
    public static class ExplorationTexts
    {
        const string Table = "Exploration";

        public static readonly GameTextKey NoShipToScan = new GameTextKey(Table, "notice.no-ship-to-scan");
        public static readonly GameTextKey NoShipToExplore = new GameTextKey(Table, "notice.no-ship-to-explore");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey ScanDispatched = new GameTextKey(Table, "notice.scan-dispatched");

        /// <summary>{ship}, {zone}</summary>
        public static readonly GameTextKey ExplorationDispatched = new GameTextKey(Table, "notice.exploration-dispatched");

        /// <summary>{ship}, {size}, {shape}, {resource}</summary>
        public static readonly GameTextKey ScanFinished = new GameTextKey(Table, "notice.scan-finished");
    }
}

using LastFleet.Shared.Text;

namespace LastFleet.AbandonedShips
{
    /// <summary>Texts of abandoned small ships and crew transfers in the string table "AbandonedShips".</summary>
    public static class AbandonedShipTexts
    {
        const string Table = "AbandonedShips";

        /// <summary>{class} ship class, {zone}</summary>
        public static readonly GameTextKey Found = new GameTextKey(Table, "notice.found");

        /// <summary>{class}: how a found ship is called until it joins, since the stranded side numbers its own ships.</summary>
        public static readonly GameTextKey FoundShipName = new GameTextKey(Table, "name.found-ship");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey CrewGone = new GameTextKey(Table, "notice.crew-gone");

        /// <summary>{donor}, {ship}</summary>
        public static readonly GameTextKey TransferOrdered = new GameTextKey(Table, "feedback.transfer-ordered");

        public static readonly GameTextKey NoDonor = new GameTextKey(Table, "feedback.no-donor");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey TransferAbandoned = new GameTextKey(Table, "notice.transfer-abandoned");

        /// <summary>{donor}, {ship}</summary>
        public static readonly GameTextKey Recrewed = new GameTextKey(Table, "notice.recrewed");

        /// <summary>{ship}, {home} carrier</summary>
        public static readonly GameTextKey Recovered = new GameTextKey(Table, "notice.recovered");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey RecoveredWithoutHome = new GameTextKey(Table, "notice.recovered-homeless");
    }
}

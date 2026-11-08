using LastFleet.Shared.Text;

namespace LastFleet.Provisions
{
    /// <summary>Texts of the fleet's provisions in the string table "Provisions".</summary>
    public static class ProvisionsTexts
    {
        const string Table = "Provisions";

        /// <summary>{minutes}</summary>
        public static readonly GameTextKey WaterLow = new GameTextKey(Table, "notice.water-low");

        public static readonly GameTextKey WaterOut = new GameTextKey(Table, "notice.water-out");

        public static readonly GameTextKey ThirstCause = new GameTextKey(Table, "cause.thirst");

        /// <summary>{count}</summary>
        public static readonly GameTextKey ThirstDeaths = new GameTextKey(Table, "notice.thirst-deaths");

        /// <summary>{minutes} - next to the water in the supply bar.</summary>
        public static readonly GameTextKey WaterMinutesLeft = new GameTextKey(Table, "hud.water-minutes");
    }
}

using LastFleet.Shared.Text;

namespace LastFleet.RunEnd
{
    /// <summary>Texts of the end screen in the string table "RunEnd".</summary>
    public static class RunEndTexts
    {
        const string Table = "RunEnd";

        public static readonly EnumTextKeys<RunOutcome> Titles = new EnumTextKeys<RunOutcome>(Table, "title");

        /// <summary>{people}</summary>
        public static readonly GameTextKey People = new GameTextKey(Table, "stat.people");

        /// <summary>{ships}</summary>
        public static readonly GameTextKey Ships = new GameTextKey(Table, "stat.ships");

        /// <summary>{time}</summary>
        public static readonly GameTextKey Time = new GameTextKey(Table, "stat.time");

        public static readonly GameTextKey NewRunButton = new GameTextKey(Table, "button.new-run");
    }
}

using LastFleet.Shared.Text;

namespace LastFleet.RunEnd
{
    /// <summary>Texts of the end screen in the string table "RunEnd".</summary>
    public static class RunEndTexts
    {
        const string Table = "RunEnd";

        public static readonly EnumTextKeys<RunOutcome> Titles = new EnumTextKeys<RunOutcome>(Table, "title");

        /// <summary>A sentence below the title that sets the tone of the outcome.</summary>
        public static readonly EnumTextKeys<RunOutcome> Moods = new EnumTextKeys<RunOutcome>(Table, "mood");

        /// <summary>The reason of a defeat when the fleet's last capital ship falls.</summary>
        public static readonly GameTextKey CapitalShipsLost = new GameTextKey(Table, "reason.capital-ships-lost");

        /// <summary>{count}</summary>
        public static readonly GameTextKey RescuedLine = new GameTextKey(Table, "line.rescued");

        /// <summary>{count}</summary>
        public static readonly GameTextKey SurvivorsLine = new GameTextKey(Table, "line.survivors");

        /// <summary>{count}</summary>
        public static readonly GameTextKey SmallShipsGainedLine = new GameTextKey(Table, "line.small-ships-gained");

        /// <summary>{count}</summary>
        public static readonly GameTextKey CapitalShipsGainedLine = new GameTextKey(Table, "line.capital-ships-gained");

        /// <summary>{count}</summary>
        public static readonly GameTextKey CluesLine = new GameTextKey(Table, "line.clues");

        public static readonly GameTextKey VictoryLine = new GameTextKey(Table, "line.victory");

        /// <summary>{count}</summary>
        public static readonly GameTextKey PeopleLostLine = new GameTextKey(Table, "line.people-lost");

        /// <summary>{count}</summary>
        public static readonly GameTextKey ShipsLostLine = new GameTextKey(Table, "line.ships-lost");

        /// <summary>{points} - the points of one line, e.g. "+340".</summary>
        public static readonly GameTextKey LinePoints = new GameTextKey(Table, "line.points");

        /// <summary>{points}</summary>
        public static readonly GameTextKey TotalPoints = new GameTextKey(Table, "total");

        /// <summary>{ships}</summary>
        public static readonly GameTextKey Ships = new GameTextKey(Table, "stat.ships");

        /// <summary>{time}</summary>
        public static readonly GameTextKey Time = new GameTextKey(Table, "stat.time");

        /// <summary>{chain} - the quest chain that won the run.</summary>
        public static readonly GameTextKey WinningChain = new GameTextKey(Table, "winning-chain");

        /// <summary>{change} - a count that grew since the last run.</summary>
        public static readonly GameTextKey ChangeUp = new GameTextKey(Table, "compare.up");

        /// <summary>{change} - a count that shrank since the last run.</summary>
        public static readonly GameTextKey ChangeDown = new GameTextKey(Table, "compare.down");

        public static readonly GameTextKey ChangeNone = new GameTextKey(Table, "compare.same");

        public static readonly GameTextKey Record = new GameTextKey(Table, "compare.record");

        /// <summary>{points}</summary>
        public static readonly GameTextKey BestTotal = new GameTextKey(Table, "compare.best-total");

        public static readonly GameTextKey NewBestTotal = new GameTextKey(Table, "compare.new-best-total");

        public static readonly GameTextKey FirstRun = new GameTextKey(Table, "compare.first-run");

        public static readonly GameTextKey HistoryTitle = new GameTextKey(Table, "history.title");

        /// <summary>{date}, {chain}, {points}</summary>
        public static readonly GameTextKey HistoryWon = new GameTextKey(Table, "history.won");

        /// <summary>{date}, {points}</summary>
        public static readonly GameTextKey HistoryLost = new GameTextKey(Table, "history.lost");

        public static readonly GameTextKey SkipHint = new GameTextKey(Table, "hint.skip");

        public static readonly GameTextKey NewRunButton = new GameTextKey(Table, "button.new-run");
    }
}

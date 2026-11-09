using LastFleet.Shared.Text;

namespace LastFleet.Repairs
{
    /// <summary>Texts of hull repairs in the string table "Repairs".</summary>
    public static class RepairTexts
    {
        const string Table = "Repairs";

        public static readonly GameTextKey RepairAction = new GameTextKey(Table, "action.repair");
        public static readonly GameTextKey RepairFirstAction = new GameTextKey(Table, "action.repair-first");
        public static readonly GameTextKey CancelAction = new GameTextKey(Table, "action.cancel");

        public static readonly GameTextKey NothingToRepair = new GameTextKey(Table, "reason.nothing-to-repair");
        public static readonly GameTextKey OnlyInHangar = new GameTextKey(Table, "reason.only-in-hangar");
        public static readonly GameTextKey AlreadyFirst = new GameTextKey(Table, "reason.already-first");

        /// <summary>{count}</summary>
        public static readonly GameTextKey Queued = new GameTextKey(Table, "feedback.queued");

        /// <summary>{count}</summary>
        public static readonly GameTextKey MovedToFront = new GameTextKey(Table, "feedback.moved-to-front");

        /// <summary>{count}</summary>
        public static readonly GameTextKey Cancelled = new GameTextKey(Table, "feedback.cancelled");

        /// <summary>{rate} hull points per second</summary>
        public static readonly GameTextKey JobActive = new GameTextKey(Table, "line.job-active");

        /// <summary>{position} (1-based), {yard} ship name</summary>
        public static readonly GameTextKey JobWaiting = new GameTextKey(Table, "line.job-waiting");

        public static readonly GameTextKey JobShortOfMetal = new GameTextKey(Table, "line.job-no-metal");

        /// <summary>{ship}, {waiting}</summary>
        public static readonly GameTextKey YardLine = new GameTextKey(Table, "line.yard");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey Repaired = new GameTextKey(Table, "notice.repaired");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey CancelledLeftHangar = new GameTextKey(Table, "notice.left-hangar");

        public static readonly GameTextKey OutOfMetal = new GameTextKey(Table, "notice.no-metal");
    }
}

using LastFleet.Shared.Text;

namespace LastFleet.ShipControl
{
    /// <summary>Command feedback in the string table "ShipControl".</summary>
    public static class ShipControlTexts
    {
        const string Table = "ShipControl";

        /// <summary>{count}, {group}</summary>
        public static readonly GameTextKey GroupStored = new GameTextKey(Table, "notice.group-stored");

        /// <summary>{group}</summary>
        public static readonly GameTextKey GroupEmpty = new GameTextKey(Table, "notice.group-empty");

        public static readonly GameTextKey SelectShipsForSquad = new GameTextKey(Table, "notice.select-ships-for-squad");
        public static readonly GameTextKey SquadNeedsOneClass = new GameTextKey(Table, "notice.squad-needs-one-class");

        /// <summary>{squad}, {count}</summary>
        public static readonly GameTextKey SquadFormed = new GameTextKey(Table, "notice.squad-formed");

        /// <summary>{count}, {withoutHangar}</summary>
        public static readonly GameTextKey ReturningToHangar = new GameTextKey(Table, "notice.returning-to-hangar");

        /// <summary>{count}</summary>
        public static readonly GameTextKey Launching = new GameTextKey(Table, "notice.launching");

        public static readonly GameTextKey NoDockedShipsSelected = new GameTextKey(Table, "notice.no-docked-ships-selected");

        /// <summary>{count}, {withoutSlot}</summary>
        public static readonly GameTextKey HarvestersMining = new GameTextKey(Table, "notice.harvesters-mining");
    }
}

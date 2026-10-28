using LastFleet.Shared.Text;

namespace LastFleet.GenerationPreview
{
    /// <summary>
    /// Player feedback of the prototype scene in the string table "GenerationPreview". Developer
    /// shortcuts report in verbatim English instead.
    /// </summary>
    public static class GenerationPreviewTexts
    {
        const string Table = "GenerationPreview";

        /// <summary>{count}, {zone}</summary>
        public static readonly GameTextKey OrderedToZone = new GameTextKey(Table, "notice.ordered-to-zone");

        /// <summary>{system}, {known}</summary>
        public static readonly GameTextKey ArrivedInSystem = new GameTextKey(Table, "notice.arrived-in-system");

        public static readonly GameTextKey NoShipSelected = new GameTextKey(Table, "notice.no-ship-selected");

        /// <summary>{zone}</summary>
        public static readonly GameTextKey ZoneRevealed = new GameTextKey(Table, "notice.zone-revealed");

        /// <summary>{zone}</summary>
        public static readonly GameTextKey ZoneAlreadyRevealed = new GameTextKey(Table, "notice.zone-already-revealed");

        public static readonly GameTextKey ZoneUnexplored = new GameTextKey(Table, "notice.zone-unexplored");
    }
}

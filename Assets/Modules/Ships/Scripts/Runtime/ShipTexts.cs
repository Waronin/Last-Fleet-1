using LastFleet.Shared.Text;

namespace LastFleet.Ships
{
    /// <summary>Ship texts in the string table "Ships".</summary>
    public static class ShipTexts
    {
        const string Table = "Ships";

        public static readonly EnumTextKeys<ShipActivity> Activities = new EnumTextKeys<ShipActivity>(Table, "activity");
        public static readonly EnumTextKeys<ShipMotionPhase> MotionPhases = new EnumTextKeys<ShipMotionPhase>(Table, "motion");
        public static readonly EnumTextKeys<ShipSize> Sizes = new EnumTextKeys<ShipSize>(Table, "size");

        /// <summary>Display names of the ship classes; every prefab's class name must be listed here.</summary>
        public static readonly NamedTextKeys Classes = new NamedTextKeys(Table, "class", "Harvester", "Refinery", "Liner", "Shuttle");

        /// <summary>{class}, {number}</summary>
        public static readonly GameTextKey ShipName = new GameTextKey(Table, "name.ship");

        /// <summary>{class}, {number}</summary>
        public static readonly GameTextKey SquadName = new GameTextKey(Table, "name.squad");

        /// <summary>{ship}, {cause}</summary>
        public static readonly GameTextKey ShipDestroyed = new GameTextKey(Table, "notice.destroyed");

        /// <summary>{ship}, {zone}, {needed}, {left}</summary>
        public static readonly GameTextKey JumpRefusedForFuel = new GameTextKey(Table, "notice.jump-refused-fuel");
    }
}

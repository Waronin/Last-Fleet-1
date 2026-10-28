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

        /// <summary>{ship}, {cause}</summary>
        public static readonly GameTextKey ShipDestroyed = new GameTextKey(Table, "notice.destroyed");

        /// <summary>{ship}, {zone}, {needed}, {left}</summary>
        public static readonly GameTextKey JumpRefusedForFuel = new GameTextKey(Table, "notice.jump-refused-fuel");
    }
}

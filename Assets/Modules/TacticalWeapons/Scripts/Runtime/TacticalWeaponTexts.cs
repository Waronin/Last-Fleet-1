using LastFleet.Shared.Text;

namespace LastFleet.TacticalWeapons
{
    /// <summary>Texts of tactical weapons in the string table "TacticalWeapons".</summary>
    public static class TacticalWeaponTexts
    {
        const string Table = "TacticalWeapons";

        public static readonly GameTextKey FireAction = new GameTextKey(Table, "action.fire-emp");
        public static readonly GameTextKey BuildAction = new GameTextKey(Table, "action.build-emp");

        public static readonly GameTextKey NoMissile = new GameTextKey(Table, "reason.no-missile");
        public static readonly GameTextKey LauncherStunned = new GameTextKey(Table, "reason.stunned");
        public static readonly GameTextKey NotInViewedZone = new GameTextKey(Table, "reason.not-in-zone");
        public static readonly GameTextKey MagazineFull = new GameTextKey(Table, "reason.magazine-full");
        public static readonly GameTextKey AlreadyBuilding = new GameTextKey(Table, "reason.already-building");

        /// <summary>{metal}, {fuel}</summary>
        public static readonly GameTextKey NotEnoughSupplies = new GameTextKey(Table, "reason.not-enough-supplies");

        /// <summary>{count}, {capacity}</summary>
        public static readonly GameTextKey MagazineLine = new GameTextKey(Table, "line.magazine");

        /// <summary>{seconds}</summary>
        public static readonly GameTextKey BuildingLine = new GameTextKey(Table, "line.building");

        /// <summary>{seconds}</summary>
        public static readonly GameTextKey StunnedLine = new GameTextKey(Table, "line.stunned");

        public static readonly GameTextKey TargetingHint = new GameTextKey(Table, "feedback.targeting");
        public static readonly GameTextKey TargetingCancelled = new GameTextKey(Table, "feedback.targeting-cancelled");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey Launched = new GameTextKey(Table, "notice.launched");

        /// <summary>{metal}, {fuel}</summary>
        public static readonly GameTextKey BuildStarted = new GameTextKey(Table, "feedback.build-started");

        /// <summary>{ship}, {count}, {capacity}</summary>
        public static readonly GameTextKey Built = new GameTextKey(Table, "notice.built");

        /// <summary>{enemies}, {own}</summary>
        public static readonly GameTextKey Detonated = new GameTextKey(Table, "notice.detonated");
    }
}

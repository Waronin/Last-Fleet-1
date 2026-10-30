using LastFleet.Shared.Text;

namespace LastFleet.Combat
{
    /// <summary>Combat texts in the string table "Combat".</summary>
    public static class CombatTexts
    {
        const string Table = "Combat";

        /// <summary>{attacker}; completes "{ship} was destroyed by {cause}".</summary>
        public static readonly GameTextKey WeaponFireCause = new GameTextKey(Table, "cause.weapon-fire");

        /// <summary>{count}, {target}</summary>
        public static readonly GameTextKey AttackOrdered = new GameTextKey(Table, "notice.attack");

        public static readonly GameTextKey NoAttackers = new GameTextKey(Table, "notice.no-attackers");

        /// <summary>{current}, {maximum}</summary>
        public static readonly GameTextKey ShieldLine = new GameTextKey(Table, "line.shield");

        /// <summary>{target}</summary>
        public static readonly GameTextKey EngagingTask = new GameTextKey(Table, "task.engaging");
    }
}

using LastFleet.Shared.Text;

namespace LastFleet.Mining
{
    /// <summary>Mining texts in the string table "Mining".</summary>
    public static class MiningTexts
    {
        const string Table = "Mining";

        public static readonly EnumTextKeys<MiningState> States = new EnumTextKeys<MiningState>(Table, "state");

        public static readonly GameTextKey ShatterCause = new GameTextKey(Table, "cause.shatter");
        public static readonly GameTextKey GasExplosionCause = new GameTextKey(Table, "cause.gas-explosion");

        /// <summary>{zone}, {count}</summary>
        public static readonly GameTextKey ShatterReport = new GameTextKey(Table, "notice.shatter");

        /// <summary>{zone}, {count}</summary>
        public static readonly GameTextKey GasExplosionReport = new GameTextKey(Table, "notice.gas-explosion");

        /// <summary>{ship}, {zone}</summary>
        public static readonly GameTextKey NoDepositLeft = new GameTextKey(Table, "notice.no-deposit-left");

        /// <summary>{ship}, {resource}</summary>
        public static readonly GameTextKey NoStorage = new GameTextKey(Table, "notice.no-storage");

        /// <summary>{storage}, {resource}</summary>
        public static readonly GameTextKey StorageFull = new GameTextKey(Table, "notice.storage-full");

        /// <summary>{ship}, {needed}, {left}</summary>
        public static readonly GameTextKey StoppedForFuel = new GameTextKey(Table, "notice.stopped-for-fuel");
    }
}

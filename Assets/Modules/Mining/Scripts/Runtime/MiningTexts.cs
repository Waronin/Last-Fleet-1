using LastFleet.Shared.Text;

namespace LastFleet.Mining
{
    /// <summary>Mining texts in the string table "Mining".</summary>
    public static class MiningTexts
    {
        const string Table = "Mining";

        public static readonly EnumTextKeys<MiningState> States = new EnumTextKeys<MiningState>(Table, "state");

        /// <summary>Supply names, e.g. "Metal".</summary>
        public static readonly EnumTextKeys<Supply> Supplies = new EnumTextKeys<Supply>(Table, "supply");

        /// <summary>{supply}, {amount}, {capacity}</summary>
        public static readonly GameTextKey SupplyAmount = new GameTextKey(Table, "supply-amount");

        public static readonly GameTextKey ShatterCause = new GameTextKey(Table, "cause.shatter");
        public static readonly GameTextKey GasExplosionCause = new GameTextKey(Table, "cause.gas-explosion");
        public static readonly GameTextKey RadiationCause = new GameTextKey(Table, "cause.radiation");

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

        /// <summary>{target}</summary>
        public static readonly GameTextKey RefuelingTask = new GameTextKey(Table, "task.refueling");

        /// <summary>{tanker}, {target}</summary>
        public static readonly GameTextKey RefuelingStarted = new GameTextKey(Table, "notice.refueling-started");

        /// <summary>{tanker}, {target}</summary>
        public static readonly GameTextKey RefuelingQueued = new GameTextKey(Table, "notice.refueling-queued");

        /// <summary>{target}</summary>
        public static readonly GameTextKey Refueled = new GameTextKey(Table, "notice.refueled");

        /// <summary>{target}</summary>
        public static readonly GameTextKey AlreadyFull = new GameTextKey(Table, "notice.already-full");

        /// <summary>{tanker}</summary>
        public static readonly GameTextKey TankerEmpty = new GameTextKey(Table, "notice.tanker-empty");

        public static readonly GameTextKey RefuelAction = new GameTextKey(Table, "action.refuel");
        public static readonly GameTextKey RefuelNeedsTarget = new GameTextKey(Table, "action.refuel.needs-target");
        public static readonly GameTextKey RefuelSelfAction = new GameTextKey(Table, "action.refuel-self");
        public static readonly GameTextKey StopRefuelingAction = new GameTextKey(Table, "action.stop-refueling");
    }
}

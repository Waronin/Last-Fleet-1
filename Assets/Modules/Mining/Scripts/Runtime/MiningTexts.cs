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

        /// <summary>{ship}, {waiting}</summary>
        public static readonly GameTextKey TankerLine = new GameTextKey(Table, "refuel.tanker-line");

        /// <summary>{tanker}</summary>
        public static readonly GameTextKey RefuelJobActive = new GameTextKey(Table, "refuel.job-active");

        /// <summary>{position}, {tanker}</summary>
        public static readonly GameTextKey RefuelJobWaiting = new GameTextKey(Table, "refuel.job-waiting");

        /// <summary>{tanker}</summary>
        public static readonly GameTextKey RefuelJobShortOfFuel = new GameTextKey(Table, "refuel.job-short");

        /// <summary>{target}</summary>
        public static readonly GameTextKey Refueled = new GameTextKey(Table, "notice.refueled");

        /// <summary>{tanker}</summary>
        public static readonly GameTextKey TankerEmpty = new GameTextKey(Table, "notice.tanker-empty");

        /// <summary>{ship}, {tanker}</summary>
        public static readonly GameTextKey RefuelCancelledLeftZone = new GameTextKey(Table, "notice.refuel-left-zone");

        public static readonly GameTextKey RefuelAction = new GameTextKey(Table, "action.refuel");
        public static readonly GameTextKey RefuelFirstAction = new GameTextKey(Table, "action.refuel-first");
        public static readonly GameTextKey CancelRefuelAction = new GameTextKey(Table, "action.cancel-refuel");
        public static readonly GameTextKey NoTankerInZone = new GameTextKey(Table, "action.refuel.no-tanker");
        public static readonly GameTextKey TankFull = new GameTextKey(Table, "action.refuel.tank-full");
        public static readonly GameTextKey AlreadyWaitingForFuel = new GameTextKey(Table, "action.refuel.already-queued");
        public static readonly GameTextKey AlreadyRefueledFirst = new GameTextKey(Table, "action.refuel.already-first");

        /// <summary>{count}</summary>
        public static readonly GameTextKey RefuelOrdered = new GameTextKey(Table, "action.refuel.done");

        /// <summary>{count}</summary>
        public static readonly GameTextKey RefuelMovedToFront = new GameTextKey(Table, "action.refuel-first.done");

        /// <summary>{count}</summary>
        public static readonly GameTextKey RefuelCancelled = new GameTextKey(Table, "action.cancel-refuel.done");
    }
}

using LastFleet.Shared.Text;

namespace LastFleet.FleetJump
{
    /// <summary>Fleet jump texts in the string table "FleetJump". Times are preformatted as "m:ss".</summary>
    public static class FleetJumpTexts
    {
        const string Table = "FleetJump";

        /// <summary>{remaining}</summary>
        public static readonly GameTextKey CoolingDownStatus = new GameTextKey(Table, "status.cooling-down");

        public static readonly GameTextKey ReadyStatus = new GameTextKey(Table, "status.ready");

        /// <summary>{system}, {remaining}</summary>
        public static readonly GameTextKey ChargingStatus = new GameTextKey(Table, "status.charging");

        public static readonly GameTextKey CancelButton = new GameTextKey(Table, "button.cancel");
        public static readonly GameTextKey ConfirmButton = new GameTextKey(Table, "button.confirm");

        /// <summary>{system}, {distance}</summary>
        public static readonly GameTextKey DialogTitle = new GameTextKey(Table, "dialog.title");

        /// <summary>{ship}, {cost}, {left}, {jumps}</summary>
        public static readonly GameTextKey DialogJumpingLine = new GameTextKey(Table, "dialog.jumping");

        /// <summary>{ship}, {cost}, {left}</summary>
        public static readonly GameTextKey DialogOutOfFuelLine = new GameTextKey(Table, "dialog.out-of-fuel");

        /// <summary>{count}</summary>
        public static readonly GameTextKey DialogSmallShipsHeader = new GameTextKey(Table, "dialog.small-ships");

        public static readonly GameTextKey DialogNobodyLeftBehind = new GameTextKey(Table, "dialog.nobody-left-behind");

        public static readonly GameTextKey DialogNobodyCanJump = new GameTextKey(Table, "dialog.nobody-can-jump");

        /// <summary>{remaining}</summary>
        public static readonly GameTextKey StillCoolingDown = new GameTextKey(Table, "notice.cooling-down");

        /// <summary>{system}</summary>
        public static readonly GameTextKey OutOfRange = new GameTextKey(Table, "notice.out-of-range");

        /// <summary>{system}, {remaining}</summary>
        public static readonly GameTextKey ChargeStarted = new GameTextKey(Table, "notice.charge-started");

        public static readonly GameTextKey ChargeCancelled = new GameTextKey(Table, "notice.charge-cancelled");

        public static readonly GameTextKey ChargeInProgress = new GameTextKey(Table, "notice.charge-in-progress");

        /// <summary>{system}, {count}</summary>
        public static readonly GameTextKey Jumped = new GameTextKey(Table, "notice.jumped");

        public static readonly GameTextKey JumpFailed = new GameTextKey(Table, "notice.jump-failed");

        public static readonly GameTextKey LeftBehindCause = new GameTextKey(Table, "cause.left-behind");
    }
}

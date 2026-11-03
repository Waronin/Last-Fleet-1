using LastFleet.Shared.Text;

namespace LastFleet.Pursuit
{
    /// <summary>Pursuit texts in the string table "Pursuit". Times are preformatted as "m:ss".</summary>
    public static class PursuitTexts
    {
        const string Table = "Pursuit";

        /// <summary>{remaining}</summary>
        public static readonly GameTextKey CountingStatus = new GameTextKey(Table, "status.counting");

        /// <summary>{remaining}</summary>
        public static readonly GameTextKey WarningStatus = new GameTextKey(Table, "status.warning");

        public static readonly GameTextKey ArrivedStatus = new GameTextKey(Table, "status.arrived");

        /// <summary>{remaining}</summary>
        public static readonly GameTextKey CountdownStarted = new GameTextKey(Table, "notice.countdown-started");

        /// <summary>{remaining}</summary>
        public static readonly GameTextKey WarningIssued = new GameTextKey(Table, "notice.warning");

        /// <summary>{ship}, {zone}</summary>
        public static readonly GameTextKey CarrierArrived = new GameTextKey(Table, "notice.carrier-arrived");

        /// <summary>{ship}, {count}</summary>
        public static readonly GameTextKey CarrierWave = new GameTextKey(Table, "notice.carrier-wave");

        /// <summary>{ship}, {zone}</summary>
        public static readonly GameTextKey CarrierStrike = new GameTextKey(Table, "notice.carrier-strike");
    }
}

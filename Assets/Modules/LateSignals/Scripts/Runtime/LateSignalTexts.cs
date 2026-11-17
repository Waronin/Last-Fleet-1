using LastFleet.Shared.Text;

namespace LastFleet.LateSignals
{
    /// <summary>Texts of late signals in the string table "LateSignals".</summary>
    public static class LateSignalTexts
    {
        const string Table = "LateSignals";

        /// <summary>{zone}</summary>
        public static readonly GameTextKey DistressCall = new GameTextKey(Table, "notice.distress-call");

        /// <summary>{zone}</summary>
        public static readonly GameTextKey RadioSilenceBroken = new GameTextKey(Table, "notice.radio-silence");

        /// <summary>{zone}</summary>
        public static readonly GameTextKey SignalPickedUp = new GameTextKey(Table, "notice.signal");
    }
}

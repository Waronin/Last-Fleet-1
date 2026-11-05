using LastFleet.Shared.Text;

namespace LastFleet.Quests
{
    /// <summary>Texts of the quest system and its reusable steps in the string table "Quests".</summary>
    public static class QuestTexts
    {
        const string Table = "Quests";

        /// <summary>{chain}, {step}, {count} — on the galaxy map and in the jump warning.</summary>
        public static readonly GameTextKey Marker = new GameTextKey(Table, "marker");

        public static readonly GameTextKey LogHeader = new GameTextKey(Table, "log.header");

        /// <summary>{chain}, {step}, {count}, {title}</summary>
        public static readonly GameTextKey LogStep = new GameTextKey(Table, "log.step");

        /// <summary>{system}</summary>
        public static readonly GameTextKey LogSystem = new GameTextKey(Table, "log.system");

        public static readonly GameTextKey LogHere = new GameTextKey(Table, "log.here");

        /// <summary>{zone}</summary>
        public static readonly GameTextKey LogZone = new GameTextKey(Table, "log.zone");

        public static readonly GameTextKey LogNotPlayable = new GameTextKey(Table, "log.not-playable");

        /// <summary>{chain}</summary>
        public static readonly GameTextKey LogCompleted = new GameTextKey(Table, "log.completed");

        /// <summary>{chain}</summary>
        public static readonly GameTextKey LogFailed = new GameTextKey(Table, "log.failed");

        /// <summary>{chain}, {step}, {title}, {system}</summary>
        public static readonly GameTextKey StepPlaced = new GameTextKey(Table, "notice.step-placed");

        /// <summary>{chain}, {title}</summary>
        public static readonly GameTextKey StepCompleted = new GameTextKey(Table, "notice.step-completed");

        /// <summary>{chain}</summary>
        public static readonly GameTextKey ChainCompleted = new GameTextKey(Table, "notice.chain-completed");

        /// <summary>{chain}, {reason}</summary>
        public static readonly GameTextKey ChainFailed = new GameTextKey(Table, "notice.chain-failed");

        /// <summary>{chain}, {title}, {reason}</summary>
        public static readonly GameTextKey StepRetry = new GameTextKey(Table, "notice.step-retry");

        /// <summary>{station}</summary>
        public static readonly GameTextKey ContactAnswered = new GameTextKey(Table, "notice.contact-answered");

        /// <summary>{remaining}</summary>
        public static readonly GameTextKey FuseStatus = new GameTextKey(Table, "status.fuse");

        /// <summary>{station}</summary>
        public static readonly GameTextKey TrapTriggered = new GameTextKey(Table, "notice.trap-triggered");

        /// <summary>{station}, {count}</summary>
        public static readonly GameTextKey StationExploded = new GameTextKey(Table, "notice.station-exploded");

        public static readonly GameTextKey StationExplosionCause = new GameTextKey(Table, "cause.station-explosion");

        /// <summary>{station}</summary>
        public static readonly GameTextKey EscortHailStatus = new GameTextKey(Table, "status.escort-hail");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey EscortUnderwayStatus = new GameTextKey(Table, "status.escort-underway");

        /// <summary>{ship}, {station}</summary>
        public static readonly GameTextKey EscortLaunched = new GameTextKey(Table, "notice.escort-launched");

        /// <summary>{count}, {ship}</summary>
        public static readonly GameTextKey EscortAttacked = new GameTextKey(Table, "notice.escort-attacked");

        /// <summary>{ship}, {carrier}</summary>
        public static readonly GameTextKey EscortArrived = new GameTextKey(Table, "notice.escort-arrived");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey EscortLost = new GameTextKey(Table, "reason.escort-lost");
    }
}

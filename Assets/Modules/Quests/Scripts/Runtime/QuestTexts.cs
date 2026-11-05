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

        /// <summary>{item}, {ship}</summary>
        public static readonly GameTextKey ItemAboard = new GameTextKey(Table, "notice.item-aboard");

        /// <summary>{item}, {ship}</summary>
        public static readonly GameTextKey ItemLost = new GameTextKey(Table, "reason.item-lost");

        /// <summary>{item}</summary>
        public static readonly GameTextKey ItemMissing = new GameTextKey(Table, "reason.item-missing");

        /// <summary>{station}</summary>
        public static readonly GameTextKey MineRockStatus = new GameTextKey(Table, "status.mine-rock");

        /// <summary>{item}, {percent}</summary>
        public static readonly GameTextKey MiningItemStatus = new GameTextKey(Table, "status.mining-item");

        /// <summary>{item}, {ship}</summary>
        public static readonly GameTextKey ItemEscapeStatus = new GameTextKey(Table, "status.item-escape");

        /// <summary>{count}, {station}</summary>
        public static readonly GameTextKey GuardsLaunched = new GameTextKey(Table, "notice.guards-launched");

        /// <summary>{item}, {ship}</summary>
        public static readonly GameTextKey ItemSecured = new GameTextKey(Table, "notice.item-secured");

        /// <summary>{item}, {ship}</summary>
        public static readonly GameTextKey ItemLostRetry = new GameTextKey(Table, "notice.item-lost-retry");

        /// <summary>{item}</summary>
        public static readonly GameTextKey RockMinedOut = new GameTextKey(Table, "reason.rock-mined-out");

        /// <summary>{item}, {ship}, {station}</summary>
        public static readonly GameTextKey DeliverItemStatus = new GameTextKey(Table, "status.deliver-item");

        /// <summary>{remaining}, {docked}, {dose}</summary>
        public static readonly GameTextKey ProcessingStatus = new GameTextKey(Table, "status.processing");

        /// <summary>{item}, {ship}</summary>
        public static readonly GameTextKey ItemHomeStatus = new GameTextKey(Table, "status.item-home");

        /// <summary>{item}, {ship}, {station}</summary>
        public static readonly GameTextKey ItemHandedOver = new GameTextKey(Table, "notice.item-handed-over");

        /// <summary>{item}, {station}</summary>
        public static readonly GameTextKey ProcessingDone = new GameTextKey(Table, "notice.processing-done");

        /// <summary>{item}, {ship}</summary>
        public static readonly GameTextKey ItemHome = new GameTextKey(Table, "notice.item-home");

        /// <summary>{ship}, {station}</summary>
        public static readonly GameTextKey RadiationWarning = new GameTextKey(Table, "notice.radiation-warning");

        public static readonly GameTextKey RadiationCause = new GameTextKey(Table, "cause.radiation");

        /// <summary>{station}, {item}</summary>
        public static readonly GameTextKey CollectItemStatus = new GameTextKey(Table, "status.collect-item");

        /// <summary>{ship}, {station}, {item}</summary>
        public static readonly GameTextKey ItemReturned = new GameTextKey(Table, "notice.item-returned");

        /// <summary>{ship}, {item}</summary>
        public static readonly GameTextKey ItemCrewLost = new GameTextKey(Table, "reason.item-crew-lost");
    }
}

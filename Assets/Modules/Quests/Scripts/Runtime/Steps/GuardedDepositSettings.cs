using System;
using LastFleet.Dialogue;
using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>What a <see cref="GuardedDepositStep"/> is about: the guarding station, its rock, the item and the guards.</summary>
    public sealed class GuardedDepositSettings
    {
        public GuardedDepositSettings(QuestStationSpec station, ResourceKind rockResource, float rockRadiusUnits, float rockAmount,
            QuestItem item, float miningSecondsForItem, int guardCount, float guardRadiusUnits,
            Func<ZoneStation, DialogueScript> warning, Func<ZoneStation, DialogueScript> outrage, Func<ShipAgent, DialogueScript> secured)
        {
            if (rockRadiusUnits <= 0f || rockAmount <= 0f || miningSecondsForItem <= 0f || guardCount < 0 || guardRadiusUnits <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rockRadiusUnits), "Rock size and amount, mining time and guard radius must be positive.");
            }
            Station = station ?? throw new ArgumentNullException(nameof(station));
            RockResource = rockResource;
            RockRadiusUnits = rockRadiusUnits;
            RockAmount = rockAmount;
            Item = item ?? throw new ArgumentNullException(nameof(item));
            MiningSecondsForItem = miningSecondsForItem;
            GuardCount = guardCount;
            GuardRadiusUnits = guardRadiusUnits;
            Warning = warning ?? throw new ArgumentNullException(nameof(warning));
            Outrage = outrage ?? throw new ArgumentNullException(nameof(outrage));
            Secured = secured;
        }

        public QuestStationSpec Station { get; }
        public ResourceKind RockResource { get; }
        public float RockRadiusUnits { get; }

        /// <summary>Large enough that the rock outlasts a few lost attempts.</summary>
        public float RockAmount { get; }

        public QuestItem Item { get; }

        /// <summary>Harvester-seconds of mining at the rock until the item is aboard the miner.</summary>
        public float MiningSecondsForItem { get; }

        public int GuardCount { get; }

        /// <summary>How far around the rock the guards go for fleet ships.</summary>
        public float GuardRadiusUnits { get; }

        /// <summary>What the station says when hailed before anyone touched the rock.</summary>
        public Func<ZoneStation, DialogueScript> Warning { get; }

        /// <summary>The radio call when the guards launch, and the answer to hails afterwards.</summary>
        public Func<ZoneStation, DialogueScript> Outrage { get; }

        /// <summary>Optional radio call once the item is safe.</summary>
        public Func<ShipAgent, DialogueScript> Secured { get; }
    }
}

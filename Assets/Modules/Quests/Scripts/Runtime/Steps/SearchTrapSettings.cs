using System;
using LastFleet.Dialogue;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>Numbers and texts of a <see cref="SearchTrapStep"/>.</summary>
    public sealed class SearchTrapSettings
    {
        public SearchTrapSettings(QuestStationSpec station, float fuseSeconds, float blastDamage, int wreckCount, float metalPerWreck,
            Func<ZoneStation, DialogueScript> triggeredMessage)
        {
            Station = station ?? throw new ArgumentNullException(nameof(station));
            if (fuseSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fuseSeconds), "The fuse needs a positive time.");
            }
            if (blastDamage < 0f || wreckCount < 0 || metalPerWreck <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(blastDamage), "Blast damage, wrecks and metal must not be negative.");
            }
            FuseSeconds = fuseSeconds;
            BlastDamage = blastDamage;
            WreckCount = wreckCount;
            MetalPerWreck = metalPerWreck;
            TriggeredMessage = triggeredMessage ?? throw new ArgumentNullException(nameof(triggeredMessage));
        }

        /// <summary>The station to search; it must be searchable (empty, walkable).</summary>
        public QuestStationSpec Station { get; }

        public float FuseSeconds { get; }

        /// <summary>Hull damage to every ship flying in the zone when it goes off; hazards bypass shields.</summary>
        public float BlastDamage { get; }

        public int WreckCount { get; }
        public float MetalPerWreck { get; }

        /// <summary>The radio call when the trap goes off, e.g. what the search party found.</summary>
        public Func<ZoneStation, DialogueScript> TriggeredMessage { get; }
    }
}

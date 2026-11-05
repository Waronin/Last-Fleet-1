using System;
using LastFleet.Dialogue;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// What a <see cref="StationProcessingStep"/> is about: the station, the item that goes in, the item
    /// that comes out (if any), how long the work takes and, for a radioactive station, how its radiation
    /// builds up in docked crews.
    /// </summary>
    public sealed class StationProcessingSettings
    {
        public StationProcessingSettings(QuestStationSpec station, QuestItem input, QuestItem output, GameTextKey workName, float processingSeconds,
            DockedRadiationSettings radiation, Func<ZoneStation, DialogueScript> briefing, Func<ZoneStation, ShipAgent, DialogueScript> received,
            Func<ZoneStation, ShipAgent, DialogueScript> finished)
        {
            if (processingSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(processingSeconds), "Processing needs a positive time.");
            }
            Station = station ?? throw new ArgumentNullException(nameof(station));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Output = output;
            WorkName = workName;
            ProcessingSeconds = processingSeconds;
            Radiation = radiation;
            Briefing = briefing ?? throw new ArgumentNullException(nameof(briefing));
            Received = received;
            Finished = finished;
        }

        public QuestStationSpec Station { get; }
        public QuestItem Input { get; }

        /// <summary>The item the work produces, or null: then the step is done as soon as the work is.</summary>
        public QuestItem Output { get; }

        /// <summary>What the work is called in the quest log, e.g. "Refining".</summary>
        public GameText WorkName { get; }

        /// <summary>Work time; it only runs while at least one fleet ship with a crew is docked.</summary>
        public float ProcessingSeconds { get; }

        /// <summary>The station's radiation, or null for a station without it.</summary>
        public DockedRadiationSettings Radiation { get; }

        /// <summary>The station's answer to a hail.</summary>
        public Func<ZoneStation, DialogueScript> Briefing { get; }

        /// <summary>Optional radio call when the input item is handed over.</summary>
        public Func<ZoneStation, ShipAgent, DialogueScript> Received { get; }

        /// <summary>Optional radio call when the output item is aboard a ship.</summary>
        public Func<ZoneStation, ShipAgent, DialogueScript> Finished { get; }
    }
}

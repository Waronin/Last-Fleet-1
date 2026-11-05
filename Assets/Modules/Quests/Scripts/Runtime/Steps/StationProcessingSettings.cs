using System;
using LastFleet.Dialogue;
using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// What a <see cref="StationProcessingStep"/> is about: the station, the item that goes in, the item
    /// that comes out, how long the work takes and how its radiation builds up in docked crews.
    /// </summary>
    public sealed class StationProcessingSettings
    {
        public StationProcessingSettings(QuestStationSpec station, QuestItem input, QuestItem output, float processingSeconds,
            float radiationSecondsToLimit, float radiationSecondsToRecover, float radiationDeathIntervalSeconds, float radiationWarningLevel,
            Func<ZoneStation, DialogueScript> briefing, Func<ZoneStation, ShipAgent, DialogueScript> received,
            Func<ZoneStation, ShipAgent, DialogueScript> finished)
        {
            if (processingSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(processingSeconds), "Processing needs a positive time.");
            }
            if (radiationWarningLevel <= 0f || radiationWarningLevel >= 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(radiationWarningLevel), "The warning comes before the limit.");
            }
            Station = station ?? throw new ArgumentNullException(nameof(station));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Output = output ?? throw new ArgumentNullException(nameof(output));
            ProcessingSeconds = processingSeconds;
            RadiationSecondsToLimit = radiationSecondsToLimit;
            RadiationSecondsToRecover = radiationSecondsToRecover;
            RadiationDeathIntervalSeconds = radiationDeathIntervalSeconds;
            RadiationWarningLevel = radiationWarningLevel;
            Briefing = briefing ?? throw new ArgumentNullException(nameof(briefing));
            Received = received;
            Finished = finished;
        }

        public QuestStationSpec Station { get; }
        public QuestItem Input { get; }
        public QuestItem Output { get; }

        /// <summary>Work time; it only runs while at least one fleet ship is docked.</summary>
        public float ProcessingSeconds { get; }

        /// <summary>Docked time from a clean crew to the limit, where crew members start to die.</summary>
        public float RadiationSecondsToLimit { get; }

        /// <summary>Time away from a full dose back to clean.</summary>
        public float RadiationSecondsToRecover { get; }

        /// <summary>At the limit, one crew member dies per interval of further exposure.</summary>
        public float RadiationDeathIntervalSeconds { get; }

        /// <summary>Dose level (0 to 1) at which the player is warned to undock.</summary>
        public float RadiationWarningLevel { get; }

        public RadiationDose CreateDose()
        {
            return new RadiationDose(RadiationSecondsToLimit, RadiationSecondsToRecover, RadiationDeathIntervalSeconds);
        }

        /// <summary>The station's answer to a hail.</summary>
        public Func<ZoneStation, DialogueScript> Briefing { get; }

        /// <summary>Optional radio call when the input item is handed over.</summary>
        public Func<ZoneStation, ShipAgent, DialogueScript> Received { get; }

        /// <summary>Optional radio call when the output item is aboard a ship.</summary>
        public Func<ZoneStation, ShipAgent, DialogueScript> Finished { get; }
    }
}

using System;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// A station a quest step needs, as its author writes it: a proper name, the type and the hidden
    /// truth about it. Where it lies is decided when the step is placed (<see cref="QuestStationSite"/>).
    /// </summary>
    public sealed class QuestStationSpec
    {
        public QuestStationSpec(string name, StationType type, StationProfile profile)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A quest station needs a name.", nameof(name));
            }
            Name = name;
            Type = type;
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        /// <summary>A proper name, shown as it is in every language.</summary>
        public string Name { get; }

        public StationType Type { get; }
        public StationProfile Profile { get; }
    }
}

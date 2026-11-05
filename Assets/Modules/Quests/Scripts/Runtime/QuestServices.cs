using LastFleet.Dialogue;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// The scene systems quest steps act on, handed over explicitly by the quest director rather than
    /// looked up. Steps only use what they need; nothing here is owned by the quests.
    /// </summary>
    public sealed class QuestServices
    {
        public QuestServices(ShipRegistry fleet, ShipRegistry pronin, ZoneWorld zoneWorld, SearchService search, DialogueService dialogue,
            QuestPrefabs prefabs)
        {
            Fleet = fleet;
            Pronin = pronin;
            ZoneWorld = zoneWorld;
            Search = search;
            Dialogue = dialogue;
            Prefabs = prefabs;
        }

        public ShipRegistry Fleet { get; }
        public ShipRegistry Pronin { get; }
        public ZoneWorld ZoneWorld { get; }
        public SearchService Search { get; }
        public DialogueService Dialogue { get; }
        public QuestPrefabs Prefabs { get; }
    }
}

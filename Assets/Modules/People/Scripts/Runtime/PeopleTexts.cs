using LastFleet.Shared.Text;

namespace LastFleet.People
{
    /// <summary>Population texts in the string table "People".</summary>
    public static class PeopleTexts
    {
        const string Table = "People";

        /// <summary>{count}, {ship}, {cause}</summary>
        public static readonly GameTextKey CasualtiesFromHit = new GameTextKey(Table, "notice.casualties-hit");

        /// <summary>{count}, {ship}</summary>
        public static readonly GameTextKey LostWithShip = new GameTextKey(Table, "notice.lost-with-ship");

        /// <summary>{count}, {ship}, {carrier}</summary>
        public static readonly GameTextKey Disembarked = new GameTextKey(Table, "notice.disembarked");

        /// <summary>{count}, {capacity}</summary>
        public static readonly GameTextKey PassengersLine = new GameTextKey(Table, "line.passengers");

        public static readonly GameTextKey Overcrowded = new GameTextKey(Table, "line.overcrowded");

        /// <summary>{count}, {capacity}</summary>
        public static readonly GameTextKey FleetPopulationLine = new GameTextKey(Table, "line.fleet-population");
    }
}

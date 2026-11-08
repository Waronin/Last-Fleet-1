using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>A station's request together with where it is and who speaks for it.</summary>
    public sealed class StationRequestEntry
    {
        public StationRequestEntry(ZoneStation station, SupplyRequest request, SupplyRequestContact contact, SystemZones system)
        {
            Station = station;
            Request = request;
            Contact = contact;
            System = system;
        }

        public ZoneStation Station { get; }
        public SupplyRequest Request { get; }
        public SupplyRequestContact Contact { get; }

        /// <summary>The star system the request belongs to; it expires when the fleet is elsewhere.</summary>
        public SystemZones System { get; }
    }
}

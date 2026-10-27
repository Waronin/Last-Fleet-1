using LastFleet.StarSystem;

namespace LastFleet.Zones
{
    /// <summary>
    /// A zone of a star system. Until it is revealed it only has its point of interest; revealing it
    /// generates the contents once, from the zone's own seed, so the reveal order does not matter.
    /// </summary>
    public sealed class ZoneRecord
    {
        readonly Star _star;

        public ZoneRecord(int pointOfInterestIndex, PointOfInterest pointOfInterest, Star star, ulong seed)
        {
            PointOfInterestIndex = pointOfInterestIndex;
            PointOfInterest = pointOfInterest;
            _star = star;
            Seed = seed;
        }

        public int PointOfInterestIndex { get; }
        public PointOfInterest PointOfInterest { get; }
        public ulong Seed { get; }

        /// <summary>Null until the zone is revealed.</summary>
        public ZoneContents Contents { get; private set; }

        public bool IsRevealed => Contents != null;

        /// <summary>Reveals the zone (ship arrival or detail scan). Calling it again has no effect.</summary>
        public ZoneContents Reveal()
        {
            if (Contents == null)
            {
                Contents = ZoneContentGenerator.Generate(Seed, PointOfInterest, _star);
            }
            return Contents;
        }
    }
}

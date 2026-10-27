using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using LastFleet.StarSystem;

namespace LastFleet.Zones
{
    /// <summary>All zones of one star system, one per point of interest, in point-of-interest order.</summary>
    public sealed class SystemZones
    {
        const ulong ZoneSeedSalt = 5000UL;

        readonly ZoneRecord[] _zones;

        public SystemZones(int systemIndex, StarSystemLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            SystemIndex = systemIndex;
            Layout = layout;
            _zones = new ZoneRecord[layout.PointsOfInterest.Count];
            for (int i = 0; i < _zones.Length; i++)
            {
                ulong zoneSeed = DeterministicRandom.DeriveSeed(layout.Seed, ZoneSeedSalt + (ulong)i);
                _zones[i] = new ZoneRecord(i, layout.PointsOfInterest[i], layout.Star, zoneSeed);
            }
        }

        public int SystemIndex { get; }
        public StarSystemLayout Layout { get; }
        public IReadOnlyList<ZoneRecord> Zones => _zones;

        public int RevealedCount
        {
            get
            {
                int count = 0;
                foreach (ZoneRecord zone in _zones)
                {
                    if (zone.IsRevealed)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }
}

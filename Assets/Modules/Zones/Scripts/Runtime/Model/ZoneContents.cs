using System;
using System.Collections.Generic;

namespace LastFleet.Zones
{
    /// <summary>
    /// Everything inside a revealed zone. Deposits and stations are run state and can be added during
    /// the run, e.g. wrecks after a fight or a quest station; the rest is fixed.
    /// </summary>
    public sealed class ZoneContents
    {
        readonly List<ResourceDeposit> _deposits;
        readonly List<ZoneStation> _stations;

        public ZoneContents(float radiusKlicks, ZoneBackdrop backdrop, IReadOnlyList<ResourceDeposit> deposits, IReadOnlyList<ZoneStation> stations)
        {
            RadiusKlicks = radiusKlicks;
            Backdrop = backdrop;
            _deposits = new List<ResourceDeposit>(deposits);
            _stations = new List<ZoneStation>(stations);
        }

        public float RadiusKlicks { get; }
        public ZoneBackdrop Backdrop { get; }
        public IReadOnlyList<ResourceDeposit> Deposits => _deposits;
        public IReadOnlyList<ZoneStation> Stations => _stations;

        /// <summary>Adds a deposit that appeared during the run; it stays for the rest of the run like any other.</summary>
        public void AddDeposit(ResourceDeposit deposit)
        {
            if (deposit == null)
            {
                throw new ArgumentNullException(nameof(deposit));
            }
            _deposits.Add(deposit);
        }

        /// <summary>Adds a station placed during the run, e.g. by a quest; find its spot with <see cref="ZoneContentGenerator.TryFindFreeSpot"/>.</summary>
        public void AddStation(ZoneStation station)
        {
            if (station == null)
            {
                throw new ArgumentNullException(nameof(station));
            }
            if (_stations.Contains(station))
            {
                throw new InvalidOperationException($"{station.Name} is already in this zone.");
            }
            _stations.Add(station);
        }
    }
}

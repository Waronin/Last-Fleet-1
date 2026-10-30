using System;
using System.Collections.Generic;

namespace LastFleet.Zones
{
    /// <summary>
    /// Everything inside a revealed zone. Deposits are mutable run state and can be added during the
    /// run, e.g. wrecks after a fight; the rest is fixed.
    /// </summary>
    public sealed class ZoneContents
    {
        readonly List<ResourceDeposit> _deposits;
        readonly ZoneStation[] _stations;

        public ZoneContents(float radiusKlicks, ZoneBackdrop backdrop, IReadOnlyList<ResourceDeposit> deposits, IReadOnlyList<ZoneStation> stations)
        {
            RadiusKlicks = radiusKlicks;
            Backdrop = backdrop;
            _deposits = new List<ResourceDeposit>(deposits);
            _stations = new ZoneStation[stations.Count];
            for (int i = 0; i < stations.Count; i++)
            {
                _stations[i] = stations[i];
            }
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
    }
}

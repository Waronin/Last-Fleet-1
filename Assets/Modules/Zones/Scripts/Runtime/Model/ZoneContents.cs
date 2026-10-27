using System.Collections.Generic;

namespace LastFleet.Zones
{
    /// <summary>Everything inside a revealed zone. Deposits are mutable run state, the rest is fixed.</summary>
    public sealed class ZoneContents
    {
        readonly ResourceDeposit[] _deposits;
        readonly ZoneStation[] _stations;

        public ZoneContents(float radiusKlicks, ZoneBackdrop backdrop, IReadOnlyList<ResourceDeposit> deposits, IReadOnlyList<ZoneStation> stations)
        {
            RadiusKlicks = radiusKlicks;
            Backdrop = backdrop;
            _deposits = new ResourceDeposit[deposits.Count];
            for (int i = 0; i < deposits.Count; i++)
            {
                _deposits[i] = deposits[i];
            }
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
    }
}

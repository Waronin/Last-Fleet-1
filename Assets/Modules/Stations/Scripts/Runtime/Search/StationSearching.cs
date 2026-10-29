using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Stations
{
    /// <summary>
    /// The crew of a docked ship searching the station, one find after another (GDD: the longer the
    /// search, the more turns up; it binds ship and crew). A short-handed crew searches more slowly.
    /// The search ends when the ship undocks, the station is empty or the search service stops it.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(StationDocking))]
    public sealed class StationSearching : MonoBehaviour, IShipAutomation
    {
        ShipAgent _ship;
        StationDocking _docking;
        SearchService _service;
        float _progressSeconds;

        public ShipAgent Ship => _ship;
        public ZoneStation Station { get; private set; }
        public bool IsSearching => Station != null;
        public bool IsAutomationActive => IsSearching;

        /// <summary>Share of the current find already done, 0 to 1.</summary>
        public float FindProgress => _progressSeconds / StationSearchRules.SecondsPerFind;

        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _docking = GetComponent<StationDocking>();
        }

        internal void Begin(SearchService service, ZoneStation station)
        {
            _service = service;
            Station = station;
            _progressSeconds = 0f;
        }

        public void Stop()
        {
            Station = null;
            _progressSeconds = 0f;
        }

        void Update()
        {
            if (!IsSearching)
            {
                return;
            }
            // Undocked, moved on or lost the crew: the search party is gone.
            if (_docking.State != DockingState.Docked || _docking.Station != Station || _ship.IsDerelict)
            {
                Stop();
                return;
            }

            _progressSeconds += Time.deltaTime * _ship.WorkEfficiency;
            if (_progressSeconds >= StationSearchRules.SecondsPerFind)
            {
                _progressSeconds -= StationSearchRules.SecondsPerFind;
                _service.TakeFind(this);
            }
        }
    }
}

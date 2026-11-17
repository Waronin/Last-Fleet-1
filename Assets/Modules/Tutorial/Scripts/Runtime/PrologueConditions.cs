using System;
using System.Collections.Generic;
using LastFleet.Combat;
using LastFleet.Mining;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;

namespace LastFleet.Tutorial
{
    /// <summary>
    /// What the prologue's objectives check, read from the fleet and the current system. Only real
    /// stations count; stranded hulks and destroyed stations do not.
    /// </summary>
    public sealed class PrologueConditions
    {
        // Ignores float noise; a delivered load is far larger.
        const float DeliveryThreshold = 0.5f;
        static readonly Supply[] Supplies = (Supply[])Enum.GetValues(typeof(Supply));

        readonly ShipRegistry _fleet;
        readonly ZoneWorld _zoneWorld;
        readonly SearchService _search;
        readonly MiningCoordinator _mining;
        readonly Dictionary<Supply, float> _suppliesAtStart = new Dictionary<Supply, float>();

        public PrologueConditions(ShipRegistry fleet, ZoneWorld zoneWorld, SearchService search, MiningCoordinator mining)
        {
            _fleet = fleet ?? throw new ArgumentNullException(nameof(fleet));
            _zoneWorld = zoneWorld ?? throw new ArgumentNullException(nameof(zoneWorld));
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _mining = mining ?? throw new ArgumentNullException(nameof(mining));
        }

        public bool IsStationZoneRevealed()
        {
            return AnyStation(station => true);
        }

        public bool WasStationVisited()
        {
            return AnyStation(station => station.WasVisited);
        }

        /// <summary>Inhabited stations cannot be searched, so hailing one counts as well.</summary>
        public bool WasStationSearchedOrHailed()
        {
            SystemZones system = _zoneWorld.CurrentSystem;
            if (system == null)
            {
                return false;
            }
            foreach (ZoneRecord zone in system.Zones)
            {
                if (!zone.IsRevealed)
                {
                    continue;
                }
                bool hasView = _zoneWorld.TryGetView(zone.PointOfInterestIndex, out ZoneView view);
                foreach (ZoneStation station in zone.Contents.Stations)
                {
                    if (!IsRealStation(station))
                    {
                        continue;
                    }
                    if (station.IsHailed || (hasView && _search.LootOf(view, station).FindsMade > 0))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool AreHarvestersMining()
        {
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (ship.TryGetComponent(out HarvesterMining mining) && mining.State != MiningState.Inactive)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Remembers the fleet's stores, so <see cref="HasDeliveredSupplies"/> sees the next delivery.</summary>
        public void RememberSupplies()
        {
            foreach (Supply supply in Supplies)
            {
                _suppliesAtStart[supply] = _mining.Supplies.AmountOf(supply);
            }
        }

        public bool HasDeliveredSupplies()
        {
            foreach (Supply supply in Supplies)
            {
                float before = _suppliesAtStart.TryGetValue(supply, out float amount) ? amount : 0f;
                if (_mining.Supplies.AmountOf(supply) > before + DeliveryThreshold)
                {
                    return true;
                }
            }
            return false;
        }

        public bool AreFightersLaunched()
        {
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (!ship.IsDocked && ship.TryGetComponent(out FighterCombat _))
                {
                    return true;
                }
            }
            return false;
        }

        bool AnyStation(Func<ZoneStation, bool> predicate)
        {
            SystemZones system = _zoneWorld.CurrentSystem;
            if (system == null)
            {
                return false;
            }
            foreach (ZoneRecord zone in system.Zones)
            {
                if (!zone.IsRevealed)
                {
                    continue;
                }
                foreach (ZoneStation station in zone.Contents.Stations)
                {
                    if (IsRealStation(station) && predicate(station))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static bool IsRealStation(ZoneStation station)
        {
            return !station.IsStrandedShip && !station.IsGone;
        }
    }
}

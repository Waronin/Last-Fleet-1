using System;
using System.Collections.Generic;
using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Stations
{
    /// <summary>
    /// Which ship holds which docking port of which station; one ship per port, one port per ship.
    /// Claims are session state and vanish with the ships, like the mining slots.
    /// </summary>
    public sealed class DockingPortClaims
    {
        readonly Dictionary<ZoneStation, ShipAgent[]> _portsByStation = new Dictionary<ZoneStation, ShipAgent[]>();
        readonly Dictionary<ShipAgent, (ZoneStation Station, int Port)> _claimByShip = new Dictionary<ShipAgent, (ZoneStation Station, int Port)>();

        public int FreePortCount(ZoneStation station)
        {
            if (!_portsByStation.TryGetValue(station, out ShipAgent[] ports))
            {
                return station.DockingPortCount;
            }

            int free = 0;
            foreach (ShipAgent holder in ports)
            {
                free += holder == null ? 1 : 0;
            }
            return free;
        }

        /// <summary>Gives the ship the first free port; a ship that already holds a port there keeps it.</summary>
        public bool TryClaim(ZoneStation station, ShipAgent ship, out int port)
        {
            if (station == null)
            {
                throw new ArgumentNullException(nameof(station));
            }
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }
            if (_claimByShip.TryGetValue(ship, out (ZoneStation Station, int Port) existing) && existing.Station == station)
            {
                port = existing.Port;
                return true;
            }

            if (!_portsByStation.TryGetValue(station, out ShipAgent[] ports))
            {
                ports = new ShipAgent[station.DockingPortCount];
                _portsByStation.Add(station, ports);
            }
            for (int i = 0; i < ports.Length; i++)
            {
                // Unity's overloaded equality treats destroyed ships as free ports.
                if (ports[i] == null)
                {
                    Release(ship);
                    ports[i] = ship;
                    _claimByShip[ship] = (station, i);
                    port = i;
                    return true;
                }
            }
            port = -1;
            return false;
        }

        public void Release(ShipAgent ship)
        {
            if (ship is null || !_claimByShip.TryGetValue(ship, out (ZoneStation Station, int Port) claim))
            {
                return;
            }
            _portsByStation[claim.Station][claim.Port] = null;
            _claimByShip.Remove(ship);
        }

        /// <summary>Ships holding a port of the station, in port order.</summary>
        public void CollectHolders(ZoneStation station, List<ShipAgent> holders)
        {
            holders.Clear();
            if (!_portsByStation.TryGetValue(station, out ShipAgent[] ports))
            {
                return;
            }
            foreach (ShipAgent holder in ports)
            {
                if (holder != null)
                {
                    holders.Add(holder);
                }
            }
        }
    }
}

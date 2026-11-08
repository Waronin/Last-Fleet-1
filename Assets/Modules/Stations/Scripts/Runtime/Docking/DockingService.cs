using System;
using System.Collections.Generic;
using LastFleet.Exploration;
using LastFleet.People;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Stations
{
    /// <summary>
    /// Sends small ships to station docking ports and applies what happens to them there: crew losses in a
    /// toxic atmosphere, under radiation or from a hostile crew, and capture by a hostile crew. When
    /// it picks a ship itself, a free ship already flying in the station's zone goes first; from other
    /// zones scouts such as shuttles go before others, so harvesters keep mining.
    /// </summary>
    public sealed class DockingService : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;

        readonly DockingPortClaims _claims = new DockingPortClaims();
        readonly List<ShipAgent> _candidates = new List<ShipAgent>();
        readonly List<ShipAgent> _picked = new List<ShipAgent>(1);
        readonly List<ShipAgent> _holders = new List<ShipAgent>();

        /// <summary>Short feedback for the player about docking ships and what befalls them.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_registry == null)
            {
                Debug.LogError("DockingService needs the ship registry.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _registry.ShipSpawned += OnShipSpawned;
            _registry.ShipRemoved += _claims.Release;
            foreach (ShipAgent ship in _registry.Ships)
            {
                OnShipSpawned(ship);
            }
        }

        void OnDisable()
        {
            _registry.ShipSpawned -= OnShipSpawned;
            _registry.ShipRemoved -= _claims.Release;
        }

        public int FreePortCount(ZoneStation station)
        {
            return _claims.FreePortCount(station);
        }

        /// <summary>Ships that hold a port of the station, approaching or docked.</summary>
        public void CollectAssignedShips(ZoneStation station, List<ShipAgent> ships)
        {
            _claims.CollectHolders(station, ships);
        }

        /// <summary>Sends every given small ship that can dock to its own free port; returns how many go.</summary>
        public int RequestDocking(ZoneView zone, ZoneStation station, IReadOnlyList<ShipAgent> ships)
        {
            if (station.IsDestroyed)
            {
                Notice?.Invoke(StationTexts.StationDestroyed.Format(("station", station.Name)));
                return 0;
            }
            int sent = 0;
            foreach (ShipAgent ship in ships)
            {
                if (CanDock(ship) && TrySend(zone, station, ship))
                {
                    sent++;
                }
            }
            if (sent == 0)
            {
                Notice?.Invoke(_claims.FreePortCount(station) == 0
                    ? StationTexts.NoFreePort.Format(("station", station.Name))
                    : StationTexts.NoShipToDock);
            }
            return sent;
        }

        /// <summary>
        /// Picks one free ship and sends it to a free port: one already flying in the station's zone first,
        /// from other zones scouts first (<see cref="ScoutingService.ScoutCost"/>). Ships already at this
        /// station are not sent again.
        /// </summary>
        public bool RequestDocking(ZoneView zone, ZoneStation station)
        {
            _candidates.Clear();
            foreach (ShipAgent ship in _registry.Ships)
            {
                bool atThisStation = ship.TryGetComponent(out StationDocking docking) && docking.Station == station;
                if (CanDock(ship) && !atThisStation)
                {
                    _candidates.Add(ship);
                }
            }

            int zoneIndex = zone.Record.PointOfInterestIndex;
            Vector2 zonePositionAU = zone.Record.PointOfInterest.PositionAU;
            ShipAvailability.PickFreeThenNearest(_candidates, ShipAvailability.IsFree,
                ship => ScoutingService.ScoutCost(ship, ShipAvailability.TravelCost(ship, zoneIndex, zonePositionAU, station.LocalPosition), zoneIndex),
                1, _picked);
            return RequestDocking(zone, station, _picked) > 0;
        }

        internal void ReleasePort(ShipAgent ship)
        {
            _claims.Release(ship);
        }

        /// <summary>The docked crew sees the station's environment on site and reports it.</summary>
        internal void ReportDocked(StationDocking docking)
        {
            ZoneStation station = docking.Station;
            station.MarkVisited();
            if (!docking.ReportsArrival)
            {
                return;
            }
            Notice?.Invoke(StationTexts.Docked.Format(("ship", docking.Ship.DisplayName), ("station", station.Name),
                ("atmosphere", ZoneTexts.Atmospheres.For(station.Profile.Atmosphere)), ("radiation", ZoneTexts.Radiation.For(station.Profile.Radiation)),
                ("condition", ZoneTexts.Conditions.For(station.Profile.Condition))));
            TalkToResidents(docking.Ship, station);
        }

        /// <summary>
        /// The docked crew meets the people aboard face to face, which tells as much as a hail: their attitude
        /// and how many want to join the fleet. Someone aboard with something to say, such as a quest giver,
        /// takes over the conversation.
        /// </summary>
        void TalkToResidents(ShipAgent ship, ZoneStation station)
        {
            if (station.HasContactToTalkTo)
            {
                station.Hail();
                GameText answer = station.Contact.Answer(station);
                if (answer != null)
                {
                    Notice?.Invoke(answer);
                }
                return;
            }
            if (!station.Profile.IsInhabited)
            {
                return;
            }
            StationHailResponse response = station.Hail();
            Notice?.Invoke(StationTexts.MetResidents.Format(("ship", ship.DisplayName), ("station", station.Name),
                ("attitude", ZoneTexts.Attitudes.For(response.ClaimedAttitude)), ("willing", station.Residents.Willing)));
        }

        /// <summary>Applies a hazard to a docked ship; returns true if the ship was captured and is gone.</summary>
        internal bool ApplyHazard(StationDocking docking, DockingHazard hazard)
        {
            ShipAgent ship = docking.Ship;
            if (hazard != DockingHazard.Capture)
            {
                if (ship.Crew != null)
                {
                    ship.Crew.Kill(1, CauseOf(hazard));
                }
                return false;
            }

            int aboard = ship.Crew != null ? ship.Crew.Complement.Count : 0;
            if (ship.TryGetComponent(out ShipPassengers passengers))
            {
                aboard += passengers.Quarters.Count;
            }
            Notice?.Invoke(StationTexts.Captured.Format(("ship", ship.DisplayName), ("station", docking.Station.Name), ("count", aboard)));

            // Captured, not destroyed: the ship leaves the fleet with everyone aboard and without a loss by damage.
            docking.Undock();
            Destroy(ship.gameObject);
            return true;
        }

        static GameText CauseOf(DockingHazard hazard)
        {
            return hazard switch
            {
                DockingHazard.ToxicAtmosphere => StationTexts.ToxicAtmosphereCause,
                DockingHazard.Radiation => StationTexts.RadiationCause,
                _ => StationTexts.HostileAttackCause
            };
        }

        /// <summary>
        /// Sends one ship to a free port without a notice, for automated trips such as evacuation runs that
        /// report on their own; false if it cannot dock or no port is free.
        /// </summary>
        internal bool TryDockQuietly(ZoneView zone, ZoneStation station, ShipAgent ship)
        {
            return !station.IsDestroyed && CanDock(ship) && TrySend(zone, station, ship, false);
        }

        bool TrySend(ZoneView zone, ZoneStation station, ShipAgent ship, bool announce = true)
        {
            if (!zone.TryGetStationView(station, out Transform view) || !view.TryGetComponent(out StationView stationView))
            {
                Debug.LogError($"No station model for {station.Name}.", this);
                return false;
            }
            if (!_claims.TryClaim(station, ship, out int port))
            {
                return false;
            }

            Transform portTransform = stationView.DockingPorts[port];
            ship.GetComponent<StationDocking>().BeginDocking(zone, station, portTransform.position, portTransform.forward, announce);
            if (announce)
            {
                Notice?.Invoke(StationTexts.DockingDispatched.Format(("ship", ship.DisplayName), ("station", station.Name)));
            }
            return true;
        }

        static bool CanDock(ShipAgent ship)
        {
            return !ship.IsCapital && !ship.IsDerelict && ship.TryGetComponent(out StationDocking _);
        }

        void OnShipSpawned(ShipAgent ship)
        {
            if (ship.TryGetComponent(out StationDocking docking))
            {
                docking.Initialize(this);
            }
        }
    }
}

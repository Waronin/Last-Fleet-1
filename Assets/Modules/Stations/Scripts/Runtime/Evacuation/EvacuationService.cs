using System;
using System.Collections.Generic;
using LastFleet.People;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Stations
{
    /// <summary>
    /// Takes people who want to join the fleet off stations (GDD: rescue). Ships with passenger seats,
    /// such as shuttles, ferry on their own after one order: dock, let the willing board one by one, fly
    /// them to a hangar of a ship with living space, where they move into its quarters, and go back, until
    /// nobody willing is left, the fleet is full or the player gives another order. Living space may be
    /// overcrowded up to its hard limit. On toxic or radioactive stations the survivors die off the longer
    /// the fleet takes, counted from the moment their zone is revealed.
    /// </summary>
    public sealed class EvacuationService : MonoBehaviour
    {
        const float PortRetrySeconds = 1f;

        enum EndReason
        {
            Finished,
            FleetFull,
            Silent
        }

        [SerializeField] ShipRegistry _fleet;
        [SerializeField] DockingService _docking;
        [SerializeField] ZoneWorld _zoneWorld;
        [Tooltip("Time for one person to come aboard a docked ship.")]
        [SerializeField, Min(0.05f)] float _secondsPerPerson = 0.5f;
        [Tooltip("Share of the survivors on a toxic or radioactive station who die per minute after their zone is revealed.")]
        [SerializeField, Range(0f, 1f)] float _hazardDeathSharePerMinute = 0.1f;

        readonly List<EvacuationRun> _runs = new List<EvacuationRun>();
        readonly List<EvacuationRun> _ending = new List<EvacuationRun>();
        readonly Dictionary<EvacuationRun, EndReason> _endReasons = new Dictionary<EvacuationRun, EndReason>();
        readonly List<ZoneStation> _hazardStations = new List<ZoneStation>();
        readonly List<ShipAgent> _candidates = new List<ShipAgent>();
        float _portRetrySeconds;

        /// <summary>Short feedback for the player about evacuations.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_fleet == null || _docking == null || _zoneWorld == null)
            {
                Debug.LogError("EvacuationService needs the fleet's ship registry, the docking service and the zone world.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _zoneWorld.ZoneRevealed += OnZoneRevealed;
            _fleet.ShipRemoved += OnShipRemoved;
        }

        void OnDisable()
        {
            _zoneWorld.ZoneRevealed -= OnZoneRevealed;
            _fleet.ShipRemoved -= OnShipRemoved;
            foreach (EvacuationRun run in _runs)
            {
                run.Ship.OrdersReplaced -= OnOrdersReplaced;
            }
            _runs.Clear();
        }

        /// <summary>A new run: forget the stations and ferries of the last one.</summary>
        public void Clear()
        {
            foreach (EvacuationRun run in _runs)
            {
                if (run.Ship != null)
                {
                    run.Ship.OrdersReplaced -= OnOrdersReplaced;
                }
            }
            _runs.Clear();
            _hazardStations.Clear();
        }

        /// <summary>A small ship with passenger seats that can dock at stations, such as a shuttle.</summary>
        public static bool CanFerry(ShipAgent ship)
        {
            return ship != null && !ship.IsCapital && !ship.IsDerelict && ship.TryGetComponent(out StationDocking _) &&
                   ship.TryGetComponent(out ShipPassengers seats) && !seats.IsHousing && seats.Quarters.Capacity > 0;
        }

        /// <summary>
        /// Ends the ship's evacuation quietly, e.g. while it refuels, and returns an action that starts it
        /// over for the same station; null when the ship ferries for none.
        /// </summary>
        public Action Suspend(ShipAgent ship)
        {
            EvacuationRun run = FindRun(ship);
            if (run == null)
            {
                return null;
            }
            ZoneView zone = run.Zone;
            ZoneStation station = run.Station;
            EndRun(run, EndReason.Silent);
            return () =>
            {
                if (ship != null && zone != null && BlockedReason(station) == null)
                {
                    RequestEvacuation(zone, station, new ShipAgent[] { ship });
                }
            };
        }

        public bool IsFerrying(ShipAgent ship)
        {
            return FindRun(ship) != null;
        }

        public int CountFerries(ZoneStation station)
        {
            int count = 0;
            foreach (EvacuationRun run in _runs)
            {
                if (run.Station == station)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// People the fleet can still take in: free living space up to the overcrowding limit, minus those
        /// already on their way in a ferry.
        /// </summary>
        public int FleetRoom()
        {
            int room = 0;
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (!ship.TryGetComponent(out ShipPassengers passengers))
                {
                    continue;
                }
                room += passengers.IsHousing ? passengers.Quarters.FreeSpace : -passengers.Quarters.Count;
            }
            return Math.Max(0, room);
        }

        /// <summary>
        /// Sends the given ferries to evacuate the station, or every free ferry of the fleet if none of the
        /// given ships can ferry. Returns how many go, or the reason why none can.
        /// </summary>
        public GameText RequestEvacuation(ZoneView zone, ZoneStation station, IReadOnlyList<ShipAgent> ships)
        {
            GameText blocked = BlockedReason(station);
            if (blocked != null)
            {
                return blocked;
            }

            _candidates.Clear();
            foreach (ShipAgent ship in ships)
            {
                if (CanFerry(ship))
                {
                    _candidates.Add(ship);
                }
            }
            if (_candidates.Count == 0)
            {
                foreach (ShipAgent ship in _fleet.Ships)
                {
                    if (CanFerry(ship) && !IsFerrying(ship) && ShipAvailability.IsFree(ship))
                    {
                        _candidates.Add(ship);
                    }
                }
            }
            if (_candidates.Count == 0)
            {
                return StationTexts.NoFerry;
            }

            foreach (ShipAgent ship in _candidates)
            {
                EvacuationRun existing = FindRun(ship);
                if (existing != null)
                {
                    EndRun(existing, EndReason.Silent);
                }
                EvacuationRun run = new EvacuationRun(ship, zone, station);
                _runs.Add(run);
                if (!ship.TryGetComponent(out EvacuationFerry ferry))
                {
                    ferry = ship.gameObject.AddComponent<EvacuationFerry>();
                }
                ferry.Initialize(this);
                ship.OrdersReplaced += OnOrdersReplaced;
                StartTrip(run);
            }
            return StationTexts.EvacuationStarted.Format(("count", _candidates.Count), ("station", station.Name),
                ("willing", station.Residents.Willing));
        }

        /// <summary>Why the station cannot be evacuated now, or null.</summary>
        public GameText BlockedReason(ZoneStation station)
        {
            if (station.IsDestroyed)
            {
                return StationTexts.StationDestroyed.Format(("station", station.Name));
            }
            if (!station.IsHailed)
            {
                return StationTexts.EvacuationNeedsHail;
            }
            if (station.Residents.Willing == 0)
            {
                return StationTexts.NobodyWantsToCome.Format(("station", station.Name));
            }
            return FleetRoom() == 0 ? StationTexts.FleetFull : null;
        }

        void Update()
        {
            float deltaSeconds = Time.deltaTime;
            PerishOnHazardStations(deltaSeconds);

            _portRetrySeconds -= deltaSeconds;
            bool retryPorts = _portRetrySeconds <= 0f;
            if (retryPorts)
            {
                _portRetrySeconds = PortRetrySeconds;
            }

            int room = FleetRoom();
            _ending.Clear();
            foreach (EvacuationRun run in _runs)
            {
                Advance(run, deltaSeconds, ref room, retryPorts);
            }
            foreach (EvacuationRun run in _ending)
            {
                FinishEnding(run);
            }
        }

        void Advance(EvacuationRun run, float deltaSeconds, ref int room, bool retryPorts)
        {
            // Unity's overloaded equality treats a zone view destroyed with its system as null.
            if (run.Zone == null || run.Station.IsDestroyed)
            {
                MarkEnding(run, EndReason.Silent);
                return;
            }

            StationDocking docking = run.Ship.GetComponent<StationDocking>();
            switch (run.Leg)
            {
                case EvacuationLeg.ToStation:
                    if (docking.State == DockingState.Docked && docking.Station == run.Station)
                    {
                        run.Leg = EvacuationLeg.Boarding;
                        run.BoardingProgressSeconds = 0f;
                    }
                    else if (docking.State == DockingState.Inactive && !run.Ship.IsDocked)
                    {
                        // The player sent the ship elsewhere.
                        MarkEnding(run, EndReason.Silent);
                    }
                    else if (docking.State == DockingState.Inactive && retryPorts)
                    {
                        // Waiting in a hangar for a free port.
                        StartTrip(run);
                    }
                    break;
                case EvacuationLeg.Boarding:
                    if (docking.State != DockingState.Docked)
                    {
                        MarkEnding(run, EndReason.Silent);
                        return;
                    }
                    Board(run, deltaSeconds, ref room);
                    break;
                case EvacuationLeg.ToFleet:
                    if (run.Ship.IsDocked)
                    {
                        // Landed: the people have moved into the carrier's quarters as far as there is room.
                        if (run.Station.Residents.Willing == 0)
                        {
                            MarkEnding(run, EndReason.Finished);
                        }
                        else if (room <= 0)
                        {
                            MarkEnding(run, EndReason.FleetFull);
                        }
                        else
                        {
                            StartTrip(run);
                        }
                    }
                    break;
            }
        }

        /// <summary>People board one by one while there are seats, willing people and room in the fleet.</summary>
        void Board(EvacuationRun run, float deltaSeconds, ref int room)
        {
            PassengerQuarters seats = run.Ship.GetComponent<ShipPassengers>().Quarters;
            StationResidents residents = run.Station.Residents;
            run.BoardingProgressSeconds += deltaSeconds;
            while (run.BoardingProgressSeconds >= _secondsPerPerson && seats.FreeComfortableSpace > 0 && residents.Willing > 0 && room > 0)
            {
                run.BoardingProgressSeconds -= _secondsPerPerson;
                seats.Add(residents.Board(1));
                room--;
            }

            bool full = seats.FreeComfortableSpace == 0;
            if (!full && residents.Willing > 0 && room > 0)
            {
                return;
            }
            if (seats.Count == 0)
            {
                MarkEnding(run, room > 0 ? EndReason.Finished : EndReason.FleetFull);
                return;
            }
            FlyToFleet(run);
        }

        void StartTrip(EvacuationRun run)
        {
            run.IsIssuingOrder = true;
            bool sent = _docking.TryDockQuietly(run.Zone, run.Station, run.Ship);
            run.IsIssuingOrder = false;
            run.Leg = EvacuationLeg.ToStation;
            if (!sent && !run.Ship.IsDocked)
            {
                // No free port and nowhere to wait: back to a hangar, the next trip starts from there.
                FlyToFleet(run);
            }
        }

        void FlyToFleet(EvacuationRun run)
        {
            Hangar hangar = FindHangarWithRoom(run.Ship);
            if (hangar == null)
            {
                MarkEnding(run, EndReason.FleetFull);
                return;
            }
            run.IsIssuingOrder = true;
            run.Ship.Order(ShipOrder.DockAt(hangar), false);
            run.IsIssuingOrder = false;
            run.Leg = EvacuationLeg.ToFleet;
        }

        /// <summary>A hangar with space whose carrier has living space left; in the ship's zone first, then the nearest.</summary>
        Hangar FindHangarWithRoom(ShipAgent ship)
        {
            Hangar best = null;
            float bestCost = float.MaxValue;
            foreach (Hangar hangar in _fleet.Hangars)
            {
                if (!hangar.HasSpace || !hangar.TryGetComponent(out ShipPassengers quarters) || !quarters.IsHousing ||
                    quarters.Quarters.FreeSpace <= 0 || hangar.Carrier.IsDerelict)
                {
                    continue;
                }
                float cost = (hangar.Carrier.WorldPosition - ship.WorldPosition).sqrMagnitude + (hangar.Carrier.Zone == ship.Zone ? 0f : 1e9f);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = hangar;
                }
            }
            return best != null ? best : _fleet.FindHangarFor(ship);
        }

        void PerishOnHazardStations(float deltaSeconds)
        {
            foreach (ZoneStation station in _hazardStations)
            {
                StationResidents residents = station.Residents;
                if (residents.Count == 0 || station.IsDestroyed)
                {
                    continue;
                }
                if (residents.Perish(_hazardDeathSharePerMinute, deltaSeconds) > 0 && residents.Count == 0)
                {
                    Notice?.Invoke(StationTexts.NoSurvivorsLeft.Format(("station", station.Name)));
                }
            }
        }

        void OnZoneRevealed(ZoneView zone)
        {
            foreach (ZoneStation station in zone.Record.Contents.Stations)
            {
                if (station.Profile.IsInhabited && station.Profile.IsHazardous && !_hazardStations.Contains(station))
                {
                    _hazardStations.Add(station);
                }
            }
        }

        void OnOrdersReplaced(ShipAgent ship)
        {
            EvacuationRun run = FindRun(ship);
            // On the way out and back the run gives the orders; anything else comes from the player.
            if (run != null && !run.IsIssuingOrder && run.Leg == EvacuationLeg.ToFleet)
            {
                EndRun(run, EndReason.Silent);
            }
        }

        void OnShipRemoved(ShipAgent ship)
        {
            EvacuationRun run = FindRun(ship);
            if (run != null)
            {
                EndRun(run, EndReason.Silent);
            }
        }

        void MarkEnding(EvacuationRun run, EndReason reason)
        {
            if (!_ending.Contains(run))
            {
                _ending.Add(run);
                _endReasons[run] = reason;
            }
        }

        void FinishEnding(EvacuationRun run)
        {
            _endReasons.TryGetValue(run, out EndReason reason);
            _endReasons.Remove(run);
            EndRun(run, reason);
        }

        void EndRun(EvacuationRun run, EndReason reason)
        {
            if (!_runs.Remove(run))
            {
                return;
            }
            if (reason != EndReason.Silent && run.Ship != null && !run.Ship.IsDocked)
            {
                // Done or no room: the ferry comes home instead of waiting at the station, maybe in its radiation.
                Hangar hangar = _fleet.FindHangarFor(run.Ship);
                if (hangar != null)
                {
                    run.IsIssuingOrder = true;
                    run.Ship.Order(ShipOrder.DockAt(hangar), false);
                    run.IsIssuingOrder = false;
                }
            }
            if (run.Ship != null)
            {
                run.Ship.OrdersReplaced -= OnOrdersReplaced;
            }
            // One report per station, when its last ferry stops.
            if (reason == EndReason.Silent || CountFerries(run.Station) > 0)
            {
                return;
            }
            StationResidents residents = run.Station.Residents;
            Notice?.Invoke(reason == EndReason.FleetFull
                ? StationTexts.EvacuationFleetFull.Format(("station", run.Station.Name), ("rescued", residents.Rescued), ("left", residents.Willing))
                : StationTexts.EvacuationFinished.Format(("station", run.Station.Name), ("rescued", residents.Rescued)));
        }

        EvacuationRun FindRun(ShipAgent ship)
        {
            foreach (EvacuationRun run in _runs)
            {
                if (ReferenceEquals(run.Ship, ship))
                {
                    return run;
                }
            }
            return null;
        }
    }
}

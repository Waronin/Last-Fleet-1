using System;
using System.Collections.Generic;
using LastFleet.Mining;
using LastFleet.People;
using LastFleet.Repairs;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.StrandedShips
{
    /// <summary>
    /// Repairs stranded capital ships and takes them into the fleet (GDD: new capital ships must be
    /// repaired with metal, engineers and time, and fuelled). The player orders the repair; it runs while a
    /// ship with a crew is docked at the hulk, at a shipyard's pace and price, and waits without one or
    /// without metal. Once the hull is whole, the fleet's own ship of that class takes the hulk's place:
    /// survivors still aboard become its passengers, its hangar and stores are empty and its tanks dry, so
    /// the tanker has to refuel it before it can jump.
    /// </summary>
    public sealed class StrandedShipRecovery : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;
        [SerializeField] DockingService _docking;
        [Tooltip("The fleet's capital ship prefabs; a recovered hulk becomes the one of its class.")]
        [SerializeField] ShipAgent[] _shipPrefabs = new ShipAgent[0];
        [Tooltip("Same price and pace as a capital ship's shipyard (Repairs).")]
        [SerializeField, Min(0f)] float _metalPerHullPoint = 0.5f;
        [SerializeField, Min(0.01f)] float _hullPerSecond = 10f;

        readonly Dictionary<StrandedShipClass, StrandedRepairPlan> _plans = new Dictionary<StrandedShipClass, StrandedRepairPlan>();
        readonly Dictionary<ZoneStation, ZoneView> _worked = new Dictionary<ZoneStation, ZoneView>();
        readonly HashSet<ZoneStation> _shortOfMetal = new HashSet<ZoneStation>();
        readonly HashSet<ZoneStation> _metalReported = new HashSet<ZoneStation>();
        readonly List<ShipAgent> _holders = new List<ShipAgent>();
        readonly List<ZoneStation> _whole = new List<ZoneStation>();
        FleetSupplies _supplies;

        /// <summary>Short feedback for the player: recovered ships and missing metal.</summary>
        public event Action<GameText> Notice;

        /// <summary>Small wins for success popups: a new ship in the fleet and the survivors it saved.</summary>
        public event Action<GameText> Success;

        FleetSupplies Supplies => _supplies ??= new FleetSupplies(_registry);

        void Awake()
        {
            if (_registry == null || _docking == null)
            {
                Debug.LogError("StrandedShipRecovery needs the fleet's ship registry and the docking service.", this);
                enabled = false;
                return;
            }
            foreach (StrandedShipClass shipClass in (StrandedShipClass[])Enum.GetValues(typeof(StrandedShipClass)))
            {
                if (FindPrefab(shipClass) == null)
                {
                    Debug.LogError($"StrandedShipRecovery has no ship prefab of class {StrandedShipClasses.ShipClassName(shipClass)}.", this);
                }
            }
        }

        public GameText OrderRepair(ZoneStation station)
        {
            RequireStranded(station);
            station.StrandedHull.OrderRepair();
            return StrandedShipTexts.RepairOrdered.Format(("station", station.Name));
        }

        public GameText CancelRepair(ZoneStation station)
        {
            RequireStranded(station);
            station.StrandedHull.CancelRepair();
            _shortOfMetal.Remove(station);
            return StrandedShipTexts.RepairCancelled.Format(("station", station.Name));
        }

        public StrandedRepairState StateOf(ZoneStation station)
        {
            RequireStranded(station);
            StrandedHull hull = station.StrandedHull;
            if (hull.IsRecovered)
            {
                return StrandedRepairState.Recovered;
            }
            if (!hull.IsRepairOrdered)
            {
                return StrandedRepairState.NotOrdered;
            }
            if (!HasCrewDocked(station))
            {
                return StrandedRepairState.WaitingForCrew;
            }
            return _shortOfMetal.Contains(station) ? StrandedRepairState.WaitingForMetal : StrandedRepairState.Repairing;
        }

        /// <summary>The plan for the hulk's class, or null when its ship prefab is missing.</summary>
        public StrandedRepairPlan PlanFor(ZoneStation station)
        {
            RequireStranded(station);
            StrandedShipClass shipClass = station.StrandedHull.Class;
            if (_plans.TryGetValue(shipClass, out StrandedRepairPlan plan))
            {
                return plan;
            }
            ShipAgent prefab = FindPrefab(shipClass);
            if (prefab == null || !prefab.TryGetComponent(out ShipDurability durability))
            {
                return null;
            }
            plan = new StrandedRepairPlan(durability.MaximumHull, _hullPerSecond, _metalPerHullPoint);
            _plans.Add(shipClass, plan);
            return plan;
        }

        void Update()
        {
            CollectWorkedHulks();
            _whole.Clear();
            foreach (KeyValuePair<ZoneStation, ZoneView> entry in _worked)
            {
                ZoneStation station = entry.Key;
                StrandedRepairPlan plan = PlanFor(station);
                if (plan == null)
                {
                    continue;
                }
                RepairStep step = plan.Advance(station.StrandedHull, Time.deltaTime, Supplies.AmountOf(Supply.Metal));
                if (step.Metal > 0f)
                {
                    Supplies.Take(Supply.Metal, step.Metal);
                }
                ReportMetal(station, step.IsShortOfMetal);
                if (station.StrandedHull.IsWhole)
                {
                    _whole.Add(station);
                }
            }
            foreach (ZoneStation station in _whole)
            {
                Recover(_worked[station], station);
            }
        }

        /// <summary>Hulks with an ordered repair and a crew docked at them, with their zones.</summary>
        void CollectWorkedHulks()
        {
            _worked.Clear();
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (!ship.TryGetComponent(out StationDocking docking) || !docking.IsDockedWithCrew)
                {
                    continue;
                }
                ZoneStation station = docking.Station;
                if (station.IsStrandedShip && station.StrandedHull.IsRepairOrdered && !station.IsGone && !_worked.ContainsKey(station))
                {
                    _worked.Add(station, docking.Zone);
                }
            }
        }

        bool HasCrewDocked(ZoneStation station)
        {
            _docking.CollectAssignedShips(station, _holders);
            foreach (ShipAgent ship in _holders)
            {
                if (ship.TryGetComponent(out StationDocking docking) && docking.IsDockedWithCrew)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>One notice per hulk when the metal runs out, and another only after there was metal again.</summary>
        void ReportMetal(ZoneStation station, bool isShort)
        {
            if (!isShort)
            {
                _shortOfMetal.Remove(station);
                _metalReported.Remove(station);
                return;
            }
            _shortOfMetal.Add(station);
            if (_metalReported.Add(station))
            {
                Notice?.Invoke(StrandedShipTexts.OutOfMetal.Format(("station", station.Name)));
            }
        }

        /// <summary>The fleet's ship of the hulk's class takes its place; the docked ships cast off.</summary>
        void Recover(ZoneView zone, ZoneStation station)
        {
            ShipAgent prefab = FindPrefab(station.StrandedHull.Class);
            _docking.CollectAssignedShips(station, _holders);
            foreach (ShipAgent holder in _holders)
            {
                holder.GetComponent<StationDocking>().Undock();
            }

            station.StrandedHull.MarkRecovered();
            _shortOfMetal.Remove(station);
            _metalReported.Remove(station);
            zone.RemoveStationView(station);

            ShipAgent ship = _registry.Spawn(prefab, zone, station.LocalPosition, station.HeadingDegrees);
            int survivors = TakeSurvivorsAboard(ship, station.Residents);
            EmptyTanksAndStores(ship);
            Notice?.Invoke(StrandedShipTexts.Recovered.Format(("ship", ship.DisplayName), ("count", survivors)));
            Success?.Invoke(ShipTexts.JoinedFleet.Format(("ship", ship.DisplayName)));
            if (survivors > 0)
            {
                Success?.Invoke(PeopleTexts.Rescued.Format(("count", survivors)));
            }
        }

        /// <summary>Only the survivors are aboard: the prefab's starting passengers belong to the fleet's own ships.</summary>
        static int TakeSurvivorsAboard(ShipAgent ship, StationResidents residents)
        {
            if (!ship.TryGetComponent(out ShipPassengers passengers))
            {
                return 0;
            }
            PassengerQuarters quarters = passengers.Quarters;
            quarters.Remove(quarters.Count);
            int boarding = residents.Board(Math.Min(residents.Willing, quarters.FreeSpace));
            return quarters.Add(boarding);
        }

        static void EmptyTanksAndStores(ShipAgent ship)
        {
            if (ship.Fuel != null)
            {
                ship.Fuel.Tank.Drain(ship.Fuel.Tank.Amount);
            }
            if (ship.TryGetComponent(out SupplyStorage storage))
            {
                foreach (Supply supply in (Supply[])Enum.GetValues(typeof(Supply)))
                {
                    storage.Stock.Take(supply, storage.Stock.AmountOf(supply));
                }
            }
        }

        ShipAgent FindPrefab(StrandedShipClass shipClass)
        {
            string className = StrandedShipClasses.ShipClassName(shipClass);
            foreach (ShipAgent prefab in _shipPrefabs)
            {
                if (prefab != null && prefab.ClassName == className)
                {
                    return prefab;
                }
            }
            return null;
        }

        static void RequireStranded(ZoneStation station)
        {
            if (station == null)
            {
                throw new ArgumentNullException(nameof(station));
            }
            if (!station.IsStrandedShip)
            {
                throw new ArgumentException($"{station.Name} is not a stranded ship.", nameof(station));
            }
        }
    }
}

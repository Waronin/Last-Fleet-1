using System;
using System.Collections.Generic;
using LastFleet.Mining;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Repairs
{
    /// <summary>
    /// Hull repairs the player orders (GDD: repairs cost metal, need engineers and run in real time;
    /// small ships are repaired in a hangar). Nothing is repaired automatically. Every capital ship has
    /// a <see cref="Shipyard"/> that works on one ship at a time, itself or a ship in its hangar, in the
    /// order the player set. Repairs go on under fire and pause while the fleet has no metal.
    /// </summary>
    public sealed class RepairService : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;
        [SerializeField, Min(0f)] float _metalPerHullPoint = 0.5f;
        [SerializeField, Min(0.01f)] float _smallShipHullPerSecond = 2f;
        [SerializeField, Min(0.01f)] float _capitalShipHullPerSecond = 10f;

        readonly Dictionary<ShipAgent, Shipyard> _yards = new Dictionary<ShipAgent, Shipyard>();
        readonly List<ShipAgent> _lostOwners = new List<ShipAgent>();
        readonly List<ShipAgent> _finished = new List<ShipAgent>();
        readonly List<ShipAgent> _leftHangar = new List<ShipAgent>();
        FleetSupplies _supplies;
        RepairWork _work;
        bool _outOfMetalReported;

        /// <summary>Short feedback for the player: finished and cancelled repairs, missing metal.</summary>
        public event Action<GameText> Notice;

        FleetSupplies Supplies => _supplies ??= new FleetSupplies(_registry);
        RepairWork Work => _work ??= new RepairWork(_metalPerHullPoint);

        void Awake()
        {
            if (_registry == null)
            {
                Debug.LogError("RepairService needs the fleet's ship registry.", this);
                enabled = false;
            }
        }

        public float HullPerSecondFor(ShipAgent ship) => ship.IsCapital ? _capitalShipHullPerSecond : _smallShipHullPerSecond;

        public RepairEligibility Check(ShipAgent ship)
        {
            if (ship == null || !ship.TryGetComponent(out ShipDurability durability) || durability.Hull.IsDestroyed)
            {
                return RepairEligibility.NoHull;
            }
            if (TryGetJob(ship, out Shipyard _, out int _))
            {
                return RepairEligibility.AlreadyQueued;
            }
            if (!durability.Hull.IsDamaged)
            {
                return RepairEligibility.Undamaged;
            }
            return ship.IsCapital || ship.IsDocked ? RepairEligibility.Eligible : RepairEligibility.NotInHangar;
        }

        /// <summary>Puts the ship at the end of its shipyard's queue: its own, or its carrier's. Returns why not otherwise.</summary>
        public RepairEligibility Order(ShipAgent ship)
        {
            RepairEligibility eligibility = Check(ship);
            if (eligibility == RepairEligibility.Eligible)
            {
                YardOf(ship.IsCapital ? ship : ship.DockedIn.Carrier).Queue.Enqueue(ship);
            }
            return eligibility;
        }

        /// <summary>Lets the ship's shipyard repair it next; the current repair waits.</summary>
        public bool RepairFirst(ShipAgent ship)
        {
            return TryGetJob(ship, out Shipyard yard, out int _) && yard.Queue.MoveToFront(ship);
        }

        public bool Cancel(ShipAgent ship)
        {
            return TryGetJob(ship, out Shipyard yard, out int _) && yard.Queue.Remove(ship);
        }

        /// <summary>Finds the shipyard a ship waits in and its zero-based place; place 0 is being repaired.</summary>
        public bool TryGetJob(ShipAgent ship, out Shipyard yard, out int position)
        {
            foreach (Shipyard candidate in _yards.Values)
            {
                position = candidate.Queue.PositionOf(ship);
                if (position >= 0)
                {
                    yard = candidate;
                    return true;
                }
            }
            yard = null;
            position = -1;
            return false;
        }

        /// <summary>The shipyard of a capital ship once it has had any work.</summary>
        public bool TryGetYard(ShipAgent capitalShip, out Shipyard yard)
        {
            return _yards.TryGetValue(capitalShip, out yard);
        }

        Shipyard YardOf(ShipAgent capitalShip)
        {
            if (!_yards.TryGetValue(capitalShip, out Shipyard yard))
            {
                yard = new Shipyard(capitalShip);
                _yards.Add(capitalShip, yard);
            }
            return yard;
        }

        void Update()
        {
            bool shortOfMetal = false;
            _lostOwners.Clear();
            foreach (Shipyard yard in _yards.Values)
            {
                // A lost capital ship takes its engineers and its hangar with it.
                if (yard.Owner == null || !_registry.Contains(yard.Owner))
                {
                    _lostOwners.Add(yard.Owner);
                    continue;
                }
                DropFinishedAndUnreachable(yard);
                RepairCurrent(yard, Time.deltaTime);
                shortOfMetal |= yard.IsShortOfMetal;
            }
            foreach (ShipAgent owner in _lostOwners)
            {
                _yards.Remove(owner);
            }
            ReportMetal(shortOfMetal);
        }

        void DropFinishedAndUnreachable(Shipyard yard)
        {
            _finished.Clear();
            _leftHangar.Clear();
            yard.Queue.RemoveAll(ship =>
            {
                if (ship == null)
                {
                    return true;
                }
                if (!ship.TryGetComponent(out ShipDurability durability) || !durability.Hull.IsDamaged)
                {
                    _finished.Add(ship);
                    return true;
                }
                if (!yard.CanReach(ship))
                {
                    _leftHangar.Add(ship);
                    return true;
                }
                return false;
            });
            foreach (ShipAgent ship in _finished)
            {
                Notice?.Invoke(RepairTexts.Repaired.Format(("ship", ship.DisplayName)));
            }
            foreach (ShipAgent ship in _leftHangar)
            {
                Notice?.Invoke(RepairTexts.CancelledLeftHangar.Format(("ship", ship.DisplayName)));
            }
        }

        void RepairCurrent(Shipyard yard, float deltaSeconds)
        {
            ShipAgent ship = yard.Current;
            yard.IsShortOfMetal = false;
            if (ship == null)
            {
                return;
            }

            ShipHull hull = ship.GetComponent<ShipDurability>().Hull;
            RepairStep step = Work.Plan(hull.Missing, HullPerSecondFor(ship), deltaSeconds, Supplies.AmountOf(Supply.Metal));
            if (step.Metal > 0f)
            {
                Supplies.Take(Supply.Metal, step.Metal);
            }
            hull.Repair(step.HullPoints);
            yard.IsShortOfMetal = step.IsShortOfMetal;
        }

        /// <summary>One notice when the metal runs out, and another only after there was metal again.</summary>
        void ReportMetal(bool shortOfMetal)
        {
            if (shortOfMetal && !_outOfMetalReported)
            {
                Notice?.Invoke(RepairTexts.OutOfMetal);
            }
            _outOfMetalReported = shortOfMetal || (_outOfMetalReported && Supplies.AmountOf(Supply.Metal) <= 0f);
        }
    }
}

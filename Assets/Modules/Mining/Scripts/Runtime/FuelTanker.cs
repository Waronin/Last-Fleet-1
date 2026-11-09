using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Tanker duty (GDD: space tanker, stores fuel and refuels other ships). The player decides who
    /// gets fuel: the tanker pumps from its store into the capital ships it was ordered to refuel, one
    /// after another, its own tank included. Being in the same zone is enough; a target in another zone
    /// makes the tanker fly there. Any other order to the tanker ends the refueling. Small ships refuel
    /// from their carrier in the hangar instead.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(SupplyStorage))]
    public sealed class FuelTanker : MonoBehaviour, IShipAutomation
    {
        // Where the tanker waits when it follows a target into another zone, beside rather than inside its hull.
        const float ArrivalOffsetUnits = 20f;

        [Tooltip("Fuel pumped into the current target per second.")]
        [SerializeField, Min(0f)] float _pumpPerSecond = 40f;

        readonly RefuelingQueue<ShipAgent> _targets = new RefuelingQueue<ShipAgent>();
        ShipAgent _ship;
        SupplyStorage _storage;
        ShipRegistry _registry;
        MiningCoordinator _coordinator;
        bool _isIssuingOrder;

        public ShipAgent Target => _targets.Current;
        public IReadOnlyList<ShipAgent> Targets => _targets.Targets;
        public bool IsRefueling => !_targets.IsEmpty;
        public bool IsAutomationActive => IsRefueling;

        // Subscribed for the object's whole life, like the other automations.
        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _storage = GetComponent<SupplyStorage>();
            _ship.OrdersReplaced += OnOrdersReplaced;
        }

        void OnDestroy()
        {
            _ship.OrdersReplaced -= OnOrdersReplaced;
            if (_registry != null)
            {
                _registry.ShipRemoved -= OnShipRemoved;
            }
        }

        public void Initialize(ShipRegistry registry, MiningCoordinator coordinator)
        {
            if (_registry != null)
            {
                _registry.ShipRemoved -= OnShipRemoved;
            }
            _registry = registry;
            _coordinator = coordinator;
            _registry.ShipRemoved += OnShipRemoved;
        }

        /// <summary>
        /// Orders the tanker to refuel a capital ship of the fleet, its own tank included. Append queues
        /// it behind the current targets; otherwise it replaces them. Returns whether it was accepted.
        /// </summary>
        public bool Refuel(ShipAgent target, bool append)
        {
            if (target == null || !target.IsCapital || _registry == null)
            {
                return false;
            }
            if (target.Fuel.Tank.FreeSpace <= 0f)
            {
                Report(MiningTexts.AlreadyFull.Format(("target", target.DisplayName)));
                return false;
            }
            if (!_targets.Add(target, append))
            {
                return false;
            }

            bool isCurrent = _targets.Current == target;
            Report((isCurrent ? MiningTexts.RefuelingStarted : MiningTexts.RefuelingQueued)
                .Format(("tanker", _ship.DisplayName), ("target", target.DisplayName)));
            return true;
        }

        public void StopRefueling()
        {
            _targets.Clear();
        }

        void Update()
        {
            ShipAgent target = _targets.Current;
            if (target == null || _ship.Zone == null)
            {
                return;
            }
            if (target.Zone != _ship.Zone)
            {
                FollowIntoZone(target);
                return;
            }
            if (_storage.Stock.AmountOf(Supply.Fuel) <= 0f)
            {
                Report(MiningTexts.TankerEmpty.Format(("tanker", _ship.DisplayName)));
                _targets.Clear();
                return;
            }

            FuelPumping.Pump(_storage.Stock, target.Fuel.Tank, _pumpPerSecond * Time.deltaTime);
            if (target.Fuel.Tank.FreeSpace <= 0f)
            {
                Report(MiningTexts.Refueled.Format(("target", target.DisplayName)));
                _targets.Remove(target);
            }
        }

        /// <summary>Flies to the target's zone once nothing else is queued; a target still jumping is awaited.</summary>
        void FollowIntoZone(ShipAgent target)
        {
            ZoneView zone = target.Zone;
            if (zone == null || _ship.QueuedOrderCount > 0 || _ship.Activity == ShipActivity.ChargingJump)
            {
                return;
            }

            Vector3 localPoint = zone.transform.InverseTransformPoint(target.WorldPosition) + Vector3.right * ArrivalOffsetUnits;
            _isIssuingOrder = true;
            _ship.Order(ShipOrder.MoveTo(zone.Record.PointOfInterestIndex, localPoint), false);
            _isIssuingOrder = false;
        }

        void OnOrdersReplaced(ShipAgent ship)
        {
            if (!_isIssuingOrder)
            {
                _targets.Clear();
            }
        }

        void OnShipRemoved(ShipAgent ship)
        {
            _targets.Remove(ship);
        }

        void Report(GameText message)
        {
            if (_coordinator != null)
            {
                _coordinator.Report(message);
            }
        }
    }
}

using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Tanker duty (GDD: space tanker, stores fuel and refuels other ships). Nothing is refueled
    /// automatically: the player orders refueling in a capital ship's window (see
    /// <see cref="FleetRefueling"/>), and the tanker pumps from its store into one ship at a time, in
    /// the order the player set, its own tank included. Tanker and ship have to stay in the same zone;
    /// a ship that is no longer there leaves the queue with a notice. Without fuel in the store the
    /// current refueling waits and goes on by itself once fuel is back. Small ships refuel from their
    /// carrier in the hangar instead.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(SupplyStorage))]
    public sealed class FuelTanker : MonoBehaviour
    {
        [Tooltip("Fuel pumped into the current ship per second.")]
        [SerializeField, Min(0f)] float _pumpPerSecond = 40f;

        readonly List<ShipAgent> _finished = new List<ShipAgent>();
        readonly List<ShipAgent> _leftZone = new List<ShipAgent>();
        ShipAgent _ship;
        SupplyStorage _storage;
        MiningCoordinator _coordinator;
        bool _emptyReported;

        public ShipAgent Ship => _ship;

        /// <summary>The ship being refueled, or null.</summary>
        public ShipAgent Current => Queue.Current;

        /// <summary>Ships in the queue behind the current one.</summary>
        public int WaitingCount => Queue.Count > 0 ? Queue.Count - 1 : 0;

        /// <summary>True while the current refueling stands still for lack of fuel in the store.</summary>
        public bool IsShortOfFuel { get; private set; }

        internal RefuelingQueue<ShipAgent> Queue { get; } = new RefuelingQueue<ShipAgent>();

        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _storage = GetComponent<SupplyStorage>();
        }

        public void Initialize(MiningCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        /// <summary>Whether the tanker can refuel the ship: a capital ship of its own zone, itself included.</summary>
        public bool CanReach(ShipAgent ship)
        {
            return ship != null && ship.IsCapital && _ship.Zone != null && ship.Zone == _ship.Zone;
        }

        void Update()
        {
            // Zones are unknown for a moment during jumps; the queue waits instead of dropping everyone.
            if (Queue.Count == 0 || _ship.Zone == null)
            {
                IsShortOfFuel = false;
                return;
            }

            DropFinishedAndUnreachable();
            ShipAgent target = Queue.Current;
            if (target == null)
            {
                IsShortOfFuel = false;
                return;
            }

            float moved = FuelPumping.Pump(_storage.Stock, target.Fuel.Tank, _pumpPerSecond * Time.deltaTime);
            IsShortOfFuel = moved <= 0f && target.Fuel.Tank.FreeSpace > 0f;
            ReportShortage();
        }

        void DropFinishedAndUnreachable()
        {
            _finished.Clear();
            _leftZone.Clear();
            Queue.RemoveAll(ship =>
            {
                if (ship == null || ship.IsDerelict)
                {
                    return true;
                }
                if (ship.Fuel.Tank.FreeSpace <= 0f)
                {
                    _finished.Add(ship);
                    return true;
                }
                // A ship still jumping keeps its place until it arrives.
                if (ship.Zone != null && !CanReach(ship))
                {
                    _leftZone.Add(ship);
                    return true;
                }
                return false;
            });
            foreach (ShipAgent ship in _finished)
            {
                Report(MiningTexts.Refueled.Format(("target", ship.DisplayName)));
            }
            foreach (ShipAgent ship in _leftZone)
            {
                Report(MiningTexts.RefuelCancelledLeftZone.Format(("ship", ship.DisplayName), ("tanker", _ship.DisplayName)));
            }
        }

        /// <summary>One notice when the store runs dry, and another only after there was fuel again.</summary>
        void ReportShortage()
        {
            if (IsShortOfFuel && !_emptyReported)
            {
                Report(MiningTexts.TankerEmpty.Format(("tanker", _ship.DisplayName)));
            }
            _emptyReported = IsShortOfFuel || (_emptyReported && _storage.Stock.AmountOf(Supply.Fuel) <= 0f);
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

using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Mining
{
    public enum MiningState
    {
        Inactive,
        MovingToDeposit,
        Extracting,
        Delivering
    }

    /// <summary>
    /// Automatic mining loop of a harvester (GDD: Asteroids and belts). After one order the ship
    /// keeps working: fly to a mining slot, mine until the hold is full, deliver to an industrial
    /// ship's landing bay, return. It stops when the zone is exhausted or the player gives another
    /// order. Attaching to a deposit scans it, since the ship is now on site.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class HarvesterMining : MonoBehaviour, IShipAutomation
    {
        const float SlotArrivalToleranceUnits = 2f;

        [SerializeField, Min(1f)] float _cargoCapacity = 120f;
        [SerializeField, Min(0.1f)] float _extractionPerSecond = 8f;

        ShipAgent _ship;
        MiningCoordinator _coordinator;
        CargoHold _cargo;
        ZoneView _zone;
        int _slot = -1;
        RawMaterialStorage _storage;
        bool _isIssuingOrder;

        public MiningState State { get; private set; }
        public CargoHold Cargo => _cargo ??= new CargoHold(_cargoCapacity);
        public ResourceDeposit Deposit { get; private set; }
        public float YieldMultiplier => Deposit != null ? MiningYield.Multiplier(Deposit.MiningSeconds) : 1f;
        public Vector3 DepositWorldPosition => _zone.transform.TransformPoint(Deposit.LocalPosition);
        public bool IsAutomationActive => State != MiningState.Inactive;

        // Subscribed for the object's whole life: docked ships are inactive but must still react.
        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _ship.OrdersReplaced += OnOrdersReplaced;
            _ship.DockedInHangar += OnDockedInHangar;
        }

        void OnDestroy()
        {
            _ship.OrdersReplaced -= OnOrdersReplaced;
            _ship.DockedInHangar -= OnDockedInHangar;
        }

        public void Initialize(MiningCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        /// <summary>Starts mining at a deposit, or at the nearest free one in the zone if it is full.</summary>
        public bool StartMining(ZoneView zone, ResourceDeposit deposit)
        {
            if (_coordinator == null)
            {
                Debug.LogError($"{name} has no mining coordinator.", this);
                return false;
            }

            StopMining();
            _zone = zone;
            if (!_coordinator.TryFindDeposit(zone, deposit, deposit.Resource, _ship, out ResourceDeposit found, out int slot))
            {
                return false;
            }

            Deposit = found;
            _slot = slot;
            ContinueWithDeposit();
            return true;
        }

        public void StopMining()
        {
            if (_coordinator != null)
            {
                _coordinator.Claims.Release(_ship);
            }
            State = MiningState.Inactive;
            Deposit = null;
            _slot = -1;
        }

        void Update()
        {
            switch (State)
            {
                case MiningState.MovingToDeposit:
                    if (HasArrivedAtSlot())
                    {
                        Deposit.Scan();
                        State = MiningState.Extracting;
                    }
                    else if (IsShipWaiting())
                    {
                        // The approach ended elsewhere, e.g. after an interrupted jump; try again.
                        GoToSlot();
                    }
                    break;
                case MiningState.Extracting:
                    Extract(Time.deltaTime);
                    break;
                case MiningState.Delivering:
                    if (IsShipWaiting())
                    {
                        CompleteDelivery();
                    }
                    break;
            }
        }

        /// <summary>Idle with nothing queued; an order issued this frame is still queued until the ship starts it.</summary>
        bool IsShipWaiting()
        {
            return _ship.Activity == ShipActivity.Idle && _ship.QueuedOrderCount == 0 && !_ship.IsDocked;
        }

        void Extract(float deltaTime)
        {
            if (Deposit.IsDepleted)
            {
                FindNextDeposit();
                return;
            }

            float multiplier = MiningYield.Multiplier(Deposit.MiningSeconds);
            float wanted = Mathf.Min(_extractionPerSecond * deltaTime, Cargo.FreeSpace / multiplier);
            float extracted = Deposit.Extract(wanted);
            Deposit.RecordMining(deltaTime);
            Cargo.Load(Deposit.Resource, extracted * multiplier);
            _zone.RefreshDeposit(Deposit);

            if (Cargo.FreeSpace <= 0.01f)
            {
                BeginDelivery();
            }
            else if (Deposit.IsDepleted)
            {
                FindNextDeposit();
            }
        }

        /// <summary>Goes mining at the current deposit, delivering first if the hold carries another material.</summary>
        void ContinueWithDeposit()
        {
            if (!Cargo.IsEmpty && Cargo.Resource != Deposit.Resource)
            {
                BeginDelivery();
            }
            else
            {
                GoToSlot();
            }
        }

        void FindNextDeposit()
        {
            _coordinator.Claims.Release(_ship);
            ResourceKind preferred = Cargo.IsEmpty ? Deposit.Resource : Cargo.Resource;
            if (_coordinator.TryFindDeposit(_zone, null, preferred, _ship, out ResourceDeposit found, out int slot))
            {
                Deposit = found;
                _slot = slot;
                ContinueWithDeposit();
                return;
            }

            if (!Cargo.IsEmpty)
            {
                BeginDelivery();
                return;
            }
            _coordinator.Report($"{_ship.name}: no free deposit left in {_zone.Record.PointOfInterest.Name}.");
            StopMining();
        }

        void BeginDelivery()
        {
            // Free the slot for other harvesters while this one is away.
            _coordinator.Claims.Release(_ship);
            _storage = _coordinator.FindStorage(_ship, Cargo.Resource);
            if (_storage == null)
            {
                _coordinator.Report($"{_ship.name}: no storage with room for {Cargo.Resource}.");
                StopMining();
                return;
            }

            IssueOrder(ShipOrder.ApproachBay(_storage.Hangar));
            State = MiningState.Delivering;
        }

        void CompleteDelivery()
        {
            if (_storage == null)
            {
                StopMining();
                return;
            }

            float stored = _storage.Stock.Store(Cargo.Resource, Cargo.Amount);
            Cargo.Unload(stored);
            if (!Cargo.IsEmpty)
            {
                _coordinator.Report($"{_storage.name} storage is full for {Cargo.Resource}.");
                StopMining();
                return;
            }

            if (Deposit != null && _coordinator.TryFindDeposit(_zone, Deposit, Deposit.Resource, _ship, out ResourceDeposit found, out int slot))
            {
                Deposit = found;
                _slot = slot;
                GoToSlot();
                return;
            }
            FindNextDeposit();
        }

        void GoToSlot()
        {
            IssueOrder(ShipOrder.MoveTo(_zone.Record.PointOfInterestIndex, Deposit.GetMiningSlotLocalPosition(_slot)));
            State = MiningState.MovingToDeposit;
        }

        bool HasArrivedAtSlot()
        {
            if (!IsShipWaiting() || _ship.Zone != _zone)
            {
                return false;
            }

            Vector3 offset = _zone.transform.InverseTransformPoint(_ship.transform.position) - Deposit.GetMiningSlotLocalPosition(_slot);
            offset.y = 0f;
            return offset.magnitude <= SlotArrivalToleranceUnits;
        }

        void IssueOrder(ShipOrder order)
        {
            _isIssuingOrder = true;
            _ship.Order(order, false);
            _isIssuingOrder = false;
        }

        void OnOrdersReplaced(ShipAgent ship)
        {
            if (!_isIssuingOrder)
            {
                StopMining();
            }
        }

        /// <summary>A harvester landing with cargo unloads into the carrier's storage.</summary>
        void OnDockedInHangar(ShipAgent ship, Hangar hangar)
        {
            if (!Cargo.IsEmpty && hangar.TryGetComponent(out RawMaterialStorage storage))
            {
                Cargo.Unload(storage.Stock.Store(Cargo.Resource, Cargo.Amount));
            }
        }
    }
}

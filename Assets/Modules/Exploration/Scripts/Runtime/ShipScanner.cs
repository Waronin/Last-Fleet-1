using System;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Exploration
{
    public enum ScanTaskState
    {
        Inactive,
        Approaching,
        Scanning
    }

    /// <summary>
    /// Lets a small ship scan a deposit on site (GDD: what a zone really holds is only known when a
    /// ship is there): fly next to it, scan for a few seconds, done. Any other order cancels the scan.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class ShipScanner : MonoBehaviour, IShipAutomation
    {
        const float ApproachMarginUnits = 1.5f;
        const float ArrivalToleranceUnits = 3f;

        [SerializeField, Min(0f)] float _scanSeconds = 3f;
        [Tooltip("Scouting is this ship's main job, e.g. a shuttle; scouts are sent before other ships.")]
        [SerializeField] bool _isScout;

        ShipAgent _ship;
        ZoneView _zone;
        Vector3 _scanPoint;
        bool _isIssuingOrder;

        public ScanTaskState State { get; private set; }
        public ResourceDeposit Target { get; private set; }
        public float RemainingSeconds { get; private set; }
        public ShipAgent Ship => _ship;
        public bool IsScout => _isScout;
        public bool IsAutomationActive => State != ScanTaskState.Inactive;

        public event Action<ShipScanner, ResourceDeposit> ScanCompleted;

        // Subscribed for the object's whole life: docked ships are inactive but can be tasked.
        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _ship.OrdersReplaced += OnOrdersReplaced;
        }

        void OnDestroy()
        {
            _ship.OrdersReplaced -= OnOrdersReplaced;
        }

        public void BeginScan(ZoneView zone, ResourceDeposit deposit)
        {
            _zone = zone;
            Target = deposit;
            _scanPoint = ApproachPoint(zone, deposit);
            IssueOrder(ShipOrder.MoveTo(zone.Record.PointOfInterestIndex, _scanPoint));
            State = ScanTaskState.Approaching;
        }

        public void Cancel()
        {
            State = ScanTaskState.Inactive;
            Target = null;
        }

        void Update()
        {
            switch (State)
            {
                case ScanTaskState.Approaching:
                    if (!IsWaiting())
                    {
                        break;
                    }
                    if (IsAtScanPoint())
                    {
                        State = ScanTaskState.Scanning;
                        RemainingSeconds = _scanSeconds;
                    }
                    else
                    {
                        IssueOrder(ShipOrder.MoveTo(_zone.Record.PointOfInterestIndex, _scanPoint));
                    }
                    break;
                case ScanTaskState.Scanning:
                    // A short-handed crew scans more slowly.
                    RemainingSeconds -= Time.deltaTime * _ship.WorkEfficiency;
                    if (RemainingSeconds <= 0f)
                    {
                        CompleteScan();
                    }
                    break;
            }
        }

        void CompleteScan()
        {
            ResourceDeposit scanned = Target;
            scanned.Scan();
            _zone.RefreshDeposit(scanned);
            Cancel();
            ScanCompleted?.Invoke(this, scanned);
        }

        /// <summary>Just outside the deposit and its mining slots, on the side facing the ship.</summary>
        Vector3 ApproachPoint(ZoneView zone, ResourceDeposit deposit)
        {
            Vector3 shipLocal = zone.transform.InverseTransformPoint(_ship.WorldPosition);
            Vector3 away = shipLocal - deposit.LocalPosition;
            away.y = 0f;
            if (_ship.Zone != zone || away.sqrMagnitude < 1e-6f)
            {
                away = -deposit.LocalPosition;
                away.y = 0f;
            }
            Vector3 direction = away.sqrMagnitude > 1e-6f ? away.normalized : Vector3.back;
            Vector3 point = deposit.LocalPosition + direction * (deposit.FootprintRadiusUnits + ApproachMarginUnits);
            point.y = 0f;
            return point;
        }

        bool IsWaiting()
        {
            return _ship.Activity == ShipActivity.Idle && _ship.QueuedOrderCount == 0 && !_ship.IsDocked;
        }

        bool IsAtScanPoint()
        {
            if (_ship.Zone != _zone)
            {
                return false;
            }
            Vector3 offset = _zone.transform.InverseTransformPoint(_ship.transform.position) - _scanPoint;
            offset.y = 0f;
            return offset.magnitude <= ArrivalToleranceUnits;
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
                Cancel();
            }
        }
    }
}

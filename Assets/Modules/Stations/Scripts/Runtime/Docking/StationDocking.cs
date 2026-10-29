using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Stations
{
    public enum DockingState
    {
        Inactive,

        /// <summary>Flying to the waiting point in front of the port.</summary>
        Approaching,

        /// <summary>Creeping on thrusters until the ship's airlock meets the port.</summary>
        Connecting,

        Docked
    }

    /// <summary>
    /// Lets a small ship dock at a station's docking port (GDD: dock with a ship). The ship flies to a
    /// point in front of the port, turns its airlock towards it and creeps in on thrusters. Docked, it
    /// keeps a departure maneuver ready: the next order first backs it away on thrusters before it
    /// turns and burns. The docking service hands out the port and applies what the hazards do.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class StationDocking : MonoBehaviour, IShipAutomation
    {
        /// <summary>Waiting point in front of the port: about 400 m out, close enough for thrusters.</summary>
        const float ApproachDistanceUnits = 4f;

        /// <summary>About 80 m/s on the last meters, slow enough to read as careful.</summary>
        const float ConnectSpeedUnitsPerSecond = 0.8f;

        const float DepartureSpeedUnitsPerSecond = 1.2f;
        const float ArrivalToleranceUnits = 1f;

        [Tooltip("The ship's airlock; it meets the station's port. Without one the ship's center docks.")]
        [SerializeField] Transform _airlock;

        readonly List<DockingHazard> _hazards = new List<DockingHazard>();
        ShipAgent _ship;
        DockingService _service;
        ZoneView _zone;
        Vector3 _approachPoint;
        Vector3 _contactPoint;
        float _dockedHeadingDegrees;
        bool _isIssuingOrder;

        public DockingState State { get; private set; }
        public ZoneStation Station { get; private set; }
        public ShipAgent Ship => _ship;
        public bool IsAutomationActive => State != DockingState.Inactive;

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

        public void Initialize(DockingService service)
        {
            _service = service;
        }

        /// <summary>Called by the docking service after it reserved a port; position and axis are in world space.</summary>
        internal void BeginDocking(ZoneView zone, ZoneStation station, Vector3 portPosition, Vector3 portOutward)
        {
            _zone = zone;
            Station = station;
            DockingHazardRules.CollectHazards(station.Profile, _hazards);
            PlanApproach(zone.transform.InverseTransformPoint(portPosition), zone.transform.InverseTransformDirection(portOutward));
            IssueOrder(ShipOrder.MoveTo(zone.Record.PointOfInterestIndex, _approachPoint));
            State = DockingState.Approaching;
        }

        public void Undock()
        {
            if (State == DockingState.Inactive)
            {
                return;
            }
            State = DockingState.Inactive;
            Station = null;
            _service.ReleasePort(_ship);
        }

        /// <summary>
        /// The ship faces the port with its airlock; the contact point puts the airlock onto the port,
        /// and the waiting point lies straight out along the port's axis.
        /// </summary>
        void PlanApproach(Vector3 portLocal, Vector3 outwardLocal)
        {
            Vector2 outward = new Vector2(outwardLocal.x, outwardLocal.z);
            outward = outward.sqrMagnitude > 1e-8f ? outward.normalized : Vector2.up;
            _dockedHeadingDegrees = ShipMotion.DirectionToHeading(-outward);

            Vector3 airlockOffset = _airlock != null ? transform.InverseTransformPoint(_airlock.position) : Vector3.zero;
            airlockOffset.y = 0f;
            Vector3 rotatedOffset = Quaternion.Euler(0f, _dockedHeadingDegrees, 0f) * airlockOffset;
            _contactPoint = new Vector3(portLocal.x - rotatedOffset.x, 0f, portLocal.z - rotatedOffset.z);
            _approachPoint = _contactPoint + new Vector3(outward.x, 0f, outward.y) * ApproachDistanceUnits;
        }

        void Update()
        {
            switch (State)
            {
                case DockingState.Approaching:
                    if (!IsWaiting())
                    {
                        break;
                    }
                    if (IsAt(_approachPoint))
                    {
                        IssueOrder(ShipOrder.ManeuverTo(_zone.Record.PointOfInterestIndex, _contactPoint, _dockedHeadingDegrees, ConnectSpeedUnitsPerSecond));
                        State = DockingState.Connecting;
                    }
                    else
                    {
                        IssueOrder(ShipOrder.MoveTo(_zone.Record.PointOfInterestIndex, _approachPoint));
                    }
                    break;
                case DockingState.Connecting:
                    if (IsWaiting())
                    {
                        CompleteDocking();
                    }
                    break;
                case DockingState.Docked:
                    RollHazards(Time.deltaTime);
                    break;
            }
        }

        void CompleteDocking()
        {
            State = DockingState.Docked;
            // Backing out keeps the heading, so the ship reverses away from the port on its thrusters.
            _ship.SetDepartureManeuver(ShipOrder.ManeuverTo(_zone.Record.PointOfInterestIndex, _approachPoint,
                _dockedHeadingDegrees, DepartureSpeedUnitsPerSecond));
            _service.ReportDocked(this);
        }

        void RollHazards(float deltaTime)
        {
            foreach (DockingHazard hazard in _hazards)
            {
                if (UnityEngine.Random.value < HazardTiming.ChanceWithin(DockingHazardRules.MeanSeconds(hazard), deltaTime))
                {
                    // A capture removes the ship; nothing else happens to it afterwards.
                    if (_service.ApplyHazard(this, hazard))
                    {
                        return;
                    }
                }
            }
        }

        bool IsWaiting()
        {
            return _ship.Activity == ShipActivity.Idle && _ship.QueuedOrderCount == 0 && !_ship.IsDocked;
        }

        bool IsAt(Vector3 localPoint)
        {
            if (_ship.Zone != _zone)
            {
                return false;
            }
            Vector3 offset = _zone.transform.InverseTransformPoint(_ship.transform.position) - localPoint;
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
            if (_isIssuingOrder)
            {
                return;
            }
            // A ship whose last crew member died stays at the port (GDD: it cannot be flown back).
            if (State == DockingState.Docked && ship.IsDerelict)
            {
                return;
            }
            // Only a docked ship backs away first; one still on its way just turns to the new order.
            if (State != DockingState.Docked)
            {
                ship.ClearDepartureManeuver();
            }
            Undock();
        }
    }
}

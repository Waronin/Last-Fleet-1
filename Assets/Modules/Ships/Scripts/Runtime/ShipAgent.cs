using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Ships
{
    public enum ShipActivity
    {
        Idle,
        Moving,
        ChargingJump,

        /// <summary>Approaching a hangar's landing bay.</summary>
        Docking,

        /// <summary>Inside a hangar: no GameObject in the zone, travels with the carrier.</summary>
        Docked,

        /// <summary>Flying attack runs at another ship.</summary>
        Pursuing,

        /// <summary>Keeping station next to another ship, e.g. as its escort.</summary>
        Following
    }

    /// <summary>
    /// A ship inside a zone: executes queued orders, performs zone jumps and, for small ships, lands in
    /// and launches from hangars. A ship in a zone is a child of its zone view and moves in zone-local
    /// coordinates; a docked ship is an inactive child of its carrier.
    /// Zone jump (GDD): the jump drive charges while the main drive accelerates towards the target
    /// zone; the ship then appears in the target zone keeping its momentum, flips and brakes.
    /// Powered flight and jumps burn fuel (see <see cref="ShipFuel"/>).
    /// </summary>
    public sealed class ShipAgent : MonoBehaviour
    {
        [SerializeField] string _className = "Harvester";
        [SerializeField] ShipSize _size = ShipSize.Small;
        [SerializeField] ShipMovementProfile _movementProfile;
        [Tooltip("Radius others fly around and hide behind; 0 for small ships, which do not block anyone.")]
        [SerializeField, Min(0f)] float _obstacleRadiusUnits;

        // Obstacle avoidance looks this far ahead, at least the minimum, and keeps this much room.
        const float AvoidanceLookAheadSeconds = 2.5f;
        const float AvoidanceMinimumLookAheadUnits = 15f;
        const float AvoidanceClearanceUnits = 2f;

        // Station keeping: each unit of gap adds this much speed over the followed ship's; closer than the
        // settle distance to a resting ship, the thrusters take over.
        const float FollowCatchUpPerUnit = 0.6f;
        const float FollowSettleDistanceUnits = 1.5f;
        const float FollowRestingSpeed = 0.5f;

        readonly Queue<ShipOrder> _orders = new Queue<ShipOrder>();
        ObstacleField _obstacles;
        ShipMotion _motion;
        ShipFuel _fuel;
        ShipCrew _crew;
        ZoneWorld _zoneWorld;
        ZoneView _zone;
        Hangar _dockedIn;
        ShipOrder _currentOrder;
        bool _holdingInterceptCourse;
        ShipOrder? _departure;
        int _jumpTargetZoneIndex = -1;

        /// <summary>Technical class identifier; players see <see cref="ClassDisplayName"/>.</summary>
        public string ClassName => _className;

        public GameText ClassDisplayName => ShipTexts.Classes.For(_className);

        /// <summary>Number within its class, given by the registry; 0 until the ship is spawned.</summary>
        public int SerialNumber { get; private set; }

        /// <summary>The name players see, e.g. "Harvester 3"; GameObject.name stays the technical name.</summary>
        public GameText DisplayName => ShipTexts.ShipName.Format(("class", ClassDisplayName), ("number", SerialNumber));

        public ShipSize Size => _size;
        public bool IsCapital => _size == ShipSize.Capital;
        public float ObstacleRadiusUnits => _obstacleRadiusUnits;

        /// <summary>The side the ship fights for, set by the registry that owns it.</summary>
        public Allegiance Allegiance { get; private set; }

        /// <summary>The zone the ship is in; docked ships are in their carrier's zone.</summary>
        public ZoneView Zone => _dockedIn != null ? _dockedIn.Carrier.Zone : _zone;

        public Hangar HomeHangar { get; private set; }
        public Hangar DockedIn => _dockedIn;
        public bool IsDocked => _dockedIn != null;
        public Vector3 WorldPosition => IsDocked ? _dockedIn.BayWorldPosition : transform.position;
        public ShipActivity Activity { get; private set; }
        public ShipMotionPhase MotionPhase => _motion.Phase;
        public float Speed => IsDocked ? 0f : _motion.Speed;
        public bool MainDriveActive => !IsDocked && _motion.MainDriveActive;
        public bool ThrustersActive => !IsDocked && _motion.ThrustersActive;
        public float HeadingDegrees => _motion.HeadingDegrees;
        public float JumpChargeRemainingSeconds { get; private set; }
        public int QueuedOrderCount => _orders.Count;
        public ShipFuel Fuel => _fuel != null ? _fuel : _fuel = GetComponent<ShipFuel>();

        /// <summary>Crew of a small ship; null for capital ships, whose crew are their passengers.</summary>
        public ShipCrew Crew => _crew != null ? _crew : _crew = GetComponent<ShipCrew>();

        /// <summary>No crew left: the ship cannot fly or take orders until it is recovered.</summary>
        public bool IsDerelict => Crew != null && Crew.IsDerelict;

        /// <summary>Share of full working speed for tasks such as mining and scanning; 1 without a crew system.</summary>
        public float WorkEfficiency => Crew != null ? Crew.Efficiency : 1f;

        /// <summary>Zone the ship is charging a jump to, or -1.</summary>
        public int TargetZoneIndex => Activity == ShipActivity.ChargingJump ? _jumpTargetZoneIndex : -1;

        /// <summary>The ship completed a zone jump and now belongs to another zone.</summary>
        public event Action<ShipAgent> ZoneChanged;

        /// <summary>All queued orders were replaced, e.g. by a new player command.</summary>
        public event Action<ShipAgent> OrdersReplaced;

        /// <summary>The ship landed in a hangar; raised before it leaves the zone.</summary>
        public event Action<ShipAgent, Hangar> DockedInHangar;

        public event Action<ShipAgent> Destroyed;

        /// <summary>A zone jump was not started because the tank held less than the given cost; the order is dropped.</summary>
        public event Action<ShipAgent, int, float> JumpRefusedForFuel;

        /// <summary>Places the ship in a zone; without an obstacle field it flies straight through everything.</summary>
        public void Initialize(ZoneWorld zoneWorld, ObstacleField obstacles, ZoneView zone, Vector3 localPosition, float headingDegrees)
        {
            if (_movementProfile == null)
            {
                throw new InvalidOperationException($"{name} has no movement profile.");
            }
            if (Fuel == null)
            {
                throw new InvalidOperationException($"{name} has no {nameof(ShipFuel)} component.");
            }

            _zoneWorld = zoneWorld;
            _obstacles = obstacles;
            _zone = zone;
            _motion = CreateMotion(localPosition, headingDegrees);
            ApplyTransform();
            if (Crew != null)
            {
                Crew.BecameDerelict += OnBecameDerelict;
            }
        }

        /// <summary>
        /// Replaces all orders, or appends to the queue (GDD: Shift chains commands). A derelict
        /// without crew ignores orders; callers can check <see cref="IsDerelict"/> to explain why.
        /// </summary>
        public void Order(ShipOrder order, bool append)
        {
            if (order.Kind == ShipOrderKind.Dock && IsCapital)
            {
                throw new InvalidOperationException($"{name} is a capital ship and cannot land in a hangar.");
            }
            if (order.TargetsShip && order.Target == this)
            {
                throw new ArgumentException($"{name} cannot intercept or follow itself.", nameof(order));
            }
            if (IsDerelict)
            {
                return;
            }

            if (!append)
            {
                _orders.Clear();
                if (!IsDocked)
                {
                    Activity = ShipActivity.Idle;
                }
                OrdersReplaced?.Invoke(this);
            }

            // Already where the order leads, unless earlier queued orders take the ship out first.
            if (IsDocked && order.TargetsHangar && order.Hangar == _dockedIn && _orders.Count == 0)
            {
                return;
            }

            // A ship held at a docking port first backs away from it, then carries out the order.
            if (_departure.HasValue && !IsDocked)
            {
                _orders.Enqueue(_departure.Value);
                _departure = null;
            }

            _orders.Enqueue(order);
            if (IsDocked)
            {
                // A docked ship launches to carry out its orders.
                _dockedIn.RequestLaunch(this);
            }
        }

        /// <summary>
        /// A maneuver the ship flies before its next order, e.g. backing away from a docking port on
        /// thrusters before it turns and burns. The next order uses it up.
        /// </summary>
        public void SetDepartureManeuver(ShipOrder maneuver)
        {
            if (maneuver.Kind != ShipOrderKind.Maneuver)
            {
                throw new ArgumentException("A departure must be a maneuver.", nameof(maneuver));
            }
            _departure = maneuver;
        }

        public void ClearDepartureManeuver()
        {
            _departure = null;
        }

        internal void AssignSerialNumber(int serialNumber)
        {
            SerialNumber = serialNumber;
            name = $"{_className} {serialNumber}";
        }

        internal void SetAllegiance(Allegiance allegiance)
        {
            Allegiance = allegiance;
        }

        /// <summary>
        /// Drops every order and lets the thrusters bring the ship to rest, e.g. when nobody is left to
        /// fly it or it changes sides. Automations stop on <see cref="OrdersReplaced"/>.
        /// </summary>
        internal void CancelOrders()
        {
            _orders.Clear();
            _departure = null;
            if (!IsDocked && _motion != null)
            {
                Activity = ShipActivity.Idle;
                JumpChargeRemainingSeconds = 0f;
                _motion.SetDestination(_motion.Position);
            }
            OrdersReplaced?.Invoke(this);
        }

        internal void SetHomeHangar(Hangar hangar)
        {
            HomeHangar = hangar;
        }

        /// <summary>
        /// Places the ship in a zone of another system after a fleet jump (GDD: The jump): its orders
        /// end and it comes out at rest. Docked ships travel along inside its hangar.
        /// </summary>
        public void ArriveAfterFleetJump(ZoneView zone, Vector3 localPosition, float headingDegrees)
        {
            if (zone == null)
            {
                throw new ArgumentNullException(nameof(zone));
            }
            if (IsDocked)
            {
                throw new InvalidOperationException($"{name} is docked and travels with its carrier.");
            }

            CancelOrders();
            _zone = zone;
            transform.SetParent(zone.transform, false);
            _motion = CreateMotion(localPosition, headingDegrees);
            Activity = ShipActivity.Idle;
            ApplyTransform();
            ZoneChanged?.Invoke(this);
        }

        /// <summary>Called by the hangar once the ship has landed.</summary>
        internal void EnterHangar(Hangar hangar)
        {
            DockedInHangar?.Invoke(this, hangar);
            _dockedIn = hangar;
            _zone = null;
            Activity = ShipActivity.Docked;
            JumpChargeRemainingSeconds = 0f;
            transform.SetParent(hangar.transform, false);
            gameObject.SetActive(false);
            if (_orders.Count > 0)
            {
                hangar.RequestLaunch(this);
            }
        }

        /// <summary>Called by the hangar when the ship leaves the landing bay.</summary>
        internal void LeaveHangar(ZoneView zone, Vector3 localPosition, float headingDegrees)
        {
            _dockedIn = null;
            _zone = zone;
            transform.SetParent(zone.transform, false);
            _motion = CreateMotion(localPosition, headingDegrees);
            Activity = ShipActivity.Idle;
            ApplyTransform();
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (_motion == null || IsDocked)
            {
                return;
            }

            if (Activity == ShipActivity.Idle && _orders.Count > 0)
            {
                StartOrder(_orders.Dequeue());
            }
            if (Activity == ShipActivity.Docking || Activity == ShipActivity.Pursuing || Activity == ShipActivity.Following)
            {
                FollowMovingTarget();
            }
            UpdateAvoidance();

            float deltaTime = Time.deltaTime;
            Vector2 previousPosition = _motion.Position;
            _motion.Step(deltaTime);
            BurnTravelFuel((_motion.Position - previousPosition).magnitude);

            switch (Activity)
            {
                case ShipActivity.ChargingJump:
                    JumpChargeRemainingSeconds -= deltaTime;
                    if (JumpChargeRemainingSeconds <= 0f)
                    {
                        CompleteJump();
                    }
                    break;
                case ShipActivity.Moving:
                    if (_motion.HasArrived)
                    {
                        Activity = ShipActivity.Idle;
                    }
                    break;
                case ShipActivity.Docking:
                    TryLand();
                    break;
            }

            if (!IsDocked)
            {
                ApplyTransform();
            }
        }

        void OnDestroy()
        {
            if (Crew != null)
            {
                Crew.BecameDerelict -= OnBecameDerelict;
            }
            Destroyed?.Invoke(this);
        }

        /// <summary>
        /// Bends the course around rocks, stations and capital ships ahead while the main drive carries
        /// the ship; the obstacle holding the destination is left alone, so the ship can still get there.
        /// </summary>
        void UpdateAvoidance()
        {
            if (_obstacles == null || !_motion.IsTravelling)
            {
                _motion.ClearAvoidance();
                return;
            }

            Vector2 destination = _motion.IntendedDestination;
            Vector2 desired = destination - _motion.Position;
            float lookAhead = Mathf.Max(AvoidanceMinimumLookAheadUnits, _motion.Speed * AvoidanceLookAheadSeconds);
            Vector2 steer = ObstacleAvoidance.Steer(_motion.Position, desired, destination, lookAhead,
                AvoidanceClearanceUnits + _obstacleRadiusUnits, _obstacles.In(_zone), this);
            if (desired.sqrMagnitude < 1e-8f || Vector2.Dot(steer, desired.normalized) > 0.9999f)
            {
                _motion.ClearAvoidance();
                return;
            }
            _motion.SetAvoidance(steer);
        }

        /// <summary>Nobody is left to fly: drop all orders and let the thrusters bring the ship to rest.</summary>
        void OnBecameDerelict(ShipCrew crew)
        {
            CancelOrders();
        }

        void StartOrder(ShipOrder order)
        {
            _currentOrder = order;
            _holdingInterceptCourse = false;
            int zoneIndex = OrderZoneIndex(order);
            if (zoneIndex < 0)
            {
                // The order's target is already gone; do not keep a previous course.
                _motion.Stop();
                Activity = ShipActivity.Idle;
                return;
            }
            if (zoneIndex != _zone.Record.PointOfInterestIndex)
            {
                BeginJump(zoneIndex);
                return;
            }

            Vector2 destination = ToPlane(OrderLocalPoint(order, _zone));
            if (order.Kind == ShipOrderKind.Maneuver)
            {
                _motion.SetPreciseDestination(destination, order.MaximumSpeed, order.HeadingDegrees);
            }
            else
            {
                _motion.SetDestination(destination);
            }
            Activity = ActivityFor(order);
        }

        static ShipActivity ActivityFor(ShipOrder order)
        {
            if (order.TargetsHangar)
            {
                return ShipActivity.Docking;
            }
            return order.Kind switch
            {
                ShipOrderKind.Intercept => ShipActivity.Pursuing,
                ShipOrderKind.Follow => ShipActivity.Following,
                _ => ShipActivity.Moving
            };
        }

        /// <summary>Powered travel costs fuel per km; drifting is free, and so are thrusters on an empty tank.</summary>
        void BurnTravelFuel(float distanceUnits)
        {
            if (_motion.MainDriveActive || _motion.ThrustersActive)
            {
                Fuel.Tank.Drain(Fuel.Consumption.ForTravel(distanceUnits));
            }
            _motion.MainDriveAvailable = !Fuel.Tank.IsEmpty;
        }

        /// <summary>The jump is paid in full when the countdown starts; without enough fuel the order is dropped.</summary>
        void BeginJump(int zoneIndex)
        {
            float cost = JumpFuelCost(zoneIndex);
            if (!Fuel.Tank.TryDrain(cost))
            {
                // Brake out of whatever the ship was doing: an attack run's course never brakes on
                // its own and would carry the ship out of the zone.
                _motion.Stop();
                Activity = ShipActivity.Idle;
                JumpRefusedForFuel?.Invoke(this, zoneIndex, cost);
                return;
            }

            _jumpTargetZoneIndex = zoneIndex;
            _motion.SetCourse(JumpDirection(zoneIndex));
            JumpChargeRemainingSeconds = _movementProfile.JumpChargeSeconds;
            Activity = ShipActivity.ChargingJump;
        }

        void CompleteJump()
        {
            Vector2 direction = JumpDirection(_jumpTargetZoneIndex);
            ZoneView target = _zoneWorld.Reveal(_jumpTargetZoneIndex);
            if (_currentOrder.FollowsMovingTarget && OrderZoneIndex(_currentOrder) < 0)
            {
                // The followed target is gone: brake instead of keeping the jump course.
                _motion.Stop();
                Activity = ShipActivity.Idle;
                return;
            }
            Vector2 arrival = ToPlane(OrderLocalPoint(_currentOrder, target));

            // Exit the jump far enough before the arrival point to flip and brake with the kept momentum.
            float speedTowards = Vector2.Dot(_motion.Velocity, direction);
            Vector2 exit = arrival - direction * _motion.StoppingDistance(speedTowards);

            _zone = target;
            transform.SetParent(target.transform, false);
            _motion.Teleport(exit);
            _motion.SetDestination(arrival);
            JumpChargeRemainingSeconds = 0f;
            Activity = ActivityFor(_currentOrder);
            ApplyTransform();
            ZoneChanged?.Invoke(this);
        }

        /// <summary>
        /// A carrier or a pursued ship may move or jump away while the ship approaches; follow it. The
        /// order ends when the target is gone or, for a pursued ship, has landed in a hangar.
        /// </summary>
        void FollowMovingTarget()
        {
            int targetZoneIndex = OrderZoneIndex(_currentOrder);
            if (targetZoneIndex < 0)
            {
                Activity = ShipActivity.Idle;
                _motion.Stop();
                return;
            }
            if (targetZoneIndex != _zone.Record.PointOfInterestIndex)
            {
                BeginJump(targetZoneIndex);
                return;
            }
            if (_currentOrder.Kind == ShipOrderKind.Intercept)
            {
                SteerIntercept();
                return;
            }
            if (_currentOrder.Kind == ShipOrderKind.Follow)
            {
                SteerFollow();
                return;
            }
            _motion.RetargetDestination(ToPlane(OrderLocalPoint(_currentOrder, _zone)));
        }

        /// <summary>
        /// Keeps station next to a moving ship: fly towards the station point at the followed ship's speed
        /// plus a share of the gap, so the ship closes in smoothly instead of braking for every position
        /// the station had. Once there and the followed ship rests, it settles on thrusters.
        /// </summary>
        void SteerFollow()
        {
            ShipAgent target = _currentOrder.Target;
            Vector2 toStation = ToPlane(OrderLocalPoint(_currentOrder, _zone)) - _motion.Position;
            float gap = toStation.magnitude;
            if (target.Speed < FollowRestingSpeed && gap < FollowSettleDistanceUnits)
            {
                _motion.RetargetDestination(_motion.Position + toStation);
                return;
            }

            Vector2 course = gap > FollowSettleDistanceUnits ? toStation : ShipMotion.HeadingToDirection(target.HeadingDegrees);
            _motion.SetCourse(course, Mathf.Max(FollowRestingSpeed, target.Speed + gap * FollowCatchUpPerUnit));
        }

        void TryLand()
        {
            Hangar hangar = _currentOrder.Hangar;
            if (hangar == null)
            {
                return;
            }

            Vector3 offset = hangar.BayWorldPosition - transform.position;
            offset.y = 0f;
            if (offset.magnitude > hangar.DockingRadiusUnits)
            {
                return;
            }

            // An approach ends next to the bay; a failed landing means the hangar filled up meanwhile.
            if (_currentOrder.Kind == ShipOrderKind.Approach || !hangar.TryDock(this))
            {
                Activity = ShipActivity.Idle;
            }
        }

        static int OrderZoneIndex(ShipOrder order)
        {
            if (order.Kind == ShipOrderKind.Move || order.Kind == ShipOrderKind.Maneuver)
            {
                return order.ZoneIndex;
            }
            if (order.TargetsShip)
            {
                // Unity's overloaded equality treats a destroyed target as null.
                ShipAgent target = order.Target;
                return target != null && !target.IsDocked && target.Zone != null ? target.Zone.Record.PointOfInterestIndex : -1;
            }
            return order.Hangar != null && order.Hangar.Carrier.Zone != null ? order.Hangar.Carrier.Zone.Record.PointOfInterestIndex : -1;
        }

        Vector3 OrderLocalPoint(ShipOrder order, ZoneView zone)
        {
            if (order.TargetsHangar)
            {
                return zone.transform.InverseTransformPoint(order.Hangar.BayWorldPosition);
            }
            return order.Kind switch
            {
                ShipOrderKind.Intercept => InterceptPoint(order, zone),
                ShipOrderKind.Follow => FollowPoint(order, zone),
                _ => order.LocalPoint
            };
        }

        /// <summary>The station next to the followed ship: the order's offset turned with the ship's heading.</summary>
        static Vector3 FollowPoint(ShipOrder order, ZoneView zone)
        {
            Vector3 targetLocal = zone.transform.InverseTransformPoint(order.Target.WorldPosition);
            Vector3 offset = Quaternion.Euler(0f, order.Target.HeadingDegrees, 0f) * order.LocalPoint;
            return new Vector3(targetLocal.x + offset.x, 0f, targetLocal.z + offset.z);
        }

        /// <summary>
        /// The point beside the target the ship aims for, seen from where it comes from: its own position
        /// in the same zone, along the jump when it arrives from another zone.
        /// </summary>
        Vector3 InterceptPoint(ShipOrder order, ZoneView zone)
        {
            Vector2 target = ToPlane(zone.transform.InverseTransformPoint(order.Target.WorldPosition));
            Vector2 from = zone == _zone ? _motion.Position : target - JumpDirection(zone.Record.PointOfInterestIndex);
            Vector2 aim = InterceptRun.AimPoint(from, target, order.PassOffsetUnits);
            return new Vector3(aim.x, 0f, aim.y);
        }

        /// <summary>Steers an attack run every frame; past the target the ship holds its course until it breaks off.</summary>
        void SteerIntercept()
        {
            Vector2 target = ToPlane(_zone.transform.InverseTransformPoint(_currentOrder.Target.WorldPosition));
            Vector2 toTarget = target - _motion.Position;
            _holdingInterceptCourse = InterceptRun.HoldsCourse(_holdingInterceptCourse, _motion.Forward, toTarget, _currentOrder.BreakOffUnits);
            if (_holdingInterceptCourse)
            {
                return;
            }

            Vector2 toAim = InterceptRun.AimPoint(_motion.Position, target, _currentOrder.PassOffsetUnits) - _motion.Position;
            if (toAim.sqrMagnitude > 1e-8f)
            {
                _motion.SetCourse(toAim, _currentOrder.MaximumSpeed);
            }
        }

        /// <summary>Direction on the zone plane from the current zone towards another zone of the system.</summary>
        public Vector2 JumpDirection(int targetZoneIndex)
        {
            Vector2 delta = JumpDeltaAU(targetZoneIndex);
            return delta.sqrMagnitude > 1e-12f ? delta.normalized : _motion.Forward;
        }

        /// <summary>Fuel a small jump from the current zone to another zone of the system costs.</summary>
        public float JumpFuelCost(int targetZoneIndex)
        {
            return Fuel.Consumption.ForJump(JumpDeltaAU(targetZoneIndex).magnitude);
        }

        Vector2 JumpDeltaAU(int targetZoneIndex)
        {
            Vector2 fromAU = Zone.Record.PointOfInterest.PositionAU;
            Vector2 toAU = _zoneWorld.CurrentSystem.Zones[targetZoneIndex].PointOfInterest.PositionAU;
            return toAU - fromAU;
        }

        ShipMotion CreateMotion(Vector3 localPosition, float headingDegrees)
        {
            ShipMotion motion = new ShipMotion(_movementProfile.Motion, ToPlane(localPosition), headingDegrees);
            motion.MainDriveAvailable = !Fuel.Tank.IsEmpty;
            return motion;
        }

        void ApplyTransform()
        {
            transform.localPosition = new Vector3(_motion.Position.x, 0f, _motion.Position.y);
            transform.localRotation = Quaternion.Euler(0f, _motion.HeadingDegrees, 0f);
        }

        static Vector2 ToPlane(Vector3 localPoint)
        {
            return new Vector2(localPoint.x, localPoint.z);
        }
    }
}

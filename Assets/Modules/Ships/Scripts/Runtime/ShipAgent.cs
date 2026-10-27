using System;
using System.Collections.Generic;
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
        Docked
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

        readonly Queue<ShipOrder> _orders = new Queue<ShipOrder>();
        ShipMotion _motion;
        ShipFuel _fuel;
        ZoneWorld _zoneWorld;
        ZoneView _zone;
        Hangar _dockedIn;
        ShipOrder _currentOrder;
        int _jumpTargetZoneIndex = -1;

        public string ClassName => _className;
        public ShipSize Size => _size;
        public bool IsCapital => _size == ShipSize.Capital;

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

        public void Initialize(ZoneWorld zoneWorld, ZoneView zone, Vector3 localPosition, float headingDegrees)
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
            _zone = zone;
            _motion = CreateMotion(localPosition, headingDegrees);
            ApplyTransform();
        }

        /// <summary>Replaces all orders, or appends to the queue (GDD: Shift chains commands).</summary>
        public void Order(ShipOrder order, bool append)
        {
            if (order.Kind == ShipOrderKind.Dock && IsCapital)
            {
                throw new InvalidOperationException($"{name} is a capital ship and cannot land in a hangar.");
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

            if (IsDocked && order.TargetsHangar && order.Hangar == _dockedIn)
            {
                return;
            }

            _orders.Enqueue(order);
            if (IsDocked)
            {
                // A docked ship launches to carry out its orders.
                _dockedIn.RequestLaunch(this);
            }
        }

        internal void SetHomeHangar(Hangar hangar)
        {
            HomeHangar = hangar;
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
            if (Activity == ShipActivity.Docking)
            {
                FollowDockingTarget();
            }

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
            Destroyed?.Invoke(this);
        }

        void StartOrder(ShipOrder order)
        {
            _currentOrder = order;
            int zoneIndex = OrderZoneIndex(order);
            if (zoneIndex < 0)
            {
                Activity = ShipActivity.Idle;
                return;
            }
            if (zoneIndex != _zone.Record.PointOfInterestIndex)
            {
                BeginJump(zoneIndex);
                return;
            }

            _motion.SetDestination(ToPlane(OrderLocalPoint(order, _zone)));
            Activity = order.TargetsHangar ? ShipActivity.Docking : ShipActivity.Moving;
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
            if (_currentOrder.TargetsHangar && _currentOrder.Hangar == null)
            {
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
            Activity = _currentOrder.TargetsHangar ? ShipActivity.Docking : ShipActivity.Moving;
            ApplyTransform();
            ZoneChanged?.Invoke(this);
        }

        /// <summary>The carrier may move or jump away while the ship approaches; follow it.</summary>
        void FollowDockingTarget()
        {
            Hangar hangar = _currentOrder.Hangar;
            if (hangar == null)
            {
                Activity = ShipActivity.Idle;
                return;
            }

            int carrierZoneIndex = OrderZoneIndex(_currentOrder);
            if (carrierZoneIndex != _zone.Record.PointOfInterestIndex)
            {
                BeginJump(carrierZoneIndex);
                return;
            }
            _motion.RetargetDestination(ToPlane(OrderLocalPoint(_currentOrder, _zone)));
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
            if (order.Kind == ShipOrderKind.Move)
            {
                return order.ZoneIndex;
            }
            return order.Hangar != null && order.Hangar.Carrier.Zone != null ? order.Hangar.Carrier.Zone.Record.PointOfInterestIndex : -1;
        }

        static Vector3 OrderLocalPoint(ShipOrder order, ZoneView zone)
        {
            return order.Kind == ShipOrderKind.Move ? order.LocalPoint : zone.transform.InverseTransformPoint(order.Hangar.BayWorldPosition);
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

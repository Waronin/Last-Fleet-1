using System;
using UnityEngine;

namespace LastFleet.Ships
{
    public enum ShipMotionPhase
    {
        Idle,

        /// <summary>Turning towards the destination before burning.</summary>
        Aligning,

        /// <summary>Main drive accelerating towards the destination.</summary>
        Burning,

        /// <summary>Coasting while turning the engine against the direction of travel.</summary>
        Flipping,

        /// <summary>Main drive decelerating towards the destination.</summary>
        Braking,

        /// <summary>Thrusters only, for short distances and the final approach.</summary>
        Maneuvering,

        /// <summary>Accelerating along a fixed direction without braking, e.g. while a jump drive charges.</summary>
        Cruising
    }

    /// <summary>
    /// Controlled ship movement on the zone plane (GDD: no physics simulation, but it should feel
    /// physical). Long moves turn, burn, flip and brake; short moves use thrusters. Position and
    /// velocity are 2D: x and y map to the zone's x and z axes. Heading 0 points along +z.
    /// </summary>
    public sealed class ShipMotion
    {
        const float SnapSpeedFactor = 0.25f;

        // Precise maneuvers settle within about a meter, at a crawl and on their heading.
        const float PreciseArrivalToleranceUnits = 0.01f;
        const float PreciseArrivalSpeed = 0.05f;
        const float PreciseHeadingToleranceDegrees = 0.1f;

        /// <summary>A precise approach starts slowing down this far out (about 100 m), so the touch is soft.</summary>
        const float PreciseBrakingDistanceUnits = 1f;

        readonly ShipMotionSettings _settings;
        Vector2 _destination;
        Vector2 _courseDirection;
        float _courseMaximumSpeed = float.PositiveInfinity;
        bool _isPrecise;
        float _preciseMaximumSpeed;
        float _preciseHeadingDegrees;

        public ShipMotion(ShipMotionSettings settings, Vector2 position, float headingDegrees)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Position = position;
            HeadingDegrees = headingDegrees;
        }

        public Vector2 Position { get; private set; }
        public Vector2 Velocity { get; private set; }
        public float HeadingDegrees { get; private set; }
        public ShipMotionPhase Phase { get; private set; } = ShipMotionPhase.Idle;
        public float Speed => Velocity.magnitude;
        public bool MainDriveActive { get; private set; }
        public bool ThrustersActive { get; private set; }

        /// <summary>True once the last destination has been reached and the ship stopped.</summary>
        public bool HasArrived { get; private set; }

        /// <summary>
        /// Without the main drive, e.g. with an empty tank, every move is a thruster maneuver
        /// (GDD: the ship can only move with its thrusters).
        /// </summary>
        public bool MainDriveAvailable { get; set; } = true;

        public Vector2 Forward => HeadingToDirection(HeadingDegrees);

        public void SetDestination(Vector2 destination)
        {
            _isPrecise = false;
            _destination = destination;
            HasArrived = false;
            float distance = (destination - Position).magnitude;
            bool thrustersSuffice = distance <= _settings.ManeuverRangeUnits && Speed <= _settings.ThrusterMaximumSpeed;
            Phase = thrustersSuffice || !MainDriveAvailable
                ? ShipMotionPhase.Maneuvering
                : ShipMotionPhase.Aligning;
        }

        /// <summary>
        /// Creeps to a destination on thrusters only, no faster than the given speed, while turning to and
        /// holding a heading, e.g. the last meters to a docking port or backing away from it. The ship has
        /// arrived once it rests at the destination with that heading.
        /// </summary>
        public void SetPreciseDestination(Vector2 destination, float maximumSpeed, float headingDegrees)
        {
            if (maximumSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSpeed), "A maneuver needs a positive speed.");
            }

            _destination = destination;
            HasArrived = false;
            _isPrecise = true;
            _preciseMaximumSpeed = Mathf.Min(maximumSpeed, _settings.ThrusterMaximumSpeed);
            _preciseHeadingDegrees = headingDegrees;
            Phase = ShipMotionPhase.Maneuvering;
        }

        /// <summary>
        /// Moves the destination without restarting the maneuver, for targets that move such as a
        /// carrier's landing bay. A ship that already stopped starts a new approach.
        /// </summary>
        public void RetargetDestination(Vector2 destination)
        {
            if (Phase == ShipMotionPhase.Idle || Phase == ShipMotionPhase.Cruising)
            {
                if ((destination - Position).magnitude > _settings.ArrivalToleranceUnits || Phase == ShipMotionPhase.Cruising)
                {
                    SetDestination(destination);
                }
                return;
            }

            _destination = destination;
        }

        /// <summary>
        /// Accelerates along a direction until told otherwise, never braking for a destination. A speed
        /// limit holds the ship at that speed, e.g. for an attack run; it can be renewed every frame to
        /// steer without restarting anything.
        /// </summary>
        public void SetCourse(Vector2 direction, float maximumSpeed = float.PositiveInfinity)
        {
            if (direction.sqrMagnitude < 1e-8f)
            {
                throw new ArgumentException("A course needs a direction.", nameof(direction));
            }
            if (maximumSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSpeed), "A course needs a positive speed.");
            }

            _courseDirection = direction.normalized;
            _courseMaximumSpeed = maximumSpeed;
            _isPrecise = false;
            HasArrived = false;
            Phase = ShipMotionPhase.Cruising;
        }

        /// <summary>
        /// Brakes to rest from the current motion, e.g. when an order is dropped mid-flight: the ship
        /// comes to a stop where its momentum carries it instead of turning back to where it was.
        /// </summary>
        public void Stop()
        {
            Vector2 ahead = Speed > 1e-4f ? Velocity / Speed * StoppingDistance(Speed) : Vector2.zero;
            SetDestination(Position + ahead);
        }

        /// <summary>Moves the ship without changing velocity or heading, e.g. when it exits a jump.</summary>
        public void Teleport(Vector2 position)
        {
            Position = position;
        }

        /// <summary>
        /// Distance needed to come to a stop from a speed towards the destination, including the coast
        /// during the flip and part of the thruster approach. Used to plan braking and jump exits.
        /// </summary>
        public float StoppingDistance(float speedTowardsDestination)
        {
            float speed = Mathf.Max(0f, speedTowardsDestination);
            return speed * speed / (2f * _settings.MainDriveAcceleration) + speed * _settings.FlipSeconds
                   + _settings.ManeuverRangeUnits * 0.5f;
        }

        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            MainDriveActive = false;
            ThrustersActive = false;
            if (!MainDriveAvailable && UsesMainDrive(Phase))
            {
                Phase = ShipMotionPhase.Maneuvering;
            }

            switch (Phase)
            {
                case ShipMotionPhase.Idle:
                    ApplyThrusters(Vector2.zero, deltaTime);
                    break;
                case ShipMotionPhase.Aligning:
                    StepAligning(deltaTime);
                    break;
                case ShipMotionPhase.Burning:
                    StepBurning(deltaTime);
                    break;
                case ShipMotionPhase.Flipping:
                    StepFlipping(deltaTime);
                    break;
                case ShipMotionPhase.Braking:
                    StepBraking(deltaTime);
                    break;
                case ShipMotionPhase.Maneuvering:
                    StepManeuvering(deltaTime);
                    break;
                case ShipMotionPhase.Cruising:
                    StepCruising(deltaTime);
                    break;
            }

            Velocity = Vector2.ClampMagnitude(Velocity, _settings.MaximumSpeed);
            Position += Velocity * deltaTime;
        }

        void StepAligning(float deltaTime)
        {
            Vector2 toDestination = _destination - Position;
            float distance = toDestination.magnitude;
            // Already there: let the thrusters take out the remaining speed, there is no direction to turn to.
            if (distance <= 1e-6f || (distance <= _settings.ManeuverRangeUnits && Speed <= _settings.ThrusterMaximumSpeed))
            {
                Phase = ShipMotionPhase.Maneuvering;
                return;
            }

            Vector2 direction = toDestination / distance;
            RotateTowards(DirectionToHeading(direction), deltaTime);
            KeepMotionTowards(direction, deltaTime);
            if (IsHeadingAligned(direction))
            {
                Phase = ShipMotionPhase.Burning;
            }
        }

        void StepBurning(float deltaTime)
        {
            Vector2 toDestination = _destination - Position;
            float distance = toDestination.magnitude;
            Vector2 direction = distance > 1e-6f ? toDestination / distance : Forward;
            if (distance <= StoppingDistance(Vector2.Dot(Velocity, direction)))
            {
                Phase = ShipMotionPhase.Flipping;
                return;
            }

            RotateTowards(DirectionToHeading(direction), deltaTime);
            Velocity += Forward * (_settings.MainDriveAcceleration * deltaTime);
            MainDriveActive = true;
            KeepMotionTowards(direction, deltaTime);
        }

        void StepFlipping(float deltaTime)
        {
            Vector2 toDestination = _destination - Position;
            if (Speed <= _settings.ThrusterMaximumSpeed)
            {
                Phase = toDestination.magnitude <= _settings.ManeuverRangeUnits ? ShipMotionPhase.Maneuvering : ShipMotionPhase.Aligning;
                return;
            }

            Vector2 retrograde = -Velocity.normalized;
            RotateTowards(DirectionToHeading(retrograde), deltaTime);
            KeepMotionTowards(toDestination.normalized, deltaTime);
            if (IsHeadingAligned(retrograde))
            {
                Phase = ShipMotionPhase.Braking;
            }
        }

        void StepBraking(float deltaTime)
        {
            Vector2 toDestination = _destination - Position;
            float distance = toDestination.magnitude;
            Vector2 direction = distance > 1e-6f ? toDestination / distance : -Forward;
            float speedTowards = Vector2.Dot(Velocity, direction);

            // Slow enough, or overshot: hand over to thrusters or plan a new burn.
            if (speedTowards <= _settings.ThrusterMaximumSpeed)
            {
                Phase = distance <= _settings.ManeuverRangeUnits * 2f ? ShipMotionPhase.Maneuvering : ShipMotionPhase.Aligning;
                return;
            }

            RotateTowards(DirectionToHeading(-Velocity.normalized), deltaTime);
            float allowedSpeed = Mathf.Sqrt(2f * _settings.MainDriveAcceleration * Mathf.Max(0f, distance - _settings.ManeuverRangeUnits * 0.5f));
            if (speedTowards > allowedSpeed)
            {
                Velocity += Forward * (_settings.MainDriveAcceleration * deltaTime);
                MainDriveActive = true;
            }
            KeepMotionTowards(direction, deltaTime);
        }

        void StepManeuvering(float deltaTime)
        {
            Vector2 toDestination = _destination - Position;
            float distance = toDestination.magnitude;
            if (_isPrecise)
            {
                RotateTowards(_preciseHeadingDegrees, deltaTime);
            }
            if (HasReachedDestination(distance))
            {
                Position = _destination;
                Velocity = Vector2.zero;
                if (_isPrecise)
                {
                    HeadingDegrees = _preciseHeadingDegrees;
                }
                Phase = ShipMotionPhase.Idle;
                HasArrived = true;
                return;
            }
            // A precise maneuver stays on thrusters however far it goes.
            if (distance > _settings.ManeuverRangeUnits * 2f && MainDriveAvailable && !_isPrecise)
            {
                Phase = ShipMotionPhase.Aligning;
                return;
            }

            // Approach speed that the thrusters can still cancel before reaching the destination. A precise
            // maneuver brakes gently all the way down to rest, so the last meters are a soft touch.
            float approachSpeed = _isPrecise
                ? _preciseMaximumSpeed * Mathf.Sqrt(Mathf.Min(1f, distance / PreciseBrakingDistanceUnits))
                : Mathf.Min(_settings.ThrusterMaximumSpeed, Mathf.Sqrt(_settings.ThrusterAcceleration * distance));
            Vector2 desiredVelocity = distance > 1e-6f ? toDestination / distance * approachSpeed : Vector2.zero;
            ApplyThrusters(desiredVelocity, deltaTime);
        }

        /// <summary>
        /// Normal moves snap within the arrival tolerance; a precise maneuver only once it practically
        /// rests on the destination with its heading, so it never visibly jumps the last meters.
        /// </summary>
        bool HasReachedDestination(float distance)
        {
            if (!_isPrecise)
            {
                return distance <= _settings.ArrivalToleranceUnits && Speed <= _settings.ThrusterMaximumSpeed * SnapSpeedFactor;
            }
            return distance <= PreciseArrivalToleranceUnits && Speed <= PreciseArrivalSpeed &&
                   Mathf.Abs(Mathf.DeltaAngle(HeadingDegrees, _preciseHeadingDegrees)) <= PreciseHeadingToleranceDegrees;
        }

        void StepCruising(float deltaTime)
        {
            RotateTowards(DirectionToHeading(_courseDirection), deltaTime);
            float speedAlong = Vector2.Dot(Velocity, _courseDirection);
            if (MainDriveAvailable && IsHeadingAligned(_courseDirection) && speedAlong < _courseMaximumSpeed)
            {
                Velocity += Forward * (Mathf.Min(_settings.MainDriveAcceleration * deltaTime, _courseMaximumSpeed - speedAlong));
                MainDriveActive = true;
            }
            KeepMotionTowards(_courseDirection, deltaTime, _courseMaximumSpeed);
        }

        /// <summary>
        /// Thrusters cancel all velocity that does not point towards the given direction, and any speed
        /// along it beyond the limit.
        /// </summary>
        void KeepMotionTowards(Vector2 direction, float deltaTime, float maximumSpeed = float.PositiveInfinity)
        {
            float along = Mathf.Clamp(Vector2.Dot(Velocity, direction), 0f, maximumSpeed);
            ApplyThrusters(direction * along, deltaTime);
        }

        void ApplyThrusters(Vector2 desiredVelocity, float deltaTime)
        {
            Vector2 corrected = Vector2.MoveTowards(Velocity, desiredVelocity, _settings.ThrusterAcceleration * deltaTime);
            ThrustersActive |= (corrected - Velocity).sqrMagnitude > 1e-8f;
            Velocity = corrected;
        }

        void RotateTowards(float targetHeadingDegrees, float deltaTime)
        {
            HeadingDegrees = Mathf.MoveTowardsAngle(HeadingDegrees, targetHeadingDegrees, _settings.TurnRateDegreesPerSecond * deltaTime);
        }

        static bool UsesMainDrive(ShipMotionPhase phase)
        {
            return phase == ShipMotionPhase.Aligning || phase == ShipMotionPhase.Burning
                || phase == ShipMotionPhase.Flipping || phase == ShipMotionPhase.Braking;
        }

        bool IsHeadingAligned(Vector2 direction)
        {
            return Mathf.Abs(Mathf.DeltaAngle(HeadingDegrees, DirectionToHeading(direction))) <= _settings.AlignToleranceDegrees;
        }

        public static Vector2 HeadingToDirection(float headingDegrees)
        {
            float radians = headingDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        public static float DirectionToHeading(Vector2 direction)
        {
            return Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        }
    }
}

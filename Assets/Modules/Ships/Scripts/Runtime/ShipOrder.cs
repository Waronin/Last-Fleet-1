using UnityEngine;

namespace LastFleet.Ships
{
    public enum ShipOrderKind
    {
        /// <summary>Move to a point in a zone, jumping there first if needed.</summary>
        Move,

        /// <summary>Fly to a hangar's landing bay and land, following its carrier across zones.</summary>
        Dock,

        /// <summary>Fly to a hangar's landing bay and stop next to it without landing, e.g. to unload cargo.</summary>
        Approach,

        /// <summary>
        /// Creep to a point on thrusters only, slowly and with a fixed heading, e.g. the last meters to a
        /// docking port or backing away from it.
        /// </summary>
        Maneuver,

        /// <summary>
        /// Fly at another ship at a set speed without stopping, pass beside it and come about for the
        /// next pass, across zones if it jumps away: an attack run. The order lasts until the target is
        /// gone or lands in a hangar.
        /// </summary>
        Intercept
    }

    public readonly struct ShipOrder
    {
        ShipOrder(ShipOrderKind kind, int zoneIndex, Vector3 localPoint, Hangar hangar, float headingDegrees, float maximumSpeed,
            ShipAgent target = null, float passOffsetUnits = 0f, float breakOffUnits = 0f)
        {
            Kind = kind;
            ZoneIndex = zoneIndex;
            LocalPoint = localPoint;
            Hangar = hangar;
            HeadingDegrees = headingDegrees;
            MaximumSpeed = maximumSpeed;
            Target = target;
            PassOffsetUnits = passOffsetUnits;
            BreakOffUnits = breakOffUnits;
        }

        public ShipOrderKind Kind { get; }

        /// <summary>Point-of-interest index of the target zone (move and maneuver orders).</summary>
        public int ZoneIndex { get; }

        /// <summary>Target point relative to the zone center (move and maneuver orders).</summary>
        public Vector3 LocalPoint { get; }

        /// <summary>Target hangar (dock orders).</summary>
        public Hangar Hangar { get; }

        /// <summary>Heading the ship holds during a maneuver.</summary>
        public float HeadingDegrees { get; }

        /// <summary>Speed limit of a maneuver or an intercept in units per second.</summary>
        public float MaximumSpeed { get; }

        /// <summary>Ship to intercept (intercept orders).</summary>
        public ShipAgent Target { get; }

        /// <summary>How far beside the target an intercepting ship passes; the sign picks the side.</summary>
        public float PassOffsetUnits { get; }

        /// <summary>How far an intercepting ship flies on past its target before it comes about.</summary>
        public float BreakOffUnits { get; }

        public static ShipOrder MoveTo(int zoneIndex, Vector3 localPoint)
        {
            return new ShipOrder(ShipOrderKind.Move, zoneIndex, localPoint, null, 0f, 0f);
        }

        public static ShipOrder ManeuverTo(int zoneIndex, Vector3 localPoint, float headingDegrees, float maximumSpeed)
        {
            return new ShipOrder(ShipOrderKind.Maneuver, zoneIndex, localPoint, null, headingDegrees, maximumSpeed);
        }

        public static ShipOrder DockAt(Hangar hangar)
        {
            return new ShipOrder(ShipOrderKind.Dock, -1, Vector3.zero, hangar, 0f, 0f);
        }

        public static ShipOrder ApproachBay(Hangar hangar)
        {
            return new ShipOrder(ShipOrderKind.Approach, -1, Vector3.zero, hangar, 0f, 0f);
        }

        public static ShipOrder Intercept(ShipAgent target, float speedUnitsPerSecond, float passOffsetUnits, float breakOffUnits)
        {
            if (target == null)
            {
                throw new System.ArgumentNullException(nameof(target));
            }
            if (speedUnitsPerSecond <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(speedUnitsPerSecond), "An intercept needs a positive speed.");
            }
            if (breakOffUnits < 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(breakOffUnits), "The break-off distance must not be negative.");
            }
            return new ShipOrder(ShipOrderKind.Intercept, -1, Vector3.zero, null, 0f, speedUnitsPerSecond, target, passOffsetUnits, breakOffUnits);
        }

        /// <summary>Whether the order follows something that moves: a hangar's landing bay or another ship.</summary>
        public bool FollowsMovingTarget => TargetsHangar || Kind == ShipOrderKind.Intercept;

        /// <summary>Whether the order targets a hangar's landing bay (dock or approach).</summary>
        public bool TargetsHangar => Kind == ShipOrderKind.Dock || Kind == ShipOrderKind.Approach;
    }
}

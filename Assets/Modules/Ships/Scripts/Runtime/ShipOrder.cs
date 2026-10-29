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
        Maneuver
    }

    public readonly struct ShipOrder
    {
        ShipOrder(ShipOrderKind kind, int zoneIndex, Vector3 localPoint, Hangar hangar, float headingDegrees, float maximumSpeed)
        {
            Kind = kind;
            ZoneIndex = zoneIndex;
            LocalPoint = localPoint;
            Hangar = hangar;
            HeadingDegrees = headingDegrees;
            MaximumSpeed = maximumSpeed;
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

        /// <summary>Speed limit of a maneuver in units per second.</summary>
        public float MaximumSpeed { get; }

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

        /// <summary>Whether the order targets a hangar's landing bay (dock or approach).</summary>
        public bool TargetsHangar => Kind == ShipOrderKind.Dock || Kind == ShipOrderKind.Approach;
    }
}

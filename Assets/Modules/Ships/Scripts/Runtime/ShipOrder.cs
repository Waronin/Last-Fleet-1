using UnityEngine;

namespace LastFleet.Ships
{
    public enum ShipOrderKind
    {
        /// <summary>Move to a point in a zone, jumping there first if needed.</summary>
        Move,

        /// <summary>Fly to a hangar's landing bay and land, following its carrier across zones.</summary>
        Dock
    }

    public readonly struct ShipOrder
    {
        ShipOrder(ShipOrderKind kind, int zoneIndex, Vector3 localPoint, Hangar hangar)
        {
            Kind = kind;
            ZoneIndex = zoneIndex;
            LocalPoint = localPoint;
            Hangar = hangar;
        }

        public ShipOrderKind Kind { get; }

        /// <summary>Point-of-interest index of the target zone (move orders).</summary>
        public int ZoneIndex { get; }

        /// <summary>Target point relative to the zone center (move orders).</summary>
        public Vector3 LocalPoint { get; }

        /// <summary>Target hangar (dock orders).</summary>
        public Hangar Hangar { get; }

        public static ShipOrder MoveTo(int zoneIndex, Vector3 localPoint)
        {
            return new ShipOrder(ShipOrderKind.Move, zoneIndex, localPoint, null);
        }

        public static ShipOrder DockAt(Hangar hangar)
        {
            return new ShipOrder(ShipOrderKind.Dock, -1, Vector3.zero, hangar);
        }
    }
}

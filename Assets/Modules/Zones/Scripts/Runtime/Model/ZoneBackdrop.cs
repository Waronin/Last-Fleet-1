using UnityEngine;

namespace LastFleet.Zones
{
    public enum ZoneBackdropKind
    {
        None,
        Star,
        Planet,
        Moon
    }

    /// <summary>The large body a zone is located at, drawn as scenery behind the zone plane.</summary>
    public sealed class ZoneBackdrop
    {
        public ZoneBackdrop(ZoneBackdropKind kind, Color color)
        {
            Kind = kind;
            Color = color;
        }

        public ZoneBackdropKind Kind { get; }
        public Color Color { get; }
    }
}

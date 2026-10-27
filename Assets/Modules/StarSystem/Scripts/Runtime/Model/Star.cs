using UnityEngine;

namespace LastFleet.StarSystem
{
    public enum StarClass
    {
        RedDwarf,
        OrangeDwarf,
        YellowDwarf,
        WhiteStar,
        BlueGiant
    }

    public sealed class Star
    {
        public Star(StarClass starClass, Color color, float radiusSolar)
        {
            Class = starClass;
            Color = color;
            RadiusSolar = radiusSolar;
        }

        public StarClass Class { get; }
        public Color Color { get; }
        public float RadiusSolar { get; }
    }
}

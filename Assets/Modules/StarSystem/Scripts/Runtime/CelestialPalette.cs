using UnityEngine;

namespace LastFleet.StarSystem
{
    /// <summary>Blockout colors of celestial bodies, shared by the system map and the zone views.</summary>
    public static class CelestialPalette
    {
        public static readonly Color MoonColor = new Color(0.6f, 0.6f, 0.6f);

        public static Color PlanetColor(PlanetKind kind)
        {
            return kind switch
            {
                PlanetKind.Rocky => new Color(0.55f, 0.5f, 0.45f),
                PlanetKind.Desert => new Color(0.8f, 0.65f, 0.4f),
                PlanetKind.Ocean => new Color(0.2f, 0.45f, 0.8f),
                PlanetKind.Ice => new Color(0.8f, 0.9f, 1f),
                PlanetKind.GasGiant => new Color(0.85f, 0.6f, 0.35f),
                PlanetKind.IceGiant => new Color(0.45f, 0.75f, 0.85f),
                _ => Color.magenta
            };
        }

        public static Color BeltColor(AsteroidBeltComposition composition)
        {
            return composition switch
            {
                AsteroidBeltComposition.Metallic => new Color(0.6f, 0.55f, 0.5f),
                AsteroidBeltComposition.Icy => new Color(0.75f, 0.85f, 0.95f),
                AsteroidBeltComposition.GasRich => new Color(1f, 0.6f, 0.25f),
                AsteroidBeltComposition.Mixed => new Color(0.65f, 0.6f, 0.55f),
                _ => Color.magenta
            };
        }
    }
}

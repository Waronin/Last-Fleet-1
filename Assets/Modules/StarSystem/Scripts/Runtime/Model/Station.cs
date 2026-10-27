using UnityEngine;

namespace LastFleet.StarSystem
{
    public enum StationKind
    {
        /// <summary>In orbit around a planet.</summary>
        Orbital,

        /// <summary>In the void between the planets.</summary>
        DeepSpace
    }

    public sealed class Station
    {
        public Station(string name, StationKind kind, Vector2 positionAU, Planet orbitedPlanet)
        {
            Name = name;
            Kind = kind;
            PositionAU = positionAU;
            OrbitedPlanet = orbitedPlanet;
        }

        public string Name { get; }
        public StationKind Kind { get; }
        public Vector2 PositionAU { get; }

        /// <summary>The planet an orbital station circles; null for deep-space stations.</summary>
        public Planet OrbitedPlanet { get; }
    }
}

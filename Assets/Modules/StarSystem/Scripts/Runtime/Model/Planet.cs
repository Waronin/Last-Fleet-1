using System.Collections.Generic;
using UnityEngine;

namespace LastFleet.StarSystem
{
    public enum PlanetKind
    {
        Rocky,
        Desert,
        Ocean,
        Ice,
        GasGiant,
        IceGiant
    }

    public sealed class Planet
    {
        readonly Moon[] _moons;

        public Planet(string name, PlanetKind kind, float orbitRadiusAU, float orbitAngleDegrees, float radiusEarth, IReadOnlyList<Moon> moons)
        {
            Name = name;
            Kind = kind;
            OrbitRadiusAU = orbitRadiusAU;
            OrbitAngleDegrees = orbitAngleDegrees;
            RadiusEarth = radiusEarth;
            _moons = new Moon[moons.Count];
            for (int i = 0; i < moons.Count; i++)
            {
                _moons[i] = moons[i];
            }
        }

        public string Name { get; }
        public PlanetKind Kind { get; }
        public float OrbitRadiusAU { get; }
        public float OrbitAngleDegrees { get; }
        public float RadiusEarth { get; }
        public IReadOnlyList<Moon> Moons => _moons;

        /// <summary>Gas giants can be skimmed for gas, which the refinery turns into fuel.</summary>
        public bool IsFuelSource => Kind == PlanetKind.GasGiant;

        public Vector2 PositionAU => OrbitMath.PositionOnOrbit(OrbitRadiusAU, OrbitAngleDegrees);
    }
}

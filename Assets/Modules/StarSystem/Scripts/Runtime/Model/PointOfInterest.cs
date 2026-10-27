using UnityEngine;

namespace LastFleet.StarSystem
{
    public enum PointOfInterestKind
    {
        Star,
        Planet,
        Moon,
        AsteroidBelt,
        Station,

        /// <summary>Empty arrival zone at the system edge where the fleet jumps in.</summary>
        JumpPoint
    }

    /// <summary>
    /// A place the fleet can visit; each one becomes a local zone (GDD: Zones, Star system).
    /// </summary>
    public sealed class PointOfInterest
    {
        public PointOfInterest(string name, PointOfInterestKind kind, Vector2 positionAU, bool isFuelSource,
            Planet associatedPlanet, AsteroidBelt belt)
        {
            Name = name;
            Kind = kind;
            PositionAU = positionAU;
            IsFuelSource = isFuelSource;
            AssociatedPlanet = associatedPlanet;
            Belt = belt;
        }

        public string Name { get; }
        public PointOfInterestKind Kind { get; }
        public Vector2 PositionAU { get; }
        public bool IsFuelSource { get; }

        /// <summary>
        /// The planet itself for planet points, the parent planet for moons and orbital stations,
        /// otherwise null.
        /// </summary>
        public Planet AssociatedPlanet { get; }

        /// <summary>The belt for asteroid belt points, otherwise null.</summary>
        public AsteroidBelt Belt { get; }

        /// <summary>True for moons and orbital stations, which are drawn relative to their planet.</summary>
        public bool IsSatellite => (Kind == PointOfInterestKind.Moon || Kind == PointOfInterestKind.Station) && AssociatedPlanet != null;
    }
}

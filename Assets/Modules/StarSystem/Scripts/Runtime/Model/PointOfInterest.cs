using System;
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
        JumpPoint,

        /// <summary>A damaged capital ship adrift, in orbit or in deep space; the fleet can repair and take it in.</summary>
        StrandedShip,

        /// <summary>The remains of an old battle drifting into sensor range: metal wrecks and one dead hull to search.</summary>
        WreckField
    }

    /// <summary>
    /// A place the fleet can visit; each one becomes a local zone (GDD: Zones, Star system).
    /// </summary>
    public sealed class PointOfInterest
    {
        public PointOfInterest(string name, PointOfInterestKind kind, Vector2 positionAU, bool isFuelSource,
            Planet associatedPlanet, AsteroidBelt belt)
            : this(name, kind, positionAU, isFuelSource, associatedPlanet, belt, 0f)
        {
        }

        /// <param name="lateSignalFraction">
        /// 0 for a place the system scan shows on arrival; otherwise the share of the countdown to the enemy's
        /// arrival after which its signal is picked up (GDD: late signals).
        /// </param>
        public PointOfInterest(string name, PointOfInterestKind kind, Vector2 positionAU, bool isFuelSource,
            Planet associatedPlanet, AsteroidBelt belt, float lateSignalFraction)
        {
            if (lateSignalFraction < 0f || lateSignalFraction >= 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(lateSignalFraction), "A late signal comes before the countdown ends.");
            }
            Name = name;
            Kind = kind;
            PositionAU = positionAU;
            IsFuelSource = isFuelSource;
            AssociatedPlanet = associatedPlanet;
            Belt = belt;
            LateSignalFraction = lateSignalFraction;
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

        /// <summary>Share of the countdown after which the signal comes in; 0 when the place is known on arrival.</summary>
        public float LateSignalFraction { get; }

        /// <summary>Hidden from the system scan until its signal comes in during the countdown.</summary>
        public bool IsLateSignal => LateSignalFraction > 0f;

        /// <summary>True for moons and for stations, stranded ships and wreck fields in orbit, which are drawn relative to their planet.</summary>
        public bool IsSatellite => (Kind == PointOfInterestKind.Moon || Kind == PointOfInterestKind.Station || Kind == PointOfInterestKind.StrandedShip ||
                                    Kind == PointOfInterestKind.WreckField) &&
                                   AssociatedPlanet != null;
    }
}

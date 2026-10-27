using System;
using System.Collections.Generic;

namespace LastFleet.StarSystem
{
    /// <summary>Immutable result of star system generation.</summary>
    public sealed class StarSystemLayout
    {
        public StarSystemLayout(ulong seed, string name, Star star, IReadOnlyList<Planet> planets,
            IReadOnlyList<AsteroidBelt> asteroidBelts, IReadOnlyList<Station> stations,
            IReadOnlyList<PointOfInterest> pointsOfInterest, int arrivalPointOfInterestIndex)
        {
            if (arrivalPointOfInterestIndex < 0 || arrivalPointOfInterestIndex >= pointsOfInterest.Count ||
                pointsOfInterest[arrivalPointOfInterestIndex].Kind != PointOfInterestKind.JumpPoint)
            {
                throw new ArgumentException("The arrival index must point to a jump point.", nameof(arrivalPointOfInterestIndex));
            }

            Seed = seed;
            Name = name;
            Star = star;
            Planets = Copy(planets);
            AsteroidBelts = Copy(asteroidBelts);
            Stations = Copy(stations);
            PointsOfInterest = Copy(pointsOfInterest);
            ArrivalPointOfInterestIndex = arrivalPointOfInterestIndex;
        }

        public ulong Seed { get; }
        public string Name { get; }
        public Star Star { get; }

        /// <summary>Ordered by orbit radius, innermost first.</summary>
        public IReadOnlyList<Planet> Planets { get; }

        /// <summary>Ordered by inner radius, innermost first.</summary>
        public IReadOnlyList<AsteroidBelt> AsteroidBelts { get; }

        public IReadOnlyList<Station> Stations { get; }
        public IReadOnlyList<PointOfInterest> PointsOfInterest { get; }

        /// <summary>The jump point where the fleet arrives in this system.</summary>
        public int ArrivalPointOfInterestIndex { get; }

        static T[] Copy<T>(IReadOnlyList<T> source)
        {
            T[] copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }
            return copy;
        }
    }
}

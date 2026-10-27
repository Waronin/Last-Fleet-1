using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using UnityEngine;

namespace LastFleet.StarSystem
{
    /// <summary>
    /// Generates the contents of a star system from its seed (GDD: Star system). Planets follow a
    /// geometric orbit progression, with rocky worlds inside the frost line and giants outside it.
    /// Every system contains at least one fuel source (GDD: guaranteed fuel source).
    /// All numbers are balancing starting points.
    /// </summary>
    public static class StarSystemGenerator
    {
        const int MinimumPlanetCount = 2;
        const int MaximumPlanetCount = 8;
        const float FrostLineAU = 2.7f;
        const float MinimumOrbitGrowth = 1.4f;
        const float MaximumOrbitGrowth = 2f;
        const int MaximumBeltCount = 2;
        const float BeltChance = 0.6f;
        const float FallbackBeltInnerFactor = 1.3f;
        const float FallbackBeltOuterFactor = 1.55f;
        const int MinimumStationCount = 1;
        const int MaximumStationCount = 4;
        const float OrbitalStationChance = 0.6f;
        const float OrbitalStationDistanceAU = 0.02f;
        const float MoonSpacingAU = 0.004f;
        const float JumpPointDistanceFactor = 1.25f;

        static readonly StarProfile[] StarProfiles =
        {
            new StarProfile(StarClass.RedDwarf, 0.35f, new Color(1f, 0.45f, 0.3f), 0.1f, 0.5f, 0.5f),
            new StarProfile(StarClass.OrangeDwarf, 0.25f, new Color(1f, 0.7f, 0.4f), 0.6f, 0.9f, 0.8f),
            new StarProfile(StarClass.YellowDwarf, 0.2f, new Color(1f, 0.92f, 0.75f), 0.9f, 1.2f, 1f),
            new StarProfile(StarClass.WhiteStar, 0.12f, new Color(0.95f, 0.97f, 1f), 1.3f, 1.8f, 1.4f),
            new StarProfile(StarClass.BlueGiant, 0.08f, new Color(0.6f, 0.75f, 1f), 3f, 8f, 2f)
        };

        static readonly (PlanetKind Kind, float Weight)[] InnerPlanetKinds =
        {
            (PlanetKind.Rocky, 0.5f), (PlanetKind.Desert, 0.3f), (PlanetKind.Ocean, 0.2f)
        };

        static readonly (PlanetKind Kind, float Weight)[] OuterPlanetKinds =
        {
            (PlanetKind.GasGiant, 0.5f), (PlanetKind.IceGiant, 0.3f), (PlanetKind.Ice, 0.2f)
        };

        static readonly (AsteroidBeltComposition Composition, float Weight)[] InnerBeltCompositions =
        {
            (AsteroidBeltComposition.Metallic, 0.6f), (AsteroidBeltComposition.Mixed, 0.4f)
        };

        static readonly (AsteroidBeltComposition Composition, float Weight)[] OuterBeltCompositions =
        {
            (AsteroidBeltComposition.Icy, 0.5f), (AsteroidBeltComposition.GasRich, 0.3f), (AsteroidBeltComposition.Mixed, 0.2f)
        };

        static readonly string[] RomanNumerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        public static StarSystemLayout Generate(ulong seed, string systemName)
        {
            if (string.IsNullOrWhiteSpace(systemName))
            {
                throw new ArgumentException("A star system needs a name.", nameof(systemName));
            }

            DeterministicRandom random = new DeterministicRandom(seed);
            StarProfile profile = PickStarProfile(random);
            Star star = new Star(profile.Class, profile.Color, random.Range(profile.MinimumRadiusSolar, profile.MaximumRadiusSolar));
            float frostLineAU = FrostLineAU * profile.OrbitScale;

            List<Planet> planets = GeneratePlanets(random, systemName, profile.OrbitScale, frostLineAU);
            List<AsteroidBelt> belts = GenerateBelts(random, systemName, planets, frostLineAU);
            EnsureFuelSource(systemName, planets, belts);
            List<Station> stations = GenerateStations(random, systemName, planets);
            List<PointOfInterest> pointsOfInterest = CollectPointsOfInterest(random, systemName, planets, belts, stations);

            // The jump point is appended last so adding it does not change any earlier point of interest.
            pointsOfInterest.Add(CreateJumpPoint(random, systemName, planets, belts));
            int arrivalIndex = pointsOfInterest.Count - 1;

            return new StarSystemLayout(seed, systemName, star, planets, belts, stations, pointsOfInterest, arrivalIndex);
        }

        /// <summary>Empty arrival zone beyond the outermost orbit, where the fleet jumps in.</summary>
        static PointOfInterest CreateJumpPoint(DeterministicRandom random, string systemName, List<Planet> planets, List<AsteroidBelt> belts)
        {
            float outermostAU = planets[planets.Count - 1].OrbitRadiusAU;
            foreach (AsteroidBelt belt in belts)
            {
                outermostAU = Mathf.Max(outermostAU, belt.OuterRadiusAU);
            }

            Vector2 positionAU = OrbitMath.PositionOnOrbit(outermostAU * JumpPointDistanceFactor, random.AngleDegrees());
            return new PointOfInterest($"{systemName} Jump Point", PointOfInterestKind.JumpPoint, positionAU, false, null, null);
        }

        static List<Planet> GeneratePlanets(DeterministicRandom random, string systemName, float orbitScale, float frostLineAU)
        {
            int count = random.Range(MinimumPlanetCount, MaximumPlanetCount + 1);
            List<Planet> planets = new List<Planet>(count);
            float orbitRadiusAU = random.Range(0.3f, 0.6f) * orbitScale;

            for (int i = 0; i < count; i++)
            {
                PlanetKind kind = random.PickWeighted(orbitRadiusAU >= frostLineAU ? OuterPlanetKinds : InnerPlanetKinds);
                string name = $"{systemName} {ToRomanNumeral(i + 1)}";
                List<Moon> moons = GenerateMoons(random, name, kind);
                planets.Add(new Planet(name, kind, orbitRadiusAU, random.AngleDegrees(), PickRadiusEarth(random, kind), moons));
                orbitRadiusAU *= random.Range(MinimumOrbitGrowth, MaximumOrbitGrowth);
            }
            return planets;
        }

        static List<Moon> GenerateMoons(DeterministicRandom random, string planetName, PlanetKind kind)
        {
            int count = kind switch
            {
                PlanetKind.GasGiant => random.Range(1, 5),
                PlanetKind.IceGiant => random.Range(0, 4),
                PlanetKind.Ice => random.Range(0, 2),
                _ => random.Chance(0.4f) ? 1 : 0
            };

            List<Moon> moons = new List<Moon>(count);
            for (int i = 0; i < count; i++)
            {
                moons.Add(new Moon($"{planetName}-{(char)('a' + i)}", MoonSpacingAU * (i + 1), random.AngleDegrees()));
            }
            return moons;
        }

        /// <summary>Places belts in gaps between planets, never overlapping a planet orbit.</summary>
        static List<AsteroidBelt> GenerateBelts(DeterministicRandom random, string systemName, List<Planet> planets, float frostLineAU)
        {
            List<int> freeGaps = new List<int>();
            for (int i = 0; i < planets.Count - 1; i++)
            {
                freeGaps.Add(i);
            }

            List<(float Inner, float Outer, AsteroidBeltComposition Composition)> ranges =
                new List<(float Inner, float Outer, AsteroidBeltComposition Composition)>();
            for (int attempt = 0; attempt < MaximumBeltCount && freeGaps.Count > 0; attempt++)
            {
                if (!random.Chance(BeltChance))
                {
                    continue;
                }

                int gapListIndex = random.Range(0, freeGaps.Count);
                int gap = freeGaps[gapListIndex];
                freeGaps.RemoveAt(gapListIndex);

                float innerPlanetAU = planets[gap].OrbitRadiusAU;
                float outerPlanetAU = planets[gap + 1].OrbitRadiusAU;
                float centerAU = Mathf.Sqrt(innerPlanetAU * outerPlanetAU);
                float halfWidthAU = (outerPlanetAU - innerPlanetAU) * random.Range(0.08f, 0.15f);
                AsteroidBeltComposition composition = random.PickWeighted(centerAU >= frostLineAU ? OuterBeltCompositions : InnerBeltCompositions);
                ranges.Add((centerAU - halfWidthAU, centerAU + halfWidthAU, composition));
            }

            ranges.Sort((first, second) => first.Inner.CompareTo(second.Inner));
            List<AsteroidBelt> belts = new List<AsteroidBelt>(ranges.Count);
            foreach ((float inner, float outer, AsteroidBeltComposition composition) in ranges)
            {
                belts.Add(new AsteroidBelt(BeltName(systemName, belts.Count), inner, outer, composition));
            }
            return belts;
        }

        /// <summary>Adds a gas-rich belt beyond the outermost planet if no other fuel source exists.</summary>
        static void EnsureFuelSource(string systemName, List<Planet> planets, List<AsteroidBelt> belts)
        {
            foreach (Planet planet in planets)
            {
                if (planet.IsFuelSource)
                {
                    return;
                }
            }
            foreach (AsteroidBelt belt in belts)
            {
                if (belt.IsFuelSource)
                {
                    return;
                }
            }

            float outermostAU = planets[planets.Count - 1].OrbitRadiusAU;
            belts.Add(new AsteroidBelt(BeltName(systemName, belts.Count), outermostAU * FallbackBeltInnerFactor,
                outermostAU * FallbackBeltOuterFactor, AsteroidBeltComposition.GasRich));
        }

        static List<Station> GenerateStations(DeterministicRandom random, string systemName, List<Planet> planets)
        {
            int count = random.Range(MinimumStationCount, MaximumStationCount + 1);
            float innermostAU = planets[0].OrbitRadiusAU;
            float outermostAU = planets[planets.Count - 1].OrbitRadiusAU;
            List<Station> stations = new List<Station>(count);

            for (int i = 0; i < count; i++)
            {
                string name = $"{systemName} Station {i + 1}";
                if (random.Chance(OrbitalStationChance))
                {
                    Planet planet = planets[random.Range(0, planets.Count)];
                    Vector2 positionAU = planet.PositionAU + OrbitMath.PositionOnOrbit(OrbitalStationDistanceAU, random.AngleDegrees());
                    stations.Add(new Station(name, StationKind.Orbital, positionAU, planet));
                }
                else
                {
                    Vector2 positionAU = OrbitMath.PositionOnOrbit(random.Range(innermostAU, outermostAU), random.AngleDegrees());
                    stations.Add(new Station(name, StationKind.DeepSpace, positionAU, null));
                }
            }
            return stations;
        }

        static List<PointOfInterest> CollectPointsOfInterest(DeterministicRandom random, string systemName,
            List<Planet> planets, List<AsteroidBelt> belts, List<Station> stations)
        {
            List<PointOfInterest> points = new List<PointOfInterest>
            {
                new PointOfInterest(systemName, PointOfInterestKind.Star, Vector2.zero, false, null, null)
            };

            foreach (Planet planet in planets)
            {
                points.Add(new PointOfInterest(planet.Name, PointOfInterestKind.Planet, planet.PositionAU, planet.IsFuelSource, planet, null));
                foreach (Moon moon in planet.Moons)
                {
                    Vector2 moonPositionAU = planet.PositionAU + OrbitMath.PositionOnOrbit(moon.OrbitRadiusAU, moon.OrbitAngleDegrees);
                    points.Add(new PointOfInterest(moon.Name, PointOfInterestKind.Moon, moonPositionAU, false, planet, null));
                }
            }

            // A belt is too large for one zone; its zone is one region along the belt.
            foreach (AsteroidBelt belt in belts)
            {
                Vector2 regionPositionAU = OrbitMath.PositionOnOrbit(belt.CenterRadiusAU, random.AngleDegrees());
                points.Add(new PointOfInterest(belt.Name, PointOfInterestKind.AsteroidBelt, regionPositionAU, belt.IsFuelSource, null, belt));
            }

            foreach (Station station in stations)
            {
                points.Add(new PointOfInterest(station.Name, PointOfInterestKind.Station, station.PositionAU, false, station.OrbitedPlanet, null));
            }
            return points;
        }

        static StarProfile PickStarProfile(DeterministicRandom random)
        {
            float totalWeight = 0f;
            foreach (StarProfile profile in StarProfiles)
            {
                totalWeight += profile.Weight;
            }

            float roll = random.Range(0f, totalWeight);
            foreach (StarProfile profile in StarProfiles)
            {
                roll -= profile.Weight;
                if (roll < 0f)
                {
                    return profile;
                }
            }
            return StarProfiles[StarProfiles.Length - 1];
        }

        static float PickRadiusEarth(DeterministicRandom random, PlanetKind kind)
        {
            return kind switch
            {
                PlanetKind.GasGiant => random.Range(6f, 14f),
                PlanetKind.IceGiant => random.Range(3.5f, 5f),
                PlanetKind.Ice => random.Range(0.3f, 1.2f),
                _ => random.Range(0.4f, 1.8f)
            };
        }

        static string BeltName(string systemName, int index)
        {
            return $"{systemName} Belt {(char)('A' + index)}";
        }

        static string ToRomanNumeral(int number)
        {
            return number >= 1 && number <= RomanNumerals.Length ? RomanNumerals[number - 1] : number.ToString();
        }

        readonly struct StarProfile
        {
            public StarProfile(StarClass starClass, float weight, Color color, float minimumRadiusSolar, float maximumRadiusSolar, float orbitScale)
            {
                Class = starClass;
                Weight = weight;
                Color = color;
                MinimumRadiusSolar = minimumRadiusSolar;
                MaximumRadiusSolar = maximumRadiusSolar;
                OrbitScale = orbitScale;
            }

            public StarClass Class { get; }
            public float Weight { get; }
            public Color Color { get; }
            public float MinimumRadiusSolar { get; }
            public float MaximumRadiusSolar { get; }

            /// <summary>Hotter stars push the planets and the frost line outward.</summary>
            public float OrbitScale { get; }
        }
    }
}

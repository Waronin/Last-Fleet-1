using System;
using NUnit.Framework;

namespace LastFleet.StarSystem.Tests
{
    public class StarSystemGeneratorTests
    {
        const int SeedSampleCount = 500;

        [Test]
        public void SameSeed_ProducesIdenticalLayout()
        {
            StarSystemLayout first = StarSystemGenerator.Generate(77UL, "Kavetor");
            StarSystemLayout second = StarSystemGenerator.Generate(77UL, "Kavetor");

            Assert.AreEqual(first.Star.Class, second.Star.Class);
            Assert.AreEqual(first.Planets.Count, second.Planets.Count);
            Assert.AreEqual(first.PointsOfInterest.Count, second.PointsOfInterest.Count);
            for (int i = 0; i < first.PointsOfInterest.Count; i++)
            {
                Assert.AreEqual(first.PointsOfInterest[i].Name, second.PointsOfInterest[i].Name);
                Assert.AreEqual(first.PointsOfInterest[i].Kind, second.PointsOfInterest[i].Kind);
                Assert.AreEqual(first.PointsOfInterest[i].PositionAU, second.PointsOfInterest[i].PositionAU);
            }
        }

        [Test]
        public void EverySystem_HasAFuelSource()
        {
            for (int seed = 0; seed < SeedSampleCount; seed++)
            {
                StarSystemLayout layout = StarSystemGenerator.Generate((ulong)seed, "Test");
                bool hasFuel = false;
                foreach (PointOfInterest point in layout.PointsOfInterest)
                {
                    hasFuel |= point.IsFuelSource;
                }
                Assert.IsTrue(hasFuel, $"Seed {seed} has no fuel source.");
            }
        }

        [Test]
        public void PlanetOrbits_StrictlyIncrease()
        {
            for (int seed = 0; seed < SeedSampleCount; seed++)
            {
                StarSystemLayout layout = StarSystemGenerator.Generate((ulong)seed, "Test");
                Assert.GreaterOrEqual(layout.Planets.Count, 2);
                for (int i = 1; i < layout.Planets.Count; i++)
                {
                    Assert.Greater(layout.Planets[i].OrbitRadiusAU, layout.Planets[i - 1].OrbitRadiusAU);
                }
            }
        }

        [Test]
        public void AsteroidBelts_DoNotOverlapPlanetOrbitsOrEachOther()
        {
            for (int seed = 0; seed < SeedSampleCount; seed++)
            {
                StarSystemLayout layout = StarSystemGenerator.Generate((ulong)seed, "Test");
                for (int b = 0; b < layout.AsteroidBelts.Count; b++)
                {
                    AsteroidBelt belt = layout.AsteroidBelts[b];
                    Assert.Less(belt.InnerRadiusAU, belt.OuterRadiusAU);
                    foreach (Planet planet in layout.Planets)
                    {
                        bool insideBelt = planet.OrbitRadiusAU >= belt.InnerRadiusAU && planet.OrbitRadiusAU <= belt.OuterRadiusAU;
                        Assert.IsFalse(insideBelt, $"Seed {seed}: {planet.Name} lies inside {belt.Name}.");
                    }
                    if (b > 0)
                    {
                        Assert.Greater(belt.InnerRadiusAU, layout.AsteroidBelts[b - 1].OuterRadiusAU);
                    }
                }
            }
        }

        [Test]
        public void PointsOfInterest_CoverEveryBody()
        {
            StarSystemLayout layout = StarSystemGenerator.Generate(12UL, "Test");
            int moonCount = 0;
            foreach (Planet planet in layout.Planets)
            {
                moonCount += planet.Moons.Count;
            }

            int expected = 1 + layout.Planets.Count + moonCount + layout.AsteroidBelts.Count + layout.Stations.Count + 1;
            Assert.AreEqual(expected, layout.PointsOfInterest.Count);
            Assert.AreEqual(PointOfInterestKind.Star, layout.PointsOfInterest[0].Kind);
        }

        [Test]
        public void ArrivalJumpPoint_LiesBeyondEveryOrbit()
        {
            for (int seed = 0; seed < SeedSampleCount; seed++)
            {
                StarSystemLayout layout = StarSystemGenerator.Generate((ulong)seed, "Test");
                PointOfInterest arrival = layout.PointsOfInterest[layout.ArrivalPointOfInterestIndex];

                Assert.AreEqual(PointOfInterestKind.JumpPoint, arrival.Kind);
                float arrivalRadiusAU = arrival.PositionAU.magnitude;
                foreach (Planet planet in layout.Planets)
                {
                    Assert.Greater(arrivalRadiusAU, planet.OrbitRadiusAU);
                }
                foreach (AsteroidBelt belt in layout.AsteroidBelts)
                {
                    Assert.Greater(arrivalRadiusAU, belt.OuterRadiusAU);
                }
            }
        }

        [Test]
        public void OrbitalStations_ReferenceAPlanetOfTheSystem()
        {
            for (int seed = 0; seed < SeedSampleCount; seed++)
            {
                StarSystemLayout layout = StarSystemGenerator.Generate((ulong)seed, "Test");
                foreach (Station station in layout.Stations)
                {
                    if (station.Kind == StationKind.Orbital)
                    {
                        CollectionAssert.Contains(layout.Planets, station.OrbitedPlanet);
                    }
                    else
                    {
                        Assert.IsNull(station.OrbitedPlanet);
                    }
                }
            }
        }

        [Test]
        public void PointsOfInterest_LinkTheirBodies()
        {
            for (int seed = 0; seed < SeedSampleCount; seed++)
            {
                StarSystemLayout layout = StarSystemGenerator.Generate((ulong)seed, "Test");
                foreach (PointOfInterest point in layout.PointsOfInterest)
                {
                    switch (point.Kind)
                    {
                        case PointOfInterestKind.Planet:
                            Assert.AreEqual(point.Name, point.AssociatedPlanet.Name);
                            break;
                        case PointOfInterestKind.Moon:
                            Assert.IsNotNull(point.AssociatedPlanet);
                            Assert.IsTrue(point.IsSatellite);
                            break;
                        case PointOfInterestKind.AsteroidBelt:
                            Assert.AreEqual(point.Name, point.Belt.Name);
                            Assert.AreEqual(point.Belt.IsFuelSource, point.IsFuelSource);
                            break;
                        case PointOfInterestKind.Star:
                            Assert.IsNull(point.AssociatedPlanet);
                            Assert.IsNull(point.Belt);
                            break;
                    }
                }
            }
        }

        [Test]
        public void EmptyName_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => StarSystemGenerator.Generate(1UL, " "));
        }
    }
}

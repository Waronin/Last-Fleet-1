using System;
using System.Collections.Generic;
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

            int strandedShips = CountStrandedShips(layout);
            int lateSignals = CountLateSignals(layout);
            int expected = 1 + layout.Planets.Count + moonCount + layout.AsteroidBelts.Count + layout.Stations.Count + strandedShips + lateSignals + 1;
            Assert.AreEqual(expected, layout.PointsOfInterest.Count);
            Assert.AreEqual(PointOfInterestKind.Star, layout.PointsOfInterest[0].Kind);
        }

        [Test]
        public void StrandedShips_AreRarerThanStations_AndAtMostOnePerSystem()
        {
            int systemsWithStrandedShip = 0;
            for (int seed = 0; seed < SeedSampleCount; seed++)
            {
                StarSystemLayout layout = StarSystemGenerator.Generate((ulong)seed, "Test");
                int count = CountStrandedShips(layout);
                Assert.LessOrEqual(count, 1, $"Seed {seed}");
                systemsWithStrandedShip += count;
                foreach (PointOfInterest point in layout.PointsOfInterest)
                {
                    if (point.Kind == PointOfInterestKind.StrandedShip && point.AssociatedPlanet != null)
                    {
                        CollectionAssert.Contains(layout.Planets, point.AssociatedPlanet, $"Seed {seed}");
                    }
                }
            }

            float share = systemsWithStrandedShip / (float)SeedSampleCount;
            Assert.That(share, Is.InRange(0.12f, 0.28f), "About one system in five has a stranded ship.");
        }

        [Test]
        public void LateSignals_AreAFewStationsStrandedShipsAndWreckFields_InTheSecondPartOfTheCountdown()
        {
            int total = 0;
            int systemsWithout = 0;
            int strandedShips = 0;
            int wreckFields = 0;
            for (int seed = 0; seed < SeedSampleCount; seed++)
            {
                StarSystemLayout layout = StarSystemGenerator.Generate((ulong)seed, "Test");
                int count = CountLateSignals(layout);
                Assert.LessOrEqual(count, 2, $"Seed {seed}");
                total += count;
                systemsWithout += count == 0 ? 1 : 0;
                HashSet<string> names = new HashSet<string>();
                foreach (PointOfInterest point in layout.PointsOfInterest)
                {
                    Assert.IsTrue(names.Add(point.Name), $"Seed {seed}: duplicate name {point.Name}");
                    if (!point.IsLateSignal)
                    {
                        continue;
                    }
                    Assert.That(point.Kind, Is.EqualTo(PointOfInterestKind.Station).Or.EqualTo(PointOfInterestKind.StrandedShip)
                        .Or.EqualTo(PointOfInterestKind.WreckField));
                    Assert.That(point.LateSignalFraction, Is.InRange(0.4f, 0.9f));
                    strandedShips += point.Kind == PointOfInterestKind.StrandedShip ? 1 : 0;
                    wreckFields += point.Kind == PointOfInterestKind.WreckField ? 1 : 0;
                }
                Assert.IsFalse(layout.PointsOfInterest[layout.ArrivalPointOfInterestIndex].IsLateSignal);
            }

            Assert.That(total / (float)SeedSampleCount, Is.InRange(0.85f, 1.25f), "About one late signal per system.");
            Assert.That(systemsWithout / (float)SeedSampleCount, Is.InRange(0.12f, 0.28f), "About one system in five has none.");
            Assert.That(strandedShips / (float)total, Is.InRange(0.3f, 0.5f), "Two in five are stranded ships.");
            Assert.That(wreckFields / (float)total, Is.InRange(0.12f, 0.28f), "One in five is a wreck field.");
        }

        static int CountLateSignals(StarSystemLayout layout)
        {
            int count = 0;
            foreach (PointOfInterest point in layout.PointsOfInterest)
            {
                count += point.IsLateSignal ? 1 : 0;
            }
            return count;
        }

        static int CountStrandedShips(StarSystemLayout layout)
        {
            int count = 0;
            foreach (PointOfInterest point in layout.PointsOfInterest)
            {
                if (point.Kind == PointOfInterestKind.StrandedShip && !point.IsLateSignal)
                {
                    count++;
                }
            }
            return count;
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

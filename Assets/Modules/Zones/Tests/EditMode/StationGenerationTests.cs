using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using NUnit.Framework;

namespace LastFleet.Zones.Tests
{
    public class StationGenerationTests
    {
        const int SampleCount = 2000;

        [Test]
        public void SameSeed_GivesTheSameStation()
        {
            DeterministicRandom first = new DeterministicRandom(42UL);
            DeterministicRandom second = new DeterministicRandom(42UL);

            StationType type = StationGeneration.PickType(first, true);
            Assert.AreEqual(type, StationGeneration.PickType(second, true));
            StationProfile a = StationGeneration.RollProfile(first, type);
            StationProfile b = StationGeneration.RollProfile(second, type);
            Assert.AreEqual(a.Atmosphere, b.Atmosphere);
            Assert.AreEqual(a.Occupants, b.Occupants);
            Assert.AreEqual(a.Attitude, b.Attitude);
            Assert.AreEqual(a.Condition, b.Condition);
            Assert.AreEqual(a.Radiation, b.Radiation);
        }

        [Test]
        public void OnlyInhabitedStations_HaveAnAttitude()
        {
            DeterministicRandom random = new DeterministicRandom(7UL);
            foreach (StationType type in (StationType[])Enum.GetValues(typeof(StationType)))
            {
                for (int i = 0; i < SampleCount / 4; i++)
                {
                    StationProfile profile = StationGeneration.RollProfile(random, type);
                    Assert.AreEqual(profile.IsInhabited, profile.Attitude != StationAttitude.None);
                }
            }
        }

        [Test]
        public void Habitats_AreMostlyInhabited_IndustrialStationsMostlyAbandoned()
        {
            Assert.Greater(InhabitedShare(StationType.Habitat), 0.75f);
            Assert.Less(InhabitedShare(StationType.Industrial), 0.4f);
        }

        [Test]
        public void SafeHabitatsHoldFarMorePeopleThanSafeOutposts()
        {
            DeterministicRandom random = new DeterministicRandom(3UL);
            int smallestHabitat = int.MaxValue;
            int largestOutpost = 0;
            for (int i = 0; i < SampleCount; i++)
            {
                StationProfile habitat = StationGeneration.RollProfile(random, StationType.Habitat);
                StationProfile outpost = StationGeneration.RollProfile(random, StationType.Outpost);
                // Toxic or radioactive stations keep only a few survivors (HazardousOccupantDivisor).
                if (habitat.IsInhabited && !habitat.IsHazardous)
                {
                    smallestHabitat = Math.Min(smallestHabitat, habitat.Occupants);
                }
                if (!outpost.IsHazardous)
                {
                    largestOutpost = Math.Max(largestOutpost, outpost.Occupants);
                }
            }

            Assert.Greater(smallestHabitat, largestOutpost);
        }

        [Test]
        public void EveryBuiltType_AppearsInOrbitOrDeepSpace()
        {
            DeterministicRandom random = new DeterministicRandom(11UL);
            HashSet<StationType> seen = new HashSet<StationType>();
            for (int i = 0; i < SampleCount; i++)
            {
                seen.Add(StationGeneration.PickType(random, i % 2 == 0));
            }

            HashSet<StationType> built = new HashSet<StationType>((StationType[])Enum.GetValues(typeof(StationType)));
            built.Remove(StationType.StrandedShip);
            built.Remove(StationType.Derelict);
            CollectionAssert.AreEquivalent(built, seen);
        }

        [Test]
        public void StrandedShips_AreDamagedAndNeverHostile()
        {
            DeterministicRandom random = new DeterministicRandom(5UL);
            int inhabited = 0;
            for (int i = 0; i < SampleCount; i++)
            {
                StationProfile profile = StationGeneration.RollProfile(random, StationType.StrandedShip);
                Assert.AreNotEqual(StationCondition.Intact, profile.Condition);
                Assert.AreNotEqual(StationAttitude.Hostile, profile.Attitude);
                if (profile.IsInhabited)
                {
                    inhabited++;
                }
            }
            Assert.That(inhabited / (float)SampleCount, Is.InRange(0.5f, 0.7f), "About 60 % carry survivors.");
        }

        [Test]
        public void StrandedHulls_AreFoundBetweenAFifthAndHalfWhole_CruisersRarest()
        {
            DeterministicRandom random = new DeterministicRandom(9UL);
            Dictionary<StrandedShipClass, int> counts = new Dictionary<StrandedShipClass, int>();
            for (int i = 0; i < SampleCount; i++)
            {
                StrandedHull hull = StationGeneration.RollStrandedHull(random);
                Assert.That(hull.Fraction, Is.InRange(StrandedHull.MinimumFoundFraction, StrandedHull.MaximumFoundFraction));
                counts.TryGetValue(hull.Class, out int count);
                counts[hull.Class] = count + 1;
            }
            foreach (StrandedShipClass shipClass in (StrandedShipClass[])Enum.GetValues(typeof(StrandedShipClass)))
            {
                Assert.IsTrue(counts.ContainsKey(shipClass), $"{shipClass} never found.");
                if (shipClass != StrandedShipClass.Cruiser)
                {
                    Assert.Greater(counts[shipClass], counts[StrandedShipClass.Cruiser]);
                }
            }
        }

        [Test]
        public void LateSignals_AreNeverRuinedOrHostile_AndStrandedOnesAlwaysCarrySurvivors()
        {
            DeterministicRandom random = new DeterministicRandom(13UL);
            foreach (StationType type in (StationType[])Enum.GetValues(typeof(StationType)))
            {
                for (int i = 0; i < SampleCount / 4; i++)
                {
                    StationProfile profile = StationGeneration.RollProfile(random, type, true);
                    Assert.AreNotEqual(StationCondition.Ruined, profile.Condition, $"{type}");
                    Assert.AreNotEqual(StationAttitude.Hostile, profile.Attitude, $"{type}");
                    if (type == StationType.StrandedShip)
                    {
                        Assert.IsTrue(profile.IsInhabited);
                    }
                }
            }
        }

        [Test]
        public void LateStrandedHulls_AreInBetterShape()
        {
            DeterministicRandom random = new DeterministicRandom(17UL);
            for (int i = 0; i < SampleCount; i++)
            {
                StrandedHull hull = StationGeneration.RollStrandedHull(random, true);
                Assert.That(hull.Fraction, Is.InRange(StationGeneration.LateSignalMinimumHullFraction, StationGeneration.LateSignalMaximumHullFraction));
                Assert.Greater(hull.Fraction, StrandedHull.MinimumFoundFraction);
            }
        }

        [Test]
        public void StrandedSurvivors_AllWantToLeave()
        {
            StationProfile profile = new StationProfile(StationAtmosphere.Breathable, 200, StationAttitude.Wary, StationCondition.Damaged,
                StationRadiation.None);
            ZoneStation hulk = new ZoneStation("Hulk", UnityEngine.Vector3.zero, 0f, profile, new StrandedHull(StrandedShipClass.Liner, 0.3f));
            ZoneStation station = new ZoneStation("Station", StationType.Habitat, UnityEngine.Vector3.zero, 0f, profile);

            Assert.AreEqual(200, hulk.Residents.Willing);
            Assert.AreEqual(20, station.Residents.Willing);
        }

        [Test]
        public void LargerTypes_HaveLargerFootprintsAndMorePorts()
        {
            StationType[] bySize = { StationType.Outpost, StationType.Research, StationType.Industrial, StationType.Habitat };
            for (int i = 1; i < bySize.Length; i++)
            {
                Assert.Greater(StationTypes.FootprintRadiusUnits(bySize[i]), StationTypes.FootprintRadiusUnits(bySize[i - 1]));
                Assert.Greater(StationTypes.DockingPortCount(bySize[i]), StationTypes.DockingPortCount(bySize[i - 1]));
            }
        }

        [Test]
        public void InconsistentProfiles_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new StationProfile(StationAtmosphere.Breathable, 0, StationAttitude.Friendly,
                StationCondition.Intact, StationRadiation.None));
            Assert.Throws<ArgumentException>(() => new StationProfile(StationAtmosphere.Breathable, 10, StationAttitude.None,
                StationCondition.Intact, StationRadiation.None));
        }

        static float InhabitedShare(StationType type)
        {
            DeterministicRandom random = new DeterministicRandom(5UL);
            int inhabited = 0;
            for (int i = 0; i < SampleCount; i++)
            {
                inhabited += StationGeneration.RollProfile(random, type).IsInhabited ? 1 : 0;
            }
            return (float)inhabited / SampleCount;
        }
    }
}

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
        public void HabitatsHoldFarMorePeopleThanOutposts()
        {
            DeterministicRandom random = new DeterministicRandom(3UL);
            int smallestHabitat = int.MaxValue;
            int largestOutpost = 0;
            for (int i = 0; i < SampleCount; i++)
            {
                StationProfile habitat = StationGeneration.RollProfile(random, StationType.Habitat);
                StationProfile outpost = StationGeneration.RollProfile(random, StationType.Outpost);
                if (habitat.IsInhabited)
                {
                    smallestHabitat = Math.Min(smallestHabitat, habitat.Occupants);
                }
                largestOutpost = Math.Max(largestOutpost, outpost.Occupants);
            }

            Assert.Greater(smallestHabitat, largestOutpost);
        }

        [Test]
        public void EveryType_AppearsInOrbitOrDeepSpace()
        {
            DeterministicRandom random = new DeterministicRandom(11UL);
            HashSet<StationType> seen = new HashSet<StationType>();
            for (int i = 0; i < SampleCount; i++)
            {
                seen.Add(StationGeneration.PickType(random, i % 2 == 0));
            }

            CollectionAssert.AreEquivalent((StationType[])Enum.GetValues(typeof(StationType)), seen);
        }

        [Test]
        public void LargerTypes_HaveLargerFootprintsAndMoreBays()
        {
            StationType[] bySize = { StationType.Outpost, StationType.Research, StationType.Industrial, StationType.Habitat };
            for (int i = 1; i < bySize.Length; i++)
            {
                Assert.Greater(StationTypes.FootprintRadiusUnits(bySize[i]), StationTypes.FootprintRadiusUnits(bySize[i - 1]));
                Assert.Greater(StationTypes.DockingBayCount(bySize[i]), StationTypes.DockingBayCount(bySize[i - 1]));
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

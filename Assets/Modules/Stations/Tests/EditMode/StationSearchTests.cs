using System.Collections.Generic;
using LastFleet.Rewards;
using LastFleet.Zones;
using NUnit.Framework;

namespace LastFleet.Stations.Tests
{
    public class StationSearchTests
    {
        [Test]
        public void OnlyWalkableAbandonedStations_CanBeSearched()
        {
            Assert.AreEqual(SearchBlocker.None, StationSearchRules.Blocker(Profile(StationAtmosphere.Thin, 0)));
            Assert.AreEqual(SearchBlocker.None, StationSearchRules.Blocker(Profile(StationAtmosphere.Breathable, 0)));
            Assert.AreEqual(SearchBlocker.ToxicAtmosphere, StationSearchRules.Blocker(Profile(StationAtmosphere.Toxic, 0)));
            Assert.AreEqual(SearchBlocker.Inhabited, StationSearchRules.Blocker(Profile(StationAtmosphere.Breathable, 30)));
            Assert.AreEqual(SearchBlocker.Radiation, StationSearchRules.Blocker(Profile(StationAtmosphere.Breathable, 0, StationRadiation.Elevated)),
                "Elevated radiation is as hostile as toxic air.");
        }

        [Test]
        public void SameSeed_GivesTheSameFinds()
        {
            List<Reward> first = StationSearchRules.RollFinds(77UL, StationType.Research, StationCondition.Damaged);
            List<Reward> second = StationSearchRules.RollFinds(77UL, StationType.Research, StationCondition.Damaged);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Kind, second[i].Kind);
                Assert.AreEqual(first[i].Amount, second[i].Amount);
            }
        }

        [Test]
        public void IndustrialStations_HoldMoreThanOutposts_AndRuinsLess()
        {
            float outposts = AverageCount(StationType.Outpost, StationCondition.Intact);
            float industrial = AverageCount(StationType.Industrial, StationCondition.Intact);
            float ruins = AverageCount(StationType.Industrial, StationCondition.Ruined);

            Assert.Greater(industrial, outposts);
            Assert.Less(ruins, industrial);
        }

        [Test]
        public void Loot_IsTakenFindByFind_AndPartlyForSurvivors()
        {
            StationLoot loot = new StationLoot(new[] { Reward.OfSurvivors(10), Reward.OfFuel(50f) });

            loot.Take(6f);
            Assert.IsTrue(loot.TryPeek(out Reward rest));
            Assert.AreEqual(4, rest.People, "Four survivors wait for the next seat.");

            loot.Postpone();
            Assert.IsTrue(loot.TryPeek(out Reward next));
            Assert.AreEqual(RewardKind.Fuel, next.Kind, "Waiting survivors do not block the other finds.");

            loot.Take(50f);
            loot.Take(4f);
            Assert.IsTrue(loot.IsEmpty);
            Assert.AreEqual(3, loot.FindsMade);
        }

        static float AverageCount(StationType type, StationCondition condition)
        {
            int total = 0;
            for (ulong seed = 0; seed < 200; seed++)
            {
                total += StationSearchRules.RollFinds(seed, type, condition).Count;
            }
            return total / 200f;
        }

        static StationProfile Profile(StationAtmosphere atmosphere, int occupants, StationRadiation radiation = StationRadiation.None)
        {
            return new StationProfile(atmosphere, occupants, occupants > 0 ? StationAttitude.Friendly : StationAttitude.None,
                StationCondition.Intact, radiation);
        }
    }
}

using LastFleet.Mining;
using LastFleet.Shared.Randomness;
using LastFleet.Zones;
using NUnit.Framework;

namespace LastFleet.Quests.Tests
{
    public class SupplyRequestTests
    {
        static StationProfile Profile(int occupants, StationAttitude attitude)
        {
            return new StationProfile(StationAtmosphere.Breathable, occupants, attitude, StationCondition.Intact, StationRadiation.None);
        }

        [Test]
        public void AboutAThirdOfInhabitedStations_Ask_AndHostileOrEmptyOnesNever()
        {
            DeterministicRandom random = new DeterministicRandom(5UL);
            int asking = 0;
            const int samples = 3000;
            for (int i = 0; i < samples; i++)
            {
                if (SupplyRequestRules.Roll(random, Profile(200, StationAttitude.Friendly)) != null)
                {
                    asking++;
                }
                Assert.IsNull(SupplyRequestRules.Roll(random, Profile(200, StationAttitude.Hostile)));
                Assert.IsNull(SupplyRequestRules.Roll(random, Profile(0, StationAttitude.None)));
            }
            Assert.That(asking / (float)samples, Is.InRange(0.28f, 0.38f));
        }

        [Test]
        public void Trades_PayAnotherSupplyAtAGoodRate()
        {
            DeterministicRandom random = new DeterministicRandom(9UL);
            int trades = 0;
            int modules = 0;
            for (int i = 0; i < 3000; i++)
            {
                SupplyRequest request = SupplyRequestRules.Roll(random, Profile(1000, StationAttitude.Wary));
                if (request == null)
                {
                    continue;
                }
                if (request.RewardKind == SupplyRequestRewardKind.UpgradeModule)
                {
                    modules++;
                    continue;
                }
                trades++;
                Assert.AreNotEqual(request.Wanted, request.RewardSupply);
                Assert.GreaterOrEqual(request.RewardAmount, request.WantedAmount);
            }
            Assert.Greater(trades, modules * 2, "Most requests are trades.");
            Assert.Greater(modules, 0);
        }

        [Test]
        public void Amount_GrowsWithTheStation_WithinLimits()
        {
            Assert.AreEqual(SupplyRequestRules.MinimumAmount, SupplyRequestRules.AmountFor(5));
            Assert.AreEqual(300, SupplyRequestRules.AmountFor(1000));
            Assert.AreEqual(SupplyRequestRules.MaximumAmount, SupplyRequestRules.AmountFor(3000));
        }

        [Test]
        public void Request_IsOnlyFulfilledOnceAcceptedAndCovered()
        {
            SupplyRequest request = new SupplyRequest(Supply.Water, 300, SupplyRequestRewardKind.Supply, Supply.Fuel, 450);

            Assert.IsFalse(request.CanBeFulfilledFrom(1000f), "Not accepted yet.");
            request.Accept();
            Assert.IsFalse(request.CanBeFulfilledFrom(299f));
            Assert.IsTrue(request.CanBeFulfilledFrom(300f));
            request.Fulfil();
            Assert.IsFalse(request.IsOpen);
            request.Expire();
            Assert.AreEqual(SupplyRequestState.Fulfilled, request.State, "A fulfilled request does not expire.");
        }
    }
}

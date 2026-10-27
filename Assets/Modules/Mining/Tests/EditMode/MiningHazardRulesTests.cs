using LastFleet.Zones;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Mining.Tests
{
    public class MiningHazardRulesTests
    {
        [Test]
        public void HarmlessDeposits_NeverGoOff()
        {
            Assert.AreEqual(0f, MiningHazardRules.Probability(DepositHazard.None, 1000f));
            Assert.AreEqual(0f, MiningHazardRules.Probability(DepositHazard.Radioactive, 1000f), "Radiation waits for the crew system.");
            Assert.AreEqual(HazardEvent.None, MiningHazardRules.Roll(DepositHazard.None, 1000f, 0f));
        }

        [Test]
        public void Probability_MatchesTheMeanWaitingTime()
        {
            // After one mean waiting time the chance is 1 - 1/e, independent of how the time is split into frames.
            float expected = 1f - Mathf.Exp(-1f);
            Assert.AreEqual(expected, MiningHazardRules.Probability(DepositHazard.Brittle, MiningHazardRules.BrittleMeanSeconds), 0.0001f);

            float perFrame = MiningHazardRules.Probability(DepositHazard.GasPockets, 0.02f);
            float survived = Mathf.Pow(1f - perFrame, MiningHazardRules.GasPocketsMeanSeconds / 0.02f);
            Assert.AreEqual(expected, 1f - survived, 0.001f);
        }

        [Test]
        public void Roll_PicksTheEventOfTheHazard()
        {
            Assert.AreEqual(HazardEvent.Shatter, MiningHazardRules.Roll(DepositHazard.Brittle, 10f, 0f));
            Assert.AreEqual(HazardEvent.GasExplosion, MiningHazardRules.Roll(DepositHazard.GasPockets, 10f, 0f));
            Assert.AreEqual(HazardEvent.None, MiningHazardRules.Roll(DepositHazard.GasPockets, 10f, 0.999f));
        }

        [Test]
        public void ShatterAndExplosion_UseUpTheHazard()
        {
            ResourceDeposit brittle = new ResourceDeposit(DepositShape.Asteroid, ResourceKind.Ore, Vector3.zero, 2f, 100f, DepositHazard.Brittle, 0f);
            brittle.Shatter();
            Assert.AreEqual(50f, brittle.RemainingAmount);
            Assert.AreEqual(DepositHazard.None, brittle.Hazard);

            ResourceDeposit gas = new ResourceDeposit(DepositShape.Asteroid, ResourceKind.Gas, Vector3.zero, 2f, 100f, DepositHazard.GasPockets, 0f);
            gas.ClearHazard();
            Assert.AreEqual(DepositHazard.None, gas.Hazard);
            Assert.AreEqual(100f, gas.RemainingAmount);
        }
    }
}

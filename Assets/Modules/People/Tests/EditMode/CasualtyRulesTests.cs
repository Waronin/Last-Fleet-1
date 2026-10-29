using System;
using NUnit.Framework;

namespace LastFleet.People.Tests
{
    public class CasualtyRulesTests
    {
        [Test]
        public void TenPercentHullDamage_KillsOneToTwoAndAHalfPercent()
        {
            Assert.AreEqual(60, CasualtyRules.DeathsFromHit(6000, 0.1f, 0f), "Lowest roll: 1 %.");
            Assert.AreEqual(150, CasualtyRules.DeathsFromHit(6000, 0.1f, 1f), "Highest roll: 2.5 %.");

            int middle = CasualtyRules.DeathsFromHit(6000, 0.1f, 0.5f);
            Assert.That(middle, Is.InRange(60, 150));
        }

        [Test]
        public void Deaths_ScaleWithTheHullLost()
        {
            int small = CasualtyRules.DeathsFromHit(8000, 0.02f, 0.5f);
            int large = CasualtyRules.DeathsFromHit(8000, 0.2f, 0.5f);

            Assert.Greater(large, small);
            Assert.AreEqual(0, CasualtyRules.DeathsFromHit(8000, 0f, 1f), "No damage, no deaths.");
        }

        [Test]
        public void OneHit_KillsAtMostAQuarter_TheDestructionTakesTheRest()
        {
            Assert.AreEqual(250, CasualtyRules.DeathsFromHit(1000, 5f, 1f), "Hull loss is capped at the whole hull.");
            Assert.AreEqual(0, CasualtyRules.DeathsFromHit(0, 0.5f, 1f));
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CasualtyRules.DeathsFromHit(-1, 0.1f, 0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => CasualtyRules.DeathsFromHit(10, -0.1f, 0.5f));
        }
    }
}

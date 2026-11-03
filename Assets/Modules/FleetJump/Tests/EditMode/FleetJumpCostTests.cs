using System;
using NUnit.Framework;

namespace LastFleet.FleetJump.Tests
{
    public class FleetJumpCostTests
    {
        [Test]
        public void FullRangeJump_CostsTheShareOfTheTank()
        {
            Assert.AreEqual(1200f, FleetJumpCost.ForShip(4000f, 50f, 50f, 0.3f), 0.001f);
        }

        [Test]
        public void ShorterJumps_CostProportionallyLess()
        {
            Assert.AreEqual(600f, FleetJumpCost.ForShip(4000f, 25f, 50f, 0.3f), 0.001f);
        }

        [Test]
        public void FullTanks_LastForThreeFullRangeJumps()
        {
            float cost = FleetJumpCost.ForShip(5000f, 50f, 50f, 0.3f);
            Assert.AreEqual(3, FleetJumpCost.JumpsLeft(5000f, cost));
            Assert.AreEqual(2, FleetJumpCost.JumpsLeft(5000f - cost, cost));
            Assert.AreEqual(0, FleetJumpCost.JumpsLeft(cost * 0.99f, cost));
        }

        [Test]
        public void InvalidInput_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FleetJumpCost.ForShip(-1f, 10f, 50f, 0.3f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FleetJumpCost.ForShip(100f, 10f, 0f, 0.3f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FleetJumpCost.JumpsLeft(100f, 0f));
        }
    }
}

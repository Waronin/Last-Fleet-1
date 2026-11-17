using LastFleet.Repairs;
using LastFleet.Zones;
using NUnit.Framework;

namespace LastFleet.StrandedShips.Tests
{
    public class StrandedRepairPlanTests
    {
        const float LinerHull = 2500f;

        [Test]
        public void ALinerAtThirtyPercent_TakesAboutThreeMinutesAndNineHundredMetal()
        {
            StrandedRepairPlan plan = new StrandedRepairPlan(LinerHull, 10f, 0.5f);
            StrandedHull hull = new StrandedHull(StrandedShipClass.Liner, 0.3f);

            Assert.AreEqual(1750f, plan.MissingHullPoints(hull), 0.01f);
            Assert.AreEqual(175f, plan.SecondsLeft(hull), 0.01f);
            Assert.AreEqual(875f, plan.MetalLeft(hull), 0.01f);
        }

        [Test]
        public void Advance_RepairsAtThePaceItsMetalPaysFor()
        {
            StrandedRepairPlan plan = new StrandedRepairPlan(LinerHull, 10f, 0.5f);
            StrandedHull hull = new StrandedHull(StrandedShipClass.Liner, 0.3f);

            RepairStep full = plan.Advance(hull, 2f, 1000f);
            Assert.AreEqual(20f, full.HullPoints, 0.01f);
            Assert.AreEqual(10f, full.Metal, 0.01f);
            Assert.IsFalse(full.IsShortOfMetal);

            RepairStep starved = plan.Advance(hull, 2f, 4f);
            Assert.AreEqual(8f, starved.HullPoints, 0.01f);
            Assert.IsTrue(starved.IsShortOfMetal);
            Assert.AreEqual((750f + 28f) / LinerHull, hull.Fraction, 1e-4f);
        }

        [Test]
        public void Advance_StopsAtAWholeHull()
        {
            StrandedRepairPlan plan = new StrandedRepairPlan(1000f, 10f, 0.5f);
            StrandedHull hull = new StrandedHull(StrandedShipClass.Tanker, 0.995f);

            RepairStep step = plan.Advance(hull, 10f, 1000f);

            Assert.AreEqual(5f, step.HullPoints, 0.01f);
            Assert.IsTrue(hull.IsWhole);
        }
    }
}

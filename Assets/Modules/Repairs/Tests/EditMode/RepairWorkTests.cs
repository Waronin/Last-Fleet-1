using System;
using NUnit.Framework;

namespace LastFleet.Repairs.Tests
{
    public class RepairWorkTests
    {
        [Test]
        public void Plan_RepairsAtTheRateAndCostsMetalPerPoint()
        {
            RepairWork work = new RepairWork(0.5f);

            RepairStep step = work.Plan(missingHull: 100f, hullPerSecond: 10f, deltaSeconds: 0.5f, metalAvailable: 1000f);

            Assert.AreEqual(5f, step.HullPoints, 1e-4f);
            Assert.AreEqual(2.5f, step.Metal, 1e-4f);
            Assert.IsFalse(step.IsShortOfMetal);
        }

        [Test]
        public void Plan_NeverRepairsMoreThanIsMissing()
        {
            RepairWork work = new RepairWork(0.5f);

            RepairStep step = work.Plan(missingHull: 3f, hullPerSecond: 10f, deltaSeconds: 1f, metalAvailable: 1000f);

            Assert.AreEqual(3f, step.HullPoints, 1e-4f);
            Assert.AreEqual(1.5f, step.Metal, 1e-4f);
        }

        [Test]
        public void Plan_IsHeldBackByMissingMetal()
        {
            RepairWork work = new RepairWork(0.5f);

            RepairStep partial = work.Plan(missingHull: 100f, hullPerSecond: 10f, deltaSeconds: 1f, metalAvailable: 2f);
            RepairStep none = work.Plan(missingHull: 100f, hullPerSecond: 10f, deltaSeconds: 1f, metalAvailable: 0f);

            Assert.AreEqual(4f, partial.HullPoints, 1e-4f, "Two metal pay for four hull points.");
            Assert.AreEqual(2f, partial.Metal, 1e-4f);
            Assert.IsTrue(partial.IsShortOfMetal);
            Assert.AreEqual(0f, none.HullPoints);
            Assert.IsTrue(none.IsShortOfMetal);
        }

        [Test]
        public void Plan_DoesNothingForAnUndamagedShip()
        {
            RepairStep step = new RepairWork(0.5f).Plan(missingHull: 0f, hullPerSecond: 10f, deltaSeconds: 1f, metalAvailable: 0f);

            Assert.AreEqual(0f, step.HullPoints);
            Assert.AreEqual(0f, step.Metal);
            Assert.IsFalse(step.IsShortOfMetal, "Nothing to pay for is no shortage.");
        }

        [Test]
        public void NegativeMetalCost_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RepairWork(-1f));
        }
    }
}

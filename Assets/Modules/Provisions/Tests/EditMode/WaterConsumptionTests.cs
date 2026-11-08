using NUnit.Framework;

namespace LastFleet.Provisions.Tests
{
    public class WaterConsumptionTests
    {
        static WaterConsumption Create()
        {
            return new WaterConsumption(50f, 0.01f, 5f, 15f);
        }

        [Test]
        public void Demand_GrowsWithThePopulation()
        {
            WaterConsumption water = Create();

            Assert.AreEqual(100f, water.DemandPerSecond(5000) * 60f, 1e-3f);
            Assert.AreEqual(200f, water.DemandPerSecond(10000) * 60f, 1e-3f);
            Assert.AreEqual(25f, water.MinutesLeft(2500f, 5000), 1e-3f);
            Assert.IsTrue(float.IsPositiveInfinity(water.MinutesLeft(100f, 0)));
        }

        [Test]
        public void EnoughWater_IsDrunk_AndNobodyDies()
        {
            WaterConsumption water = Create();

            WaterStep step = water.Advance(60f, 5000, 1000f);

            Assert.AreEqual(100f, step.WaterToTake, 1e-3f);
            Assert.AreEqual(0, step.Deaths);
            Assert.IsFalse(step.RanDry);
        }

        [Test]
        public void WithoutWater_OnePercentDiesPerMinute_InBatches()
        {
            WaterConsumption water = Create();

            WaterStep first = water.Advance(10f, 6000, 0f);
            Assert.AreEqual(0, first.Deaths, "Deaths wait for the batch.");
            Assert.IsTrue(first.RanDry);

            WaterStep second = water.Advance(50f, 6000, 0f);
            Assert.AreEqual(60, second.Deaths);
            Assert.IsFalse(second.RanDry, "Running dry is reported once.");
        }

        [Test]
        public void HalfTheWaterNeeded_KillsHalfAsMany()
        {
            WaterConsumption water = Create();

            WaterStep step = water.Advance(60f, 6000, 60f);

            Assert.AreEqual(60f, step.WaterToTake, 1e-3f);
            Assert.AreEqual(30, step.Deaths);
        }

        [Test]
        public void LowWater_IsWarnedOnce_UntilItRecovers()
        {
            WaterConsumption water = Create();

            Assert.IsFalse(water.Advance(1f, 5000, 2000f).BecameLow);
            Assert.IsTrue(water.Advance(1f, 5000, 400f).BecameLow);
            Assert.IsFalse(water.Advance(1f, 5000, 390f).BecameLow);
            Assert.IsFalse(water.Advance(1f, 5000, 2000f).BecameLow);
            Assert.IsTrue(water.Advance(1f, 5000, 400f).BecameLow);
        }
    }
}

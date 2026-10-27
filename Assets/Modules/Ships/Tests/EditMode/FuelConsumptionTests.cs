using System;
using NUnit.Framework;

namespace LastFleet.Ships.Tests
{
    public class FuelConsumptionTests
    {
        [Test]
        public void Travel_CostsPerKilometre()
        {
            FuelConsumption consumption = new FuelConsumption(0.5f, 2f);

            // 1 unit is 100 m, so 30 units are 3 km.
            Assert.AreEqual(1.5f, consumption.ForTravel(30f), 1e-5f);
            Assert.AreEqual(0f, consumption.ForTravel(0f));
        }

        [Test]
        public void Jump_CostsPerAU()
        {
            FuelConsumption consumption = new FuelConsumption(0.5f, 2f);

            Assert.AreEqual(7f, consumption.ForJump(3.5f), 1e-5f);
        }

        [Test]
        public void NegativeRates_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FuelConsumption(-1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FuelConsumption(1f, -1f));
        }
    }
}

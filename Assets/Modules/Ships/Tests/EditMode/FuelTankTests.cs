using System;
using NUnit.Framework;

namespace LastFleet.Ships.Tests
{
    public class FuelTankTests
    {
        [Test]
        public void NewTank_StartsFull()
        {
            FuelTank tank = new FuelTank(100f);

            Assert.AreEqual(100f, tank.Amount);
            Assert.AreEqual(0f, tank.FreeSpace);
            Assert.IsFalse(tank.IsEmpty);
        }

        [Test]
        public void Drain_BurnsAtMostWhatIsLeft()
        {
            FuelTank tank = new FuelTank(10f);

            Assert.AreEqual(4f, tank.Drain(4f));
            Assert.AreEqual(6f, tank.Drain(50f), "Only the rest can be burnt.");
            Assert.IsTrue(tank.IsEmpty);
            Assert.AreEqual(0f, tank.Drain(1f));
        }

        [Test]
        public void TryDrain_IsAllOrNothing()
        {
            FuelTank tank = new FuelTank(10f);

            Assert.IsFalse(tank.TryDrain(10.5f));
            Assert.AreEqual(10f, tank.Amount, "A refused drain burns nothing.");
            Assert.IsTrue(tank.TryDrain(10f));
            Assert.IsTrue(tank.IsEmpty);
        }

        [Test]
        public void TransferTo_IsLimitedBySourceTargetSpaceAndMaximum()
        {
            FuelTank carrier = new FuelTank(1000f);
            FuelTank harvester = new FuelTank(100f);
            harvester.Drain(80f);

            Assert.AreEqual(25f, carrier.TransferTo(harvester, 25f), "Limited by the pump amount.");
            Assert.AreEqual(55f, carrier.TransferTo(harvester, 500f), "Limited by the target's free space.");
            Assert.AreEqual(100f, harvester.Amount);
            Assert.AreEqual(920f, carrier.Amount);

            carrier.Drain(915f);
            harvester.Drain(100f);
            Assert.AreEqual(5f, carrier.TransferTo(harvester, 50f), "Limited by what the source holds.");
            Assert.IsTrue(carrier.IsEmpty);
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            FuelTank tank = new FuelTank(10f);

            Assert.Throws<ArgumentOutOfRangeException>(() => new FuelTank(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => tank.Drain(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => tank.Fill(-1f));
            Assert.Throws<ArgumentException>(() => tank.TransferTo(tank, 1f));
            Assert.Throws<ArgumentNullException>(() => tank.TransferTo(null, 1f));
        }
    }
}

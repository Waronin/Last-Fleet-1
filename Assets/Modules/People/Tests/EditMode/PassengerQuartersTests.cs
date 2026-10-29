using System;
using NUnit.Framework;

namespace LastFleet.People.Tests
{
    public class PassengerQuartersTests
    {
        [Test]
        public void AboveCapacity_IsOvercrowdedUpToTheHardLimit()
        {
            PassengerQuarters quarters = new PassengerQuarters(1000, 900);

            Assert.IsFalse(quarters.IsOvercrowded);
            Assert.AreEqual(1500, quarters.HardLimit);
            Assert.AreEqual(400, quarters.Add(400));
            Assert.IsTrue(quarters.IsOvercrowded, "1300 of 1000 is overcrowded.");
            Assert.AreEqual(200, quarters.Add(500), "Only 200 more fit below the hard limit.");
            Assert.AreEqual(1500, quarters.Count);
            Assert.AreEqual(0, quarters.FreeSpace);
        }

        [Test]
        public void Remove_TakesAtMostEveryone()
        {
            PassengerQuarters quarters = new PassengerQuarters(100, 30);

            Assert.AreEqual(10, quarters.Remove(10));
            Assert.AreEqual(20, quarters.Remove(50));
            Assert.AreEqual(0, quarters.Count);
        }

        [Test]
        public void TransferTo_IsLimitedByPeopleAboardAndTargetRoom()
        {
            PassengerQuarters liner = new PassengerQuarters(1000, 600);
            PassengerQuarters shuttle = new PassengerQuarters(20, 0);

            Assert.AreEqual(30, liner.TransferTo(shuttle, 100), "The shuttle takes 30 at most (150 % of 20).");
            Assert.AreEqual(570, liner.Count);
            Assert.AreEqual(30, shuttle.Count);
            Assert.AreEqual(30, shuttle.TransferTo(liner, 50), "Only the 30 aboard can move back.");
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            PassengerQuarters quarters = new PassengerQuarters(10, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => new PassengerQuarters(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PassengerQuarters(10, 16));
            Assert.Throws<ArgumentOutOfRangeException>(() => quarters.Add(-1));
            Assert.Throws<ArgumentException>(() => quarters.TransferTo(quarters, 1));
        }
    }
}

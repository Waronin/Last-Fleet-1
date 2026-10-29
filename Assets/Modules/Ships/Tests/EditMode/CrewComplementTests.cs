using System;
using NUnit.Framework;

namespace LastFleet.Ships.Tests
{
    public class CrewComplementTests
    {
        [Test]
        public void Efficiency_IsTheShareOfTheRequiredCrew()
        {
            CrewComplement crew = new CrewComplement(3, 3);

            Assert.AreEqual(1f, crew.Efficiency);
            crew.Remove(1);
            Assert.AreEqual(2f / 3f, crew.Efficiency, 1e-5f);
            Assert.AreEqual(1, crew.Missing);
        }

        [Test]
        public void LastCrewMemberGone_LeavesAnEmptyShip()
        {
            CrewComplement crew = new CrewComplement(2, 2);

            Assert.AreEqual(2, crew.Remove(5), "Only those aboard can die.");
            Assert.IsTrue(crew.IsEmpty);
            Assert.AreEqual(0f, crew.Efficiency);
        }

        [Test]
        public void Add_FillsOnlyTheMissingPosts()
        {
            CrewComplement crew = new CrewComplement(3, 1);

            Assert.AreEqual(2, crew.Add(5));
            Assert.IsTrue(crew.IsFull);
            Assert.AreEqual(0, crew.Add(1));
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrewComplement(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrewComplement(2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrewComplement(2, 2).Remove(-1));
        }
    }
}

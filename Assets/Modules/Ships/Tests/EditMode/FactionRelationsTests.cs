using System;
using NUnit.Framework;

namespace LastFleet.Ships.Tests
{
    public class FactionRelationsTests
    {
        [Test]
        public void ThePronin_FightTheFleetAndStrandedShips_InBothDirections()
        {
            Assert.IsTrue(FactionRelations.AreHostile(Allegiance.HumanFleet, Allegiance.Pronin));
            Assert.IsTrue(FactionRelations.AreHostile(Allegiance.Pronin, Allegiance.HumanFleet));
            Assert.IsTrue(FactionRelations.AreHostile(Allegiance.Pronin, Allegiance.Stranded));
            Assert.IsTrue(FactionRelations.AreHostile(Allegiance.Stranded, Allegiance.Pronin));
        }

        [Test]
        public void TheFleet_LeavesStrandedShipsAlone()
        {
            Assert.IsFalse(FactionRelations.AreHostile(Allegiance.HumanFleet, Allegiance.Stranded));
        }

        [Test]
        public void NoSide_IsHostileToItself()
        {
            foreach (Allegiance side in (Allegiance[])Enum.GetValues(typeof(Allegiance)))
            {
                Assert.IsFalse(FactionRelations.AreHostile(side, side), side.ToString());
            }
        }
    }
}

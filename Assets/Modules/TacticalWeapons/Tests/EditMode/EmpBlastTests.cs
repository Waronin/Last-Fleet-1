using System;
using LastFleet.Ships;
using NUnit.Framework;

namespace LastFleet.TacticalWeapons.Tests
{
    public class EmpBlastTests
    {
        [Test]
        public void StunTime_FallsLinearlyFromTheCenterToTheEdge()
        {
            EmpBlast blast = new EmpBlast(radiusUnits: 200f, proninStunSeconds: 10f, humanStunSeconds: 7f);

            Assert.AreEqual(10f, blast.StunSecondsFor(Allegiance.Pronin, 0f), 1e-4f);
            Assert.AreEqual(5f, blast.StunSecondsFor(Allegiance.Pronin, 100f), 1e-4f);
            Assert.AreEqual(2.5f, blast.StunSecondsFor(Allegiance.Pronin, 150f), 1e-4f);
            Assert.AreEqual(0f, blast.StunSecondsFor(Allegiance.Pronin, 200f), "Nothing at the edge.");
            Assert.AreEqual(0f, blast.StunSecondsFor(Allegiance.Pronin, 350f), "Nothing beyond it.");
        }

        [Test]
        public void HumanShips_RecoverFaster()
        {
            EmpBlast blast = new EmpBlast(radiusUnits: 200f, proninStunSeconds: 10f, humanStunSeconds: 7f);

            Assert.AreEqual(7f, blast.StunSecondsFor(Allegiance.HumanFleet, 0f), 1e-4f);
            Assert.AreEqual(3.5f, blast.StunSecondsFor(Allegiance.HumanFleet, 100f), 1e-4f);
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EmpBlast(0f, 10f, 7f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new EmpBlast(200f, -1f, 7f));
        }
    }
}

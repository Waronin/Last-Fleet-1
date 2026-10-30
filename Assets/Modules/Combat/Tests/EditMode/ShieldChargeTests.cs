using System;
using NUnit.Framework;

namespace LastFleet.Combat.Tests
{
    public class ShieldChargeTests
    {
        [Test]
        public void Absorb_TakesTheHitUntilTheShieldIsDown()
        {
            ShieldCharge shield = new ShieldCharge(50f, 5f, 2f);

            Assert.AreEqual(0f, shield.Absorb(30f));
            Assert.AreEqual(20f, shield.Current);
            Assert.AreEqual(10f, shield.Absorb(30f));
            Assert.IsTrue(shield.IsDown);
            Assert.AreEqual(30f, shield.Absorb(30f));
        }

        [Test]
        public void Tick_RechargesOnlyAfterTheDelayWithoutHits()
        {
            ShieldCharge shield = new ShieldCharge(50f, 5f, 2f);
            shield.Absorb(40f);

            shield.Tick(1.5f);
            Assert.AreEqual(10f, shield.Current);

            shield.Tick(1f);
            Assert.AreEqual(15f, shield.Current, 0.001f);

            shield.Absorb(1f);
            shield.Tick(1f);
            Assert.AreEqual(14f, shield.Current, 0.001f);
        }

        [Test]
        public void Tick_NeverExceedsTheMaximum()
        {
            ShieldCharge shield = new ShieldCharge(50f, 5f, 0f);
            shield.Absorb(1f);

            shield.Tick(100f);

            Assert.AreEqual(50f, shield.Current);
        }

        [Test]
        public void RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShieldCharge(0f, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShieldCharge(10f, -1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShieldCharge(10f, 1f, 1f).Absorb(-1f));
        }
    }
}

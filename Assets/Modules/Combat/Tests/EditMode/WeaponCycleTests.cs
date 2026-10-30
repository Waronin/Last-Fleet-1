using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Combat.Tests
{
    public class WeaponCycleTests
    {
        const float Range = 10f;
        const float Reload = 0.5f;

        static WeaponSettings Settings()
        {
            return new WeaponSettings(Range, 5f, Reload, 0.8f, 0.4f);
        }

        [Test]
        public void HitChance_FallsLinearlyTowardsTheEdgeOfTheRange()
        {
            WeaponSettings settings = Settings();

            Assert.AreEqual(0.8f, settings.HitChance(0f), 0.001f);
            Assert.AreEqual(0.6f, settings.HitChance(5f), 0.001f);
            Assert.AreEqual(0.4f, settings.HitChance(Range), 0.001f);
            Assert.AreEqual(0f, settings.HitChance(Range + 0.1f));
        }

        [Test]
        public void TryFire_HitsWhenTheRollIsBelowTheChance()
        {
            Assert.AreEqual(ShotResult.Hit, new WeaponCycle(Settings()).TryFire(0f, 0.79f));
            Assert.AreEqual(ShotResult.Miss, new WeaponCycle(Settings()).TryFire(0f, 0.81f));
        }

        [Test]
        public void TryFire_WaitsForTheReloadAfterEveryShot()
        {
            WeaponCycle cycle = new WeaponCycle(Settings());

            Assert.AreEqual(ShotResult.Miss, cycle.TryFire(2f, 0.99f));
            Assert.AreEqual(ShotResult.NotFired, cycle.TryFire(2f, 0f));

            cycle.Tick(Reload * 0.5f);
            Assert.IsFalse(cycle.IsReady);

            cycle.Tick(Reload);
            Assert.AreEqual(ShotResult.Hit, cycle.TryFire(2f, 0f));
        }

        [Test]
        public void TryFire_PausesAfterEachBurst()
        {
            WeaponCycle cycle = new WeaponCycle(new WeaponSettings(Range, 5f, 0.1f, 0.8f, 0.4f, 3, 2f));

            for (int round = 0; round < 3; round++)
            {
                Assert.AreNotEqual(ShotResult.NotFired, cycle.TryFire(1f, 0f), $"round {round + 1}");
                Assert.AreEqual(round < 2 ? 0.1f : 2f, cycle.ReloadRemainingSeconds, 0.001f);
                cycle.Tick(cycle.ReloadRemainingSeconds);
            }
            Assert.AreNotEqual(ShotResult.NotFired, cycle.TryFire(1f, 0f));
            Assert.AreEqual(0.1f, cycle.ReloadRemainingSeconds, 0.001f);
        }

        [Test]
        public void TryFire_DoesNotShootOutOfRange()
        {
            WeaponCycle cycle = new WeaponCycle(Settings());

            Assert.AreEqual(ShotResult.NotFired, cycle.TryFire(Range + 1f, 0f));
            Assert.IsTrue(cycle.IsReady);
        }

        [Test]
        public void IsInArc_LimitsForwardGunsAndLeavesTurretsFree()
        {
            WeaponSettings forwardGuns = new WeaponSettings(Range, 5f, Reload, 0.8f, 0.4f, firingArcDegrees: 30f);
            WeaponSettings turret = Settings();

            Assert.IsTrue(forwardGuns.IsInArc(Vector2.up, new Vector2(0.2f, 1f)));
            Assert.IsFalse(forwardGuns.IsInArc(Vector2.up, new Vector2(1f, 1f)));
            Assert.IsTrue(turret.IsInArc(Vector2.up, Vector2.down));
        }

        [Test]
        public void FlightSeconds_DependOnTheRoundSpeed()
        {
            Assert.AreEqual(0.5f, new WeaponSettings(Range, 5f, Reload, 0.8f, 0.4f, projectileSpeedUnitsPerSecond: 20f).FlightSeconds(10f), 0.001f);
            Assert.AreEqual(0f, Settings().FlightSeconds(10f));
        }
    }
}

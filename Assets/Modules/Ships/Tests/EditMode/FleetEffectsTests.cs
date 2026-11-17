using System;
using LastFleet.Zones;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Ships.Tests
{
    public class FleetEffectsTests
    {
        [Test]
        public void WithoutEffects_EverythingIsUnchanged()
        {
            FleetEffects effects = new FleetEffects();

            Assert.AreEqual(1f, effects.Multiplier(FleetStat.MaximumHull));
            Assert.AreEqual(1f, effects.MiningRateMultiplier(ResourceKind.Gas));
            Assert.IsFalse(effects.Has(FleetAbility.RadiationImmunity));
        }

        [Test]
        public void SharesOfTheSameStat_AddUp()
        {
            FleetEffects effects = new FleetEffects();

            effects.Replace(new[] { new FleetStatBonus(FleetStat.MaximumHull, 0.3f), new FleetStatBonus(FleetStat.MaximumHull, -0.1f) },
                new[] { FleetAbility.RadiationImmunity });

            Assert.AreEqual(1.2f, effects.Multiplier(FleetStat.MaximumHull), 1e-5f);
            Assert.IsTrue(effects.Has(FleetAbility.RadiationImmunity));
        }

        [Test]
        public void Multiplier_NeverDropsToZero()
        {
            FleetEffects effects = new FleetEffects();

            effects.Replace(new[] { new FleetStatBonus(FleetStat.LateSignalDelay, -2f) }, Array.Empty<FleetAbility>());

            Assert.AreEqual(FleetEffects.MinimumMultiplier, effects.Multiplier(FleetStat.LateSignalDelay));
        }

        [Test]
        public void SizeAndResource_PickTheirOwnStat()
        {
            FleetEffects effects = new FleetEffects();

            effects.Replace(new[]
            {
                new FleetStatBonus(FleetStat.CapitalShipSpeed, 0.5f), new FleetStatBonus(FleetStat.SmallShipFuelUse, 0.08f),
                new FleetStatBonus(FleetStat.IceMiningRate, 0.2f)
            }, Array.Empty<FleetAbility>());

            Assert.AreEqual(1.5f, effects.SpeedMultiplier(ShipSize.Capital), 1e-5f);
            Assert.AreEqual(1f, effects.SpeedMultiplier(ShipSize.Small));
            Assert.AreEqual(1.08f, effects.FuelUseMultiplier(ShipSize.Small), 1e-5f);
            Assert.AreEqual(1f, effects.FuelUseMultiplier(ShipSize.Capital));
            Assert.AreEqual(1.2f, effects.MiningRateMultiplier(ResourceKind.Ice), 1e-5f);
            Assert.AreEqual(1f, effects.MiningRateMultiplier(ResourceKind.Ore));
        }

        [Test]
        public void Replace_DropsThePreviousEffectsAndReportsTheChange()
        {
            FleetEffects effects = new FleetEffects();
            effects.Replace(new[] { new FleetStatBonus(FleetStat.RepairRate, -0.1f) }, new[] { FleetAbility.PacifyHostileStations });
            int changes = 0;
            effects.Changed += () => changes++;

            effects.Replace(Array.Empty<FleetStatBonus>(), Array.Empty<FleetAbility>());

            Assert.AreEqual(1f, effects.Multiplier(FleetStat.RepairRate));
            Assert.IsFalse(effects.Has(FleetAbility.PacifyHostileStations));
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void HullRescale_KeepsTheIntactShare()
        {
            ShipHull hull = new ShipHull(100f);
            hull.TakeDamage(50f);

            hull.Rescale(130f);

            Assert.AreEqual(130f, hull.Maximum);
            Assert.AreEqual(65f, hull.Current, 1e-4f);
        }

        [Test]
        public void TimeScaledMotion_KeepsThePathAndTakesLessTime()
        {
            ShipMotionSettings settings = new ShipMotionSettings(40f, 12f, 60f, 4f, 6f, 25f, 0.5f, 5f);
            ShipMotion normal = new ShipMotion(settings, Vector2.zero, 0f);
            ShipMotion fast = new ShipMotion(settings.TimeScaled(1.5f), Vector2.zero, 0f);
            Vector2 destination = new Vector2(0f, 300f);
            normal.SetDestination(destination);
            fast.SetDestination(destination);

            float normalSeconds = SecondsToArrive(normal);
            float fastSeconds = SecondsToArrive(fast);

            Assert.AreEqual(normalSeconds / 1.5f, fastSeconds, normalSeconds * 0.03f);
        }

        static float SecondsToArrive(ShipMotion motion)
        {
            const float Step = 0.01f;
            float seconds = 0f;
            while (!motion.HasArrived && seconds < 600f)
            {
                motion.Step(Step);
                seconds += Step;
            }
            Assert.IsTrue(motion.HasArrived, "The ship never arrived.");
            return seconds;
        }
    }
}

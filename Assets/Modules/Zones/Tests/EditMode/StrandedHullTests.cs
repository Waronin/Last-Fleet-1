using System;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Zones.Tests
{
    public class StrandedHullTests
    {
        [Test]
        public void Repair_StopsAtWhole_AndReportsWhatItRestored()
        {
            StrandedHull hull = new StrandedHull(StrandedShipClass.Tanker, 0.8f);

            Assert.AreEqual(0.1f, hull.Repair(0.1f), 1e-5f);
            Assert.AreEqual(0.1f, hull.Repair(0.5f), 1e-5f);
            Assert.IsTrue(hull.IsWhole);
            Assert.AreEqual(0f, hull.Repair(0.1f));
        }

        [Test]
        public void Damage_UndoesRepairs_AndBreaksTheHullAtZero()
        {
            StrandedHull hull = new StrandedHull(StrandedShipClass.Refinery, 0.4f);
            hull.Repair(0.3f);

            Assert.IsFalse(hull.TakeDamage(0.5f));
            Assert.AreEqual(0.2f, hull.Fraction, 1e-5f);
            Assert.IsTrue(hull.TakeDamage(0.5f));
            Assert.AreEqual(0f, hull.Fraction);
            Assert.IsTrue(hull.IsBroken);
        }

        [Test]
        public void OnlyAWholeHull_CanBeRecovered_AndRecoveryEndsTheOrder()
        {
            StrandedHull hull = new StrandedHull(StrandedShipClass.Liner, 0.5f);
            hull.OrderRepair();

            Assert.Throws<InvalidOperationException>(hull.MarkRecovered);
            hull.Repair(0.5f);
            hull.MarkRecovered();

            Assert.IsTrue(hull.IsRecovered);
            Assert.IsFalse(hull.IsRepairOrdered);
            hull.OrderRepair();
            Assert.IsFalse(hull.IsRepairOrdered, "A recovered hulk takes no new orders.");
        }

        [Test]
        public void RecoveredHulk_IsGoneFromItsZone()
        {
            StationProfile profile = new StationProfile(StationAtmosphere.Thin, 0, StationAttitude.None, StationCondition.Ruined, StationRadiation.None);
            StrandedHull hull = new StrandedHull(StrandedShipClass.Cruiser, 1f);
            ZoneStation hulk = new ZoneStation("Hulk", Vector3.zero, 0f, profile, hull);

            Assert.AreEqual(StationType.StrandedShip, hulk.Type);
            Assert.IsFalse(hulk.IsGone);
            hull.MarkRecovered();
            Assert.IsTrue(hulk.IsGone);
            Assert.IsFalse(hulk.IsDestroyed);
        }

        [Test]
        public void OnlyStrandedShips_CarryAHull()
        {
            StationProfile profile = new StationProfile(StationAtmosphere.Thin, 0, StationAttitude.None, StationCondition.Ruined, StationRadiation.None);

            Assert.Throws<ArgumentException>(() => new ZoneStation("Hulk", StationType.StrandedShip, Vector3.zero, 0f, profile));
            Assert.AreEqual(StrandedShipClasses.FootprintRadiusUnits(StrandedShipClass.Refinery),
                new ZoneStation("Hulk", Vector3.zero, 0f, profile, new StrandedHull(StrandedShipClass.Refinery, 0.3f)).FootprintRadiusUnits);
        }
    }
}

using System;
using NUnit.Framework;

namespace LastFleet.Ships.Tests
{
    public class ShipHullTests
    {
        [Test]
        public void Damage_ReducesHullAndReportsTheDestroyingHitOnce()
        {
            ShipHull hull = new ShipHull(100f);

            Assert.IsFalse(hull.TakeDamage(70f));
            Assert.AreEqual(30f, hull.Current);
            Assert.IsTrue(hull.TakeDamage(70f), "The hit that reaches zero destroys the ship.");
            Assert.AreEqual(0f, hull.Current, "Hull does not go below zero.");
            Assert.IsFalse(hull.TakeDamage(10f), "A destroyed ship is not destroyed again.");
            Assert.IsTrue(hull.IsDestroyed);
        }

        [Test]
        public void Repair_RestoresUpToTheMaximumAndNeverRevivesADestroyedHull()
        {
            ShipHull hull = new ShipHull(100f);
            hull.TakeDamage(30f);

            Assert.IsTrue(hull.IsDamaged);
            Assert.AreEqual(30f, hull.Missing);
            Assert.AreEqual(10f, hull.Repair(10f));
            Assert.AreEqual(80f, hull.Current);
            Assert.AreEqual(20f, hull.Repair(50f), "Only the missing points are restored.");
            Assert.AreEqual(100f, hull.Current);
            Assert.IsFalse(hull.IsDamaged);

            hull.TakeDamage(100f);
            Assert.AreEqual(0f, hull.Repair(10f), "A destroyed ship is lost for good.");
            Assert.IsTrue(hull.IsDestroyed);
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShipHull(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShipHull(10f).TakeDamage(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShipHull(10f).Repair(-1f));
        }
    }
}

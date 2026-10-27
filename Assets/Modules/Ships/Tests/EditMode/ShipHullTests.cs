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
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShipHull(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShipHull(10f).TakeDamage(-1f));
        }
    }
}

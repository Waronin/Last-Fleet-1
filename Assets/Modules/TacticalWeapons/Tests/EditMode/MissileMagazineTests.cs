using System;
using NUnit.Framework;

namespace LastFleet.TacticalWeapons.Tests
{
    public class MissileMagazineTests
    {
        [Test]
        public void Build_TakesItsTimeAndAddsOneMissile()
        {
            MissileMagazine magazine = new MissileMagazine(capacity: 2, startingMissiles: 1, buildSeconds: 60f);

            Assert.IsTrue(magazine.StartBuild());
            Assert.IsFalse(magazine.StartBuild(), "One missile at a time.");
            Assert.IsFalse(magazine.Tick(59f));
            Assert.AreEqual(1f, magazine.BuildSecondsLeft, 1e-4f);
            Assert.IsTrue(magazine.Tick(1f));
            Assert.AreEqual(2, magazine.Count);
            Assert.IsFalse(magazine.IsBuilding);
        }

        [Test]
        public void FullMagazine_BuildsNothing()
        {
            MissileMagazine magazine = new MissileMagazine(capacity: 2, startingMissiles: 2, buildSeconds: 60f);

            Assert.IsTrue(magazine.IsFull);
            Assert.IsFalse(magazine.CanStartBuild);
            Assert.IsFalse(magazine.StartBuild());
        }

        [Test]
        public void TryTake_FiresUntilEmptyAndFreesRoomToBuild()
        {
            MissileMagazine magazine = new MissileMagazine(capacity: 2, startingMissiles: 2, buildSeconds: 60f);

            Assert.IsTrue(magazine.TryTake());
            Assert.IsTrue(magazine.CanStartBuild);
            Assert.IsTrue(magazine.TryTake());
            Assert.IsFalse(magazine.TryTake(), "No missile left.");
            Assert.AreEqual(0, magazine.Count);
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MissileMagazine(0, 0, 60f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MissileMagazine(2, 3, 60f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MissileMagazine(2, 1, 0f));
        }
    }
}

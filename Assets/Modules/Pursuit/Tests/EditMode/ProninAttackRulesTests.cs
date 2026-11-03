using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Pursuit.Tests
{
    public class ProninAttackRulesTests
    {
        [Test]
        public void StrikeZone_IsTheZoneWithTheMostCapitalShips()
        {
            Assert.AreEqual(4, ProninAttackRules.StrikeZone(new List<int> { 2, 4, 4, 0, 4, 2 }));
        }

        [Test]
        public void StrikeZone_TieGoesToTheLowerIndex()
        {
            Assert.AreEqual(1, ProninAttackRules.StrikeZone(new List<int> { 3, 1, 3, 1 }));
        }

        [Test]
        public void StrikeZone_WithoutCapitalShips_IsNone()
        {
            Assert.AreEqual(-1, ProninAttackRules.StrikeZone(new List<int>()));
        }

        [Test]
        public void ArrivalZone_AvoidsTheFleetsZone_AndReachesEveryOther()
        {
            DeterministicRandom random = new DeterministicRandom(3UL);
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < 500; i++)
            {
                int zone = ProninAttackRules.ArrivalZone(5, 2, random);
                Assert.That(zone, Is.InRange(0, 4));
                Assert.AreNotEqual(2, zone);
                seen.Add(zone);
            }
            CollectionAssert.AreEquivalent(new[] { 0, 1, 3, 4 }, seen);
        }

        [Test]
        public void ArrivalZone_WithASingleZone_UsesIt()
        {
            Assert.AreEqual(0, ProninAttackRules.ArrivalZone(1, 0, new DeterministicRandom(1UL)));
        }

        [Test]
        public void NeedsNewCourse_WhenTheFleetIsElsewhere()
        {
            Assert.IsTrue(ProninAttackRules.NeedsNewCourse(3, 0, true, 0f, 200f));
            // Also while the carrier is still flying or jumping somewhere else.
            Assert.IsTrue(ProninAttackRules.NeedsNewCourse(3, 0, false, 0f, 200f));
        }

        [Test]
        public void NeedsNewCourse_InTheFleetsZone_OnlyWhenIdleOutOfReach()
        {
            Assert.IsTrue(ProninAttackRules.NeedsNewCourse(3, 3, true, 350f, 200f));
            Assert.IsFalse(ProninAttackRules.NeedsNewCourse(3, 3, true, 150f, 200f));
            Assert.IsFalse(ProninAttackRules.NeedsNewCourse(3, 3, false, 350f, 200f));
        }

        [Test]
        public void NeedsNewCourse_NotWithoutCapitalShips()
        {
            Assert.IsFalse(ProninAttackRules.NeedsNewCourse(-1, 0, true, 0f, 200f));
        }

        [Test]
        public void StrikePoint_StopsShortOfTheTargetsOnTheApproachSide()
        {
            Vector2 point = ProninAttackRules.StrikePoint(new Vector2(10f, 0f), new Vector2(1f, 0f), 120f, 450f);
            Assert.AreEqual(-110f, point.x, 0.001f);
            Assert.AreEqual(0f, point.y, 0.001f);
        }

        [Test]
        public void StrikePoint_StaysInsideTheZone()
        {
            Vector2 point = ProninAttackRules.StrikePoint(new Vector2(400f, 0f), new Vector2(-1f, 0f), 120f, 450f);
            Assert.AreEqual(450f, point.magnitude, 0.001f);
        }
    }
}

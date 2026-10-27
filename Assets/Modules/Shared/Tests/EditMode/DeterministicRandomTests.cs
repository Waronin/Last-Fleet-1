using System;
using LastFleet.Shared.Randomness;
using NUnit.Framework;

namespace LastFleet.Shared.Tests
{
    public class DeterministicRandomTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            DeterministicRandom first = new DeterministicRandom(42UL);
            DeterministicRandom second = new DeterministicRandom(42UL);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(first.NextULong(), second.NextULong());
            }
        }

        [Test]
        public void KnownSeed_ProducesFixedFirstValue()
        {
            // Guards against accidental algorithm changes, which would silently change every generated galaxy.
            DeterministicRandom random = new DeterministicRandom(0UL);
            Assert.AreEqual(0xE220A8397B1DCDAFUL, random.NextULong());
        }

        [Test]
        public void DeriveSeed_DiffersPerSalt()
        {
            Assert.AreNotEqual(DeterministicRandom.DeriveSeed(7UL, 1UL), DeterministicRandom.DeriveSeed(7UL, 2UL));
        }

        [Test]
        public void NextFloat_StaysInUnitInterval()
        {
            DeterministicRandom random = new DeterministicRandom(3UL);
            for (int i = 0; i < 10000; i++)
            {
                float value = random.NextFloat();
                Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void IntRange_StaysInBounds()
        {
            DeterministicRandom random = new DeterministicRandom(5UL);
            for (int i = 0; i < 10000; i++)
            {
                int value = random.Range(-3, 4);
                Assert.That(value, Is.GreaterThanOrEqualTo(-3).And.LessThan(4));
            }
        }

        [Test]
        public void IntRange_RejectsEmptyRange()
        {
            DeterministicRandom random = new DeterministicRandom(5UL);
            Assert.Throws<ArgumentOutOfRangeException>(() => random.Range(2, 2));
        }
    }
}

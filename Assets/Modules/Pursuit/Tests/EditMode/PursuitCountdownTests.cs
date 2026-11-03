using System;
using NUnit.Framework;

namespace LastFleet.Pursuit.Tests
{
    public class PursuitCountdownTests
    {
        static PursuitCountdown Create(float arrival = 780f, float warningLead = 30f)
        {
            return new PursuitCountdown(new PursuitSettings(arrival, warningLead, 30f));
        }

        [Test]
        public void Phases_FollowTheTimeline()
        {
            PursuitCountdown countdown = Create();
            Assert.AreEqual(PursuitPhase.Counting, countdown.Phase);
            Assert.AreEqual(780f, countdown.SecondsUntilArrival);

            Assert.IsFalse(countdown.Advance(749f));
            Assert.IsTrue(countdown.Advance(1f));
            Assert.AreEqual(PursuitPhase.Warning, countdown.Phase);
            Assert.AreEqual(30f, countdown.SecondsUntilArrival, 0.001f);

            Assert.IsTrue(countdown.Advance(30f));
            Assert.AreEqual(PursuitPhase.Arrived, countdown.Phase);
        }

        [Test]
        public void LongStep_SkipsStraightToArrival_AndStops()
        {
            PursuitCountdown countdown = Create();
            Assert.IsTrue(countdown.Advance(10000f));
            Assert.AreEqual(PursuitPhase.Arrived, countdown.Phase);
            Assert.IsFalse(countdown.Advance(1f));
            Assert.AreEqual(0f, countdown.SecondsUntilArrival);
        }

        [Test]
        public void WarningLeadLongerThanTheCountdown_WarnsAtOnce()
        {
            Assert.AreEqual(PursuitPhase.Warning, Create(arrival: 20f, warningLead: 30f).Phase);
        }

        [Test]
        public void InvalidSettings_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => Create(arrival: 0f));
            Assert.Throws<ArgumentException>(() => Create(warningLead: -1f));
        }

        [Test]
        public void NegativeTime_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create().Advance(-1f));
        }

        [TestCase(0f, "0:00")]
        [TestCase(0.2f, "0:01")]
        [TestCase(59.5f, "1:00")]
        [TestCase(780f, "13:00")]
        [TestCase(-5f, "0:00")]
        public void ClockFormat_RoundsUp(float seconds, string expected)
        {
            Assert.AreEqual(expected, ClockFormat.MinutesSeconds(seconds));
        }
    }
}

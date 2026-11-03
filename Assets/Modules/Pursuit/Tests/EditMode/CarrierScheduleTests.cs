using System;
using NUnit.Framework;

namespace LastFleet.Pursuit.Tests
{
    public class CarrierScheduleTests
    {
        [Test]
        public void Hunt_StartsAfterTheStrikeDelay_WithACourseCheck()
        {
            CarrierSchedule schedule = new CarrierSchedule(30f, 60f, 2f);
            Assert.AreEqual(CarrierDuties.None, schedule.Advance(29f));
            Assert.IsFalse(schedule.IsHunting);

            Assert.AreEqual(CarrierDuties.StartHunt | CarrierDuties.CheckPursuit, schedule.Advance(1f));
            Assert.IsTrue(schedule.IsHunting);
        }

        [Test]
        public void WhileHunting_TheCourseIsCheckedRegularly()
        {
            CarrierSchedule schedule = new CarrierSchedule(0f, 60f, 2f);
            schedule.Advance(0f);
            Assert.AreEqual(CarrierDuties.None, schedule.Advance(1.5f));
            Assert.AreEqual(CarrierDuties.CheckPursuit, schedule.Advance(0.5f));
            Assert.AreEqual(CarrierDuties.None, schedule.Advance(1.75f));
            Assert.AreEqual(CarrierDuties.CheckPursuit, schedule.Advance(0.25f));
        }

        [Test]
        public void Waves_FollowAtTheInterval_FromTheArrival()
        {
            CarrierSchedule schedule = new CarrierSchedule(30f, 60f, 2f);
            int waves = 0;
            for (int second = 0; second < 180; second++)
            {
                if ((schedule.Advance(1f) & CarrierDuties.LaunchWave) != 0)
                {
                    waves++;
                }
            }
            Assert.AreEqual(3, waves);
        }

        [Test]
        public void LongStep_LaunchesOnlyOneWave()
        {
            CarrierSchedule schedule = new CarrierSchedule(30f, 60f, 2f);
            Assert.IsTrue((schedule.Advance(500f) & CarrierDuties.LaunchWave) != 0);
            Assert.IsTrue((schedule.Advance(0.1f) & CarrierDuties.LaunchWave) != 0);
            Assert.IsFalse((schedule.Advance(0.1f) & CarrierDuties.LaunchWave) != 0);
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CarrierSchedule(-1f, 60f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CarrierSchedule(30f, 0f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CarrierSchedule(30f, 60f, 2f).Advance(-1f));
        }
    }
}

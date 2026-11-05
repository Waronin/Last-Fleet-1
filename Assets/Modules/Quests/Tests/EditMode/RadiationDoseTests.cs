using NUnit.Framework;

namespace LastFleet.Quests.Tests
{
    public class RadiationDoseTests
    {
        [Test]
        public void Exposure_ReachesTheLimitAfterItsTime_WithoutDeaths()
        {
            RadiationDose dose = new RadiationDose(40f, 60f, 8f);

            Assert.AreEqual(0, dose.Expose(20f));
            Assert.AreEqual(0.5f, dose.Level, 1e-4f);
            Assert.AreEqual(0, dose.Expose(20f));
            Assert.IsTrue(dose.IsAtLimit);
        }

        [Test]
        public void ExposureAtTheLimit_KillsOneCrewMemberPerInterval()
        {
            RadiationDose dose = new RadiationDose(40f, 60f, 8f);
            dose.Expose(40f);

            Assert.AreEqual(0, dose.Expose(7f));
            Assert.AreEqual(1, dose.Expose(1f));
            Assert.AreEqual(2, dose.Expose(16f));
        }

        [Test]
        public void ExposurePastTheLimit_InOneStep_CountsOnlyTheTimeBeyondIt()
        {
            RadiationDose dose = new RadiationDose(40f, 60f, 8f);

            Assert.AreEqual(1, dose.Expose(48f));
        }

        [Test]
        public void Recovery_WearsTheDoseOff_AndStartsTheDeathIntervalAnew()
        {
            RadiationDose dose = new RadiationDose(40f, 60f, 8f);
            dose.Expose(40f + 7f);

            dose.Recover(30f);
            Assert.AreEqual(0.5f, dose.Level, 1e-4f);

            dose.Expose(20f);
            Assert.AreEqual(0, dose.Expose(7f), "Time at the limit before the break does not count.");
            dose.Recover(60f);
            Assert.IsTrue(dose.IsClean);
        }
    }
}

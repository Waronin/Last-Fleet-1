using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Zones.Tests
{
    public class StationHailTests
    {
        [TestCase(StationAttitude.Friendly)]
        [TestCase(StationAttitude.Wary)]
        [TestCase(StationAttitude.Hostile)]
        public void InhabitedStation_AnswersWithItsTrueAttitude(StationAttitude attitude)
        {
            ZoneStation station = Station(40, attitude);

            StationHailResponse response = station.Hail();

            Assert.IsTrue(response.Answered);
            Assert.AreEqual(attitude, response.ClaimedAttitude);
            Assert.IsTrue(station.IsHailed);
        }

        [Test]
        public void AbandonedStation_StaysSilent()
        {
            ZoneStation station = Station(0, StationAttitude.None);

            Assert.IsFalse(station.Hail().Answered);
        }

        [Test]
        public void Hail_IsAnsweredOnce()
        {
            ZoneStation station = Station(40, StationAttitude.Wary);
            Assert.IsFalse(station.IsHailed);

            StationHailResponse first = station.Hail();

            Assert.AreSame(first, station.Hail());
        }

        static ZoneStation Station(int occupants, StationAttitude attitude)
        {
            StationProfile profile = new StationProfile(StationAtmosphere.Breathable, occupants, attitude, StationCondition.Intact, StationRadiation.None);
            return new ZoneStation("Test", StationType.Outpost, Vector3.zero, 0f, profile);
        }
    }
}

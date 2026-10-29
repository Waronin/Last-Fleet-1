using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Zones.Tests
{
    public class StationScanReportTests
    {
        [Test]
        public void TrueOccupants_AlwaysLieInTheLifeSignRange()
        {
            foreach (int occupants in new[] { 1, 7, 46, 500, 2900 })
            {
                StationProfile profile = Inhabited(occupants);
                for (float roll = 0f; roll <= 1f; roll += 0.05f)
                {
                    StationScanReport report = StationScanReport.FromProfile(profile, roll);
                    Assert.That(occupants, Is.InRange(report.MinimumLifeSigns, report.MaximumLifeSigns), $"{occupants} at roll {roll}");
                }
            }
        }

        [Test]
        public void Range_IsAboutAQuarterEachWay_AndShiftsWithTheRoll()
        {
            StationProfile profile = Inhabited(1000);

            StationScanReport low = StationScanReport.FromProfile(profile, 0f);
            StationScanReport high = StationScanReport.FromProfile(profile, 1f);

            Assert.AreEqual(637, low.MinimumLifeSigns, 1);
            Assert.AreEqual(1063, low.MaximumLifeSigns, 1);
            Assert.AreEqual(862, high.MinimumLifeSigns, 1);
            Assert.AreEqual(1438, high.MaximumLifeSigns, 1);
        }

        [Test]
        public void AbandonedStation_ShowsNoLifeSigns()
        {
            StationProfile profile = new StationProfile(StationAtmosphere.Toxic, 0, StationAttitude.None, StationCondition.Ruined, StationRadiation.Elevated);

            StationScanReport report = StationScanReport.FromProfile(profile, 0.5f);

            Assert.IsFalse(report.HasLifeSigns);
            Assert.AreEqual(StationAtmosphere.Toxic, report.Atmosphere);
            Assert.AreEqual(StationCondition.Ruined, report.Condition);
            Assert.AreEqual(StationRadiation.Elevated, report.Radiation);
        }

        [Test]
        public void Station_KeepsItsFirstScan()
        {
            ZoneStation station = new ZoneStation("Test", StationType.Outpost, Vector3.zero, 0f, Inhabited(50));
            Assert.IsFalse(station.IsScanned);

            station.CompleteScan(0f);
            StationScanReport first = station.ScanReport;
            station.CompleteScan(1f);

            Assert.IsTrue(station.IsScanned);
            Assert.AreSame(first, station.ScanReport, "A second scan does not reroll the estimate.");
        }

        [Test]
        public void DockedCrew_LearnsTheEnvironment_ButNotTheLifeSigns()
        {
            ZoneStation station = new ZoneStation("Test", StationType.Outpost, Vector3.zero, 0f, Inhabited(50));
            Assert.IsFalse(station.IsEnvironmentKnown);

            station.MarkVisited();

            Assert.IsTrue(station.IsEnvironmentKnown);
            Assert.IsFalse(station.IsScanned, "Counting life signs still takes a scan.");
        }

        [Test]
        public void ScanTargets_KeepTheirDistanceAndLargerStationsTakeLonger()
        {
            ZoneStation outpost = new ZoneStation("A", StationType.Outpost, Vector3.zero, 0f, Inhabited(10));
            ZoneStation habitat = new ZoneStation("B", StationType.Habitat, Vector3.zero, 0f, Inhabited(1000));

            Assert.AreEqual(outpost.FootprintRadiusUnits + ZoneStation.ScanStandoffMarginUnits, outpost.ScanStandoffUnits);
            Assert.Greater(habitat.ScanDurationFactor, outpost.ScanDurationFactor);
            Assert.Greater(outpost.ScanDurationFactor, 1f, "A station takes longer than a deposit.");
        }

        static StationProfile Inhabited(int occupants)
        {
            return new StationProfile(StationAtmosphere.Breathable, occupants, StationAttitude.Wary, StationCondition.Damaged, StationRadiation.None);
        }
    }
}

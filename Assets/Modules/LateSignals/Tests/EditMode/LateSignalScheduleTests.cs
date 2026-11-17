using System.Collections.Generic;
using LastFleet.StarSystem;
using LastFleet.Zones;
using NUnit.Framework;

namespace LastFleet.LateSignals.Tests
{
    public class LateSignalScheduleTests
    {
        [Test]
        public void Signals_ComeInOnceTheirShareOfTheCountdownHasPassed_AndOnlyOnce()
        {
            SystemZones system = FindSystemWithLateSignals(out List<int> lateIndices);
            List<ZoneRecord> received = new List<ZoneRecord>();

            LateSignalSchedule.ReceiveDue(system, 0f, received);
            Assert.IsEmpty(received);
            foreach (int index in lateIndices)
            {
                Assert.IsFalse(system.Zones[index].IsKnown);
            }

            LateSignalSchedule.ReceiveDue(system, 0.95f, received);
            Assert.AreEqual(lateIndices.Count, received.Count);
            foreach (int index in lateIndices)
            {
                Assert.IsTrue(system.Zones[index].IsKnown);
            }

            LateSignalSchedule.ReceiveDue(system, 1f, received);
            Assert.IsEmpty(received, "A received signal does not come in again.");
        }

        [Test]
        public void ASignal_WaitsForItsOwnTime()
        {
            SystemZones system = FindSystemWithLateSignals(out List<int> lateIndices);
            ZoneRecord zone = system.Zones[lateIndices[0]];
            float due = zone.PointOfInterest.LateSignalFraction;
            List<ZoneRecord> received = new List<ZoneRecord>();

            LateSignalSchedule.ReceiveDue(system, due - 0.01f, received);
            CollectionAssert.DoesNotContain(received, zone);
            LateSignalSchedule.ReceiveDue(system, due, received);
            CollectionAssert.Contains(received, zone);
        }

        [Test]
        public void PlacesKnownOnArrival_NeverComeInAsSignals()
        {
            SystemZones system = FindSystemWithLateSignals(out List<int> lateIndices);
            List<ZoneRecord> received = new List<ZoneRecord>();

            LateSignalSchedule.ReceiveDue(system, 1f, received);

            foreach (ZoneRecord zone in received)
            {
                Assert.IsTrue(zone.PointOfInterest.IsLateSignal);
            }
            foreach (ZoneRecord zone in system.Zones)
            {
                Assert.IsTrue(zone.IsKnown);
                Assert.IsFalse(zone.ReceiveSignal());
            }
        }

        static SystemZones FindSystemWithLateSignals(out List<int> lateIndices)
        {
            lateIndices = new List<int>();
            for (ulong seed = 0; seed < 100; seed++)
            {
                StarSystemLayout layout = StarSystemGenerator.Generate(seed, "Test");
                for (int i = 0; i < layout.PointsOfInterest.Count; i++)
                {
                    if (layout.PointsOfInterest[i].IsLateSignal)
                    {
                        lateIndices.Add(i);
                    }
                }
                if (lateIndices.Count > 0)
                {
                    return new SystemZones(0, layout);
                }
            }
            Assert.Fail("No system with a late signal among the first 100 seeds.");
            return null;
        }
    }
}

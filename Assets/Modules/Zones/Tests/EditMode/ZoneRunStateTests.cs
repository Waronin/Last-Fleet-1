using System;
using LastFleet.StarSystem;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Zones.Tests
{
    public class ZoneRunStateTests
    {
        [Test]
        public void Zone_HasNoContentsUntilRevealed()
        {
            SystemZones system = CreateSystem(1UL);

            foreach (ZoneRecord zone in system.Zones)
            {
                Assert.IsFalse(zone.IsRevealed);
                Assert.IsNull(zone.Contents);
            }
            Assert.AreEqual(0, system.RevealedCount);
        }

        [Test]
        public void Reveal_IsIdempotent()
        {
            ZoneRecord zone = CreateSystem(1UL).Zones[1];

            ZoneContents first = zone.Reveal();
            ZoneContents second = zone.Reveal();

            Assert.AreSame(first, second);
            Assert.IsTrue(zone.IsRevealed);
        }

        [Test]
        public void RevealOrder_DoesNotChangeContents()
        {
            SystemZones inOrder = CreateSystem(9UL);
            SystemZones reversed = CreateSystem(9UL);

            for (int i = reversed.Zones.Count - 1; i >= 0; i--)
            {
                reversed.Zones[i].Reveal();
            }

            for (int i = 0; i < inOrder.Zones.Count; i++)
            {
                ZoneContents expected = inOrder.Zones[i].Reveal();
                ZoneContents actual = reversed.Zones[i].Contents;
                Assert.AreEqual(expected.Deposits.Count, actual.Deposits.Count);
                for (int d = 0; d < expected.Deposits.Count; d++)
                {
                    Assert.AreEqual(expected.Deposits[d].LocalPosition, actual.Deposits[d].LocalPosition);
                }
            }
        }

        [Test]
        public void Extract_ClampsToRemainingAmount()
        {
            ResourceDeposit deposit = new ResourceDeposit(DepositShape.Asteroid, ResourceKind.Ore, Vector3.zero, 1f, 100f);

            Assert.AreEqual(60f, deposit.Extract(60f));
            Assert.AreEqual(40f, deposit.Extract(60f));
            Assert.AreEqual(0f, deposit.Extract(10f));
            Assert.IsTrue(deposit.IsDepleted);
            Assert.Throws<ArgumentOutOfRangeException>(() => deposit.Extract(-1f));
        }

        [Test]
        public void Registry_KeepsZoneStateWhenReturningToASystem()
        {
            RunZoneRegistry registry = new RunZoneRegistry();
            SystemZones firstVisit = registry.RegisterSystem(4, StarSystemGenerator.Generate(77UL, "Kavetor"));
            ZoneRecord zone = FindZoneWithDeposits(firstVisit);
            ResourceDeposit deposit = zone.Reveal().Deposits[0];
            deposit.Extract(deposit.InitialAmount);

            Assert.IsTrue(registry.TryGetSystem(4, out SystemZones secondVisit));
            Assert.AreSame(firstVisit, secondVisit);
            ZoneRecord returnedZone = secondVisit.Zones[zone.PointOfInterestIndex];
            Assert.IsTrue(returnedZone.IsRevealed);
            Assert.IsTrue(returnedZone.Contents.Deposits[0].IsDepleted);
        }

        [Test]
        public void Registry_RejectsRegisteringASystemTwice()
        {
            RunZoneRegistry registry = new RunZoneRegistry();
            StarSystemLayout layout = StarSystemGenerator.Generate(1UL, "Test");
            registry.RegisterSystem(0, layout);

            Assert.Throws<InvalidOperationException>(() => registry.RegisterSystem(0, layout));
            Assert.IsFalse(registry.TryGetSystem(1, out SystemZones _));
        }

        static SystemZones CreateSystem(ulong seed)
        {
            return new SystemZones(0, StarSystemGenerator.Generate(seed, "Test"));
        }

        static ZoneRecord FindZoneWithDeposits(SystemZones system)
        {
            foreach (ZoneRecord zone in system.Zones)
            {
                if (ZoneContentGenerator.Generate(zone.Seed, zone.PointOfInterest, system.Layout.Star).Deposits.Count > 0)
                {
                    return zone;
                }
            }
            throw new InvalidOperationException("Test system has no zone with deposits.");
        }
    }
}

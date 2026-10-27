using System;
using System.Collections.Generic;
using LastFleet.StarSystem;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Zones.Tests
{
    public class ResourceDepositTests
    {
        const int SystemSampleCount = 150;

        [TestCase(1f, DepositSizeClass.Small, 1)]
        [TestCase(2f, DepositSizeClass.Medium, 2)]
        [TestCase(3.5f, DepositSizeClass.Large, 3)]
        public void AsteroidSize_SetsClassAndMiningSlots(float radius, DepositSizeClass expectedClass, int expectedSlots)
        {
            ResourceDeposit deposit = CreateAsteroid(Vector3.zero, radius, DepositHazard.None);

            Assert.AreEqual(expectedClass, deposit.SizeClass);
            Assert.AreEqual(expectedSlots, deposit.MiningSlotCount);
        }

        [Test]
        public void MiningSlots_SitOutsideTheSurfaceAndApart()
        {
            ResourceDeposit deposit = CreateAsteroid(new Vector3(10f, 2f, -5f), 3.5f, DepositHazard.None);

            for (int i = 0; i < deposit.MiningSlotCount; i++)
            {
                Vector3 slot = deposit.GetMiningSlotLocalPosition(i);
                Vector2 planarOffset = new Vector2(slot.x - deposit.LocalPosition.x, slot.z - deposit.LocalPosition.z);
                Assert.AreEqual(deposit.FootprintRadiusUnits, planarOffset.magnitude, 0.001f);
                Assert.AreEqual(0f, slot.y, "Slots lie on the zone plane where ships fly.");
                for (int j = i + 1; j < deposit.MiningSlotCount; j++)
                {
                    Assert.Greater(Vector3.Distance(slot, deposit.GetMiningSlotLocalPosition(j)), deposit.RadiusUnits);
                }
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => deposit.GetMiningSlotLocalPosition(deposit.MiningSlotCount));
        }

        [Test]
        public void Scan_IsRunStateThatStartsUnscanned()
        {
            ResourceDeposit deposit = CreateAsteroid(Vector3.zero, 1f, DepositHazard.Brittle);

            Assert.IsFalse(deposit.IsScanned);
            deposit.Scan();
            Assert.IsTrue(deposit.IsScanned);
        }

        [Test]
        public void GasClouds_CannotHaveHazards()
        {
            Assert.Throws<ArgumentException>(() =>
                new ResourceDeposit(DepositShape.GasCloud, ResourceKind.Gas, Vector3.zero, 8f, 100f, DepositHazard.GasPockets, 0f));
        }

        [Test]
        public void GeneratedZones_ContainEveryHazardButNeverOnGasClouds()
        {
            HashSet<DepositHazard> seen = new HashSet<DepositHazard>();
            int asteroids = 0;
            int hazardous = 0;
            foreach (ResourceDeposit deposit in GenerateDeposits())
            {
                if (deposit.Shape == DepositShape.GasCloud)
                {
                    Assert.AreEqual(DepositHazard.None, deposit.Hazard);
                    continue;
                }

                asteroids++;
                seen.Add(deposit.Hazard);
                if (deposit.Hazard != DepositHazard.None)
                {
                    hazardous++;
                }
            }

            CollectionAssert.IsSupersetOf(seen, new DepositHazard[] { DepositHazard.None, DepositHazard.Brittle, DepositHazard.Radioactive, DepositHazard.GasPockets });
            float hazardousShare = hazardous / (float)asteroids;
            Assert.That(hazardousShare, Is.InRange(0.15f, 0.35f));
        }

        [Test]
        public void GeneratedMiningSlots_DoNotLieInsideOtherDeposits()
        {
            foreach (SystemZones system in GenerateSystems())
            {
                foreach (ZoneRecord zone in system.Zones)
                {
                    IReadOnlyList<ResourceDeposit> deposits = zone.Reveal().Deposits;
                    foreach (ResourceDeposit deposit in deposits)
                    {
                        for (int slot = 0; slot < deposit.MiningSlotCount; slot++)
                        {
                            Vector3 slotPosition = deposit.GetMiningSlotLocalPosition(slot);
                            foreach (ResourceDeposit other in deposits)
                            {
                                if (other != deposit)
                                {
                                    // Planar: a slot must not lie inside or under another deposit.
                                    Vector2 offset = new Vector2(slotPosition.x - other.LocalPosition.x, slotPosition.z - other.LocalPosition.z);
                                    Assert.Greater(offset.magnitude, other.RadiusUnits,
                                        $"A mining slot in {zone.PointOfInterest.Name} lies under another deposit.");
                                }
                            }
                        }
                    }
                }
            }
        }

        static ResourceDeposit CreateAsteroid(Vector3 position, float radius, DepositHazard hazard)
        {
            return new ResourceDeposit(DepositShape.Asteroid, ResourceKind.Ore, position, radius, 100f, hazard, 30f);
        }

        static IEnumerable<ResourceDeposit> GenerateDeposits()
        {
            foreach (SystemZones system in GenerateSystems())
            {
                foreach (ZoneRecord zone in system.Zones)
                {
                    foreach (ResourceDeposit deposit in zone.Reveal().Deposits)
                    {
                        yield return deposit;
                    }
                }
            }
        }

        static IEnumerable<SystemZones> GenerateSystems()
        {
            for (int seed = 0; seed < SystemSampleCount; seed++)
            {
                yield return new SystemZones(seed, StarSystemGenerator.Generate((ulong)seed, "Test"));
            }
        }
    }
}

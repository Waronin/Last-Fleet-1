using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using LastFleet.StarSystem;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Zones.Tests
{
    public class AbandonedShipTests
    {
        const int SampleCount = 2000;

        [Test]
        public void FoundShips_AreDamagedAndNearlyDry_ShuttlesMostOften()
        {
            DeterministicRandom random = new DeterministicRandom(21UL);
            Dictionary<AbandonedShipClass, int> counts = new Dictionary<AbandonedShipClass, int>();
            for (int i = 0; i < SampleCount; i++)
            {
                AbandonedShip ship = AbandonedShip.Roll(random, Vector3.zero);
                Assert.That(ship.HullFraction, Is.InRange(AbandonedShip.MinimumFoundHullFraction, AbandonedShip.MaximumFoundHullFraction));
                Assert.That(ship.FuelFraction, Is.InRange(AbandonedShip.MinimumFoundFuelFraction, AbandonedShip.MaximumFoundFuelFraction));
                counts.TryGetValue(ship.Class, out int count);
                counts[ship.Class] = count + 1;
            }

            Assert.Greater(counts[AbandonedShipClass.Shuttle], counts[AbandonedShipClass.Harvester]);
            Assert.Greater(counts[AbandonedShipClass.Harvester], counts[AbandonedShipClass.Fighter]);
        }

        [Test]
        public void RecoveredShips_CannotBeDestroyedAnymore_AndDestroyedOnesNotRecovered()
        {
            AbandonedShip recovered = new AbandonedShip(AbandonedShipClass.Fighter, Vector3.zero, 0f, 0.5f, 0.1f);
            recovered.MarkRecovered();
            recovered.MarkDestroyed();
            Assert.IsTrue(recovered.IsRecovered);
            Assert.IsFalse(recovered.IsDestroyed);
            Assert.IsTrue(recovered.IsGone);

            AbandonedShip destroyed = new AbandonedShip(AbandonedShipClass.Shuttle, Vector3.zero, 0f, 0.5f, 0.1f);
            destroyed.MarkDestroyed();
            Assert.Throws<InvalidOperationException>(destroyed.MarkRecovered);
        }

        [Test]
        public void AbandonedShips_DriftMostlyInWreckFields_OnThePlane_ClearOfEverythingElse()
        {
            Dictionary<PointOfInterestKind, int> zones = new Dictionary<PointOfInterestKind, int>();
            Dictionary<PointOfInterestKind, int> withShips = new Dictionary<PointOfInterestKind, int>();
            for (int seed = 0; seed < 300; seed++)
            {
                SystemZones system = new SystemZones(seed, StarSystemGenerator.Generate((ulong)seed, "Test"));
                foreach (ZoneRecord zone in system.Zones)
                {
                    PointOfInterestKind kind = zone.PointOfInterest.Kind;
                    ZoneContents contents = zone.Reveal();
                    zones.TryGetValue(kind, out int count);
                    zones[kind] = count + 1;
                    if (contents.AbandonedShips.Count == 0)
                    {
                        continue;
                    }
                    withShips.TryGetValue(kind, out int shipZones);
                    withShips[kind] = shipZones + 1;
                    foreach (AbandonedShip ship in contents.AbandonedShips)
                    {
                        Assert.AreEqual(0f, ship.LocalPosition.y);
                        foreach (ZoneStation station in contents.Stations)
                        {
                            Assert.Greater(Vector3.Distance(ship.LocalPosition, station.LocalPosition), station.FootprintRadiusUnits);
                        }
                    }
                }
            }

            Assert.That(Share(withShips, zones, PointOfInterestKind.WreckField), Is.InRange(0.45f, 0.75f));
            Assert.That(Share(withShips, zones, PointOfInterestKind.Station), Is.InRange(0.15f, 0.35f));
            Assert.IsFalse(withShips.ContainsKey(PointOfInterestKind.Planet), "Only wreck fields, hulks and stations hold abandoned ships.");
        }

        static float Share(Dictionary<PointOfInterestKind, int> part, Dictionary<PointOfInterestKind, int> whole, PointOfInterestKind kind)
        {
            part.TryGetValue(kind, out int count);
            return count / (float)whole[kind];
        }
    }
}

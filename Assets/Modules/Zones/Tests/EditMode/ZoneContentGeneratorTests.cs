using System.Collections.Generic;
using LastFleet.Space;
using LastFleet.StarSystem;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Zones.Tests
{
    public class ZoneContentGeneratorTests
    {
        const int SystemSampleCount = 150;

        [Test]
        public void SameSeed_ProducesIdenticalContents()
        {
            StarSystemLayout layout = StarSystemGenerator.Generate(3UL, "Test");
            foreach (PointOfInterest point in layout.PointsOfInterest)
            {
                ZoneContents first = ZoneContentGenerator.Generate(42UL, point, layout.Star);
                ZoneContents second = ZoneContentGenerator.Generate(42UL, point, layout.Star);

                Assert.AreEqual(first.Deposits.Count, second.Deposits.Count);
                for (int i = 0; i < first.Deposits.Count; i++)
                {
                    Assert.AreEqual(first.Deposits[i].LocalPosition, second.Deposits[i].LocalPosition);
                    Assert.AreEqual(first.Deposits[i].Resource, second.Deposits[i].Resource);
                    Assert.AreEqual(first.Deposits[i].InitialAmount, second.Deposits[i].InitialAmount);
                }
            }
        }

        [Test]
        public void FuelSourceZones_ContainGas()
        {
            int fuelZones = 0;
            foreach (SystemZones system in GenerateSystems())
            {
                foreach (ZoneRecord zone in system.Zones)
                {
                    if (!zone.PointOfInterest.IsFuelSource)
                    {
                        continue;
                    }

                    fuelZones++;
                    bool hasGas = false;
                    foreach (ResourceDeposit deposit in zone.Reveal().Deposits)
                    {
                        hasGas |= deposit.Resource == ResourceKind.Gas;
                    }
                    Assert.IsTrue(hasGas, $"{zone.PointOfInterest.Name} is a fuel source without gas.");
                }
            }
            Assert.Greater(fuelZones, 0);
        }

        [Test]
        public void Objects_StayInsideZoneOutsideArrivalAreaAndDoNotOverlap()
        {
            float zoneRadiusUnits = SpaceUnits.KlicksToUnits(ZoneContentGenerator.ZoneRadiusKlicks);
            foreach (SystemZones system in GenerateSystems())
            {
                foreach (ZoneRecord zone in system.Zones)
                {
                    ZoneContents contents = zone.Reveal();
                    List<(Vector3 Position, float Radius)> objects = new List<(Vector3 Position, float Radius)>();
                    foreach (ResourceDeposit deposit in contents.Deposits)
                    {
                        objects.Add((deposit.LocalPosition, deposit.RadiusUnits));
                    }
                    foreach (ZoneStation station in contents.Stations)
                    {
                        objects.Add((station.LocalPosition, station.FootprintRadiusUnits));
                    }

                    for (int i = 0; i < objects.Count; i++)
                    {
                        Vector2 planar = new Vector2(objects[i].Position.x, objects[i].Position.z);
                        Assert.LessOrEqual(planar.magnitude + objects[i].Radius, zoneRadiusUnits);
                        Assert.Greater(planar.magnitude, objects[i].Radius);
                        for (int j = i + 1; j < objects.Count; j++)
                        {
                            float distance = Vector3.Distance(objects[i].Position, objects[j].Position);
                            Assert.Greater(distance, objects[i].Radius + objects[j].Radius, $"Overlap in {zone.PointOfInterest.Name}");
                        }
                    }
                }
            }
        }

        [Test]
        public void StationZones_ContainTheirStation()
        {
            foreach (SystemZones system in GenerateSystems())
            {
                foreach (ZoneRecord zone in system.Zones)
                {
                    if (zone.PointOfInterest.Kind == PointOfInterestKind.Station)
                    {
                        Assert.AreEqual(1, zone.Reveal().Stations.Count);
                        Assert.AreEqual(zone.PointOfInterest.Name, zone.Contents.Stations[0].Name);
                        Assert.AreEqual(0f, zone.Contents.Stations[0].LocalPosition.y, "Stations lie on the plane where ships fly.");
                    }
                }
            }
        }

        [Test]
        public void WreckFields_HoldADerelictAndMuchMetal_SomeOfItDangerous()
        {
            int fields = 0;
            int wrecks = 0;
            int hazardous = 0;
            foreach (SystemZones system in GenerateSystems())
            {
                foreach (ZoneRecord zone in system.Zones)
                {
                    if (zone.PointOfInterest.Kind != PointOfInterestKind.WreckField)
                    {
                        continue;
                    }
                    fields++;
                    ZoneContents contents = zone.Reveal();
                    Assert.AreEqual(1, contents.Stations.Count);
                    ZoneStation derelict = contents.Stations[0];
                    Assert.AreEqual(StationType.Derelict, derelict.Type);
                    Assert.IsFalse(derelict.Profile.IsInhabited);
                    Assert.AreEqual(StationCondition.Damaged, derelict.Profile.Condition);
                    Assert.That(contents.Deposits.Count, Is.InRange(6, 14), "A few may not fit.");
                    foreach (ResourceDeposit wreck in contents.Deposits)
                    {
                        Assert.AreEqual(DepositShape.Wreck, wreck.Shape);
                        Assert.That(wreck.InitialAmount, Is.InRange(100f, 300f));
                        Assert.IsFalse(wreck.IsScanned);
                        Assert.That(wreck.Hazard, Is.EqualTo(DepositHazard.None).Or.EqualTo(DepositHazard.Radioactive).Or.EqualTo(DepositHazard.GasPockets));
                        wrecks++;
                        hazardous += wreck.Hazard != DepositHazard.None ? 1 : 0;
                    }
                }
            }

            Assert.Greater(fields, 5, "Enough wreck fields in the sample.");
            Assert.That(hazardous / (float)wrecks, Is.InRange(0.2f, 0.45f), "About a third of the wrecks is dangerous.");
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

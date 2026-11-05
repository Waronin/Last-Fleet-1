using System.Collections.Generic;
using LastFleet.Galaxy;
using LastFleet.Shared.Randomness;
using LastFleet.StarSystem;
using LastFleet.Zones;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Quests.Tests
{
    public class QuestPlacementTests
    {
        static readonly StationProfile Abandoned = new StationProfile(StationAtmosphere.Breathable, 0, StationAttitude.None,
            StationCondition.Damaged, StationRadiation.None);

        [Test]
        public void Picker_ChoosesAnUnvisitedSystemInJumpRange()
        {
            GalaxyLayout galaxy = GalaxyGenerator.Generate(3UL, new GalaxySettings());
            int start = galaxy.StartSystemIndex;

            int picked = QuestSystemPicker.Pick(galaxy, start, index => index == start, 11UL);

            Assert.AreNotEqual(start, picked);
            CollectionAssert.Contains(galaxy.GetNeighbours(start), picked);
            Assert.AreEqual(picked, QuestSystemPicker.Pick(galaxy, start, index => index == start, 11UL), "Same seed, same system.");
        }

        [Test]
        public void Picker_FallsBackToTheNearestUnvisitedSystem()
        {
            GalaxyLayout galaxy = GalaxyGenerator.Generate(3UL, new GalaxySettings());
            int start = galaxy.StartSystemIndex;
            HashSet<int> visited = new HashSet<int>(galaxy.GetNeighbours(start)) { start };

            int picked = QuestSystemPicker.Pick(galaxy, start, visited.Contains, 11UL);

            Assert.IsFalse(visited.Contains(picked));
            for (int i = 0; i < galaxy.Systems.Count; i++)
            {
                if (!visited.Contains(i))
                {
                    Assert.LessOrEqual(galaxy.DistanceLightYears(start, picked), galaxy.DistanceLightYears(start, i) + 1e-4f);
                }
            }
        }

        [Test]
        public void Site_AvoidsJumpPointAndStarAndPrefersUnexploredStationZones()
        {
            SystemZones system = CreateSystem(5UL);
            for (ulong seed = 0; seed < 20; seed++)
            {
                int index = QuestStationSite.ChooseZone(system, new DeterministicRandom(seed));
                PointOfInterestKind kind = system.Zones[index].PointOfInterest.Kind;
                Assert.AreNotEqual(PointOfInterestKind.JumpPoint, kind);
                Assert.AreNotEqual(PointOfInterestKind.Star, kind);
                if (HasKind(system, PointOfInterestKind.Station))
                {
                    Assert.AreEqual(PointOfInterestKind.Station, kind);
                }
            }
        }

        [Test]
        public void Site_AddsItsStationWhenTheZoneIsRevealed_ClearOfEverythingElse()
        {
            SystemZones system = CreateSystem(5UL);
            ZoneStation placed = null;
            QuestStationSite site = QuestStationSite.Place(system, new QuestStationSpec("Ascalon", StationType.Industrial, Abandoned),
                new DeterministicRandom(1UL), null, station => placed = station);
            Assert.IsNull(site.Station, "Nothing is generated before the zone is revealed.");

            ZoneContents contents = system.Zones[site.PointOfInterestIndex].Reveal();

            Assert.AreSame(placed, site.Station);
            CollectionAssert.Contains(contents.Stations, site.Station);
            Assert.AreEqual(0f, site.Station.LocalPosition.y);
            foreach (ZoneStation other in contents.Stations)
            {
                if (other != site.Station)
                {
                    Assert.Greater(PlanarDistance(other.LocalPosition, site.Station.LocalPosition),
                        other.FootprintRadiusUnits + site.Station.FootprintRadiusUnits);
                }
            }
            foreach (ResourceDeposit deposit in contents.Deposits)
            {
                Assert.Greater(PlanarDistance(deposit.LocalPosition, site.Station.LocalPosition),
                    deposit.FootprintRadiusUnits + site.Station.FootprintRadiusUnits);
            }
        }

        [Test]
        public void Site_InARevealedZone_IsPlacedAtOnce()
        {
            SystemZones system = CreateSystem(5UL);
            foreach (ZoneRecord zone in system.Zones)
            {
                zone.Reveal();
            }

            QuestStationSite site = QuestStationSite.Place(system, new QuestStationSpec("Meridian", StationType.Research, Abandoned),
                new DeterministicRandom(2UL), null, null);

            Assert.IsNotNull(site.Station);
            CollectionAssert.Contains(system.Zones[site.PointOfInterestIndex].Contents.Stations, site.Station);
        }

        [Test]
        public void DepositSite_PutsTheRockNextToItsStation_ClearOfEverythingElse()
        {
            SystemZones system = CreateSystem(5UL);
            QuestStationSite station = QuestStationSite.Place(system, new QuestStationSpec("Vigil", StationType.Outpost, Abandoned),
                new DeterministicRandom(3UL), null, null);
            QuestDepositSite rock = QuestDepositSite.PlaceNextTo(station, system, ResourceKind.Ore, 8f, 3000f, 40f, null, null);
            Assert.IsNull(rock.Deposit, "Nothing is generated before the zone is revealed.");

            ZoneContents contents = system.Zones[station.PointOfInterestIndex].Reveal();

            CollectionAssert.Contains(contents.Deposits, rock.Deposit);
            Assert.AreEqual(DepositSizeClass.Large, rock.Deposit.SizeClass);
            float gap = PlanarDistance(rock.Deposit.LocalPosition, station.Station.LocalPosition) -
                        station.Station.FootprintRadiusUnits - rock.Deposit.FootprintRadiusUnits;
            Assert.That(gap, Is.InRange(0f, 10f), "The rock lies right next to the station.");
            foreach (ResourceDeposit other in contents.Deposits)
            {
                if (other != rock.Deposit)
                {
                    Assert.Greater(PlanarDistance(other.LocalPosition, rock.Deposit.LocalPosition),
                        other.FootprintRadiusUnits + rock.Deposit.FootprintRadiusUnits);
                }
            }
        }

        static SystemZones CreateSystem(ulong seed)
        {
            return new SystemZones(0, StarSystemGenerator.Generate(seed, "Test"));
        }

        static bool HasKind(SystemZones system, PointOfInterestKind kind)
        {
            foreach (ZoneRecord zone in system.Zones)
            {
                if (zone.PointOfInterest.Kind == kind)
                {
                    return true;
                }
            }
            return false;
        }

        static float PlanarDistance(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }
    }
}

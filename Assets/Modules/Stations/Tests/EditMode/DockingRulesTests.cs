using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using LastFleet.Ships;
using LastFleet.Zones;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Stations.Tests
{
    public class DockingRulesTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void DestroyShips()
        {
            foreach (GameObject shipObject in _created)
            {
                if (shipObject != null)
                {
                    Object.DestroyImmediate(shipObject);
                }
            }
            _created.Clear();
        }

        [Test]
        public void HarmlessStation_HasNoHazards()
        {
            List<DockingHazard> hazards = new List<DockingHazard>();

            DockingHazardRules.CollectHazards(Profile(StationAtmosphere.Thin, StationRadiation.None, StationAttitude.Wary), hazards);

            Assert.IsEmpty(hazards, "Thin air and a wary crew are harmless.");
        }

        [Test]
        public void ToxicIrradiatedHostileStation_HasEveryHazard()
        {
            List<DockingHazard> hazards = new List<DockingHazard>();

            DockingHazardRules.CollectHazards(Profile(StationAtmosphere.Toxic, StationRadiation.Elevated, StationAttitude.Hostile), hazards);

            CollectionAssert.AreEquivalent(
                new[] { DockingHazard.ToxicAtmosphere, DockingHazard.Radiation, DockingHazard.HostileAttack, DockingHazard.Capture }, hazards);
        }

        [Test]
        public void HazardTiming_MatchesTheMeanWaitingTime_AtAnyFrameRate()
        {
            float expected = 1f - Mathf.Exp(-1f);
            float mean = DockingHazardRules.CaptureMeanSeconds;
            Assert.AreEqual(expected, HazardTiming.ChanceWithin(mean, mean), 1e-4f);

            float perFrame = HazardTiming.ChanceWithin(mean, 0.02f);
            Assert.AreEqual(expected, 1f - Mathf.Pow(1f - perFrame, mean / 0.02f), 1e-3f);
            Assert.AreEqual(0f, HazardTiming.ChanceWithin(float.PositiveInfinity, 10f));
        }

        [Test]
        public void PortClaims_GiveEachShipItsOwnPort_UntilTheStationIsFull()
        {
            ZoneStation outpost = Station(StationType.Outpost);
            DockingPortClaims claims = new DockingPortClaims();
            ShipAgent first = CreateShip();
            ShipAgent second = CreateShip();
            ShipAgent third = CreateShip();

            Assert.IsTrue(claims.TryClaim(outpost, first, out int firstPort));
            Assert.IsTrue(claims.TryClaim(outpost, second, out int secondPort));
            Assert.AreNotEqual(firstPort, secondPort);
            Assert.IsFalse(claims.TryClaim(outpost, third, out _), "An outpost has two ports.");
            Assert.AreEqual(0, claims.FreePortCount(outpost));

            claims.Release(first);
            Assert.IsTrue(claims.TryClaim(outpost, third, out int thirdPort));
            Assert.AreEqual(firstPort, thirdPort, "The freed port is reused.");
        }

        [Test]
        public void PortClaims_MoveAShipToAnotherStation()
        {
            ZoneStation outpost = Station(StationType.Outpost);
            ZoneStation habitat = Station(StationType.Habitat);
            DockingPortClaims claims = new DockingPortClaims();
            ShipAgent ship = CreateShip();

            claims.TryClaim(outpost, ship, out _);
            claims.TryClaim(habitat, ship, out _);

            Assert.AreEqual(2, claims.FreePortCount(outpost), "Claiming elsewhere frees the old port.");
            Assert.AreEqual(5, claims.FreePortCount(habitat));
        }

        [Test]
        public void DestroyedShips_FreeTheirPort()
        {
            ZoneStation outpost = Station(StationType.Outpost);
            DockingPortClaims claims = new DockingPortClaims();
            ShipAgent ship = CreateShip();
            claims.TryClaim(outpost, ship, out _);

            Object.DestroyImmediate(ship.gameObject);

            Assert.AreEqual(2, claims.FreePortCount(outpost));
        }

        ShipAgent CreateShip()
        {
            GameObject shipObject = new GameObject("Test Ship");
            _created.Add(shipObject);
            return shipObject.AddComponent<ShipAgent>();
        }

        static ZoneStation Station(StationType type)
        {
            return new ZoneStation(type.ToString(), type, Vector3.zero, 0f, Profile(StationAtmosphere.Breathable, StationRadiation.None, StationAttitude.Friendly));
        }

        static StationProfile Profile(StationAtmosphere atmosphere, StationRadiation radiation, StationAttitude attitude)
        {
            return new StationProfile(atmosphere, 20, attitude, StationCondition.Intact, radiation);
        }
    }
}

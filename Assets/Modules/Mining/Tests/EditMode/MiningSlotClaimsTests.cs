using System.Collections.Generic;
using LastFleet.Ships;
using LastFleet.Zones;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastFleet.Mining.Tests
{
    public class MiningSlotClaimsTests
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
        public void Claim_TakesTheSlotNearestToTheShip()
        {
            MiningSlotClaims claims = new MiningSlotClaims();
            ResourceDeposit deposit = CreateDeposit(3.5f);
            Vector3 nearSlotTwo = deposit.GetMiningSlotLocalPosition(2) * 3f;

            Assert.IsTrue(claims.TryClaim(deposit, CreateShip(), nearSlotTwo, out int slot));
            Assert.AreEqual(2, slot);
            Assert.AreEqual(deposit.MiningSlotCount - 1, claims.FreeSlotCount(deposit));
        }

        [Test]
        public void Claim_FailsWhenAllSlotsAreTaken()
        {
            MiningSlotClaims claims = new MiningSlotClaims();
            ResourceDeposit deposit = CreateDeposit(2f);
            for (int i = 0; i < deposit.MiningSlotCount; i++)
            {
                Assert.IsTrue(claims.TryClaim(deposit, CreateShip(), Vector3.zero, out int _));
            }

            Assert.IsFalse(claims.TryClaim(deposit, CreateShip(), Vector3.zero, out int slot));
            Assert.AreEqual(-1, slot);
        }

        [Test]
        public void Claim_IsOnePerShipAndReleasable()
        {
            MiningSlotClaims claims = new MiningSlotClaims();
            ResourceDeposit first = CreateDeposit(1f);
            ResourceDeposit second = CreateDeposit(1f);
            ShipAgent ship = CreateShip();

            Assert.IsTrue(claims.TryClaim(first, ship, Vector3.zero, out int _));
            Assert.IsTrue(claims.TryClaim(first, ship, Vector3.zero, out int _), "Claiming the same deposit again keeps the slot.");
            Assert.IsTrue(claims.TryClaim(second, ship, Vector3.zero, out int _));
            Assert.AreEqual(1, claims.FreeSlotCount(first), "Moving to another deposit frees the old slot.");

            claims.Release(ship);
            Assert.AreEqual(1, claims.FreeSlotCount(second));
        }

        [Test]
        public void DepletedDeposits_AndDestroyedShips_FreeTheirSlots()
        {
            MiningSlotClaims claims = new MiningSlotClaims();
            ResourceDeposit deposit = CreateDeposit(1f);
            ShipAgent ship = CreateShip();
            claims.TryClaim(deposit, ship, Vector3.zero, out int _);

            Object.DestroyImmediate(ship.gameObject);
            Assert.AreEqual(1, claims.FreeSlotCount(deposit));

            deposit.Extract(deposit.InitialAmount);
            Assert.IsFalse(claims.TryClaim(deposit, CreateShip(), Vector3.zero, out int _));
        }

        static ResourceDeposit CreateDeposit(float radius)
        {
            return new ResourceDeposit(DepositShape.Asteroid, ResourceKind.Ore, Vector3.zero, radius, 100f, DepositHazard.None, 0f);
        }

        ShipAgent CreateShip()
        {
            GameObject shipObject = new GameObject("Harvester");
            _created.Add(shipObject);
            return shipObject.AddComponent<ShipAgent>();
        }
    }
}

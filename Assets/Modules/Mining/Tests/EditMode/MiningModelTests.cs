using System;
using LastFleet.Ships;
using LastFleet.Zones;
using NUnit.Framework;

namespace LastFleet.Mining.Tests
{
    public class MiningModelTests
    {
        [Test]
        public void CargoHold_FillsUpToCapacityWithOneMaterial()
        {
            CargoHold hold = new CargoHold(100f);

            Assert.AreEqual(60f, hold.Load(ResourceKind.Ore, 60f));
            Assert.AreEqual(0f, hold.Load(ResourceKind.Ice, 10f), "Another material must not mix in.");
            Assert.AreEqual(40f, hold.Load(ResourceKind.Ore, 70f));
            Assert.IsTrue(hold.IsFull);

            hold.Unload(100f);
            Assert.IsTrue(hold.IsEmpty);
            Assert.AreEqual(10f, hold.Load(ResourceKind.Ice, 10f), "An empty hold takes any material.");
        }

        [Test]
        public void CargoHold_RejectsInvalidAmounts()
        {
            CargoHold hold = new CargoHold(10f);
            hold.Load(ResourceKind.Gas, 5f);

            Assert.Throws<ArgumentOutOfRangeException>(() => hold.Load(ResourceKind.Gas, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => hold.Unload(6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CargoHold(0f));
        }

        [Test]
        public void Stock_StoresPerSupplyUpToCapacity()
        {
            SupplyStock stock = new SupplyStock(0f, 100f, 50f);

            Assert.AreEqual(80f, stock.Store(Supply.Metal, 80f));
            Assert.AreEqual(20f, stock.Store(Supply.Metal, 80f));
            Assert.AreEqual(50f, stock.Store(Supply.Water, 70f));
            Assert.AreEqual(100f, stock.AmountOf(Supply.Metal));
            Assert.AreEqual(0f, stock.FreeSpaceFor(Supply.Water));
            Assert.AreEqual(0f, stock.Store(Supply.Fuel, 5f), "A store without fuel capacity keeps no fuel.");
        }

        [Test]
        public void Stock_GivesOutNoMoreThanItHolds()
        {
            SupplyStock stock = new SupplyStock(100f, 0f, 0f);
            stock.Store(Supply.Fuel, 30f);

            Assert.AreEqual(20f, stock.Take(Supply.Fuel, 20f));
            Assert.AreEqual(10f, stock.Take(Supply.Fuel, 50f));
            Assert.AreEqual(0f, stock.AmountOf(Supply.Fuel));
            Assert.Throws<ArgumentOutOfRangeException>(() => stock.Take(Supply.Fuel, -1f));
        }

        [Test]
        public void Pumping_IsLimitedByRateStoreAndFreeTankSpace()
        {
            SupplyStock store = new SupplyStock(1000f, 0f, 0f);
            store.Store(Supply.Fuel, 100f);
            FuelTank tank = new FuelTank(200f);
            tank.Drain(50f);

            Assert.AreEqual(40f, FuelPumping.Pump(store, tank, 40f), "The rate limits a single step.");
            Assert.AreEqual(10f, FuelPumping.Pump(store, tank, 40f), "The tank takes only what is free.");
            Assert.AreEqual(50f, store.AmountOf(Supply.Fuel));

            tank.Drain(200f);
            Assert.AreEqual(50f, FuelPumping.Pump(store, tank, 500f), "An empty store stops the pump.");
            Assert.AreEqual(0f, FuelPumping.Pump(store, tank, 500f));
        }

        [Test]
        public void DeliveredRawMaterials_BecomeSupplies()
        {
            Assert.AreEqual(Supply.Metal, SupplyConversion.From(ResourceKind.Ore));
            Assert.AreEqual(Supply.Metal, SupplyConversion.From(ResourceKind.Metal));
            Assert.AreEqual(Supply.Water, SupplyConversion.From(ResourceKind.Ice));
            Assert.AreEqual(Supply.Fuel, SupplyConversion.From(ResourceKind.Gas));
        }

        [Test]
        public void Yield_GrowsWithMiningTimeUpToTheMaximumBonus()
        {
            Assert.AreEqual(1f, MiningYield.Multiplier(0f));
            Assert.AreEqual(1f + MiningYield.MaximumBonus * 0.5f, MiningYield.Multiplier(MiningYield.SecondsToFullBonus * 0.5f), 0.0001f);
            Assert.AreEqual(1f + MiningYield.MaximumBonus, MiningYield.Multiplier(MiningYield.SecondsToFullBonus));
            Assert.AreEqual(1f + MiningYield.MaximumBonus, MiningYield.Multiplier(MiningYield.SecondsToFullBonus * 10f));
        }

        [Test]
        public void Deposit_AccumulatesMiningTimeAsRunState()
        {
            ResourceDeposit deposit = new ResourceDeposit(DepositShape.Asteroid, ResourceKind.Ore, UnityEngine.Vector3.zero, 2f, 100f,
                DepositHazard.None, 0f);

            deposit.RecordMining(3f);
            deposit.RecordMining(2f);

            Assert.AreEqual(5f, deposit.MiningSeconds);
            Assert.Throws<ArgumentOutOfRangeException>(() => deposit.RecordMining(-1f));
        }
    }
}

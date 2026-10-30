using System;
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
        public void Stock_StoresPerMaterialUpToCapacity()
        {
            RawMaterialStock stock = new RawMaterialStock(100f, 50f, 0f, 30f);

            Assert.AreEqual(80f, stock.Store(ResourceKind.Ore, 80f));
            Assert.AreEqual(20f, stock.Store(ResourceKind.Ore, 80f));
            Assert.AreEqual(50f, stock.Store(ResourceKind.Ice, 70f));
            Assert.AreEqual(0f, stock.Store(ResourceKind.Gas, 5f));
            Assert.AreEqual(100f, stock.AmountOf(ResourceKind.Ore));
            Assert.AreEqual(0f, stock.FreeSpaceFor(ResourceKind.Ice));
            Assert.AreEqual(30f, stock.Store(ResourceKind.Metal, 40f));
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

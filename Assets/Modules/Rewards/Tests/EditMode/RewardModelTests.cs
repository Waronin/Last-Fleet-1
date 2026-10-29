using System;
using LastFleet.Zones;
using NUnit.Framework;

namespace LastFleet.Rewards.Tests
{
    public class RewardModelTests
    {
        [Test]
        public void Hold_StacksEqualFinds_AndKeepsDifferentOnesApart()
        {
            RewardHold hold = new RewardHold();

            hold.Add(Reward.OfResource(ResourceKind.Ore, 100f));
            hold.Add(Reward.OfResource(ResourceKind.Ore, 50f));
            hold.Add(Reward.OfResource(ResourceKind.Ice, 30f));
            hold.Add(Reward.OfItem(ItemKind.Clue));
            hold.Add(Reward.OfItem(ItemKind.Clue));
            hold.Add(Reward.OfItem(ItemKind.UpgradeModule));

            Assert.AreEqual(4, hold.Rewards.Count);
            Assert.AreEqual(150f, hold.Rewards[0].Amount);
            Assert.AreEqual(2f, hold.Rewards[2].Amount, "Two clues stack.");
        }

        [Test]
        public void Hold_KeepsWhatDidNotFit()
        {
            RewardHold hold = new RewardHold();
            hold.Add(Reward.OfFuel(120f));

            hold.ReplaceWith(new[] { Reward.OfFuel(20f) });

            Assert.AreEqual(1, hold.Rewards.Count);
            Assert.AreEqual(20f, hold.Rewards[0].Amount);
            hold.ReplaceWith(Array.Empty<Reward>());
            Assert.IsTrue(hold.IsEmpty);
        }

        [Test]
        public void Inventory_CountsItems()
        {
            FleetInventory inventory = new FleetInventory();

            inventory.Add(ItemKind.ArchiveRecord, 2);
            inventory.Add(ItemKind.ArchiveRecord, 1);

            Assert.AreEqual(3, inventory.CountOf(ItemKind.ArchiveRecord));
            Assert.AreEqual(0, inventory.CountOf(ItemKind.Clue));
        }

        [Test]
        public void InvalidRewards_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Reward.OfFuel(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Reward.OfSurvivors(-3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FleetInventory().Add(ItemKind.Clue, -1));
        }
    }
}

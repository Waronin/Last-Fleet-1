using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Zones;

namespace LastFleet.Rewards
{
    /// <summary>Reward texts in the string table "Rewards".</summary>
    public static class RewardTexts
    {
        const string Table = "Rewards";

        /// <summary>Item names as found, with their article, e.g. "an upgrade module".</summary>
        public static readonly EnumTextKeys<ItemKind> FoundItems = new EnumTextKeys<ItemKind>(Table, "item-found");

        /// <summary>Item names as counted in the inventory, e.g. "Upgrade modules".</summary>
        public static readonly EnumTextKeys<ItemKind> InventoryItems = new EnumTextKeys<ItemKind>(Table, "item-inventory");

        /// <summary>{amount}, {resource}</summary>
        public static readonly GameTextKey ResourceAmount = new GameTextKey(Table, "reward.resource");

        /// <summary>{amount}</summary>
        public static readonly GameTextKey FuelAmount = new GameTextKey(Table, "reward.fuel");

        /// <summary>{count}</summary>
        public static readonly GameTextKey Survivors = new GameTextKey(Table, "reward.survivors");

        /// <summary>{ship}, {carrier}</summary>
        public static readonly GameTextKey Delivered = new GameTextKey(Table, "notice.delivered");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey NoRoomForFinds = new GameTextKey(Table, "notice.no-room");

        /// <summary>{finds}</summary>
        public static readonly GameTextKey HoldLine = new GameTextKey(Table, "line.hold");

        /// <summary>{first}, {rest}</summary>
        public static readonly GameTextKey ListJoin = new GameTextKey(Table, "list.join");

        /// <summary>{item}, {count}</summary>
        public static readonly GameTextKey InventoryLine = new GameTextKey(Table, "line.inventory");

        /// <summary>Several rewards as one phrase, e.g. "120 ore, 40 fuel, a clue".</summary>
        public static GameText DescribeAll(IReadOnlyList<Reward> rewards)
        {
            return DescribeFrom(rewards, 0);
        }

        static GameText DescribeFrom(IReadOnlyList<Reward> rewards, int start)
        {
            GameText first = Describe(rewards[start]);
            return start == rewards.Count - 1 ? first : ListJoin.Format(("first", first), ("rest", DescribeFrom(rewards, start + 1)));
        }

        /// <summary>A reward as it reads in a sentence, e.g. "120 ore" or "a clue".</summary>
        public static GameText Describe(Reward reward)
        {
            return reward.Kind switch
            {
                RewardKind.Resource => ResourceAmount.Format(("amount", reward.Amount), ("resource", ZoneTexts.Resources.For(reward.Resource))),
                RewardKind.Fuel => FuelAmount.Format(("amount", reward.Amount)),
                RewardKind.Survivors => Survivors.Format(("count", reward.People)),
                RewardKind.Item => FoundItems.For(reward.Item),
                _ => throw new ArgumentOutOfRangeException(nameof(reward), reward.Kind, null)
            };
        }
    }
}

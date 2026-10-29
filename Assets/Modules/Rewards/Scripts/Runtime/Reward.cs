using System;
using LastFleet.Zones;

namespace LastFleet.Rewards
{
    public enum RewardKind
    {
        Resource,
        Fuel,

        /// <summary>People in cryo stasis; they board the finding ship's seats.</summary>
        Survivors,

        /// <summary>A thing counted in the fleet inventory, such as an upgrade module.</summary>
        Item
    }

    /// <summary>Things without an effect yet; their systems (upgrades, end scenarios, archive) come later.</summary>
    public enum ItemKind
    {
        UpgradeModule,
        Clue,
        ArchiveRecord
    }

    /// <summary>
    /// One thing the fleet can gain from searching a station or completing a quest (GDD: rewards for
    /// searching and quests). Rewards are values; where they end up is decided when they are delivered.
    /// </summary>
    public readonly struct Reward
    {
        Reward(RewardKind kind, ResourceKind resource, ItemKind item, float amount)
        {
            Kind = kind;
            Resource = resource;
            Item = item;
            Amount = amount;
        }

        public RewardKind Kind { get; }

        /// <summary>The raw material of a resource reward.</summary>
        public ResourceKind Resource { get; }

        /// <summary>The thing of an item reward.</summary>
        public ItemKind Item { get; }

        /// <summary>Units of a resource or of fuel, people for survivors, pieces for items.</summary>
        public float Amount { get; }

        /// <summary>Survivors come as whole people.</summary>
        public int People => (int)Amount;

        public static Reward OfResource(ResourceKind resource, float amount)
        {
            return new Reward(RewardKind.Resource, resource, default, Positive(amount));
        }

        public static Reward OfFuel(float amount)
        {
            return new Reward(RewardKind.Fuel, default, default, Positive(amount));
        }

        public static Reward OfSurvivors(int people)
        {
            return new Reward(RewardKind.Survivors, default, default, Positive(people));
        }

        public static Reward OfItem(ItemKind item)
        {
            return new Reward(RewardKind.Item, default, item, 1f);
        }

        /// <summary>The same reward with less of it, e.g. the survivors who did not fit aboard.</summary>
        public Reward WithAmount(float amount)
        {
            return new Reward(Kind, Resource, Item, Positive(amount));
        }

        static float Positive(float amount)
        {
            if (amount <= 0f || float.IsNaN(amount))
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "A reward needs a positive amount.");
            }
            return amount;
        }
    }
}

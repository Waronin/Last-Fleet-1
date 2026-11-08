using System;
using LastFleet.Mining;
using LastFleet.Shared.Randomness;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// Which stations ask for a delivery and what they offer. Values are balancing starting points:
    /// about every third inhabited, not hostile station asks; bigger stations ask for more; most pay in
    /// another supply at a good rate, a few with an upgrade module.
    /// </summary>
    public static class SupplyRequestRules
    {
        public const float RequestChance = 1f / 3f;
        public const float UpgradeModuleChance = 0.2f;
        public const int MinimumAmount = 100;
        public const int MaximumAmount = 600;
        public const float AmountPerOccupant = 0.2f;

        /// <summary>A trade pays more than it costs: the fleet spends time and risk on the delivery.</summary>
        public const float TradeRate = 1.5f;

        static readonly Supply[] Supplies = { Supply.Fuel, Supply.Metal, Supply.Water };

        /// <summary>The station's request, or null if it asks for nothing.</summary>
        public static SupplyRequest Roll(DeterministicRandom random, StationProfile profile)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            // Always draw the same numbers, so whether one station asks does not change another's roll.
            bool asks = random.Chance(RequestChance);
            Supply wanted = Supplies[random.Range(0, Supplies.Length)];
            bool paysWithModule = random.Chance(UpgradeModuleChance);
            Supply reward = Supplies[(Array.IndexOf(Supplies, wanted) + random.Range(1, Supplies.Length)) % Supplies.Length];

            if (!asks || !profile.IsInhabited || profile.Attitude == StationAttitude.Hostile)
            {
                return null;
            }
            int amount = AmountFor(profile.Occupants);
            return paysWithModule
                ? new SupplyRequest(wanted, amount, SupplyRequestRewardKind.UpgradeModule, wanted, 1)
                : new SupplyRequest(wanted, amount, SupplyRequestRewardKind.Supply, reward, RoundToTen(amount * TradeRate));
        }

        public static int AmountFor(int occupants)
        {
            int raw = RoundToTen(MinimumAmount + Math.Max(0, occupants) * AmountPerOccupant);
            return Math.Min(MaximumAmount, Math.Max(MinimumAmount, raw));
        }

        static int RoundToTen(float value)
        {
            return (int)Math.Round(value / 10f) * 10;
        }
    }
}

using System;
using LastFleet.Mining;

namespace LastFleet.Quests
{
    public enum SupplyRequestState
    {
        /// <summary>The station asks; the player has not agreed yet.</summary>
        Offered,

        /// <summary>Agreed: the next docked ship hands the goods over once the fleet has enough.</summary>
        Accepted,

        Fulfilled,

        /// <summary>The fleet left the system before delivering.</summary>
        Expired
    }

    /// <summary>What a station pays for a delivery.</summary>
    public enum SupplyRequestRewardKind
    {
        /// <summary>Another supply, e.g. fuel for metal.</summary>
        Supply,

        /// <summary>An upgrade module for the fleet inventory.</summary>
        UpgradeModule
    }

    /// <summary>
    /// A small quest of an inhabited station (GDD: quests, trade): "bring us so much of a supply, and you get
    /// this in return". It lives in one system only; jumping away lets it expire.
    /// </summary>
    public sealed class SupplyRequest
    {
        public SupplyRequest(Supply wanted, int wantedAmount, SupplyRequestRewardKind rewardKind, Supply rewardSupply, int rewardAmount)
        {
            if (wantedAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wantedAmount), "A request needs a positive amount.");
            }
            if (rewardKind == SupplyRequestRewardKind.Supply && (rewardAmount <= 0 || rewardSupply == wanted))
            {
                throw new ArgumentException("A trade pays a positive amount of another supply.", nameof(rewardSupply));
            }
            Wanted = wanted;
            WantedAmount = wantedAmount;
            RewardKind = rewardKind;
            RewardSupply = rewardSupply;
            RewardAmount = rewardKind == SupplyRequestRewardKind.Supply ? rewardAmount : 1;
        }

        public Supply Wanted { get; }
        public int WantedAmount { get; }
        public SupplyRequestRewardKind RewardKind { get; }

        /// <summary>The supply paid; only meaningful for a trade.</summary>
        public Supply RewardSupply { get; }

        /// <summary>Units of the reward supply, or 1 for an upgrade module.</summary>
        public int RewardAmount { get; }

        public SupplyRequestState State { get; private set; }
        public bool IsOpen => State == SupplyRequestState.Offered || State == SupplyRequestState.Accepted;

        public void Accept()
        {
            if (State == SupplyRequestState.Offered)
            {
                State = SupplyRequestState.Accepted;
            }
        }

        /// <summary>Whether the fleet's stock covers the request.</summary>
        public bool CanBeFulfilledFrom(float stock)
        {
            return State == SupplyRequestState.Accepted && stock >= WantedAmount;
        }

        public void Fulfil()
        {
            if (State != SupplyRequestState.Accepted)
            {
                throw new InvalidOperationException("Only an accepted request can be fulfilled.");
            }
            State = SupplyRequestState.Fulfilled;
        }

        public void Expire()
        {
            if (IsOpen)
            {
                State = SupplyRequestState.Expired;
            }
        }
    }
}

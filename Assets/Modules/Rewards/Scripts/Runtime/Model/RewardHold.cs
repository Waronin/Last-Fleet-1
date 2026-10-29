using System.Collections.Generic;

namespace LastFleet.Rewards
{
    /// <summary>
    /// Finds carried aboard a ship until it lands (GDD: greed against safety). Equal finds stack, e.g.
    /// two ore finds become one larger one. Lost with the ship if it is destroyed or captured.
    /// </summary>
    public sealed class RewardHold
    {
        readonly List<Reward> _rewards = new List<Reward>();

        public IReadOnlyList<Reward> Rewards => _rewards;
        public bool IsEmpty => _rewards.Count == 0;

        public void Add(Reward reward)
        {
            for (int i = 0; i < _rewards.Count; i++)
            {
                if (IsSameKind(_rewards[i], reward))
                {
                    _rewards[i] = _rewards[i].WithAmount(_rewards[i].Amount + reward.Amount);
                    return;
                }
            }
            _rewards.Add(reward);
        }

        /// <summary>Replaces the contents, e.g. with what did not fit when unloading.</summary>
        public void ReplaceWith(IReadOnlyList<Reward> remaining)
        {
            List<Reward> copy = new List<Reward>(remaining);
            _rewards.Clear();
            _rewards.AddRange(copy);
        }

        static bool IsSameKind(Reward a, Reward b)
        {
            return a.Kind == b.Kind && (a.Kind != RewardKind.Resource || a.Resource == b.Resource) && (a.Kind != RewardKind.Item || a.Item == b.Item);
        }
    }
}

using System.Collections.Generic;
using LastFleet.Rewards;

namespace LastFleet.Stations
{
    /// <summary>
    /// What is still to be found aboard a station, in the order it turns up. Searching takes one find
    /// after another until the station is empty; the state lasts for the run.
    /// </summary>
    public sealed class StationLoot
    {
        readonly List<Reward> _remaining;

        public StationLoot(IEnumerable<Reward> finds)
        {
            _remaining = new List<Reward>(finds);
        }

        public int FindsMade { get; private set; }
        public bool IsEmpty => _remaining.Count == 0;
        public int RemainingCount => _remaining.Count;

        public int CountOf(RewardKind kind)
        {
            int count = 0;
            foreach (Reward reward in _remaining)
            {
                count += reward.Kind == kind ? 1 : 0;
            }
            return count;
        }

        public bool TryPeek(out Reward next)
        {
            if (IsEmpty)
            {
                next = default;
                return false;
            }
            next = _remaining[0];
            return true;
        }

        /// <summary>Takes the next find, or only part of it when the finder cannot carry all, e.g. survivors.</summary>
        public void Take(float amount)
        {
            Reward next = _remaining[0];
            if (amount >= next.Amount - 0.01f)
            {
                _remaining.RemoveAt(0);
            }
            else
            {
                _remaining[0] = next.WithAmount(next.Amount - amount);
            }
            FindsMade++;
        }

        /// <summary>Moves the next find to the end, e.g. survivors when no seat is free.</summary>
        public void Postpone()
        {
            if (_remaining.Count > 1)
            {
                Reward next = _remaining[0];
                _remaining.RemoveAt(0);
                _remaining.Add(next);
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace LastFleet.Rewards
{
    /// <summary>
    /// Items the fleet owns, such as upgrade modules, clues and archive records. They are only counted
    /// for now; upgrades, end scenarios and the archive will use them.
    /// </summary>
    public sealed class FleetInventory
    {
        readonly Dictionary<ItemKind, int> _counts = new Dictionary<ItemKind, int>();

        public int CountOf(ItemKind item)
        {
            return _counts.TryGetValue(item, out int count) ? count : 0;
        }

        public void Add(ItemKind item, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Cannot add a negative number of items.");
            }
            _counts[item] = CountOf(item) + count;
        }
    }
}

using System;
using System.Collections.Generic;

namespace LastFleet.Rewards
{
    /// <summary>
    /// Items the fleet owns, such as upgrade modules, clues and archive records. Upgrade modules wait
    /// here until the player installs them (FleetModules); clues and records are only counted for now.
    /// </summary>
    public sealed class FleetInventory
    {
        readonly Dictionary<ItemKind, int> _counts = new Dictionary<ItemKind, int>();

        /// <summary>The count of an item went up or down.</summary>
        public event Action<ItemKind> Changed;

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
            if (count == 0)
            {
                return;
            }
            _counts[item] = CountOf(item) + count;
            Changed?.Invoke(item);
        }

        /// <summary>Takes one item out of the inventory; false if there is none.</summary>
        public bool TryTake(ItemKind item)
        {
            int count = CountOf(item);
            if (count <= 0)
            {
                return false;
            }
            _counts[item] = count - 1;
            Changed?.Invoke(item);
            return true;
        }
    }
}

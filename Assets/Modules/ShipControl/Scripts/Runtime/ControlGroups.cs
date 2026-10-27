using System;
using System.Collections.Generic;
using LastFleet.Ships;

namespace LastFleet.ShipControl
{
    /// <summary>
    /// Stored selections on the number keys (GDD: Ctrl + number stores, number recalls).
    /// Groups hold a copy of the ships, so later selection changes do not alter them.
    /// </summary>
    public sealed class ControlGroups
    {
        public const int GroupCount = 10;

        readonly List<ShipAgent>[] _groups = new List<ShipAgent>[GroupCount];

        public ControlGroups()
        {
            for (int i = 0; i < GroupCount; i++)
            {
                _groups[i] = new List<ShipAgent>();
            }
        }

        public void Store(int group, IReadOnlyList<ShipAgent> ships)
        {
            List<ShipAgent> stored = GetGroup(group);
            stored.Clear();
            foreach (ShipAgent ship in ships)
            {
                stored.Add(ship);
            }
        }

        /// <summary>Ships of a group; lost ships are dropped first.</summary>
        public IReadOnlyList<ShipAgent> Get(int group)
        {
            List<ShipAgent> stored = GetGroup(group);
            // Unity's overloaded equality treats destroyed ships as null.
            stored.RemoveAll(ship => ship == null);
            return stored;
        }

        public void Remove(ShipAgent ship)
        {
            foreach (List<ShipAgent> group in _groups)
            {
                group.Remove(ship);
            }
        }

        List<ShipAgent> GetGroup(int group)
        {
            if (group < 0 || group >= GroupCount)
            {
                throw new ArgumentOutOfRangeException(nameof(group), $"Control groups are 0 to {GroupCount - 1}.");
            }
            return _groups[group];
        }
    }
}

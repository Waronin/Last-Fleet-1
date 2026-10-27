using System;
using System.Collections.Generic;
using LastFleet.Ships;

namespace LastFleet.ShipControl
{
    /// <summary>The ships the player currently commands, in selection order.</summary>
    public sealed class ShipSelection
    {
        readonly List<ShipAgent> _ships = new List<ShipAgent>();

        public IReadOnlyList<ShipAgent> Ships => _ships;
        public int Count => _ships.Count;

        public event Action Changed;

        public bool Contains(ShipAgent ship)
        {
            return _ships.Contains(ship);
        }

        /// <summary>Replaces the selection, or adds to it when <paramref name="additive"/> is set.</summary>
        public void Select(IReadOnlyList<ShipAgent> ships, bool additive)
        {
            if (!additive)
            {
                _ships.Clear();
            }
            foreach (ShipAgent ship in ships)
            {
                if (!_ships.Contains(ship))
                {
                    _ships.Add(ship);
                }
            }
            Changed?.Invoke();
        }

        public void Clear()
        {
            if (_ships.Count == 0)
            {
                return;
            }
            _ships.Clear();
            Changed?.Invoke();
        }

        public void Remove(ShipAgent ship)
        {
            if (_ships.Remove(ship))
            {
                Changed?.Invoke();
            }
        }
    }
}

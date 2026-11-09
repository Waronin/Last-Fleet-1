using System;
using System.Collections.Generic;

namespace LastFleet.Mining
{
    /// <summary>
    /// The order in which one tanker refuels: only the first entry gets fuel, the player decides the
    /// rest. Entries are compared by reference, so two destroyed Unity objects never count as the same ship.
    /// </summary>
    public sealed class RefuelingQueue<TShip> where TShip : class
    {
        readonly List<TShip> _ships = new List<TShip>();

        public IReadOnlyList<TShip> Ships => _ships;
        public int Count => _ships.Count;

        /// <summary>The ship being refueled, or null when the queue is empty.</summary>
        public TShip Current => _ships.Count > 0 ? _ships[0] : null;

        /// <summary>Zero-based place in the queue, or -1.</summary>
        public int PositionOf(TShip ship)
        {
            for (int index = 0; index < _ships.Count; index++)
            {
                if (ReferenceEquals(_ships[index], ship))
                {
                    return index;
                }
            }
            return -1;
        }

        public bool Contains(TShip ship) => PositionOf(ship) >= 0;

        /// <summary>Adds the ship at the end; false when it is already waiting.</summary>
        public bool Enqueue(TShip ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }
            if (Contains(ship))
            {
                return false;
            }
            _ships.Add(ship);
            return true;
        }

        /// <summary>Moves a waiting ship to the front, so it gets fuel next; false when it is not queued or already first.</summary>
        public bool MoveToFront(TShip ship)
        {
            int position = PositionOf(ship);
            if (position <= 0)
            {
                return false;
            }
            _ships.RemoveAt(position);
            _ships.Insert(0, ship);
            return true;
        }

        public bool Remove(TShip ship)
        {
            int position = PositionOf(ship);
            if (position < 0)
            {
                return false;
            }
            _ships.RemoveAt(position);
            return true;
        }

        /// <summary>Removes every ship the predicate matches, keeping the order of the rest; returns how many left.</summary>
        public int RemoveAll(Predicate<TShip> match)
        {
            return _ships.RemoveAll(match);
        }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Ships;

namespace LastFleet.Quests
{
    /// <summary>
    /// Which ship carries which quest item during a run. An item stays aboard through hangars and fleet
    /// jumps and is lost when its ship leaves the fleet (destroyed, captured or left behind). Steps load
    /// and unload items; the quest director reports removed ships.
    /// </summary>
    public sealed class QuestCargo
    {
        readonly Dictionary<QuestItem, ShipAgent> _carriers = new Dictionary<QuestItem, ShipAgent>();
        readonly List<QuestItem> _lost = new List<QuestItem>();

        /// <summary>An item went down with its ship; the ship is already gone from the fleet.</summary>
        public event Action<QuestItem, ShipAgent> Lost;

        public void Load(QuestItem item, ShipAgent carrier)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }
            if (_carriers.ContainsKey(item))
            {
                throw new InvalidOperationException($"Quest item {item.Id} is already aboard a ship.");
            }
            _carriers.Add(item, carrier);
        }

        /// <summary>Takes the item off its ship, e.g. when it is handed over at a station; false if nobody carries it.</summary>
        public bool Unload(QuestItem item)
        {
            return item != null && _carriers.Remove(item);
        }

        public bool TryGetCarrier(QuestItem item, out ShipAgent carrier)
        {
            return _carriers.TryGetValue(item, out carrier);
        }

        public bool IsCarriedBy(QuestItem item, ShipAgent ship)
        {
            return _carriers.TryGetValue(item, out ShipAgent carrier) && carrier == ship;
        }

        /// <summary>A new run starts with empty holds.</summary>
        public void Clear()
        {
            _carriers.Clear();
        }

        /// <summary>A ship left the fleet: everything aboard is lost.</summary>
        public void ShipRemoved(ShipAgent ship)
        {
            _lost.Clear();
            foreach (KeyValuePair<QuestItem, ShipAgent> entry in _carriers)
            {
                // Compared by reference: the ship may already be a destroyed Unity object.
                if (ReferenceEquals(entry.Value, ship))
                {
                    _lost.Add(entry.Key);
                }
            }
            foreach (QuestItem item in _lost)
            {
                _carriers.Remove(item);
                Lost?.Invoke(item, ship);
            }
        }
    }
}

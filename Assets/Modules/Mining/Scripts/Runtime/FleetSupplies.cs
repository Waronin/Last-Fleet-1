using System;
using LastFleet.Ships;

namespace LastFleet.Mining
{
    /// <summary>
    /// The supplies of the whole fleet. Metal and water lie in the stores of industrial ships, fuel in
    /// the tanks of capital ships (GDD: without a tanker only what is in the ships' tanks remains).
    /// The tanks of small ships are their own and do not count as stock.
    /// </summary>
    public sealed class FleetSupplies
    {
        readonly ShipRegistry _registry;

        public FleetSupplies(ShipRegistry registry)
        {
            _registry = registry != null ? registry : throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>Stores into the given ship first, then into any other ship with room; returns how much was stored.</summary>
        public float Store(Supply supply, float amount, ShipAgent firstShip)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Cannot store a negative amount.");
            }

            float left = amount;
            if (firstShip != null)
            {
                left -= StoreOn(firstShip, supply, left);
            }
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (left <= 0f)
                {
                    break;
                }
                if (ship != firstShip)
                {
                    left -= StoreOn(ship, supply, left);
                }
            }
            return amount - left;
        }

        public float AmountOf(Supply supply)
        {
            float amount = 0f;
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (supply == Supply.Fuel)
                {
                    amount += HoldsFuelStock(ship) ? ship.Fuel.Tank.Amount : 0f;
                }
                else if (TryGetStorage(ship, out SupplyStorage storage))
                {
                    amount += storage.Stock.AmountOf(supply);
                }
            }
            return amount;
        }

        public float CapacityOf(Supply supply)
        {
            float capacity = 0f;
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (supply == Supply.Fuel)
                {
                    capacity += HoldsFuelStock(ship) ? ship.Fuel.Tank.Capacity : 0f;
                }
                else if (TryGetStorage(ship, out SupplyStorage storage))
                {
                    capacity += storage.Stock.CapacityOf(supply);
                }
            }
            return capacity;
        }

        public bool HasRoomFor(Supply supply)
        {
            return CapacityOf(supply) - AmountOf(supply) > 0f;
        }

        static float StoreOn(ShipAgent ship, Supply supply, float amount)
        {
            if (supply == Supply.Fuel)
            {
                return HoldsFuelStock(ship) ? ship.Fuel.Tank.Fill(amount) : 0f;
            }
            return TryGetStorage(ship, out SupplyStorage storage) ? storage.Stock.Store(supply, amount) : 0f;
        }

        static bool HoldsFuelStock(ShipAgent ship)
        {
            return ship.IsCapital && !ship.IsDerelict;
        }

        static bool TryGetStorage(ShipAgent ship, out SupplyStorage storage)
        {
            storage = null;
            return !ship.IsDerelict && ship.TryGetComponent(out storage);
        }
    }
}

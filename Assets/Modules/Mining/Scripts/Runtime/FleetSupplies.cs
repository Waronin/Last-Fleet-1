using System;
using LastFleet.Ships;

namespace LastFleet.Mining
{
    /// <summary>
    /// The supplies of the whole fleet: what lies in the stores of industrial ships, and for fuel also
    /// what is in the tanks of capital ships. Fuel goes into the tanker's store first; without a tanker,
    /// or once it is full, only the capital ships' tanks remain (GDD: Industrial ships). The tanks of
    /// small ships are their own and do not count as stock.
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

            float left = amount - StoreEverywhere(supply, amount, firstShip, StoreInStorage);
            if (supply == Supply.Fuel)
            {
                left -= StoreEverywhere(supply, left, firstShip, StoreInTank);
            }
            return amount - left;
        }

        public float AmountOf(Supply supply)
        {
            float amount = 0f;
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (TryGetStorage(ship, out SupplyStorage storage))
                {
                    amount += storage.Stock.AmountOf(supply);
                }
                if (supply == Supply.Fuel && HoldsFuelStock(ship))
                {
                    amount += ship.Fuel.Tank.Amount;
                }
            }
            return amount;
        }

        public float CapacityOf(Supply supply)
        {
            float capacity = 0f;
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (TryGetStorage(ship, out SupplyStorage storage))
                {
                    capacity += storage.Stock.CapacityOf(supply);
                }
                if (supply == Supply.Fuel && HoldsFuelStock(ship))
                {
                    capacity += ship.Fuel.Tank.Capacity;
                }
            }
            return capacity;
        }

        public bool HasRoomFor(Supply supply)
        {
            return CapacityOf(supply) - AmountOf(supply) > 0f;
        }

        float StoreEverywhere(Supply supply, float amount, ShipAgent firstShip, Func<ShipAgent, Supply, float, float> storeOn)
        {
            float left = amount;
            if (firstShip != null)
            {
                left -= storeOn(firstShip, supply, left);
            }
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (left <= 0f)
                {
                    break;
                }
                if (ship != firstShip)
                {
                    left -= storeOn(ship, supply, left);
                }
            }
            return amount - left;
        }

        static float StoreInStorage(ShipAgent ship, Supply supply, float amount)
        {
            return TryGetStorage(ship, out SupplyStorage storage) ? storage.Stock.Store(supply, amount) : 0f;
        }

        static float StoreInTank(ShipAgent ship, Supply supply, float amount)
        {
            return HoldsFuelStock(ship) ? ship.Fuel.Tank.Fill(amount) : 0f;
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

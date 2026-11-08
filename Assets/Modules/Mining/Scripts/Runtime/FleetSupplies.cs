using System;
using LastFleet.Ships;

namespace LastFleet.Mining
{
    /// <summary>
    /// The supplies of the whole fleet. Amounts and capacities count the stores of industrial ships
    /// only; fuel already in a ship's tank is in use, not in stock. Fuel goes into the tanker's store
    /// first; without a tanker, or once it is full, only the capital ships' tanks remain (GDD:
    /// Industrial ships).
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

        /// <summary>Takes up to the amount out of the industrial ships' stores, e.g. water the people drink; returns how much there was.</summary>
        public float Take(Supply supply, float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Cannot take a negative amount.");
            }

            float left = amount;
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (left <= 0f)
                {
                    break;
                }
                if (TryGetStorage(ship, out SupplyStorage storage))
                {
                    left -= storage.Stock.Take(supply, left);
                }
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
            }
            return capacity;
        }

        /// <summary>Whether a delivery fits somewhere: in a store, or for fuel also in a capital ship's tank.</summary>
        public bool HasRoomFor(Supply supply)
        {
            if (CapacityOf(supply) - AmountOf(supply) > 0f)
            {
                return true;
            }
            if (supply != Supply.Fuel)
            {
                return false;
            }
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (HoldsFuelStock(ship) && ship.Fuel.Tank.FreeSpace > 0f)
                {
                    return true;
                }
            }
            return false;
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

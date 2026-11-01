using System;
using System.Collections.Generic;
using LastFleet.Mining;
using LastFleet.People;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Rewards
{
    /// <summary>
    /// Puts rewards where they belong: raw materials as supplies into storage and fuel into the tanks
    /// of capital ships (the carrier first), items into the fleet inventory, survivors aboard.
    /// What finds no room stays aboard the ship that brought it.
    /// </summary>
    public sealed class RewardDelivery : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;

        readonly FleetInventory _inventory = new FleetInventory();
        readonly List<Reward> _remaining = new List<Reward>();
        FleetSupplies _supplies;

        public FleetInventory Inventory => _inventory;
        FleetSupplies Supplies => _supplies ??= new FleetSupplies(_registry);

        /// <summary>Short feedback for the player when finds are unloaded.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_registry == null)
            {
                Debug.LogError("RewardDelivery needs the ship registry.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _registry.ShipSpawned += OnShipSpawned;
            foreach (ShipAgent ship in _registry.Ships)
            {
                OnShipSpawned(ship);
            }
        }

        void OnDisable()
        {
            _registry.ShipSpawned -= OnShipSpawned;
        }

        /// <summary>Unloads a landed ship's finds into the fleet; what does not fit stays aboard.</summary>
        public void Deliver(ShipRewardHold hold, Hangar hangar)
        {
            _remaining.Clear();
            foreach (Reward reward in hold.Hold.Rewards)
            {
                float left = Store(reward, hangar.Carrier, hold.Ship);
                if (left > 0.01f)
                {
                    _remaining.Add(reward.WithAmount(left));
                }
            }
            hold.Hold.ReplaceWith(_remaining);

            Notice?.Invoke(RewardTexts.Delivered.Format(("ship", hold.Ship.DisplayName), ("carrier", hangar.Carrier.DisplayName)));
            if (!hold.Hold.IsEmpty)
            {
                Notice?.Invoke(RewardTexts.NoRoomForFinds.Format(("ship", hold.Ship.DisplayName)));
            }
        }

        /// <summary>Stores one reward and returns how much of it is left over.</summary>
        float Store(Reward reward, ShipAgent carrier, ShipAgent finder)
        {
            switch (reward.Kind)
            {
                case RewardKind.Resource:
                    return StoreSupply(SupplyConversion.From(reward.Resource), reward.Amount, carrier);
                case RewardKind.Fuel:
                    return StoreSupply(Supply.Fuel, reward.Amount, carrier);
                case RewardKind.Item:
                    _inventory.Add(reward.Item, reward.People);
                    return 0f;
                case RewardKind.Survivors:
                    return finder.TryGetComponent(out ShipPassengers seats) ? reward.People - seats.Quarters.Add(reward.People) : reward.Amount;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reward), reward.Kind, null);
            }
        }

        /// <summary>Raw materials become supplies on delivery, like the harvesters' cargo.</summary>
        float StoreSupply(Supply supply, float amount, ShipAgent carrier)
        {
            return amount - Supplies.Store(supply, amount, carrier);
        }

        void OnShipSpawned(ShipAgent ship)
        {
            if (ship.TryGetComponent(out ShipRewardHold hold))
            {
                hold.Initialize(this);
            }
        }
    }
}

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
    /// Puts rewards where they belong: raw materials into storage (the carrier first), fuel into the
    /// tanks of capital ships (the carrier first), items into the fleet inventory, survivors aboard.
    /// What finds no room stays aboard the ship that brought it.
    /// </summary>
    public sealed class RewardDelivery : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;

        readonly FleetInventory _inventory = new FleetInventory();
        readonly List<Reward> _remaining = new List<Reward>();

        public FleetInventory Inventory => _inventory;

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
                    return StoreResource(reward, carrier);
                case RewardKind.Fuel:
                    return StoreFuel(reward.Amount, carrier);
                case RewardKind.Item:
                    _inventory.Add(reward.Item, reward.People);
                    return 0f;
                case RewardKind.Survivors:
                    return finder.TryGetComponent(out ShipPassengers seats) ? reward.People - seats.Quarters.Add(reward.People) : reward.Amount;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reward), reward.Kind, null);
            }
        }

        float StoreResource(Reward reward, ShipAgent carrier)
        {
            float left = reward.Amount;
            if (carrier.TryGetComponent(out RawMaterialStorage home))
            {
                left -= home.Stock.Store(reward.Resource, left);
            }
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (left <= 0f)
                {
                    break;
                }
                if (ship != carrier && ship.TryGetComponent(out RawMaterialStorage storage))
                {
                    left -= storage.Stock.Store(reward.Resource, left);
                }
            }
            return left;
        }

        float StoreFuel(float amount, ShipAgent carrier)
        {
            float left = amount - carrier.Fuel.Tank.Fill(amount);
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (left <= 0f)
                {
                    break;
                }
                if (ship != carrier && ship.IsCapital)
                {
                    left -= ship.Fuel.Tank.Fill(left);
                }
            }
            return left;
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

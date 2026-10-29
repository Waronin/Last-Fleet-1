using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Rewards
{
    /// <summary>
    /// The finds a small ship carries home. Landing in a hangar unloads them through the reward
    /// delivery; if the ship is lost, so are they.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class ShipRewardHold : MonoBehaviour
    {
        RewardHold _hold;
        ShipAgent _ship;
        RewardDelivery _delivery;

        public RewardHold Hold => _hold ??= new RewardHold();
        public ShipAgent Ship => _ship != null ? _ship : _ship = GetComponent<ShipAgent>();

        // Subscribed for the object's whole life: the ship is still active when it lands.
        void Awake()
        {
            Ship.DockedInHangar += OnDockedInHangar;
        }

        void OnDestroy()
        {
            Ship.DockedInHangar -= OnDockedInHangar;
        }

        public void Initialize(RewardDelivery delivery)
        {
            _delivery = delivery;
        }

        void OnDockedInHangar(ShipAgent ship, Hangar hangar)
        {
            if (!Hold.IsEmpty && _delivery != null)
            {
                _delivery.Deliver(this, hangar);
            }
        }
    }
}

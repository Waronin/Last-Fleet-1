using System;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.People
{
    /// <summary>
    /// The people aboard a capital ship (GDD: all capital ships carry passengers). Hits kill a share
    /// of them in proportion to the hull lost; when the ship is destroyed everyone aboard is lost.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(ShipDurability))]
    public sealed class ShipPassengers : MonoBehaviour
    {
        [Tooltip("Comfortable occupancy; up to 150 % fit, but the ship counts as overcrowded.")]
        [SerializeField, Min(1)] int _capacity = 8000;
        [SerializeField, Min(0)] int _startingPassengers = 6000;

        PassengerQuarters _quarters;
        ShipAgent _ship;
        ShipDurability _durability;

        public PassengerQuarters Quarters => _quarters ??= new PassengerQuarters(_capacity, Mathf.Min(_startingPassengers, Mathf.FloorToInt(_capacity * PassengerQuarters.OvercrowdingLimitFactor)));
        public ShipAgent Ship => _ship != null ? _ship : _ship = GetComponent<ShipAgent>();

        /// <summary>People who died aboard: the ship, the number and the cause.</summary>
        public event Action<ShipPassengers, int, GameText> Casualties;

        /// <summary>Everyone aboard died with the ship: the ship and the number.</summary>
        public event Action<ShipPassengers, int> LostWithShip;

        // Subscribed for the object's whole life, like the hull it listens to.
        void Awake()
        {
            _durability = GetComponent<ShipDurability>();
            _durability.Damaged += OnDamaged;
            _durability.DestroyedBy += OnDestroyedBy;
        }

        void OnDestroy()
        {
            _durability.Damaged -= OnDamaged;
            _durability.DestroyedBy -= OnDestroyedBy;
        }

        void OnDamaged(ShipDurability durability, float damage, GameText cause)
        {
            // The destroying hit is handled as a total loss.
            if (durability.Hull.IsDestroyed)
            {
                return;
            }

            int deaths = CasualtyRules.DeathsFromHit(Quarters.Count, damage / durability.Hull.Maximum, UnityEngine.Random.value);
            int removed = Quarters.Remove(deaths);
            if (removed > 0)
            {
                Casualties?.Invoke(this, removed, cause);
            }
        }

        void OnDestroyedBy(ShipAgent ship, GameText cause)
        {
            int lost = Quarters.Remove(Quarters.Count);
            if (lost > 0)
            {
                LostWithShip?.Invoke(this, lost);
            }
        }
    }
}

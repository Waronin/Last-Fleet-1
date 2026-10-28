using System;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Takes damage for a ship and destroys it at zero hull. Losses are permanent (GDD); the
    /// registry, squads, selection and mining slots drop the ship when its GameObject is destroyed.
    /// Repairs come later with metal, engineers and the hangar UI.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class ShipDurability : MonoBehaviour
    {
        [SerializeField, Min(1f)] float _maximumHull = 100f;

        ShipHull _hull;
        ShipAgent _ship;

        public ShipHull Hull => _hull ??= new ShipHull(_maximumHull);
        ShipAgent Ship => _ship != null ? _ship : _ship = GetComponent<ShipAgent>();

        /// <summary>Raised for every hit, with the damage and its cause.</summary>
        public event Action<ShipDurability, float, GameText> Damaged;

        /// <summary>Raised once, with the cause, right before the ship is removed.</summary>
        public event Action<ShipAgent, GameText> DestroyedBy;

        public void ApplyDamage(float amount, GameText cause)
        {
            // Docked ships are inside the carrier and only share its fate.
            if (Ship.IsDocked || Hull.IsDestroyed)
            {
                return;
            }

            bool destroyed = Hull.TakeDamage(amount);
            Damaged?.Invoke(this, amount, cause);
            if (destroyed)
            {
                DestroyedBy?.Invoke(Ship, cause);
                Destroy(gameObject);
            }
        }
    }
}

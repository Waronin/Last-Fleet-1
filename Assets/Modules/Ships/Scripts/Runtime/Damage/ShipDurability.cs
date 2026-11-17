using System;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Takes damage for a ship and destroys it at zero hull. Losses are permanent (GDD); the
    /// registry, squads, selection and mining slots drop the ship when its GameObject is destroyed.
    /// The player orders repairs of the hull; the Repairs module restores it with metal over time.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class ShipDurability : MonoBehaviour
    {
        [SerializeField, Min(1f)] float _maximumHull = 100f;

        ShipHull _hull;
        ShipAgent _ship;

        public ShipHull Hull => _hull ??= new ShipHull(EffectiveMaximumHull);

        /// <summary>The configured full hull without fleet effects; safe to read on a prefab, where no hull state should be created.</summary>
        public float MaximumHull => _maximumHull;

        float EffectiveMaximumHull => _maximumHull * Ship.Effects.Multiplier(FleetStat.MaximumHull);

        // Subscribed for the object's whole life: effects change also while the ship sits in a hangar.
        void Awake()
        {
            Ship.EffectsChanged += OnEffectsChanged;
        }

        void OnDestroy()
        {
            Ship.EffectsChanged -= OnEffectsChanged;
        }

        void OnEffectsChanged(ShipAgent ship)
        {
            if (_hull != null)
            {
                _hull.Rescale(EffectiveMaximumHull);
            }
        }

        /// <summary>
        /// Starts the hull at the given size and state instead of whole at the configured maximum, for a ship
        /// whose hull is kept elsewhere, e.g. a stranded hulk. Only before anything used the hull.
        /// </summary>
        public void InitializeHull(float maximum, float current)
        {
            if (_hull != null)
            {
                throw new InvalidOperationException($"{name}'s hull is already in use.");
            }
            if (current <= 0f || current > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(current), "The hull must be between empty (exclusive) and full.");
            }
            _maximumHull = maximum;
            _hull = new ShipHull(maximum);
            _hull.TakeDamage(maximum - current);
            _hull.Rescale(EffectiveMaximumHull);
        }
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

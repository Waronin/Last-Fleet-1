using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Shows where hits land, so a fight reads at a glance: a ring around the ship flares when its
    /// shield takes a hit, sparks fly when the hull does. Hazard damage to the hull sparks too.
    /// </summary>
    [RequireComponent(typeof(ShipDurability))]
    public sealed class HitFeedbackView : MonoBehaviour
    {
        [Tooltip("Optional. Ring around the ship, hidden until the shield takes a hit.")]
        [SerializeField] LineRenderer _shieldRing;
        [SerializeField, Min(0.01f)] float _shieldFlashSeconds = 0.15f;
        [Tooltip("Optional. Emits sparks at the ship for every hull hit.")]
        [SerializeField] ParticleSystem _sparks;
        [SerializeField, Min(1)] int _sparksPerHit = 8;

        ShipDurability _durability;
        ShipShield _shield;
        float _shieldRingWidth;
        float _shieldFlashRemainingSeconds;

        void Awake()
        {
            _durability = GetComponent<ShipDurability>();
            _shield = GetComponent<ShipShield>();
            if (_shieldRing != null)
            {
                _shieldRingWidth = _shieldRing.widthMultiplier;
                _shieldRing.enabled = false;
            }
        }

        void OnEnable()
        {
            _durability.Damaged += OnHullDamaged;
            if (_shield != null)
            {
                _shield.Absorbed += OnShieldHit;
            }
        }

        void OnDisable()
        {
            _durability.Damaged -= OnHullDamaged;
            if (_shield != null)
            {
                _shield.Absorbed -= OnShieldHit;
            }
            _shieldFlashRemainingSeconds = 0f;
            if (_shieldRing != null)
            {
                _shieldRing.enabled = false;
            }
        }

        void Update()
        {
            if (_shieldFlashRemainingSeconds <= 0f)
            {
                return;
            }
            _shieldFlashRemainingSeconds -= Time.deltaTime;
            // The ring starts bright and thins out, like a flash fading on the shield.
            _shieldRing.widthMultiplier = _shieldRingWidth * Mathf.Clamp01(_shieldFlashRemainingSeconds / _shieldFlashSeconds);
            if (_shieldFlashRemainingSeconds <= 0f)
            {
                _shieldRing.enabled = false;
            }
        }

        void OnShieldHit(float absorbed)
        {
            if (_shieldRing == null)
            {
                return;
            }
            _shieldRing.enabled = true;
            _shieldRing.widthMultiplier = _shieldRingWidth;
            _shieldFlashRemainingSeconds = _shieldFlashSeconds;
        }

        void OnHullDamaged(ShipDurability durability, float damage, GameText cause)
        {
            if (_sparks != null)
            {
                _sparks.Emit(_sparksPerHit);
            }
        }
    }
}

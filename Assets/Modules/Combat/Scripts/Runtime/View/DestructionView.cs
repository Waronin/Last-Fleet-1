using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Plays an explosion where the ship is destroyed. The explosion lives in the zone, since the ship
    /// itself is removed right after; it goes away with the zone when the fleet leaves.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(ShipDurability))]
    public sealed class DestructionView : MonoBehaviour
    {
        [SerializeField] ExplosionEffect _explosionPrefab;
        [Tooltip("Roughly the ship's length in units.")]
        [SerializeField, Min(0.1f)] float _explosionSize = 1f;

        ShipAgent _ship;
        ShipDurability _durability;

        // Subscribed for the object's whole life, like the hull it listens to.
        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _durability = GetComponent<ShipDurability>();
            _durability.DestroyedBy += OnDestroyedBy;
            if (_explosionPrefab == null)
            {
                Debug.LogError("DestructionView needs an explosion prefab.", this);
            }
        }

        void OnDestroy()
        {
            _durability.DestroyedBy -= OnDestroyedBy;
        }

        void OnDestroyedBy(ShipAgent ship, GameText cause)
        {
            ZoneView zone = _ship.Zone;
            if (_explosionPrefab == null || zone == null || _ship.IsDocked)
            {
                return;
            }

            ExplosionEffect explosion = Instantiate(_explosionPrefab, _ship.WorldPosition, Quaternion.identity, zone.transform);
            explosion.Play(_explosionSize);
        }
    }
}

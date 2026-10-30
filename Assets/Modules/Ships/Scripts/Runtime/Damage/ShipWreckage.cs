using LastFleet.Shared.Text;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Leaves salvageable wreckage where the ship is destroyed (GDD: destroyed enemy fighters leave
    /// wreckage that can yield metal; staying to collect it risks the main attack). The wreck becomes a
    /// deposit of the zone, so harvesters salvage it like a rock and it stays for the rest of the run.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(ShipDurability))]
    public sealed class ShipWreckage : MonoBehaviour
    {
        [SerializeField, Min(1f)] float _metalAmount = 25f;

        ShipAgent _ship;
        ShipDurability _durability;

        public float MetalAmount => _metalAmount;

        // Subscribed for the object's whole life, like the hull it listens to.
        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _durability = GetComponent<ShipDurability>();
            _durability.DestroyedBy += OnDestroyedBy;
        }

        void OnDestroy()
        {
            _durability.DestroyedBy -= OnDestroyedBy;
        }

        void OnDestroyedBy(ShipAgent ship, GameText cause)
        {
            ZoneView zone = _ship.Zone;
            if (zone == null || _ship.IsDocked)
            {
                return;
            }

            Vector3 localPosition = zone.transform.InverseTransformPoint(_ship.WorldPosition);
            localPosition.y = 0f;
            zone.AddDeposit(ResourceDeposit.Wreck(localPosition, _metalAmount, _ship.HeadingDegrees));
        }
    }
}

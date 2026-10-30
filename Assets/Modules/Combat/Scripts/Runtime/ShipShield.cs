using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Shield of a ship. Only weapon hits go through it (see <see cref="CombatService.DealHit"/>);
    /// hazards such as rock shards or radiation reach the hull directly. A docked ship's shield does
    /// not recharge until it flies again, since the GameObject is inactive.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(ShipDurability))]
    public sealed class ShipShield : MonoBehaviour
    {
        [SerializeField, Min(1f)] float _maximum = 60f;
        [SerializeField, Min(0f)] float _rechargePerSecond = 6f;
        [SerializeField, Min(0f)] float _rechargeDelaySeconds = 4f;

        ShieldCharge _charge;

        public ShieldCharge Charge => _charge ??= new ShieldCharge(_maximum, _rechargePerSecond, _rechargeDelaySeconds);

        void Update()
        {
            Charge.Tick(Time.deltaTime);
        }
    }
}

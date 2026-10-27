using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>Shared movement configuration of one ship class, e.g. all harvesters.</summary>
    [CreateAssetMenu(menuName = "Last Fleet/Ship Movement Profile", fileName = "ShipMovementProfile")]
    public sealed class ShipMovementProfile : ScriptableObject
    {
        [SerializeField] ShipMotionSettings _motion = new ShipMotionSettings();
        [Tooltip("Charge time of the small jump drive before a zone jump (GDD: countdown).")]
        [SerializeField, Min(0f)] float _jumpChargeSeconds = 5f;

        public ShipMotionSettings Motion => _motion;
        public float JumpChargeSeconds => _jumpChargeSeconds;
    }
}

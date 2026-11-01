using LastFleet.Ships;
using UnityEngine;
using UnityEngine.Serialization;

namespace LastFleet.Mining
{
    /// <summary>
    /// Supply store of an industrial ship. Harvesters unload at its landing bay, or when they land in
    /// its hangar with cargo aboard.
    /// </summary>
    [RequireComponent(typeof(Hangar))]
    public sealed class SupplyStorage : MonoBehaviour
    {
        [Tooltip("GDD: the refinery stores metal; ore and wrecks are delivered as metal.")]
        [SerializeField, Min(0f)] float _metalCapacity = 5000f;
        [FormerlySerializedAs("_iceCapacity")]
        [SerializeField, Min(0f)] float _waterCapacity = 5000f;

        SupplyStock _stock;
        Hangar _hangar;

        public SupplyStock Stock => _stock ??= new SupplyStock(_metalCapacity, _waterCapacity);
        public Hangar Hangar => _hangar != null ? _hangar : _hangar = GetComponent<Hangar>();
    }
}

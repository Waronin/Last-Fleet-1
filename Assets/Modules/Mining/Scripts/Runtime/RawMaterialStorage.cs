using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Raw material store of an industrial ship. Harvesters unload at its landing bay, or when they
    /// land in its hangar with cargo aboard.
    /// </summary>
    [RequireComponent(typeof(Hangar))]
    public sealed class RawMaterialStorage : MonoBehaviour
    {
        [SerializeField, Min(0f)] float _oreCapacity = 5000f;
        [SerializeField, Min(0f)] float _iceCapacity = 5000f;
        [SerializeField, Min(0f)] float _gasCapacity = 5000f;

        RawMaterialStock _stock;
        Hangar _hangar;

        public RawMaterialStock Stock => _stock ??= new RawMaterialStock(_oreCapacity, _iceCapacity, _gasCapacity);
        public Hangar Hangar => _hangar != null ? _hangar : _hangar = GetComponent<Hangar>();
    }
}

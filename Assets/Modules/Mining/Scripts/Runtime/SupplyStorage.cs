using UnityEngine;
using UnityEngine.Serialization;

namespace LastFleet.Mining
{
    /// <summary>
    /// Supply store of an industrial ship. Harvesters unload at the landing bay of a store ship with a
    /// hangar, or when they land in its hangar with cargo aboard; the fleet spreads what it cannot keep.
    /// </summary>
    public sealed class SupplyStorage : MonoBehaviour
    {
        [Tooltip("GDD: the tanker stores the fuel that does not fit into the ships' tanks.")]
        [SerializeField, Min(0f)] float _fuelCapacity;
        [Tooltip("GDD: the refinery stores metal; ore and wrecks are delivered as metal.")]
        [SerializeField, Min(0f)] float _metalCapacity = 5000f;
        [FormerlySerializedAs("_iceCapacity")]
        [SerializeField, Min(0f)] float _waterCapacity = 5000f;
        [Tooltip("Fuel aboard when the ship arrives; capped by the fuel capacity.")]
        [SerializeField, Min(0f)] float _startingFuel;

        SupplyStock _stock;

        public SupplyStock Stock => _stock ??= CreateStock();

        SupplyStock CreateStock()
        {
            SupplyStock stock = new SupplyStock(_fuelCapacity, _metalCapacity, _waterCapacity);
            stock.Store(Supply.Fuel, _startingFuel);
            return stock;
        }
    }
}

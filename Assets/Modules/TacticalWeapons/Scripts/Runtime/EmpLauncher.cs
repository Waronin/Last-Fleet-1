using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.TacticalWeapons
{
    /// <summary>
    /// The EMP missile bay of a warship (GDD: only tactical weapons use ammunition). It holds the
    /// magazine and its balancing; <see cref="EmpService"/> fires missiles and builds new ones.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class EmpLauncher : MonoBehaviour
    {
        [SerializeField, Min(1)] int _capacity = 2;
        [SerializeField, Min(0)] int _startingMissiles = 1;
        [SerializeField, Min(0.1f)] float _buildSeconds = 60f;
        [SerializeField, Min(0f)] float _metalPerMissile = 200f;
        [SerializeField, Min(0f)] float _fuelPerMissile = 100f;

        MissileMagazine _magazine;
        ShipAgent _ship;

        public MissileMagazine Magazine => _magazine ??= new MissileMagazine(_capacity, Mathf.Min(_startingMissiles, _capacity), _buildSeconds);
        public ShipAgent Ship => _ship != null ? _ship : _ship = GetComponent<ShipAgent>();
        public float MetalPerMissile => _metalPerMissile;
        public float FuelPerMissile => _fuelPerMissile;
    }
}

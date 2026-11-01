using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Tanker duty (GDD: space tanker, stores fuel and refuels other ships). Pumps fuel from its store
    /// into the tanks of the capital ships in its zone, its own included, so the fleet stays ready to
    /// jump while the tanker flies with it. Small ships refuel from their carrier in the hangar.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(SupplyStorage))]
    public sealed class FuelTanker : MonoBehaviour
    {
        [Tooltip("Fuel pumped into each capital ship in the zone per second, like a hangar's refuel rate.")]
        [SerializeField, Min(0f)] float _pumpPerSecond = 40f;

        ShipAgent _ship;
        SupplyStorage _storage;
        ShipRegistry _registry;

        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _storage = GetComponent<SupplyStorage>();
        }

        public void Initialize(ShipRegistry registry)
        {
            _registry = registry;
        }

        void Update()
        {
            if (_registry == null || _ship.Zone == null || _ship.IsDerelict || _storage.Stock.AmountOf(Supply.Fuel) <= 0f)
            {
                return;
            }

            float maximumAmount = _pumpPerSecond * Time.deltaTime;
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (ship.IsCapital && !ship.IsDerelict && ship.Zone == _ship.Zone)
                {
                    FuelPumping.Pump(_storage.Stock, ship.Fuel.Tank, maximumAmount);
                }
            }
        }
    }
}

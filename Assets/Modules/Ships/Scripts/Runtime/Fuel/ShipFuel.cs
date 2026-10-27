using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Tank and consumption of a ship. Ships start a system with full tanks; small ships refuel in a
    /// hangar from their carrier's tank. An empty ship cannot jump or use its main drive and crawls on
    /// its thrusters, which run on a reserve that costs nothing. The tanker and refining come later.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class ShipFuel : MonoBehaviour
    {
        [SerializeField, Min(1f)] float _tankCapacity = 100f;
        [Tooltip("Main drive and thrusters, per km travelled under power.")]
        [SerializeField, Min(0f)] float _fuelPerKlick = 0.1f;
        [Tooltip("Small jump drive, per AU between zones.")]
        [SerializeField, Min(0f)] float _fuelPerAU = 2f;

        FuelTank _tank;
        FuelConsumption _consumption;

        public FuelTank Tank => _tank ??= new FuelTank(_tankCapacity);
        public FuelConsumption Consumption => _consumption ??= new FuelConsumption(_fuelPerKlick, _fuelPerAU);
    }
}

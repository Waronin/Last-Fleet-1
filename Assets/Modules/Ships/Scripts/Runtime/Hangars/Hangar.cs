using System;
using System.Collections.Generic;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Hangar of a capital ship (GDD: Capital ships). Small ships land through the landing bay,
    /// travel with the carrier while docked and launch one after another. No launches happen while
    /// the carrier charges a jump, so nobody is left behind. Docked ships refuel from the carrier's tank.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class Hangar : MonoBehaviour
    {
        [SerializeField, Min(1)] int _capacity = 8;
        [SerializeField, Min(0f)] float _launchIntervalSeconds = 1.5f;
        [Tooltip("Landing bay; its forward axis points out of the bay.")]
        [SerializeField] Transform _bay;
        [SerializeField, Min(0.1f)] float _dockingRadiusUnits = 3f;
        [SerializeField, Min(0f)] float _launchExitDistanceUnits = 4f;
        [Tooltip("Fuel pumped into each docked ship per second.")]
        [SerializeField, Min(0f)] float _refuelPerSecond = 25f;

        HangarBay _slots;
        ShipAgent _carrier;

        public ShipAgent Carrier => _carrier != null ? _carrier : _carrier = GetComponent<ShipAgent>();
        public int Capacity => _capacity;
        public IReadOnlyList<ShipAgent> DockedShips => Slots.Docked;
        public int LaunchQueueCount => Slots.LaunchQueueCount;
        public bool HasSpace => Slots.HasSpace;
        public float DockingRadiusUnits => _dockingRadiusUnits;
        public Vector3 BayWorldPosition => _bay.position;

        // Created on first use, so ships can dock in the same frame the carrier is instantiated.
        HangarBay Slots => _slots ??= new HangarBay(_capacity, _launchIntervalSeconds);

        public event Action<Hangar> ContentsChanged;

        void Awake()
        {
            if (_bay == null)
            {
                Debug.LogError("Hangar needs a landing bay transform.", this);
                enabled = false;
            }
        }

        public bool TryDock(ShipAgent ship)
        {
            if (ship.IsCapital || !Slots.TryDock(ship))
            {
                return false;
            }

            ship.EnterHangar(this);
            ContentsChanged?.Invoke(this);
            return true;
        }

        /// <summary>Queues a docked ship for launch; a ship without crew stays in the hangar.</summary>
        public void RequestLaunch(ShipAgent ship)
        {
            if (ship.IsDerelict)
            {
                return;
            }
            Slots.RequestLaunch(ship);
        }

        /// <summary>Pumps up to the given amount from the carrier's tank into a ship and returns how much moved.</summary>
        public float Refuel(ShipAgent ship, float maximumAmount)
        {
            if (ship == null || ship == Carrier)
            {
                return 0f;
            }
            return Carrier.Fuel.Tank.TransferTo(ship.Fuel.Tank, maximumAmount);
        }

        void Update()
        {
            bool canLaunch = Carrier.Zone != null && Carrier.Activity != ShipActivity.ChargingJump;
            ShipAgent launching = Slots.Tick(Time.deltaTime, canLaunch);
            if (launching != null)
            {
                Launch(launching);
            }

            float refuelAmount = _refuelPerSecond * Time.deltaTime;
            foreach (ShipAgent docked in Slots.Docked)
            {
                Refuel(docked, refuelAmount);
            }
        }

        void Launch(ShipAgent ship)
        {
            ZoneView zone = Carrier.Zone;
            Vector3 exit = _bay.position + _bay.forward * _launchExitDistanceUnits;
            ship.LeaveHangar(zone, zone.transform.InverseTransformPoint(exit), Carrier.HeadingDegrees);
            ContentsChanged?.Invoke(this);
        }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.People
{
    /// <summary>
    /// Everyone in the fleet (GDD: population in the resource bar) and the reports when people die.
    /// It follows the registry, so ships that join or leave the fleet are counted automatically.
    /// </summary>
    public sealed class FleetPopulation : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;

        readonly List<ShipPassengers> _ships = new List<ShipPassengers>();

        public int TotalPassengers
        {
            get
            {
                int total = 0;
                foreach (ShipPassengers passengers in _ships)
                {
                    total += passengers.Quarters.Count;
                }
                return total;
            }
        }

        /// <summary>Living space only; the seats of transports are not housing.</summary>
        public int TotalCapacity
        {
            get
            {
                int total = 0;
                foreach (ShipPassengers passengers in _ships)
                {
                    total += passengers.IsHousing ? passengers.Quarters.Capacity : 0;
                }
                return total;
            }
        }

        /// <summary>Short feedback for the player when people die or move between ships.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_registry == null)
            {
                Debug.LogError("FleetPopulation needs the ship registry.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _registry.ShipSpawned += OnShipSpawned;
            _registry.ShipRemoved += OnShipRemoved;
            foreach (ShipAgent ship in _registry.Ships)
            {
                OnShipSpawned(ship);
            }
        }

        void OnDisable()
        {
            _registry.ShipSpawned -= OnShipSpawned;
            _registry.ShipRemoved -= OnShipRemoved;
            foreach (ShipPassengers passengers in _ships)
            {
                Unsubscribe(passengers);
            }
            _ships.Clear();
        }

        void OnShipSpawned(ShipAgent ship)
        {
            if (!ship.TryGetComponent(out ShipPassengers passengers) || _ships.Contains(passengers))
            {
                return;
            }
            _ships.Add(passengers);
            passengers.Casualties += OnCasualties;
            passengers.LostWithShip += OnLostWithShip;
            passengers.Disembarked += OnDisembarked;
        }

        void OnShipRemoved(ShipAgent ship)
        {
            if (ship.TryGetComponent(out ShipPassengers passengers) && _ships.Remove(passengers))
            {
                Unsubscribe(passengers);
            }
        }

        void Unsubscribe(ShipPassengers passengers)
        {
            passengers.Casualties -= OnCasualties;
            passengers.LostWithShip -= OnLostWithShip;
            passengers.Disembarked -= OnDisembarked;
        }

        void OnCasualties(ShipPassengers passengers, int deaths, GameText cause)
        {
            Notice?.Invoke(PeopleTexts.CasualtiesFromHit.Format(("count", deaths), ("ship", passengers.Ship.DisplayName), ("cause", cause)));
        }

        void OnLostWithShip(ShipPassengers passengers, int lost)
        {
            Notice?.Invoke(PeopleTexts.LostWithShip.Format(("count", lost), ("ship", passengers.Ship.DisplayName)));
        }

        void OnDisembarked(ShipPassengers transport, ShipPassengers carrier, int people)
        {
            Notice?.Invoke(PeopleTexts.Disembarked.Format(("count", people), ("ship", transport.Ship.DisplayName), ("carrier", carrier.Ship.DisplayName)));
        }
    }
}

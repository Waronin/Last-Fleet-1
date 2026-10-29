using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.People
{
    /// <summary>
    /// Everyone in the fleet, passengers and crews of small ships (GDD: population in the resource
    /// bar), and the reports when people die or change ships. It follows the registry, so ships that
    /// join or leave the fleet are counted automatically.
    /// </summary>
    public sealed class FleetPopulation : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;

        readonly List<ShipPassengers> _passengers = new List<ShipPassengers>();
        readonly List<ShipCrew> _crews = new List<ShipCrew>();

        // Crew and passengers of a destroyed ship report separately; one notice sums them up.
        readonly Dictionary<ShipAgent, int> _lostWithShip = new Dictionary<ShipAgent, int>();

        public int TotalPassengers
        {
            get
            {
                int total = 0;
                foreach (ShipPassengers passengers in _passengers)
                {
                    total += passengers.Quarters.Count;
                }
                return total;
            }
        }

        public int TotalCrew
        {
            get
            {
                int total = 0;
                foreach (ShipCrew crew in _crews)
                {
                    total += crew.Complement.Count;
                }
                return total;
            }
        }

        public int TotalPeople => TotalPassengers + TotalCrew;

        /// <summary>Living space only; the seats of transports are not housing.</summary>
        public int TotalCapacity
        {
            get
            {
                int total = 0;
                foreach (ShipPassengers passengers in _passengers)
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
            _registry.ShipLost += OnShipLost;
            foreach (ShipAgent ship in _registry.Ships)
            {
                OnShipSpawned(ship);
            }
        }

        void OnDisable()
        {
            _registry.ShipSpawned -= OnShipSpawned;
            _registry.ShipRemoved -= OnShipRemoved;
            _registry.ShipLost -= OnShipLost;
            foreach (ShipPassengers passengers in _passengers)
            {
                Unsubscribe(passengers);
            }
            foreach (ShipCrew crew in _crews)
            {
                Unsubscribe(crew);
            }
            _passengers.Clear();
            _crews.Clear();
            _lostWithShip.Clear();
        }

        void OnShipSpawned(ShipAgent ship)
        {
            if (ship.TryGetComponent(out ShipPassengers passengers) && !_passengers.Contains(passengers))
            {
                _passengers.Add(passengers);
                passengers.Casualties += OnPassengerCasualties;
                passengers.LostWithShip += OnPassengersLostWithShip;
                passengers.Disembarked += OnDisembarked;
            }
            if (ship.Crew != null && !_crews.Contains(ship.Crew))
            {
                _crews.Add(ship.Crew);
                ship.Crew.CrewLost += OnCrewLost;
                ship.Crew.LostWithShip += OnCrewLostWithShip;
            }
        }

        void OnShipRemoved(ShipAgent ship)
        {
            if (ship.TryGetComponent(out ShipPassengers passengers) && _passengers.Remove(passengers))
            {
                Unsubscribe(passengers);
            }
            if (ship.Crew != null && _crews.Remove(ship.Crew))
            {
                Unsubscribe(ship.Crew);
            }
            _lostWithShip.Remove(ship);
        }

        void Unsubscribe(ShipPassengers passengers)
        {
            passengers.Casualties -= OnPassengerCasualties;
            passengers.LostWithShip -= OnPassengersLostWithShip;
            passengers.Disembarked -= OnDisembarked;
        }

        void Unsubscribe(ShipCrew crew)
        {
            crew.CrewLost -= OnCrewLost;
            crew.LostWithShip -= OnCrewLostWithShip;
        }

        void OnPassengerCasualties(ShipPassengers passengers, int deaths, GameText cause)
        {
            ReportDeaths(passengers.Ship, deaths, cause);
        }

        void OnCrewLost(ShipCrew crew, int deaths, GameText cause)
        {
            ReportDeaths(crew.Ship, deaths, cause);
        }

        void ReportDeaths(ShipAgent ship, int deaths, GameText cause)
        {
            Notice?.Invoke(PeopleTexts.CasualtiesFromHit.Format(("count", deaths), ("ship", ship.DisplayName), ("cause", cause)));
        }

        void OnPassengersLostWithShip(ShipPassengers passengers, int lost)
        {
            AddLostWithShip(passengers.Ship, lost);
        }

        void OnCrewLostWithShip(ShipCrew crew, int lost)
        {
            AddLostWithShip(crew.Ship, lost);
        }

        void AddLostWithShip(ShipAgent ship, int lost)
        {
            _lostWithShip.TryGetValue(ship, out int total);
            _lostWithShip[ship] = total + lost;
        }

        // The registry reports the loss after the ship's own components have counted their dead.
        void OnShipLost(ShipAgent ship, GameText cause)
        {
            if (_lostWithShip.TryGetValue(ship, out int lost) && lost > 0)
            {
                Notice?.Invoke(PeopleTexts.LostWithShip.Format(("count", lost), ("ship", ship.DisplayName)));
            }
            _lostWithShip.Remove(ship);
        }

        void OnDisembarked(ShipPassengers transport, ShipPassengers carrier, int people)
        {
            Notice?.Invoke(PeopleTexts.Disembarked.Format(("count", people), ("ship", transport.Ship.DisplayName), ("carrier", carrier.Ship.DisplayName)));
        }
    }
}

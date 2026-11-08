using System;
using System.Collections.Generic;
using LastFleet.Mining;
using LastFleet.People;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Provisions
{
    /// <summary>
    /// The people of the fleet drink from the industrial ships' water stores, in proportion to their
    /// number (<see cref="WaterConsumption"/>). When the water runs out they die of thirst, passengers
    /// before crews, so more people saved also means more ice to mine. Warns when the water runs low.
    /// </summary>
    public sealed class FleetWaterSupply : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;
        [SerializeField] FleetPopulation _population;
        [Tooltip("How many people drink one unit of water per minute.")]
        [SerializeField, Min(1f)] float _peoplePerUnitPerMinute = 50f;
        [Tooltip("Share of the people who die per minute without any water.")]
        [SerializeField, Range(0.001f, 1f)] float _deathFractionPerMinute = 0.01f;
        [Tooltip("Warn when the water lasts less than this at the current population.")]
        [SerializeField, Min(0f)] float _warningMinutes = 5f;
        [Tooltip("Deaths of thirst are reported in batches of this much game time.")]
        [SerializeField, Min(1f)] float _deathBatchSeconds = 15f;

        readonly List<ShipPassengers> _passengers = new List<ShipPassengers>();
        WaterConsumption _consumption;
        FleetSupplies _supplies;

        public WaterConsumption Consumption => _consumption ??= new WaterConsumption(_peoplePerUnitPerMinute, _deathFractionPerMinute,
            _warningMinutes, _deathBatchSeconds);

        /// <summary>How long the water lasts at the current population, for the HUD.</summary>
        public float MinutesLeft => Consumption.MinutesLeft(Supplies.AmountOf(Supply.Water), _population.TotalPeople);

        /// <summary>Short feedback for the player when the water runs low or out.</summary>
        public event Action<GameText> Notice;

        FleetSupplies Supplies => _supplies ??= new FleetSupplies(_registry);

        void Awake()
        {
            if (_registry == null || _population == null)
            {
                Debug.LogError("FleetWaterSupply needs the fleet's ship registry and population.", this);
                enabled = false;
            }
        }

        void Update()
        {
            int people = _population.TotalPeople;
            WaterStep step = Consumption.Advance(Time.deltaTime, people, Supplies.AmountOf(Supply.Water));
            if (step.WaterToTake > 0f)
            {
                Supplies.Take(Supply.Water, step.WaterToTake);
            }

            if (step.RanDry)
            {
                Notice?.Invoke(ProvisionsTexts.WaterOut);
            }
            else if (step.BecameLow)
            {
                Notice?.Invoke(ProvisionsTexts.WaterLow.Format(("minutes", Mathf.CeilToInt(MinutesLeft))));
            }
            if (step.Deaths > 0)
            {
                KillOfThirst(step.Deaths);
            }
        }

        /// <summary>
        /// Passengers die first, spread over the ships by how many are aboard; crews only when no passenger
        /// is left. One notice per batch sums the dead up instead of one per ship.
        /// </summary>
        void KillOfThirst(int deaths)
        {
            int died = KillPassengers(deaths);
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (died >= deaths)
                {
                    break;
                }
                if (ship.Crew != null)
                {
                    // Crews report their own losses: a ship can be left without anyone to fly it.
                    died += ship.Crew.Kill(deaths - died, ProvisionsTexts.ThirstCause);
                }
            }
            if (died > 0)
            {
                Notice?.Invoke(ProvisionsTexts.ThirstDeaths.Format(("count", died)));
            }
        }

        int KillPassengers(int deaths)
        {
            _passengers.Clear();
            int aboard = 0;
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (ship.TryGetComponent(out ShipPassengers passengers) && passengers.Quarters.Count > 0)
                {
                    _passengers.Add(passengers);
                    aboard += passengers.Quarters.Count;
                }
            }

            if (aboard == 0)
            {
                return 0;
            }

            // Proportional shares first, then whatever the rounding left over from the fullest ships.
            int left = deaths;
            foreach (ShipPassengers passengers in _passengers)
            {
                left -= passengers.Quarters.Remove(deaths * passengers.Quarters.Count / aboard);
            }
            _passengers.Sort((a, b) => b.Quarters.Count.CompareTo(a.Quarters.Count));
            foreach (ShipPassengers passengers in _passengers)
            {
                if (left <= 0)
                {
                    break;
                }
                left -= passengers.Quarters.Remove(left);
            }
            return deaths - left;
        }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.ShipControl
{
    /// <summary>Where a ship stands in an automatic refueling trip.</summary>
    public enum FuelReturnState
    {
        None,
        Returning,
        Refueling
    }

    /// <summary>
    /// Automatic refueling of small ships: below the threshold the player set (off, 15, 30 or 50 %,
    /// 30 % to start) a ship interrupts what it does, even in combat, and lands in the nearest hangar
    /// with room. Once the tank is full it takes up its task again: automations that can resume
    /// (<see cref="IResumableAutomation"/>) start over, otherwise its open orders are given again;
    /// with neither it stays in the hangar. Any order from the player on the way ends the trip, and the
    /// ship then flies on low until it lands or is above its threshold again.
    /// </summary>
    public sealed class FuelReturnService : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;
        [Tooltip("A carrier with this little fuel left is treated as dry: the ship stops waiting for more.")]
        [SerializeField, Min(0f)] float _dryCarrierFuel = 0.5f;

        sealed class Trip
        {
            public FuelReturnState State;
            public Hangar Hangar;
            public readonly List<Action> Resumes = new List<Action>();
            public readonly List<ShipOrder> Orders = new List<ShipOrder>();
            public bool ReportedDryCarrier;
        }

        readonly Dictionary<ShipAgent, float> _thresholds = new Dictionary<ShipAgent, float>();
        readonly Dictionary<ShipAgent, Trip> _trips = new Dictionary<ShipAgent, Trip>();
        readonly List<ShipAgent> _finished = new List<ShipAgent>();
        readonly List<IResumableAutomation> _automations = new List<IResumableAutomation>();
        // Ships whose trip the player cut short: they fly on low until they land or are above the threshold again.
        readonly HashSet<ShipAgent> _overridden = new HashSet<ShipAgent>();
        readonly List<ShipAgent> _released = new List<ShipAgent>();
        ShipAgent _issuingFor;

        /// <summary>Short feedback for the player about ships that turn back or cannot refuel.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_registry == null)
            {
                Debug.LogError("FuelReturnService needs the fleet's ship registry.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _registry.ShipRemoved += Forget;
        }

        void OnDisable()
        {
            _registry.ShipRemoved -= Forget;
        }

        /// <summary>Whether the setting applies: small ships return to hangars, capital ships do not.</summary>
        public static bool Applies(ShipAgent ship)
        {
            return ship != null && !ship.IsCapital;
        }

        public float ThresholdOf(ShipAgent ship)
        {
            return _thresholds.TryGetValue(ship, out float threshold) ? threshold : RefuelThresholds.Default;
        }

        /// <summary>Sets the threshold of every small ship among the given ones; returns how many it applied to.</summary>
        public int SetThreshold(IReadOnlyList<ShipAgent> ships, float threshold)
        {
            int count = 0;
            foreach (ShipAgent ship in ships)
            {
                if (Applies(ship))
                {
                    _thresholds[ship] = threshold;
                    count++;
                }
            }
            return count;
        }

        public FuelReturnState StateOf(ShipAgent ship)
        {
            return _trips.TryGetValue(ship, out Trip trip) ? trip.State : FuelReturnState.None;
        }

        void Update()
        {
            ReleaseOverrides();
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (Applies(ship) && !_trips.ContainsKey(ship) && ShouldStartTrip(ship))
                {
                    StartTrip(ship);
                }
            }

            _finished.Clear();
            foreach (KeyValuePair<ShipAgent, Trip> entry in _trips)
            {
                if (UpdateTrip(entry.Key, entry.Value))
                {
                    _finished.Add(entry.Key);
                }
            }
            foreach (ShipAgent ship in _finished)
            {
                EndTrip(ship);
            }
        }

        void ReleaseOverrides()
        {
            _released.Clear();
            foreach (ShipAgent ship in _overridden)
            {
                if (ship == null || ship.IsDocked || !RefuelThresholds.ShouldReturn(ship.Fuel.Tank.Fraction, ThresholdOf(ship)))
                {
                    _released.Add(ship);
                }
            }
            foreach (ShipAgent ship in _released)
            {
                _overridden.Remove(ship);
            }
        }

        bool ShouldStartTrip(ShipAgent ship)
        {
            if (_overridden.Contains(ship))
            {
                return false;
            }
            // Busy elsewhere for a moment, or already on the way to a hangar (e.g. by the player's H).
            if (ship.IsDocked || ship.IsDerelict || ship.IsStunned || ship.Activity == ShipActivity.ChargingJump ||
                ship.Activity == ShipActivity.Docking)
            {
                return false;
            }
            return RefuelThresholds.ShouldReturn(ship.Fuel.Tank.Fraction, ThresholdOf(ship));
        }

        void StartTrip(ShipAgent ship)
        {
            Hangar hangar = FindHangar(ship);
            if (hangar == null)
            {
                // A waiting entry keeps the ship from asking every frame; tried again once a hangar has room.
                _trips[ship] = new Trip { State = FuelReturnState.None };
                Notice?.Invoke(ShipControlTexts.NoHangarToRefuel.Format(("ship", ship.DisplayName)));
                return;
            }

            Trip trip = new Trip { State = FuelReturnState.Returning, Hangar = hangar };
            // Automations stop themselves and hand back how to start over; plain orders are copied.
            ship.GetComponents(_automations);
            foreach (IResumableAutomation automation in _automations)
            {
                Action resume = automation.Suspend();
                if (resume != null)
                {
                    trip.Resumes.Add(resume);
                }
            }
            if (trip.Resumes.Count == 0)
            {
                ship.CopyOrders(trip.Orders);
            }

            _trips[ship] = trip;
            ship.OrdersReplaced += OnOrdersReplaced;
            Issue(ship, () => ship.Order(ShipOrder.DockAt(hangar), false));
            Notice?.Invoke(ShipControlTexts.ReturningToRefuel.Format(("ship", ship.DisplayName),
                ("percent", Mathf.RoundToInt(ThresholdOf(ship) * 100f))));
        }

        /// <summary>Advances a trip; returns true when it is over.</summary>
        bool UpdateTrip(ShipAgent ship, Trip trip)
        {
            if (ship == null)
            {
                return true;
            }
            switch (trip.State)
            {
                case FuelReturnState.None:
                    // Waiting for a free hangar. Once one has room, or the tank is fine again, the
                    // wait ends; the next frame starts a new trip if one is still needed.
                    return FindHangar(ship) != null || !RefuelThresholds.ShouldReturn(ship.Fuel.Tank.Fraction, ThresholdOf(ship));
                case FuelReturnState.Returning:
                    if (ship.IsDocked)
                    {
                        trip.State = FuelReturnState.Refueling;
                    }
                    return false;
                default:
                    return UpdateRefueling(ship, trip);
            }
        }

        bool UpdateRefueling(ShipAgent ship, Trip trip)
        {
            // Launched by the player meanwhile: the trip is over, the task is not taken up again.
            if (!ship.IsDocked)
            {
                return true;
            }

            FuelTank tank = ship.Fuel.Tank;
            bool full = tank.FreeSpace <= 0.01f;
            bool carrierDry = ship.DockedIn.Carrier.Fuel.Tank.Amount <= _dryCarrierFuel;
            if (!full && carrierDry && !trip.ReportedDryCarrier)
            {
                trip.ReportedDryCarrier = true;
                Notice?.Invoke(ShipControlTexts.CarrierOutOfFuel.Format(("ship", ship.DisplayName)));
            }
            // With a dry carrier the ship goes on as soon as it is above its threshold again.
            if (!full && !(carrierDry && !RefuelThresholds.ShouldReturn(tank.Fraction, ThresholdOf(ship))))
            {
                return false;
            }

            Resume(ship, trip);
            return true;
        }

        void Resume(ShipAgent ship, Trip trip)
        {
            // Stop listening first: resuming gives new orders, which are not the player's.
            ship.OrdersReplaced -= OnOrdersReplaced;
            trip.State = FuelReturnState.None;
            foreach (Action resume in trip.Resumes)
            {
                resume();
            }
            for (int i = 0; i < trip.Orders.Count; i++)
            {
                ship.Order(trip.Orders[i], i > 0);
            }
        }

        void EndTrip(ShipAgent ship)
        {
            if (ship != null)
            {
                ship.OrdersReplaced -= OnOrdersReplaced;
            }
            _trips.Remove(ship);
        }

        void OnOrdersReplaced(ShipAgent ship)
        {
            if (ship == _issuingFor)
            {
                return;
            }
            // The player took over: no automatic return of the task, and no new trip right away.
            EndTrip(ship);
            if (ship != null && !ship.IsDocked)
            {
                _overridden.Add(ship);
            }
        }

        void Issue(ShipAgent ship, Action order)
        {
            _issuingFor = ship;
            order();
            _issuingFor = null;
        }

        /// <summary>The nearest hangar with room in the ship's zone, else its home hangar or any with room.</summary>
        Hangar FindHangar(ShipAgent ship)
        {
            Hangar nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (Hangar hangar in _registry.Hangars)
            {
                if (hangar == null || !hangar.HasSpace || hangar.Carrier.Zone != ship.Zone)
                {
                    continue;
                }
                float distance = (hangar.BayWorldPosition - ship.WorldPosition).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = hangar;
                    nearestDistance = distance;
                }
            }
            return nearest != null ? nearest : _registry.FindHangarFor(ship);
        }

        void Forget(ShipAgent ship)
        {
            EndTrip(ship);
            _thresholds.Remove(ship);
            _overridden.Remove(ship);
        }
    }
}

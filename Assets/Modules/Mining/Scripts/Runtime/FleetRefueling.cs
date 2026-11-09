using System;
using LastFleet.Ships;

namespace LastFleet.Mining
{
    /// <summary>
    /// Refueling of capital ships as the player orders it in the ship window, the same way repairs are
    /// ordered. A ship joins the queue of a tanker in its own zone, the one with the shortest queue;
    /// without a tanker there the order is refused. The queues live in the tankers, so this class keeps
    /// no state of its own.
    /// </summary>
    public sealed class FleetRefueling
    {
        readonly ShipRegistry _registry;

        public FleetRefueling(ShipRegistry registry)
        {
            _registry = registry != null ? registry : throw new ArgumentNullException(nameof(registry));
        }

        public RefuelEligibility Check(ShipAgent ship)
        {
            if (ship == null || !ship.IsCapital)
            {
                return RefuelEligibility.NotCapital;
            }
            if (TryGetJob(ship, out FuelTanker _, out int _))
            {
                return RefuelEligibility.AlreadyQueued;
            }
            if (ship.Fuel.Tank.FreeSpace <= 0f)
            {
                return RefuelEligibility.TankFull;
            }
            return TryFindTanker(ship, out FuelTanker _) ? RefuelEligibility.Eligible : RefuelEligibility.NoTankerInZone;
        }

        /// <summary>Puts the ship at the end of a tanker's queue in its zone. Returns why not otherwise.</summary>
        public RefuelEligibility Order(ShipAgent ship)
        {
            RefuelEligibility eligibility = Check(ship);
            if (eligibility == RefuelEligibility.Eligible && TryFindTanker(ship, out FuelTanker tanker))
            {
                tanker.Queue.Enqueue(ship);
            }
            return eligibility;
        }

        /// <summary>Lets the tanker refuel the ship next; the current refueling waits.</summary>
        public bool RefuelFirst(ShipAgent ship)
        {
            return TryGetJob(ship, out FuelTanker tanker, out int _) && tanker.Queue.MoveToFront(ship);
        }

        public bool Cancel(ShipAgent ship)
        {
            return TryGetJob(ship, out FuelTanker tanker, out int _) && tanker.Queue.Remove(ship);
        }

        /// <summary>Finds the tanker a ship waits at and its zero-based place; place 0 is being refueled.</summary>
        public bool TryGetJob(ShipAgent ship, out FuelTanker tanker, out int position)
        {
            foreach (ShipAgent candidate in _registry.Ships)
            {
                if (candidate.TryGetComponent(out tanker))
                {
                    position = tanker.Queue.PositionOf(ship);
                    if (position >= 0)
                    {
                        return true;
                    }
                }
            }
            tanker = null;
            position = -1;
            return false;
        }

        /// <summary>A tanker in the ship's zone, the one with the fewest ships waiting.</summary>
        bool TryFindTanker(ShipAgent ship, out FuelTanker best)
        {
            best = null;
            foreach (ShipAgent candidate in _registry.Ships)
            {
                if (!candidate.IsDerelict && candidate.TryGetComponent(out FuelTanker tanker) && tanker.CanReach(ship) &&
                    (best == null || tanker.Queue.Count < best.Queue.Count))
                {
                    best = tanker;
                }
            }
            return best != null;
        }
    }
}

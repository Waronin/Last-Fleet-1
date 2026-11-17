using System;
using LastFleet.Repairs;
using LastFleet.Zones;

namespace LastFleet.StrandedShips
{
    public enum StrandedRepairState
    {
        /// <summary>The player has not ordered the repair.</summary>
        NotOrdered,

        /// <summary>Ordered, but no ship with a crew is docked to do the work.</summary>
        WaitingForCrew,

        /// <summary>A crew is aboard, but the fleet has no metal.</summary>
        WaitingForMetal,

        Repairing,

        /// <summary>Whole and taken into the fleet.</summary>
        Recovered
    }

    /// <summary>
    /// The repair of one stranded hull in hull points of its class, at the pace and price of a capital
    /// ship's shipyard (see the Repairs module): a docked crew restores the hull while the fleet has metal.
    /// </summary>
    public sealed class StrandedRepairPlan
    {
        readonly RepairWork _work;

        public StrandedRepairPlan(float maximumHullPoints, float hullPerSecond, float metalPerHullPoint)
        {
            if (maximumHullPoints <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumHullPoints), "A ship class needs a hull.");
            }
            if (hullPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hullPerSecond), "Repairs need a positive pace.");
            }
            MaximumHullPoints = maximumHullPoints;
            HullPerSecond = hullPerSecond;
            _work = new RepairWork(metalPerHullPoint);
        }

        public float MaximumHullPoints { get; }
        public float HullPerSecond { get; }

        public float MissingHullPoints(StrandedHull hull) => hull.MissingFraction * MaximumHullPoints;
        public float SecondsLeft(StrandedHull hull) => MissingHullPoints(hull) / HullPerSecond;
        public float MetalLeft(StrandedHull hull) => MissingHullPoints(hull) * _work.MetalPerHullPoint;

        /// <summary>Repairs the hull for one step and returns what it restored and cost.</summary>
        public RepairStep Advance(StrandedHull hull, float deltaSeconds, float metalAvailable)
        {
            if (hull == null)
            {
                throw new ArgumentNullException(nameof(hull));
            }
            RepairStep step = _work.Plan(MissingHullPoints(hull), HullPerSecond, deltaSeconds, metalAvailable);
            hull.Repair(step.HullPoints / MaximumHullPoints);
            return step;
        }
    }
}

using System;

namespace LastFleet.Repairs
{
    /// <summary>Hull points and metal of one repair step.</summary>
    public readonly struct RepairStep
    {
        public RepairStep(float hullPoints, float metal, bool isShortOfMetal)
        {
            HullPoints = hullPoints;
            Metal = metal;
            IsShortOfMetal = isShortOfMetal;
        }

        public float HullPoints { get; }
        public float Metal { get; }

        /// <summary>True when missing metal held the step back.</summary>
        public bool IsShortOfMetal { get; }
    }

    /// <summary>
    /// How much hull a shipyard restores in a step: its rate over the elapsed time, no more than the
    /// ship is missing, and no more than the fleet's metal pays for (GDD: repairs cost metal and run
    /// in real time).
    /// </summary>
    public sealed class RepairWork
    {
        public RepairWork(float metalPerHullPoint)
        {
            if (metalPerHullPoint < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(metalPerHullPoint), "Metal per hull point cannot be negative.");
            }
            MetalPerHullPoint = metalPerHullPoint;
        }

        public float MetalPerHullPoint { get; }

        public RepairStep Plan(float missingHull, float hullPerSecond, float deltaSeconds, float metalAvailable)
        {
            if (missingHull <= 0f || hullPerSecond <= 0f || deltaSeconds <= 0f)
            {
                return new RepairStep(0f, 0f, false);
            }

            float wanted = Math.Min(missingHull, hullPerSecond * deltaSeconds);
            if (MetalPerHullPoint <= 0f)
            {
                return new RepairStep(wanted, 0f, false);
            }

            float affordable = Math.Max(0f, metalAvailable) / MetalPerHullPoint;
            float points = Math.Min(wanted, affordable);
            return new RepairStep(points, points * MetalPerHullPoint, points < wanted);
        }
    }
}

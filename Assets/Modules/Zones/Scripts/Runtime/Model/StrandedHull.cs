using System;

namespace LastFleet.Zones
{
    /// <summary>
    /// The hull of a stranded capital ship during the run (GDD: new capital ships must be repaired and
    /// fuelled before they join). It is found badly damaged; the player orders the repair, which runs
    /// while a crew is docked. Once whole, the ship is taken into the fleet and the hulk is gone.
    /// The hull is kept as a share of the class's maximum; whoever repairs it knows the hull points.
    /// </summary>
    public sealed class StrandedHull
    {
        public const float MinimumFoundFraction = 0.2f;
        public const float MaximumFoundFraction = 0.5f;

        public StrandedHull(StrandedShipClass shipClass, float fraction)
        {
            if (fraction <= 0f || fraction > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(fraction), "A stranded hull is between empty (exclusive) and whole.");
            }
            Class = shipClass;
            Fraction = fraction;
        }

        public StrandedShipClass Class { get; }

        /// <summary>Share of the class's full hull, 1 when whole.</summary>
        public float Fraction { get; private set; }

        public bool IsWhole => Fraction >= 1f;
        public float MissingFraction => 1f - Fraction;

        /// <summary>The player ordered the repair; it runs whenever a crew is docked.</summary>
        public bool IsRepairOrdered { get; private set; }

        /// <summary>Taken into the fleet as a ship; the hulk is gone from the zone.</summary>
        public bool IsRecovered { get; private set; }

        public void OrderRepair()
        {
            if (!IsRecovered)
            {
                IsRepairOrdered = true;
            }
        }

        public void CancelRepair()
        {
            IsRepairOrdered = false;
        }

        /// <summary>Restores a share of the hull, no more than is missing; returns the share restored.</summary>
        public float Repair(float fraction)
        {
            if (fraction < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fraction), "Cannot repair a negative share.");
            }
            float restored = Math.Min(fraction, MissingFraction);
            Fraction = Math.Min(1f, Fraction + restored);
            return restored;
        }

        /// <summary>Takes damage as a share of the full hull, e.g. from Pronin fire; true once nothing is left.</summary>
        public bool TakeDamage(float fraction)
        {
            if (fraction < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fraction), "Cannot take negative damage.");
            }
            Fraction = Math.Max(0f, Fraction - fraction);
            return IsBroken;
        }

        /// <summary>Shot to pieces: there is nothing left to repair.</summary>
        public bool IsBroken => Fraction <= 0f;

        public void MarkRecovered()
        {
            if (!IsWhole)
            {
                throw new InvalidOperationException("Only a whole hull can join the fleet.");
            }
            IsRecovered = true;
            IsRepairOrdered = false;
        }
    }
}

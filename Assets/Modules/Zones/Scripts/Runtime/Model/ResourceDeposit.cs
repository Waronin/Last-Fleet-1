using System;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>Raw materials that harvesters mine (GDD: Resources). Refineries turn them into products.</summary>
    public enum ResourceKind
    {
        Ore,
        Ice,
        Gas
    }

    public enum DepositShape
    {
        Asteroid,
        GasCloud
    }

    /// <summary>
    /// A mineable deposit inside a zone. Its remaining amount is run state: it survives leaving
    /// and re-entering the system, so a mined-out deposit stays mined out.
    /// </summary>
    public sealed class ResourceDeposit
    {
        public ResourceDeposit(DepositShape shape, ResourceKind resource, Vector3 localPosition, float radiusUnits, float amount)
        {
            if (radiusUnits <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radiusUnits), "Radius must be positive.");
            }
            if (amount <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }

            Shape = shape;
            Resource = resource;
            LocalPosition = localPosition;
            RadiusUnits = radiusUnits;
            InitialAmount = amount;
            RemainingAmount = amount;
        }

        public DepositShape Shape { get; }
        public ResourceKind Resource { get; }

        /// <summary>Position relative to the zone center, in Unity units.</summary>
        public Vector3 LocalPosition { get; }

        public float RadiusUnits { get; }
        public float InitialAmount { get; }
        public float RemainingAmount { get; private set; }
        public float RemainingFraction => RemainingAmount / InitialAmount;
        public bool IsDepleted => RemainingAmount <= 0f;

        /// <summary>Removes up to the requested amount and returns how much was actually extracted.</summary>
        public float Extract(float requestedAmount)
        {
            if (requestedAmount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedAmount), "Cannot extract a negative amount.");
            }

            float extracted = Mathf.Min(requestedAmount, RemainingAmount);
            RemainingAmount -= extracted;
            return extracted;
        }
    }
}

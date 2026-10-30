using System;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>Raw materials that harvesters mine (GDD: Resources). Refineries turn them into products.</summary>
    public enum ResourceKind
    {
        Ore,
        Ice,
        Gas,

        /// <summary>Salvaged from wrecks (GDD: destroyed enemy fighters leave wreckage that yields metal).</summary>
        Metal
    }

    public enum DepositShape
    {
        Asteroid,
        GasCloud,

        /// <summary>Wreckage of a destroyed ship, salvaged like a deposit.</summary>
        Wreck
    }

    /// <summary>GDD: Asteroids and belts, hazards while mining. Known only after a scan.</summary>
    public enum DepositHazard
    {
        None,

        /// <summary>Can break apart; the debris damages nearby ships.</summary>
        Brittle,

        /// <summary>The crew takes damage over time; a doctor can reduce it.</summary>
        Radioactive,

        /// <summary>Mining can trigger an explosion.</summary>
        GasPockets
    }

    /// <summary>Rough size estimate that is visible without a scan.</summary>
    public enum DepositSizeClass
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// A mineable deposit inside a zone. Remaining amount and scan state are run state: they survive
    /// leaving and re-entering the system, so a mined-out deposit stays mined out.
    /// </summary>
    public sealed class ResourceDeposit : IScanTarget
    {
        public const float MediumRadiusUnits = 1.5f;
        public const float LargeRadiusUnits = 3f;

        /// <summary>Distance of a mining slot from the deposit surface, where a harvester attaches.</summary>
        public const float SlotClearanceUnits = 1.2f;
        const float ScanStandoffMarginUnits = 1.5f;

        const int MaximumGasCloudSlots = 4;

        /// <summary>Wreckage of a fighter is a small, tumbling cluster of plates.</summary>
        const float WreckRadiusUnits = 0.8f;

        public ResourceDeposit(DepositShape shape, ResourceKind resource, Vector3 localPosition, float radiusUnits, float amount,
            DepositHazard hazard, float slotAngleOffsetDegrees)
        {
            if (radiusUnits <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radiusUnits), "Radius must be positive.");
            }
            if (amount <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }
            if (shape != DepositShape.Asteroid && hazard != DepositHazard.None)
            {
                throw new ArgumentException("Only asteroids have mining hazards.", nameof(hazard));
            }

            Shape = shape;
            Resource = resource;
            LocalPosition = localPosition;
            RadiusUnits = radiusUnits;
            InitialAmount = amount;
            RemainingAmount = amount;
            Hazard = hazard;
            SlotAngleOffsetDegrees = slotAngleOffsetDegrees;
        }

        /// <summary>
        /// Wreckage left where a ship was destroyed. It is known on sight, since the fleet saw the ship
        /// go down, so it needs no scan.
        /// </summary>
        public static ResourceDeposit Wreck(Vector3 localPosition, float metalAmount, float slotAngleOffsetDegrees)
        {
            ResourceDeposit wreck = new ResourceDeposit(DepositShape.Wreck, ResourceKind.Metal, localPosition, WreckRadiusUnits, metalAmount,
                DepositHazard.None, slotAngleOffsetDegrees);
            wreck.Scan();
            return wreck;
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
        /// <summary>Run state: a hazard can be used up, e.g. when gas pockets have exploded.</summary>
        public DepositHazard Hazard { get; private set; }
        public float SlotAngleOffsetDegrees { get; }

        /// <summary>Whether a scan revealed resource, exact amount, hazard and mining slots.</summary>
        public bool IsScanned { get; private set; }

        /// <summary>Total harvester-seconds spent mining this deposit; drives the mining bonus.</summary>
        public float MiningSeconds { get; private set; }

        public DepositSizeClass SizeClass => SizeClassFor(RadiusUnits);

        /// <summary>
        /// How many harvesters can mine at the same time (GDD: large asteroids take several ships).
        /// Gas clouds are skimmed along their edge and take more.
        /// </summary>
        public int MiningSlotCount => Shape == DepositShape.GasCloud
            ? Mathf.Min(MaximumGasCloudSlots, (int)SizeClass + 2)
            : (int)SizeClass + 1;

        /// <summary>Outer radius including the mining slots; nothing else should be placed inside it.</summary>
        public float FootprintRadiusUnits => RadiusUnits + SlotClearanceUnits;

        /// <summary>Scanning ships stop just outside the rock and its mining slots.</summary>
        public float ScanStandoffUnits => FootprintRadiusUnits + ScanStandoffMarginUnits;

        public float ScanDurationFactor => 1f;

        public static DepositSizeClass SizeClassFor(float radiusUnits)
        {
            if (radiusUnits >= LargeRadiusUnits)
            {
                return DepositSizeClass.Large;
            }
            return radiusUnits >= MediumRadiusUnits ? DepositSizeClass.Medium : DepositSizeClass.Small;
        }

        /// <summary>
        /// Position of a mining slot relative to the zone center, evenly spaced around the deposit.
        /// Slots lie on the zone plane, where ships fly, even if the deposit floats above or below it.
        /// </summary>
        public Vector3 GetMiningSlotLocalPosition(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MiningSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Deposit has {MiningSlotCount} mining slots.");
            }

            float angle = (SlotAngleOffsetDegrees + slotIndex * 360f / MiningSlotCount) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 slot = LocalPosition + direction * FootprintRadiusUnits;
            slot.y = 0f;
            return slot;
        }

        public void Scan()
        {
            IsScanned = true;
        }

        /// <summary>A deposit scan is exact, so the roll does not matter.</summary>
        public void CompleteScan(float accuracyRoll)
        {
            Scan();
        }

        /// <summary>The hazard is used up, e.g. the gas pockets have gone off.</summary>
        public void ClearHazard()
        {
            Hazard = DepositHazard.None;
        }

        /// <summary>A brittle rock breaks apart: half of what is left drifts off as debris.</summary>
        public void Shatter()
        {
            Extract(RemainingAmount * 0.5f);
            ClearHazard();
        }

        public void RecordMining(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), "Mining time cannot be negative.");
            }
            MiningSeconds += seconds;
        }

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

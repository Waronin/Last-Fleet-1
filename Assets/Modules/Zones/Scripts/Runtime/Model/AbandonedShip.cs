using System;
using LastFleet.Shared.Randomness;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>The small ship classes that can be found abandoned; the same classes the fleet flies.</summary>
    public enum AbandonedShipClass
    {
        Shuttle,
        Harvester,
        Fighter
    }

    /// <summary>
    /// A small ship adrift in a zone without anyone aboard (GDD: new small ships are only found, never built).
    /// It is damaged and nearly dry; once a crew member of the fleet climbs aboard, it joins the fleet. The record
    /// is run state, so a ship left in a system is still there on a return, as damaged as it was.
    /// </summary>
    public sealed class AbandonedShip
    {
        public const float MinimumFoundHullFraction = 0.3f;
        public const float MaximumFoundHullFraction = 0.8f;
        public const float MinimumFoundFuelFraction = 0.05f;
        public const float MaximumFoundFuelFraction = 0.2f;

        static readonly (AbandonedShipClass Value, float Weight)[] ClassOdds =
        {
            (AbandonedShipClass.Shuttle, 0.4f), (AbandonedShipClass.Harvester, 0.35f), (AbandonedShipClass.Fighter, 0.25f)
        };

        public AbandonedShip(AbandonedShipClass shipClass, Vector3 localPosition, float headingDegrees, float hullFraction, float fuelFraction)
        {
            if (hullFraction <= 0f || hullFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(hullFraction), "An abandoned ship has some hull left and at most a whole one.");
            }
            if (fuelFraction < 0f || fuelFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(fuelFraction), "Fuel is a share of the tank.");
            }
            Class = shipClass;
            LocalPosition = localPosition;
            HeadingDegrees = headingDegrees;
            HullFraction = hullFraction;
            FuelFraction = fuelFraction;
        }

        public AbandonedShipClass Class { get; }
        public Vector3 LocalPosition { get; }
        public float HeadingDegrees { get; }

        /// <summary>Share of the class's full hull; hits while it waits lower it.</summary>
        public float HullFraction { get; private set; }

        public float FuelFraction { get; }

        /// <summary>A crew member of the fleet climbed aboard; it flies with the fleet now.</summary>
        public bool IsRecovered { get; private set; }

        /// <summary>Shot to pieces before anyone came for it.</summary>
        public bool IsDestroyed { get; private set; }

        public bool IsGone => IsRecovered || IsDestroyed;

        /// <summary>The technical class name of the fleet ship it is, e.g. "Shuttle".</summary>
        public string ShipClassName => Class.ToString();

        /// <summary>Rolls a found ship: 40 % shuttles, 35 % harvesters, 25 % fighters, 30–80 % hull, 5–20 % fuel.</summary>
        public static AbandonedShip Roll(DeterministicRandom random, Vector3 localPosition)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }
            AbandonedShipClass shipClass = random.PickWeighted(ClassOdds);
            float hull = random.Range(MinimumFoundHullFraction, MaximumFoundHullFraction);
            float fuel = random.Range(MinimumFoundFuelFraction, MaximumFoundFuelFraction);
            return new AbandonedShip(shipClass, localPosition, random.AngleDegrees(), hull, fuel);
        }

        public void SetHullFraction(float fraction)
        {
            HullFraction = Mathf.Clamp01(fraction);
        }

        public void MarkRecovered()
        {
            if (IsDestroyed)
            {
                throw new InvalidOperationException("A destroyed ship cannot be recovered.");
            }
            IsRecovered = true;
        }

        public void MarkDestroyed()
        {
            if (!IsRecovered)
            {
                IsDestroyed = true;
            }
        }
    }
}

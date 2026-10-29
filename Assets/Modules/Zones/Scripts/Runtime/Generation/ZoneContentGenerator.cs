using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using LastFleet.Space;
using LastFleet.StarSystem;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>
    /// Generates the contents of a zone from its point of interest when the zone is revealed
    /// (GDD: Zones, Asteroids and belts, Stations). A zone at a fuel source always contains at least
    /// one gas deposit. All counts and sizes are balancing starting points.
    /// </summary>
    public static class ZoneContentGenerator
    {
        public const float ZoneRadiusKlicks = 50f;

        const float PlacementRadiusFraction = 0.85f;
        const float ArrivalClearanceUnits = 60f;
        const float MinimumGapUnits = 2f;
        const float VerticalSpreadUnits = 8f;
        const int PlacementAttemptsPerObject = 40;
        const float AsteroidAmountPerCubicUnit = 100f;
        const float GasAmountPerSquareUnit = 50f;

        static readonly (ResourceKind Value, float Weight)[] OreOnly = { (ResourceKind.Ore, 1f) };
        static readonly (ResourceKind Value, float Weight)[] IceOnly = { (ResourceKind.Ice, 1f) };
        static readonly (ResourceKind Value, float Weight)[] GasOnly = { (ResourceKind.Gas, 1f) };
        static readonly (ResourceKind Value, float Weight)[] MetallicBelt = { (ResourceKind.Ore, 0.85f), (ResourceKind.Ice, 0.15f) };
        static readonly (ResourceKind Value, float Weight)[] IcyBelt = { (ResourceKind.Ice, 0.8f), (ResourceKind.Ore, 0.2f) };
        static readonly (ResourceKind Value, float Weight)[] GasRichBelt = { (ResourceKind.Gas, 0.6f), (ResourceKind.Ice, 0.25f), (ResourceKind.Ore, 0.15f) };
        static readonly (ResourceKind Value, float Weight)[] MixedBelt = { (ResourceKind.Ore, 0.5f), (ResourceKind.Ice, 0.35f), (ResourceKind.Gas, 0.15f) };

        // About one asteroid in four is dangerous to mine (GDD: brittle, radioactive, gas pockets).
        static readonly (DepositHazard Value, float Weight)[] AsteroidHazards =
        {
            (DepositHazard.None, 0.75f), (DepositHazard.Brittle, 0.1f), (DepositHazard.Radioactive, 0.08f), (DepositHazard.GasPockets, 0.07f)
        };

        static readonly DepositRecipe GasCloud = new DepositRecipe(DepositShape.GasCloud, GasOnly, 6f, 14f);
        static readonly DepositRecipe GasAsteroid = new DepositRecipe(DepositShape.Asteroid, GasOnly, 1.5f, 4f);
        static readonly DepositRecipe OreDebris = new DepositRecipe(DepositShape.Asteroid, OreOnly, 0.5f, 2.5f);
        static readonly DepositRecipe IceDebris = new DepositRecipe(DepositShape.Asteroid, IceOnly, 0.6f, 3f);
        static readonly DepositRecipe StationDebris = new DepositRecipe(DepositShape.Asteroid, OreOnly, 0.4f, 1.5f);

        public static ZoneContents Generate(ulong seed, PointOfInterest pointOfInterest, Star star)
        {
            if (pointOfInterest == null)
            {
                throw new ArgumentNullException(nameof(pointOfInterest));
            }
            if (star == null)
            {
                throw new ArgumentNullException(nameof(star));
            }

            DeterministicRandom random = new DeterministicRandom(seed);
            float usableRadiusUnits = SpaceUnits.KlicksToUnits(ZoneRadiusKlicks) * PlacementRadiusFraction;
            ZonePlacement placement = new ZonePlacement(random, usableRadiusUnits, ArrivalClearanceUnits,
                MinimumGapUnits, VerticalSpreadUnits, PlacementAttemptsPerObject);
            List<ResourceDeposit> deposits = new List<ResourceDeposit>();
            List<ZoneStation> stations = new List<ZoneStation>();

            // The guaranteed fuel deposit is placed first, into the still empty zone, so it always fits.
            if (pointOfInterest.IsFuelSource)
            {
                DepositRecipe fuelRecipe = pointOfInterest.Kind == PointOfInterestKind.Planet ? GasCloud : GasAsteroid;
                AddDeposits(random, placement, deposits, 1, fuelRecipe);
            }

            switch (pointOfInterest.Kind)
            {
                case PointOfInterestKind.Planet:
                    AddPlanetDeposits(random, placement, deposits, pointOfInterest.AssociatedPlanet.Kind);
                    break;
                case PointOfInterestKind.Moon:
                    DepositRecipe moonRecipe = IsIcy(pointOfInterest.AssociatedPlanet.Kind) ? IceDebris : OreDebris;
                    AddDeposits(random, placement, deposits, random.Range(4, 11), moonRecipe);
                    break;
                case PointOfInterestKind.AsteroidBelt:
                    DepositRecipe beltRecipe = new DepositRecipe(DepositShape.Asteroid, BeltResources(pointOfInterest.Belt.Composition), 0.6f, 4f);
                    AddDeposits(random, placement, deposits, random.Range(30, 61), beltRecipe);
                    break;
                case PointOfInterestKind.Station:
                    // The station is placed first, so its large footprint always fits; debris drifts around it.
                    AddStation(random, placement, stations, pointOfInterest.Name, pointOfInterest.AssociatedPlanet != null);
                    AddDeposits(random, placement, deposits, random.Range(3, 9), StationDebris);
                    break;
                case PointOfInterestKind.Star:
                    break;
            }

            return new ZoneContents(ZoneRadiusKlicks, CreateBackdrop(pointOfInterest, star), deposits, stations);
        }

        static void AddPlanetDeposits(DeterministicRandom random, ZonePlacement placement, List<ResourceDeposit> deposits, PlanetKind kind)
        {
            switch (kind)
            {
                case PlanetKind.GasGiant:
                    AddDeposits(random, placement, deposits, random.Range(6, 13), GasCloud);
                    break;
                case PlanetKind.IceGiant:
                case PlanetKind.Ice:
                    AddDeposits(random, placement, deposits, random.Range(6, 13), IceDebris);
                    break;
                default:
                    AddDeposits(random, placement, deposits, random.Range(4, 11), OreDebris);
                    break;
            }
        }

        static void AddDeposits(DeterministicRandom random, ZonePlacement placement, List<ResourceDeposit> deposits, int count, DepositRecipe recipe)
        {
            for (int i = 0; i < count; i++)
            {
                float radius = random.Range(recipe.MinimumRadiusUnits, recipe.MaximumRadiusUnits);
                ResourceKind resource = random.PickWeighted(recipe.Resources);
                DepositHazard hazard = recipe.Shape == DepositShape.Asteroid ? random.PickWeighted(AsteroidHazards) : DepositHazard.None;
                float slotAngleOffset = random.AngleDegrees();

                // The footprint includes the mining slots, so no slot ends up inside a neighbour.
                if (!placement.TryPlace(radius + ResourceDeposit.SlotClearanceUnits, out Vector3 position))
                {
                    continue;
                }

                float amount = recipe.Shape == DepositShape.GasCloud
                    ? radius * radius * GasAmountPerSquareUnit
                    : radius * radius * radius * AsteroidAmountPerCubicUnit;
                deposits.Add(new ResourceDeposit(recipe.Shape, resource, position, radius, Mathf.Round(amount), hazard, slotAngleOffset));
            }
        }

        static void AddStation(DeterministicRandom random, ZonePlacement placement, List<ZoneStation> stations, string name, bool isOrbital)
        {
            StationType type = StationGeneration.PickType(random, isOrbital);
            StationProfile profile = StationGeneration.RollProfile(random, type);
            float headingDegrees = random.AngleDegrees();
            if (placement.TryPlace(StationTypes.FootprintRadiusUnits(type), out Vector3 position))
            {
                // On the zone plane, where ships fly, so the docking bays can be reached.
                position.y = 0f;
                stations.Add(new ZoneStation(name, type, position, headingDegrees, profile));
            }
        }

        static ZoneBackdrop CreateBackdrop(PointOfInterest pointOfInterest, Star star)
        {
            return pointOfInterest.Kind switch
            {
                PointOfInterestKind.Star => new ZoneBackdrop(ZoneBackdropKind.Star, star.Color),
                PointOfInterestKind.Planet => new ZoneBackdrop(ZoneBackdropKind.Planet, CelestialPalette.PlanetColor(pointOfInterest.AssociatedPlanet.Kind)),
                PointOfInterestKind.Moon => new ZoneBackdrop(ZoneBackdropKind.Moon, CelestialPalette.MoonColor),
                PointOfInterestKind.Station when pointOfInterest.AssociatedPlanet != null =>
                    new ZoneBackdrop(ZoneBackdropKind.Planet, CelestialPalette.PlanetColor(pointOfInterest.AssociatedPlanet.Kind)),
                _ => new ZoneBackdrop(ZoneBackdropKind.None, Color.black)
            };
        }

        static (ResourceKind Value, float Weight)[] BeltResources(AsteroidBeltComposition composition)
        {
            return composition switch
            {
                AsteroidBeltComposition.Metallic => MetallicBelt,
                AsteroidBeltComposition.Icy => IcyBelt,
                AsteroidBeltComposition.GasRich => GasRichBelt,
                _ => MixedBelt
            };
        }

        static bool IsIcy(PlanetKind kind)
        {
            return kind == PlanetKind.Ice || kind == PlanetKind.IceGiant;
        }

        readonly struct DepositRecipe
        {
            public DepositRecipe(DepositShape shape, (ResourceKind Value, float Weight)[] resources, float minimumRadiusUnits, float maximumRadiusUnits)
            {
                Shape = shape;
                Resources = resources;
                MinimumRadiusUnits = minimumRadiusUnits;
                MaximumRadiusUnits = maximumRadiusUnits;
            }

            public DepositShape Shape { get; }
            public (ResourceKind Value, float Weight)[] Resources { get; }
            public float MinimumRadiusUnits { get; }
            public float MaximumRadiusUnits { get; }
        }
    }
}

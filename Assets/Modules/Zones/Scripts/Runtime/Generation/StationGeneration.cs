using System;
using LastFleet.Shared.Randomness;

namespace LastFleet.Zones
{
    /// <summary>
    /// Rolls a station's type and hidden profile. Orbital stations lean towards habitats and
    /// industry, deep-space stations towards outposts and research. The type shifts the odds of the
    /// profile: habitats are mostly inhabited, industrial stations often abandoned and harsher.
    /// All weights are balancing starting points.
    /// </summary>
    public static class StationGeneration
    {
        static readonly (StationType Value, float Weight)[] OrbitalTypes =
        {
            (StationType.Habitat, 0.35f), (StationType.Industrial, 0.3f), (StationType.Outpost, 0.2f), (StationType.Research, 0.15f)
        };

        static readonly (StationType Value, float Weight)[] DeepSpaceTypes =
        {
            (StationType.Outpost, 0.4f), (StationType.Research, 0.35f), (StationType.Industrial, 0.15f), (StationType.Habitat, 0.1f)
        };

        static readonly (StationAttitude Value, float Weight)[] Attitudes =
        {
            (StationAttitude.Friendly, 0.45f), (StationAttitude.Wary, 0.35f), (StationAttitude.Hostile, 0.2f)
        };

        // The people of a station that breaks its radio silence want to be found.
        static readonly (StationAttitude Value, float Weight)[] PeacefulAttitudes =
        {
            (StationAttitude.Friendly, 0.55f), (StationAttitude.Wary, 0.45f)
        };

        // Survivors of a stranded ship hope for rescue: never hostile, though some are wary of strangers.
        static readonly (StationAttitude Value, float Weight)[] StrandedAttitudes =
        {
            (StationAttitude.Friendly, 0.6f), (StationAttitude.Wary, 0.4f)
        };

        /// <summary>One of the built station types; stranded ships and derelicts are generated on their own (<see cref="RollStrandedHull"/>).</summary>
        public static StationType PickType(DeterministicRandom random, bool isOrbital)
        {
            return random.PickWeighted(isOrbital ? OrbitalTypes : DeepSpaceTypes);
        }

        // A late signal is worth more (GDD: late signals): a stranded ship that sent a distress call is in better shape.
        public const float LateSignalMinimumHullFraction = 0.4f;
        public const float LateSignalMaximumHullFraction = 0.75f;

        /// <summary>The class of a stranded ship and how much of its hull is left (20–50 %).</summary>
        public static StrandedHull RollStrandedHull(DeterministicRandom random)
        {
            return RollStrandedHull(random, false);
        }

        /// <summary>As <see cref="RollStrandedHull(DeterministicRandom)"/>; a ship found by its late signal keeps 40–75 % of its hull.</summary>
        public static StrandedHull RollStrandedHull(DeterministicRandom random, bool isLateSignal)
        {
            StrandedShipClass shipClass = StrandedShipClasses.Pick(random);
            float fraction = isLateSignal
                ? random.Range(LateSignalMinimumHullFraction, LateSignalMaximumHullFraction)
                : random.Range(StrandedHull.MinimumFoundFraction, StrandedHull.MaximumFoundFraction);
            return new StrandedHull(shipClass, fraction);
        }

        /// <summary>A toxic or radioactive station keeps only this fraction of the usual occupants alive.</summary>
        public const int HazardousOccupantDivisor = 10;

        public static StationProfile RollProfile(DeterministicRandom random, StationType type)
        {
            return RollProfile(random, type, false);
        }

        /// <summary>
        /// As <see cref="RollProfile(DeterministicRandom, StationType)"/>; a place found by its late signal is worth more:
        /// never ruined and never hostile, and a stranded ship that sent a distress call always has survivors aboard.
        /// The rolls stay the same, so the rest of the zone does not change.
        /// </summary>
        public static StationProfile RollProfile(DeterministicRandom random, StationType type, bool isLateSignal)
        {
            TypeOdds odds = isLateSignal ? OddsFor(type).ForLateSignal(type) : OddsFor(type);
            StationAtmosphere atmosphere = random.PickWeighted(odds.Atmospheres);
            StationCondition condition = random.PickWeighted(odds.Conditions);
            StationRadiation radiation = random.Chance(odds.ElevatedRadiationChance) ? StationRadiation.Elevated : StationRadiation.None;

            if (!random.Chance(odds.InhabitedChance))
            {
                return new StationProfile(atmosphere, 0, StationAttitude.None, condition, radiation);
            }
            int occupants = random.Range(odds.MinimumOccupants, odds.MaximumOccupants + 1);
            if (atmosphere == StationAtmosphere.Toxic || radiation == StationRadiation.Elevated)
            {
                // Few hold out in poison or radiation; the roll stays, so the stations after it do not change.
                occupants = Math.Max(1, occupants / HazardousOccupantDivisor);
            }
            StationAttitude attitude = random.PickWeighted(type == StationType.StrandedShip ? StrandedAttitudes : isLateSignal ? PeacefulAttitudes : Attitudes);
            return new StationProfile(atmosphere, occupants, attitude, condition, radiation);
        }

        static TypeOdds OddsFor(StationType type)
        {
            return type switch
            {
                StationType.Outpost => new TypeOdds(0.55f, 10, 80, 0.6f, 0.3f, 0.35f, 0.45f, 0.15f),
                StationType.Research => new TypeOdds(0.5f, 15, 120, 0.6f, 0.25f, 0.4f, 0.4f, 0.35f),
                StationType.Industrial => new TypeOdds(0.3f, 30, 300, 0.35f, 0.35f, 0.25f, 0.45f, 0.25f),
                // A stranded ship is never intact: its hull is what needs repairing.
                StationType.StrandedShip => new TypeOdds(0.6f, 20, 400, 0.5f, 0.3f, 0f, 0.6f, 0.2f),
                // A dead hull, always damaged (a ruin would hold fewer finds); some leak poison or radiation and stay closed.
                StationType.Derelict => new TypeOdds(0f, 0, 0, 0.35f, 0.5f, 0f, 1f, 0.15f),
                _ => new TypeOdds(0.85f, 800, 3000, 0.8f, 0.15f, 0.4f, 0.45f, 0.1f)
            };
        }

        readonly struct TypeOdds
        {
            readonly float _breathable;
            readonly float _thin;
            readonly float _intact;
            readonly float _damaged;

            public TypeOdds(float inhabitedChance, int minimumOccupants, int maximumOccupants, float breathable, float thin,
                float intact, float damaged, float elevatedRadiationChance)
            {
                _breathable = breathable;
                _thin = thin;
                _intact = intact;
                _damaged = damaged;
                InhabitedChance = inhabitedChance;
                MinimumOccupants = minimumOccupants;
                MaximumOccupants = maximumOccupants;
                Atmospheres = new (StationAtmosphere, float)[]
                {
                    (StationAtmosphere.Breathable, breathable), (StationAtmosphere.Thin, thin), (StationAtmosphere.Toxic, 1f - breathable - thin)
                };
                Conditions = new (StationCondition, float)[]
                {
                    (StationCondition.Intact, intact), (StationCondition.Damaged, damaged), (StationCondition.Ruined, 1f - intact - damaged)
                };
                ElevatedRadiationChance = elevatedRadiationChance;
            }

            public float InhabitedChance { get; }
            public int MinimumOccupants { get; }
            public int MaximumOccupants { get; }
            public (StationAtmosphere Value, float Weight)[] Atmospheres { get; }
            public (StationCondition Value, float Weight)[] Conditions { get; }
            public float ElevatedRadiationChance { get; }

            /// <summary>Never ruined; a stranded ship that sent a distress call always has survivors.</summary>
            public TypeOdds ForLateSignal(StationType type)
            {
                float inhabitedChance = type == StationType.StrandedShip ? 1f : InhabitedChance;
                float intact = _intact / (_intact + _damaged);
                return new TypeOdds(inhabitedChance, MinimumOccupants, MaximumOccupants, _breathable, _thin, intact, 1f - intact,
                    ElevatedRadiationChance);
            }
        }
    }
}

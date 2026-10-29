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

        public static StationType PickType(DeterministicRandom random, bool isOrbital)
        {
            return random.PickWeighted(isOrbital ? OrbitalTypes : DeepSpaceTypes);
        }

        public static StationProfile RollProfile(DeterministicRandom random, StationType type)
        {
            TypeOdds odds = OddsFor(type);
            StationAtmosphere atmosphere = random.PickWeighted(odds.Atmospheres);
            StationCondition condition = random.PickWeighted(odds.Conditions);
            StationRadiation radiation = random.Chance(odds.ElevatedRadiationChance) ? StationRadiation.Elevated : StationRadiation.None;

            if (!random.Chance(odds.InhabitedChance))
            {
                return new StationProfile(atmosphere, 0, StationAttitude.None, condition, radiation);
            }
            int occupants = random.Range(odds.MinimumOccupants, odds.MaximumOccupants + 1);
            return new StationProfile(atmosphere, occupants, random.PickWeighted(Attitudes), condition, radiation);
        }

        static TypeOdds OddsFor(StationType type)
        {
            return type switch
            {
                StationType.Outpost => new TypeOdds(0.55f, 10, 80, 0.6f, 0.3f, 0.35f, 0.45f, 0.15f),
                StationType.Research => new TypeOdds(0.5f, 15, 120, 0.6f, 0.25f, 0.4f, 0.4f, 0.35f),
                StationType.Industrial => new TypeOdds(0.3f, 30, 300, 0.35f, 0.35f, 0.25f, 0.45f, 0.25f),
                _ => new TypeOdds(0.85f, 800, 3000, 0.8f, 0.15f, 0.4f, 0.45f, 0.1f)
            };
        }

        readonly struct TypeOdds
        {
            public TypeOdds(float inhabitedChance, int minimumOccupants, int maximumOccupants, float breathable, float thin,
                float intact, float damaged, float elevatedRadiationChance)
            {
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
        }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Rewards;
using LastFleet.Shared.Randomness;
using LastFleet.Zones;

namespace LastFleet.Stations
{
    public enum SearchBlocker
    {
        None,

        /// <summary>People live aboard; they decide what happens (GDD: quests or hostility).</summary>
        Inhabited,

        /// <summary>The air would kill the search party.</summary>
        ToxicAtmosphere,

        /// <summary>The radiation aboard would kill the search party.</summary>
        Radiation
    }

    /// <summary>
    /// Which stations can be searched and what they hold (GDD: a walkable, unoccupied station can be
    /// searched; the longer the search, the more turns up). Walkable means breathable or thin air and no
    /// elevated radiation: both hostile environments would kill the search party. The
    /// finds are fixed per station by the zone seed: the type sets how many and of what kind, a ruin
    /// holds fewer. Counts and amounts are balancing starting points.
    /// </summary>
    public static class StationSearchRules
    {
        /// <summary>Time for one find with a full crew.</summary>
        public const float SecondsPerFind = 4f;

        public static SearchBlocker Blocker(StationProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            if (profile.IsInhabited)
            {
                return SearchBlocker.Inhabited;
            }
            if (profile.Atmosphere == StationAtmosphere.Toxic)
            {
                return SearchBlocker.ToxicAtmosphere;
            }
            return profile.Radiation == StationRadiation.Elevated ? SearchBlocker.Radiation : SearchBlocker.None;
        }

        public static List<Reward> RollFinds(ulong seed, StationType type, StationCondition condition)
        {
            DeterministicRandom random = new DeterministicRandom(seed);
            (int minimum, int maximum) = FindCount(type);
            int count = random.Range(minimum, maximum + 1);
            if (condition == StationCondition.Ruined)
            {
                count = Math.Max(1, (int)Math.Round(count * 0.6f));
            }

            (FindKind Value, float Weight)[] weights = Weights(type);
            List<Reward> finds = new List<Reward>(count);
            for (int i = 0; i < count; i++)
            {
                finds.Add(Roll(random, random.PickWeighted(weights)));
            }
            return finds;
        }

        enum FindKind
        {
            Resource,
            Fuel,
            Survivors,
            UpgradeModule,
            Clue,
            ArchiveRecord
        }

        static readonly (ResourceKind Value, float Weight)[] Resources =
        {
            (ResourceKind.Ore, 0.5f), (ResourceKind.Ice, 0.3f), (ResourceKind.Gas, 0.2f)
        };

        static Reward Roll(DeterministicRandom random, FindKind kind)
        {
            return kind switch
            {
                FindKind.Resource => Reward.OfResource(random.PickWeighted(Resources), Round(random.Range(50f, 200f))),
                FindKind.Fuel => Reward.OfFuel(Round(random.Range(40f, 150f))),
                FindKind.Survivors => Reward.OfSurvivors(random.Range(3, 16)),
                FindKind.UpgradeModule => Reward.OfItem(ItemKind.UpgradeModule),
                FindKind.Clue => Reward.OfItem(ItemKind.Clue),
                _ => Reward.OfItem(ItemKind.ArchiveRecord)
            };
        }

        static (int Minimum, int Maximum) FindCount(StationType type)
        {
            return type switch
            {
                StationType.Outpost => (3, 5),
                StationType.Research => (4, 7),
                StationType.Industrial => (6, 10),
                StationType.Habitat => (6, 10),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        static (FindKind Value, float Weight)[] Weights(StationType type)
        {
            return type switch
            {
                StationType.Outpost => new[]
                {
                    (FindKind.Resource, 0.4f), (FindKind.Fuel, 0.25f), (FindKind.Survivors, 0.15f),
                    (FindKind.UpgradeModule, 0.1f), (FindKind.ArchiveRecord, 0.1f)
                },
                StationType.Research => new[]
                {
                    (FindKind.Resource, 0.2f), (FindKind.Fuel, 0.1f), (FindKind.Survivors, 0.15f),
                    (FindKind.UpgradeModule, 0.1f), (FindKind.Clue, 0.15f), (FindKind.ArchiveRecord, 0.3f)
                },
                StationType.Industrial => new[]
                {
                    (FindKind.Resource, 0.45f), (FindKind.Fuel, 0.25f), (FindKind.Survivors, 0.05f),
                    (FindKind.UpgradeModule, 0.2f), (FindKind.ArchiveRecord, 0.05f)
                },
                _ => new[]
                {
                    (FindKind.Resource, 0.2f), (FindKind.Fuel, 0.15f), (FindKind.Survivors, 0.35f),
                    (FindKind.UpgradeModule, 0.1f), (FindKind.Clue, 0.05f), (FindKind.ArchiveRecord, 0.15f)
                }
            };
        }

        static float Round(float amount)
        {
            return (float)Math.Round(amount / 5f) * 5f;
        }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using LastFleet.Space;
using LastFleet.StarSystem;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// Where a quest station lies: a zone of the step's system, chosen when the fleet first gets there,
    /// and a free spot in it. The station joins the zone's contents when the zone is revealed (at once
    /// if it already is), so it is run state like any generated station and survives leaving the system.
    /// </summary>
    public sealed class QuestStationSite : IZoneAmendment
    {
        readonly QuestStationSpec _spec;
        readonly ulong _spotSeed;
        readonly float _headingDegrees;
        readonly Action<ZoneStation> _placed;

        QuestStationSite(QuestStationSpec spec, int pointOfInterestIndex, ulong spotSeed, float headingDegrees, Action<ZoneStation> placed)
        {
            _spec = spec;
            PointOfInterestIndex = pointOfInterestIndex;
            _spotSeed = spotSeed;
            _headingDegrees = headingDegrees;
            _placed = placed;
        }

        public int PointOfInterestIndex { get; }

        /// <summary>Null until the zone is revealed.</summary>
        public ZoneStation Station { get; private set; }

        /// <summary>
        /// Picks a zone for the station and adds it there; <paramref name="placed"/> runs once the station
        /// exists, which may be right away. A zone view already built shows the station at once.
        /// </summary>
        public static QuestStationSite Place(SystemZones system, QuestStationSpec spec, DeterministicRandom random, ZoneWorld zoneWorld,
            Action<ZoneStation> placed)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            int zoneIndex = ChooseZone(system, random);
            QuestStationSite site = new QuestStationSite(spec, zoneIndex, random.NextULong(), random.AngleDegrees(), placed);
            system.Zones[zoneIndex].Amend(site);
            if (site.Station != null && zoneWorld != null && zoneWorld.CurrentSystem == system && zoneWorld.TryGetView(zoneIndex, out ZoneView view))
            {
                view.ShowStation(site.Station);
            }
            return site;
        }

        /// <summary>
        /// A zone away from the jump point and the star, unexplored ones first so the player finds the
        /// station rather than seeing it appear, and station zones before other kinds.
        /// </summary>
        public static int ChooseZone(SystemZones system, DeterministicRandom random)
        {
            List<int> best = new List<int>();
            int bestRank = int.MaxValue;
            for (int i = 0; i < system.Zones.Count; i++)
            {
                ZoneRecord zone = system.Zones[i];
                PointOfInterestKind kind = zone.PointOfInterest.Kind;
                if (kind == PointOfInterestKind.JumpPoint || kind == PointOfInterestKind.Star)
                {
                    continue;
                }

                int rank = (zone.IsRevealed ? 2 : 0) + (kind == PointOfInterestKind.Station ? 0 : 1);
                if (rank < bestRank)
                {
                    bestRank = rank;
                    best.Clear();
                }
                if (rank == bestRank)
                {
                    best.Add(i);
                }
            }

            if (best.Count == 0)
            {
                // Only jump point and star: anything but the arrival zone, where the fleet would see it appear.
                for (int i = 0; i < system.Zones.Count; i++)
                {
                    if (i != system.Layout.ArrivalPointOfInterestIndex)
                    {
                        return i;
                    }
                }
                return system.Layout.ArrivalPointOfInterestIndex;
            }
            return best[random.Range(0, best.Count)];
        }

        void IZoneAmendment.Apply(ZoneRecord zone, ZoneContents contents)
        {
            float radius = StationTypes.FootprintRadiusUnits(_spec.Type);
            if (!ZoneContentGenerator.TryFindFreeSpot(contents, radius, _spotSeed, out Vector3 position))
            {
                // A full zone is no reason to lose the quest: the station drifts in at the zone's edge.
                Debug.LogWarning($"No free spot for quest station {_spec.Name} in {zone.PointOfInterest.Name}; placing it at the edge.");
                position = new Vector3(0f, 0f, -(SpaceUnits.KlicksToUnits(contents.RadiusKlicks) - radius));
            }

            Station = new ZoneStation(_spec.Name, _spec.Type, position, _headingDegrees, _spec.Profile);
            contents.AddStation(Station);
            _placed?.Invoke(Station);
        }
    }
}

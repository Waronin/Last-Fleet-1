using System;
using LastFleet.Space;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// A quest deposit, such as a guarded rock, placed right next to a quest station so the player can
    /// tell it from the zone's other deposits. It joins the zone's contents together with the station
    /// (amend the zone with the station's site first), so it is ordinary run state.
    /// </summary>
    public sealed class QuestDepositSite : IZoneAmendment
    {
        const int CandidateDirections = 12;

        /// <summary>Open space between the station's footprint and the rock's mining slots.</summary>
        const float GapUnits = 6f;

        readonly QuestStationSite _stationSite;
        readonly ResourceKind _resource;
        readonly float _radiusUnits;
        readonly float _amount;
        readonly float _startAngleDegrees;
        readonly Action<ResourceDeposit> _placed;

        QuestDepositSite(QuestStationSite stationSite, ResourceKind resource, float radiusUnits, float amount, float startAngleDegrees,
            Action<ResourceDeposit> placed)
        {
            _stationSite = stationSite;
            _resource = resource;
            _radiusUnits = radiusUnits;
            _amount = amount;
            _startAngleDegrees = startAngleDegrees;
            _placed = placed;
        }

        /// <summary>Null until the zone is revealed.</summary>
        public ResourceDeposit Deposit { get; private set; }

        /// <summary>
        /// Adds an asteroid next to the station of the site, in the same zone; <paramref name="placed"/> runs
        /// once it exists. A zone view already built shows it at once.
        /// </summary>
        public static QuestDepositSite PlaceNextTo(QuestStationSite stationSite, SystemZones system, ResourceKind resource, float radiusUnits,
            float amount, float startAngleDegrees, ZoneWorld zoneWorld, Action<ResourceDeposit> placed)
        {
            if (stationSite == null)
            {
                throw new ArgumentNullException(nameof(stationSite));
            }
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            QuestDepositSite site = new QuestDepositSite(stationSite, resource, radiusUnits, amount, startAngleDegrees, placed);
            system.Zones[stationSite.PointOfInterestIndex].Amend(site);
            if (site.Deposit != null && zoneWorld != null && zoneWorld.CurrentSystem == system &&
                zoneWorld.TryGetView(stationSite.PointOfInterestIndex, out ZoneView view))
            {
                view.ShowDeposit(site.Deposit);
            }
            return site;
        }

        void IZoneAmendment.Apply(ZoneRecord zone, ZoneContents contents)
        {
            ZoneStation station = _stationSite.Station;
            if (station == null)
            {
                throw new InvalidOperationException("A quest deposit needs its station placed first.");
            }

            Deposit = new ResourceDeposit(DepositShape.Asteroid, _resource, FindSpot(contents, station), _radiusUnits, _amount,
                DepositHazard.None, _startAngleDegrees);
            contents.AddDeposit(Deposit);
            _placed?.Invoke(Deposit);
        }

        /// <summary>The first direction around the station where the rock overlaps nothing, else the first one.</summary>
        Vector3 FindSpot(ZoneContents contents, ZoneStation station)
        {
            float footprint = _radiusUnits + ResourceDeposit.SlotClearanceUnits;
            float distance = station.FootprintRadiusUnits + GapUnits + footprint;
            Vector3 first = Vector3.zero;
            for (int i = 0; i < CandidateDirections; i++)
            {
                float angle = (_startAngleDegrees + i * 360f / CandidateDirections) * Mathf.Deg2Rad;
                Vector3 candidate = station.LocalPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                if (i == 0)
                {
                    first = candidate;
                }
                if (IsFree(contents, candidate, footprint))
                {
                    return candidate;
                }
            }
            return first;
        }

        static bool IsFree(ZoneContents contents, Vector3 position, float footprint)
        {
            if (new Vector2(position.x, position.z).magnitude + footprint > SpaceUnits.KlicksToUnits(contents.RadiusKlicks))
            {
                return false;
            }
            foreach (ResourceDeposit deposit in contents.Deposits)
            {
                if (PlanarDistance(deposit.LocalPosition, position) < deposit.FootprintRadiusUnits + footprint)
                {
                    return false;
                }
            }
            foreach (ZoneStation station in contents.Stations)
            {
                if (PlanarDistance(station.LocalPosition, position) < station.FootprintRadiusUnits + footprint)
                {
                    return false;
                }
            }
            return true;
        }

        static float PlanarDistance(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }
    }
}

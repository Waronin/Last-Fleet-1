using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.StarSystem;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Exploration
{
    /// <summary>
    /// Sends ships to find things out: a ship to scan a deposit or a station, or a ship to explore an
    /// unknown zone.
    /// It picks a free small ship first, otherwise the nearest one, and never sends a second ship for
    /// the same target. A free ship already in the target zone goes first; from other zones scouts such as
    /// shuttles go before other ships, so harvesters keep mining.
    /// </summary>
    public sealed class ScoutingService : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;

        readonly List<ShipAgent> _candidates = new List<ShipAgent>();
        /// <summary>Larger than any travel cost, so a scout anywhere in the system is picked first.</summary>
        const float NonScoutPenaltyUnits = 10000000f;

        readonly List<ShipAgent> _picked = new List<ShipAgent>(1);

        /// <summary>Short feedback for the player about dispatched and finished tasks.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_registry == null)
            {
                Debug.LogError("ScoutingService needs the ship registry.", this);
                enabled = false;
            }
        }

        /// <summary>The scanner currently assigned to the target, if any.</summary>
        public bool TryGetActiveScan(IScanTarget target, out ShipScanner scanner)
        {
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (ship.TryGetComponent(out scanner) && scanner.State != ScanTaskState.Inactive && scanner.Target == target)
                {
                    return true;
                }
            }
            scanner = null;
            return false;
        }

        public bool RequestScan(ZoneView zone, IScanTarget target)
        {
            return RequestScan(zone, target, _registry.Ships);
        }

        /// <summary>Scans with one of the given ships, e.g. the selection of a context command.</summary>
        public bool RequestScan(ZoneView zone, IScanTarget target, IReadOnlyList<ShipAgent> ships)
        {
            if (target.IsScanned || TryGetActiveScan(target, out ShipScanner _))
            {
                return false;
            }

            _candidates.Clear();
            foreach (ShipAgent ship in ships)
            {
                if (!ship.IsCapital && !ship.IsDerelict && ship.TryGetComponent(out ShipScanner _))
                {
                    _candidates.Add(ship);
                }
            }

            int zoneIndex = zone.Record.PointOfInterestIndex;
            Vector2 zonePositionAU = zone.Record.PointOfInterest.PositionAU;
            ShipAvailability.PickFreeThenNearest(_candidates, ShipAvailability.IsFree,
                ship => ScoutCost(ship, ShipAvailability.TravelCost(ship, zoneIndex, zonePositionAU, target.LocalPosition), zoneIndex), 1, _picked);
            if (_picked.Count == 0)
            {
                Notice?.Invoke(ExplorationTexts.NoShipToScan);
                return false;
            }

            ShipScanner scanner = _picked[0].GetComponent<ShipScanner>();
            scanner.ScanCompleted -= OnScanCompleted;
            scanner.ScanCompleted += OnScanCompleted;
            scanner.BeginScan(zone, target);
            Notice?.Invoke(ExplorationTexts.ScanDispatched.Format(("ship", scanner.Ship.DisplayName)));
            return true;
        }

        /// <summary>Sends a small ship to the center of a zone; arriving there reveals the zone.</summary>
        public bool RequestExploration(int zoneIndex, PointOfInterest pointOfInterest)
        {
            _candidates.Clear();
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (!ship.IsCapital && !ship.IsDerelict)
                {
                    _candidates.Add(ship);
                }
            }

            ShipAvailability.PickFreeThenNearest(_candidates, ShipAvailability.IsFree,
                ship => ScoutCost(ship, ShipAvailability.TravelCost(ship, zoneIndex, pointOfInterest.PositionAU, Vector3.zero), zoneIndex), 1, _picked);
            if (_picked.Count == 0)
            {
                Notice?.Invoke(ExplorationTexts.NoShipToExplore);
                return false;
            }

            _picked[0].Order(ShipOrder.MoveTo(zoneIndex, Vector3.zero), false);
            Notice?.Invoke(ExplorationTexts.ExplorationDispatched.Format(("ship", _picked[0].DisplayName), ("zone", pointOfInterest.Name)));
            return true;
        }

        /// <summary>
        /// Travel cost for a task in a zone: a ship already flying in the target zone is nearest, whatever it
        /// is; from other zones scouts come first, since any non-scout costs more than every scout in the
        /// system, so harvesters elsewhere keep mining.
        /// </summary>
        public static float ScoutCost(ShipAgent ship, float travelCost, int targetZoneIndex)
        {
            bool inTargetZone = !ship.IsDocked && ship.Zone != null && ship.Zone.Record.PointOfInterestIndex == targetZoneIndex;
            if (inTargetZone)
            {
                return travelCost;
            }
            return ship.TryGetComponent(out ShipScanner scanner) && scanner.IsScout ? travelCost : travelCost + NonScoutPenaltyUnits;
        }

        void OnScanCompleted(ShipScanner scanner, IScanTarget target)
        {
            scanner.ScanCompleted -= OnScanCompleted;
            if (target is ZoneStation station)
            {
                Notice?.Invoke(ExplorationTexts.StationScanFinished.Format(("ship", scanner.Ship.DisplayName), ("station", station.Name),
                    ("lifeSigns", LifeSigns(station.ScanReport))));
                return;
            }
            if (!(target is ResourceDeposit deposit))
            {
                return;
            }
            Notice?.Invoke(ExplorationTexts.ScanFinished.Format(("ship", scanner.Ship.DisplayName), ("size", ZoneTexts.SizeClasses.For(deposit.SizeClass)),
                ("shape", ZoneTexts.Shapes.For(deposit.Shape)), ("resource", ZoneTexts.Resources.For(deposit.Resource))));
        }

        /// <summary>The life signs of a scan report: a range, or none at all.</summary>
        public static GameText LifeSigns(StationScanReport report)
        {
            return report.HasLifeSigns
                ? ExplorationTexts.LifeSignsRange.Format(("minimum", report.MinimumLifeSigns), ("maximum", report.MaximumLifeSigns))
                : ExplorationTexts.NoLifeSigns;
        }
    }
}

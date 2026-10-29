using System;
using System.Collections.Generic;
using LastFleet.Exploration;
using LastFleet.Shared.Text;
using LastFleet.Zones;

namespace LastFleet.Inspection
{
    /// <summary>
    /// A station in a zone: its type, size and docking bays are visible from afar. A scan reveals
    /// atmosphere, condition, radiation and a range of life signs; whether the people aboard are
    /// hostile stays unknown until they are hailed.
    /// </summary>
    public sealed class StationInspection : IInspectionTarget
    {
        readonly ZoneView _zone;
        readonly ZoneStation _station;
        readonly ScoutingService _scouting;

        public StationInspection(ZoneView zone, ZoneStation station, ScoutingService scouting)
        {
            _zone = zone;
            _station = station ?? throw new ArgumentNullException(nameof(station));
            _scouting = scouting;
        }

        public ZoneStation Station => _station;
        public GameText Title => GameText.Verbatim(_station.Name);
        public bool IsValid => _zone != null;

        public void Describe(List<GameText> lines)
        {
            lines.Clear();
            lines.Add(InspectionTexts.ZoneLine.Format(("zone", _zone.Record.PointOfInterest.Name)));
            lines.Add(TypeLine(_station));
            lines.Add(InspectionTexts.StationBays.Format(("count", _station.DockingBayCount)));
            DescribeScan(_station.ScanReport, lines);

            if (_scouting.TryGetActiveScan(_station, out ShipScanner scanner))
            {
                lines.Add(scanner.State == ScanTaskState.Scanning
                    ? InspectionTexts.DepositScanning.Format(("ship", scanner.Ship.DisplayName), ("seconds", scanner.RemainingSeconds))
                    : InspectionTexts.DepositScanApproaching.Format(("ship", scanner.Ship.DisplayName)));
            }
        }

        public void CollectActions(List<InspectionAction> actions)
        {
            actions.Clear();
            bool scanActive = _scouting.TryGetActiveScan(_station, out ShipScanner _);
            GameText scanBlocked = _station.IsScanned ? InspectionTexts.AlreadyScanned : scanActive ? InspectionTexts.ScanAlreadyAssigned : null;
            actions.Add(new InspectionAction(InspectionTexts.ScanAction, InspectionTexts.RightClickHint, scanBlocked == null, scanBlocked,
                () => _scouting.RequestScan(_zone, _station) ? InspectionTexts.ScanRequested : ExplorationTexts.NoShipToScan));
        }

        /// <summary>Type and rough size, which are visible from afar.</summary>
        public static GameText TypeLine(ZoneStation station)
        {
            return InspectionTexts.StationTypeLine.Format(("type", ZoneTexts.StationTypeNames.For(station.Type)),
                ("length", StationTypes.NominalLengthKlicks(station.Type)));
        }

        /// <summary>What the scan found; before a scan nothing of the hidden profile shows.</summary>
        public static void DescribeScan(StationScanReport report, List<GameText> lines)
        {
            if (report == null)
            {
                lines.Add(InspectionTexts.StationUnscanned);
                return;
            }

            lines.Add(InspectionTexts.StationAtmosphere.Format(("value", ZoneTexts.Atmospheres.For(report.Atmosphere))));
            lines.Add(InspectionTexts.StationCondition.Format(("value", ZoneTexts.Conditions.For(report.Condition))));
            lines.Add(InspectionTexts.StationRadiation.Format(("value", ZoneTexts.Radiation.For(report.Radiation))));
            lines.Add(InspectionTexts.StationLifeSigns.Format(("lifeSigns", ScoutingService.LifeSigns(report))));
            if (report.HasLifeSigns)
            {
                lines.Add(InspectionTexts.StationAttitudeUnknown);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Exploration;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;

namespace LastFleet.Inspection
{
    /// <summary>
    /// A station in a zone: its type, size and docking ports are visible from afar. A scan reveals
    /// atmosphere, condition, radiation and a range of life signs; whether the people aboard are
    /// hostile stays unknown until they are hailed, which takes a ship of the fleet in the zone. Small
    /// ships can dock at its ports, on their own risk if the station was not scanned, and search it
    /// if it is walkable and nobody lives aboard.
    /// </summary>
    public sealed class StationInspection : IInspectionTarget
    {
        readonly ZoneView _zone;
        readonly ZoneStation _station;
        readonly ScoutingService _scouting;
        readonly ShipRegistry _registry;
        readonly DockingService _docking;
        readonly SearchService _search;
        readonly Func<IReadOnlyList<ShipAgent>> _selectedShips;
        readonly List<ShipAgent> _docked = new List<ShipAgent>();
        readonly List<ShipAgent> _assigned = new List<ShipAgent>();
        readonly List<ShipAgent> _selectedSmallShips = new List<ShipAgent>();

        public StationInspection(ZoneView zone, ZoneStation station, ScoutingService scouting, ShipRegistry registry,
            DockingService docking, SearchService search, Func<IReadOnlyList<ShipAgent>> selectedShips)
        {
            _zone = zone;
            _station = station ?? throw new ArgumentNullException(nameof(station));
            _scouting = scouting;
            _registry = registry;
            _docking = docking;
            _search = search;
            _selectedShips = selectedShips;
        }

        public ZoneStation Station => _station;
        public GameText Title => GameText.Verbatim(_station.Name);
        public bool IsValid => _zone != null;

        public void Describe(List<GameText> lines)
        {
            lines.Clear();
            lines.Add(InspectionTexts.ZoneLine.Format(("zone", _zone.Record.PointOfInterest.Name)));
            lines.Add(TypeLine(_station));
            lines.Add(InspectionTexts.StationPorts.Format(("count", _station.DockingPortCount)));
            DescribeKnowledge(_station, lines);
            DescribeHail(_station, lines);
            DescribeDocking(lines);

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

            GameText hailBlocked = _station.IsHailed ? InspectionTexts.AlreadyHailed : !HasShipInZone() ? InspectionTexts.NoShipInRadioRange : null;
            actions.Add(new InspectionAction(InspectionTexts.HailAction, null, hailBlocked == null, hailBlocked, Hail));

            GameText dockBlocked = _docking.FreePortCount(_station) == 0 ? StationTexts.AllPortsTaken : null;
            actions.Add(new InspectionAction(StationTexts.DockAction, InspectionTexts.RightClickHint, dockBlocked == null, dockBlocked, Dock));

            _search.CollectDockedShips(_station, _docked);
            GameText searchBlocked = _docked.Count == 0 ? StationTexts.SearchNeedsDockedShip
                : _search.LootOf(_zone, _station).IsEmpty ? StationTexts.SearchAlreadyEmpty : null;
            actions.Add(new InspectionAction(StationTexts.SearchAction, null, searchBlocked == null, searchBlocked,
                () => _search.RequestSearch(_zone, _station)));
        }

        /// <summary>Type and rough size, which are visible from afar.</summary>
        public static GameText TypeLine(ZoneStation station)
        {
            return InspectionTexts.StationTypeLine.Format(("type", ZoneTexts.StationTypeNames.For(station.Type)),
                ("length", StationTypes.NominalLengthKlicks(station.Type)));
        }

        /// <summary>
        /// What the fleet knows: atmosphere, condition and radiation from a scan or a docked crew, the
        /// life signs only from a scan. Nothing else of the hidden profile shows.
        /// </summary>
        public static void DescribeKnowledge(ZoneStation station, List<GameText> lines)
        {
            if (!station.IsEnvironmentKnown)
            {
                lines.Add(InspectionTexts.StationUnscanned);
                return;
            }

            StationProfile profile = station.Profile;
            lines.Add(InspectionTexts.StationAtmosphere.Format(("value", ZoneTexts.Atmospheres.For(profile.Atmosphere))));
            lines.Add(InspectionTexts.StationCondition.Format(("value", ZoneTexts.Conditions.For(profile.Condition))));
            lines.Add(InspectionTexts.StationRadiation.Format(("value", ZoneTexts.Radiation.For(profile.Radiation))));
            lines.Add(station.IsScanned
                ? InspectionTexts.StationLifeSigns.Format(("lifeSigns", ScoutingService.LifeSigns(station.ScanReport)))
                : InspectionTexts.StationLifeSignsUnscanned);
        }

        /// <summary>The attitude the station claimed, or a hint to hail it once a scan shows people aboard.</summary>
        public static void DescribeHail(ZoneStation station, List<GameText> lines)
        {
            if (station.IsHailed)
            {
                lines.Add(station.HailResponse.Answered
                    ? InspectionTexts.StationAttitudeLine.Format(("value", ZoneTexts.Attitudes.For(station.HailResponse.ClaimedAttitude)))
                    : InspectionTexts.StationNoAnswer);
                return;
            }
            if (station.IsScanned && station.ScanReport.HasLifeSigns)
            {
                lines.Add(InspectionTexts.StationAttitudeUnknown);
            }
        }

        void DescribeDocking(List<GameText> lines)
        {
            _docking.CollectAssignedShips(_station, _assigned);
            lines.Add(StationTexts.PortsInUse.Format(("used", _assigned.Count), ("total", _station.DockingPortCount)));
            foreach (ShipAgent ship in _assigned)
            {
                if (ship.TryGetComponent(out StationSearching searching) && searching.IsSearching)
                {
                    lines.Add(StationTexts.SearchingShipLine.Format(("ship", ship.DisplayName), ("progress", searching.FindProgress)));
                }
                else if (ship.TryGetComponent(out StationDocking docking))
                {
                    lines.Add(StationTexts.DockedShipLine.Format(("ship", ship.DisplayName), ("state", StationTexts.DockingStates.For(docking.State))));
                }
            }

            StationLoot loot = _search.LootOf(_zone, _station);
            if (loot.IsEmpty && loot.FindsMade > 0)
            {
                lines.Add(StationTexts.NothingLeftLine);
            }
            else if (loot.FindsMade > 0)
            {
                lines.Add(StationTexts.FindsMadeLine.Format(("count", loot.FindsMade)));
            }
        }

        /// <summary>Selected small ships dock if there are any; otherwise a free ship is picked, scouts first.</summary>
        GameText Dock()
        {
            _selectedSmallShips.Clear();
            foreach (ShipAgent ship in _selectedShips())
            {
                if (!ship.IsCapital)
                {
                    _selectedSmallShips.Add(ship);
                }
            }
            if (_selectedSmallShips.Count > 0)
            {
                _docking.RequestDocking(_zone, _station, _selectedSmallShips);
            }
            else
            {
                _docking.RequestDocking(_zone, _station);
            }
            return null;
        }

        GameText Hail()
        {
            StationHailResponse response = _station.Hail();
            return response.Answered
                ? InspectionTexts.HailAnswered.Format(("station", _station.Name), ("reply", InspectionTexts.HailReplies.For(response.ClaimedAttitude)))
                : InspectionTexts.HailNoAnswer.Format(("station", _station.Name));
        }

        /// <summary>Radio range: any ship of the fleet in the station's zone, docked ones included.</summary>
        bool HasShipInZone()
        {
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (ship.Zone == _zone)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

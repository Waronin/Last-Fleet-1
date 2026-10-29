using LastFleet.Shared.Text;
using LastFleet.Zones;

namespace LastFleet.Inspection
{
    /// <summary>Texts of the inspection and ship windows in the string table "Inspection".</summary>
    public static class InspectionTexts
    {
        const string Table = "Inspection";

        public static readonly GameTextKey Close = new GameTextKey(Table, "panel.close");

        /// <summary>{zone}</summary>
        public static readonly GameTextKey ZoneLine = new GameTextKey(Table, "line.zone");

        public static readonly GameTextKey RightClickHint = new GameTextKey(Table, "hint.right-click");
        public static readonly GameTextKey DoubleClickHint = new GameTextKey(Table, "hint.double-click");

        // Deposits
        /// <summary>{size}, {shape}</summary>
        public static readonly GameTextKey DepositTitle = new GameTextKey(Table, "deposit.title");

        /// <summary>{ship}, {seconds}</summary>
        public static readonly GameTextKey DepositScanning = new GameTextKey(Table, "deposit.scanning");

        /// <summary>{ship}</summary>
        public static readonly GameTextKey DepositScanApproaching = new GameTextKey(Table, "deposit.scan-approaching");

        public static readonly GameTextKey DepositDepleted = new GameTextKey(Table, "deposit.depleted");
        public static readonly GameTextKey DepositUnknown = new GameTextKey(Table, "deposit.unknown");

        /// <summary>{resource}</summary>
        public static readonly GameTextKey DepositResource = new GameTextKey(Table, "deposit.resource");

        /// <summary>{remaining}, {initial}</summary>
        public static readonly GameTextKey DepositRemaining = new GameTextKey(Table, "deposit.remaining");

        /// <summary>{hazard}</summary>
        public static readonly GameTextKey DepositHazard = new GameTextKey(Table, "deposit.hazard");

        /// <summary>{used}, {total}</summary>
        public static readonly GameTextKey DepositSlots = new GameTextKey(Table, "deposit.slots");

        /// <summary>{bonus}</summary>
        public static readonly GameTextKey DepositMiningBonus = new GameTextKey(Table, "deposit.mining-bonus");

        public static readonly GameTextKey ScanAction = new GameTextKey(Table, "action.scan");
        public static readonly GameTextKey AlreadyScanned = new GameTextKey(Table, "action.scan.already-scanned");
        public static readonly GameTextKey ScanAlreadyAssigned = new GameTextKey(Table, "action.scan.already-assigned");
        public static readonly GameTextKey ScanRequested = new GameTextKey(Table, "action.scan.requested");
        public static readonly GameTextKey MineAction = new GameTextKey(Table, "action.mine");
        public static readonly GameTextKey AllSlotsTaken = new GameTextKey(Table, "action.mine.slots-taken");
        public static readonly GameTextKey NoHarvesterAvailable = new GameTextKey(Table, "action.mine.no-harvester");

        /// <summary>{count}</summary>
        public static readonly GameTextKey SelectedHarvestersMining = new GameTextKey(Table, "action.mine.selected-started");

        /// <summary>{count}</summary>
        public static readonly GameTextKey HarvestersCalled = new GameTextKey(Table, "action.mine.called");

        // Points of interest
        /// <summary>{kind}, {distance}</summary>
        public static readonly GameTextKey PointOfInterestKindLine = new GameTextKey(Table, "poi.kind");

        public static readonly GameTextKey FuelSource = new GameTextKey(Table, "poi.fuel-source");
        public static readonly GameTextKey Unexplored = new GameTextKey(Table, "poi.unexplored");

        /// <summary>{count}, {scanned}</summary>
        public static readonly GameTextKey DepositCount = new GameTextKey(Table, "poi.deposits");

        /// <summary>{count}</summary>
        public static readonly GameTextKey StationCount = new GameTextKey(Table, "poi.stations");

        public static readonly GameTextKey ViewZoneAction = new GameTextKey(Table, "action.view-zone");
        public static readonly GameTextKey NotExploredYet = new GameTextKey(Table, "action.view-zone.not-explored");
        public static readonly GameTextKey ExploreAction = new GameTextKey(Table, "action.explore");
        public static readonly GameTextKey AlreadyExplored = new GameTextKey(Table, "action.explore.already-explored");

        // Stations
        /// <summary>{type}, {length}</summary>
        public static readonly GameTextKey StationTypeLine = new GameTextKey(Table, "station.type");

        /// <summary>{count}</summary>
        public static readonly GameTextKey StationPorts = new GameTextKey(Table, "station.ports");

        public static readonly GameTextKey StationUnscanned = new GameTextKey(Table, "station.unscanned");

        /// <summary>{value}</summary>
        public static readonly GameTextKey StationAtmosphere = new GameTextKey(Table, "station.atmosphere");

        /// <summary>{value}</summary>
        public static readonly GameTextKey StationCondition = new GameTextKey(Table, "station.condition");

        /// <summary>{value}</summary>
        public static readonly GameTextKey StationRadiation = new GameTextKey(Table, "station.radiation");

        /// <summary>{lifeSigns}</summary>
        public static readonly GameTextKey StationLifeSigns = new GameTextKey(Table, "station.life-signs");

        public static readonly GameTextKey StationAttitudeUnknown = new GameTextKey(Table, "station.attitude-unknown");

        /// <summary>{value}</summary>
        public static readonly GameTextKey StationAttitudeLine = new GameTextKey(Table, "station.attitude");

        public static readonly GameTextKey StationNoAnswer = new GameTextKey(Table, "station.no-answer");
        public static readonly GameTextKey HailAction = new GameTextKey(Table, "action.hail");
        public static readonly GameTextKey AlreadyHailed = new GameTextKey(Table, "action.hail.already-hailed");
        public static readonly GameTextKey NoShipInRadioRange = new GameTextKey(Table, "action.hail.no-ship-in-range");

        /// <summary>The radio reply of a station, per attitude; None is silence.</summary>
        public static readonly EnumTextKeys<StationAttitude> HailReplies = new EnumTextKeys<StationAttitude>(Table, "hail.reply");

        /// <summary>{station}, {reply}</summary>
        public static readonly GameTextKey HailAnswered = new GameTextKey(Table, "notice.hail-answered");

        /// <summary>{station}</summary>
        public static readonly GameTextKey HailNoAnswer = new GameTextKey(Table, "notice.hail-no-answer");

        // Ship window
        /// <summary>{count}</summary>
        public static readonly GameTextKey ShipsSelected = new GameTextKey(Table, "ship.selected-count");

        /// <summary>{ship}, {hull}, {fuel}, {task}</summary>
        public static readonly GameTextKey ShipGroupLine = new GameTextKey(Table, "ship.group-line");

        /// <summary>{class}, {size}</summary>
        public static readonly GameTextKey ShipClass = new GameTextKey(Table, "ship.class");

        /// <summary>{current}, {maximum}</summary>
        public static readonly GameTextKey Hull = new GameTextKey(Table, "ship.hull");

        public static readonly GameTextKey NoHull = new GameTextKey(Table, "ship.no-hull");

        /// <summary>{amount}, {capacity}</summary>
        public static readonly GameTextKey Fuel = new GameTextKey(Table, "ship.fuel");

        public static readonly GameTextKey OutOfFuel = new GameTextKey(Table, "ship.out-of-fuel");

        /// <summary>{count}, {required}, {efficiency}</summary>
        public static readonly GameTextKey CrewLine = new GameTextKey(Table, "ship.crew");

        public static readonly GameTextKey Derelict = new GameTextKey(Table, "ship.derelict");

        /// <summary>{task}</summary>
        public static readonly GameTextKey TaskLine = new GameTextKey(Table, "ship.task");

        /// <summary>{state}</summary>
        public static readonly GameTextKey MiningTask = new GameTextKey(Table, "ship.task.mining");

        /// <summary>{phase}, {speed}</summary>
        public static readonly GameTextKey Motion = new GameTextKey(Table, "ship.motion");

        /// <summary>{zone}, {carrier}</summary>
        public static readonly GameTextKey ZoneDockedLine = new GameTextKey(Table, "ship.zone-docked");

        /// <summary>{squad}</summary>
        public static readonly GameTextKey SquadLine = new GameTextKey(Table, "ship.squad");

        /// <summary>{cargo}</summary>
        public static readonly GameTextKey CargoLine = new GameTextKey(Table, "ship.cargo");

        public static readonly GameTextKey CargoEmpty = new GameTextKey(Table, "ship.cargo.empty");

        /// <summary>{resource}, {amount}, {capacity}</summary>
        public static readonly GameTextKey CargoAmount = new GameTextKey(Table, "ship.cargo.amount");

        /// <summary>{docked}, {capacity}, {launching}</summary>
        public static readonly GameTextKey HangarLine = new GameTextKey(Table, "ship.hangar");

        /// <summary>{ore}, {ice}, {gas}</summary>
        public static readonly GameTextKey StoredLine = new GameTextKey(Table, "ship.stored");

        public static readonly GameTextKey ReturnToHangarAction = new GameTextKey(Table, "action.return-to-hangar");
        public static readonly GameTextKey NoSmallShipInFlight = new GameTextKey(Table, "action.return-to-hangar.none-in-flight");
        public static readonly GameTextKey LaunchAction = new GameTextKey(Table, "action.launch");
        public static readonly GameTextKey NoDockedShipSelected = new GameTextKey(Table, "action.launch.none-docked");
        public static readonly GameTextKey FormSquadAction = new GameTextKey(Table, "action.form-squad");
        public static readonly GameTextKey FormSquadNeedsOneClass = new GameTextKey(Table, "action.form-squad.needs-one-class");

        /// <summary>{squad}</summary>
        public static readonly GameTextKey SquadFormed = new GameTextKey(Table, "action.form-squad.formed");
    }
}

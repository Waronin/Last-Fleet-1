using LastFleet.Shared.Text;

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

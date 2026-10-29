using System;
using System.Collections.Generic;
using LastFleet.Exploration;
using LastFleet.Mining;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Inspection
{
    /// <summary>
    /// Asteroid or gas cloud: what is known about it and the interactions scan and mine. Before a scan
    /// only the visible size is shown, so the panel never gives away what a scan would reveal.
    /// </summary>
    public sealed class DepositInspection : IInspectionTarget
    {
        readonly ZoneView _zone;
        readonly ResourceDeposit _deposit;
        readonly MiningCoordinator _mining;
        readonly ScoutingService _scouting;
        readonly Func<IReadOnlyList<ShipAgent>> _selectedShips;

        public DepositInspection(ZoneView zone, ResourceDeposit deposit, MiningCoordinator mining, ScoutingService scouting,
            Func<IReadOnlyList<ShipAgent>> selectedShips)
        {
            _zone = zone;
            _deposit = deposit ?? throw new ArgumentNullException(nameof(deposit));
            _mining = mining;
            _scouting = scouting;
            _selectedShips = selectedShips;
        }

        public ResourceDeposit Deposit => _deposit;

        public GameText Title => InspectionTexts.DepositTitle.Format(("size", ZoneTexts.SizeClasses.For(_deposit.SizeClass)),
            ("shape", ZoneTexts.Shapes.For(_deposit.Shape)));

        public bool IsValid => _zone != null;

        public void Describe(List<GameText> lines)
        {
            lines.Clear();
            lines.Add(InspectionTexts.ZoneLine.Format(("zone", _zone.Record.PointOfInterest.Name)));
            int occupied = _deposit.MiningSlotCount - _mining.Claims.FreeSlotCount(_deposit);
            DescribeDeposit(_deposit, occupied, lines);

            if (_scouting.TryGetActiveScan(_deposit, out ShipScanner scanner))
            {
                lines.Add(scanner.State == ScanTaskState.Scanning
                    ? InspectionTexts.DepositScanning.Format(("ship", scanner.Ship.DisplayName), ("seconds", scanner.RemainingSeconds))
                    : InspectionTexts.DepositScanApproaching.Format(("ship", scanner.Ship.DisplayName)));
            }
        }

        public void CollectActions(List<InspectionAction> actions)
        {
            actions.Clear();
            bool scanActive = _scouting.TryGetActiveScan(_deposit, out ShipScanner _);
            GameText scanBlocked = _deposit.IsScanned ? InspectionTexts.AlreadyScanned : scanActive ? InspectionTexts.ScanAlreadyAssigned : null;
            actions.Add(new InspectionAction(InspectionTexts.ScanAction, null, scanBlocked == null, scanBlocked,
                () => _scouting.RequestScan(_zone, _deposit) ? InspectionTexts.ScanRequested : ExplorationTexts.NoShipToScan));

            int freeSlots = _mining.Claims.FreeSlotCount(_deposit);
            GameText mineBlocked = _deposit.IsDepleted ? InspectionTexts.DepositDepleted : freeSlots == 0 ? InspectionTexts.AllSlotsTaken : null;
            actions.Add(new InspectionAction(InspectionTexts.MineAction, InspectionTexts.RightClickHint, mineBlocked == null, mineBlocked, Mine));
        }

        /// <summary>Information lines for a deposit; hidden values stay hidden until it is scanned.</summary>
        public static void DescribeDeposit(ResourceDeposit deposit, int occupiedSlots, List<GameText> lines)
        {
            if (deposit.IsDepleted)
            {
                lines.Add(InspectionTexts.DepositDepleted);
                return;
            }
            if (!deposit.IsScanned)
            {
                lines.Add(InspectionTexts.DepositUnknown);
                return;
            }

            lines.Add(InspectionTexts.DepositResource.Format(("resource", ZoneTexts.Resources.For(deposit.Resource))));
            lines.Add(InspectionTexts.DepositRemaining.Format(("remaining", deposit.RemainingAmount), ("initial", deposit.InitialAmount)));
            lines.Add(InspectionTexts.DepositHazard.Format(("hazard", ZoneTexts.Hazards.For(deposit.Hazard))));
            lines.Add(InspectionTexts.DepositSlots.Format(("used", occupiedSlots), ("total", deposit.MiningSlotCount)));
            lines.Add(InspectionTexts.DepositMiningBonus.Format(("bonus", MiningYield.Multiplier(deposit.MiningSeconds))));
        }

        GameText Mine()
        {
            bool usedSelection = HasSelectedHarvester();
            int started = _mining.AssignHarvesters(_zone, _deposit, _selectedShips());
            if (started == 0)
            {
                return InspectionTexts.NoHarvesterAvailable;
            }
            return usedSelection
                ? InspectionTexts.SelectedHarvestersMining.Format(("count", started))
                : InspectionTexts.HarvestersCalled.Format(("count", started));
        }

        bool HasSelectedHarvester()
        {
            foreach (ShipAgent ship in _selectedShips())
            {
                if (ship.TryGetComponent(out HarvesterMining _))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

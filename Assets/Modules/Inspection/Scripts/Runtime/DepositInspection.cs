using System;
using System.Collections.Generic;
using LastFleet.Exploration;
using LastFleet.Mining;
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
        public string Title => $"{_deposit.SizeClass} {(_deposit.Shape == DepositShape.GasCloud ? "Gas Cloud" : "Asteroid")}";
        public bool IsValid => _zone != null;

        public void Describe(List<string> lines)
        {
            lines.Clear();
            lines.Add($"Zone: {_zone.Record.PointOfInterest.Name}");
            int occupied = _deposit.MiningSlotCount - _mining.Claims.FreeSlotCount(_deposit);
            DescribeDeposit(_deposit, occupied, lines);

            if (_scouting.TryGetActiveScan(_deposit, out ShipScanner scanner))
            {
                lines.Add(scanner.State == ScanTaskState.Scanning
                    ? $"Scanning: {scanner.Ship.name}, {scanner.RemainingSeconds:0.0} s left"
                    : $"Scan: {scanner.Ship.name} on its way");
            }
        }

        public void CollectActions(List<InspectionAction> actions)
        {
            actions.Clear();
            bool scanActive = _scouting.TryGetActiveScan(_deposit, out ShipScanner _);
            string scanBlocked = _deposit.IsScanned ? "Already scanned." : scanActive ? "A ship is already on it." : null;
            actions.Add(new InspectionAction("Scan", null, scanBlocked == null, scanBlocked,
                () => _scouting.RequestScan(_zone, _deposit) ? "Scan requested." : "No ship available to scan."));

            int freeSlots = _mining.Claims.FreeSlotCount(_deposit);
            string mineBlocked = _deposit.IsDepleted ? "Depleted." : freeSlots == 0 ? "All mining slots are taken." : null;
            actions.Add(new InspectionAction("Mine", "right click", mineBlocked == null, mineBlocked, Mine));
        }

        /// <summary>Information lines for a deposit; hidden values stay hidden until it is scanned.</summary>
        public static void DescribeDeposit(ResourceDeposit deposit, int occupiedSlots, List<string> lines)
        {
            if (deposit.IsDepleted)
            {
                lines.Add("Depleted.");
                return;
            }
            if (!deposit.IsScanned)
            {
                lines.Add("Contents unknown. Scan to reveal resource, amount and hazards.");
                return;
            }

            lines.Add($"Resource: {deposit.Resource}");
            lines.Add($"Remaining: {deposit.RemainingAmount:0} of {deposit.InitialAmount:0}");
            lines.Add($"Hazard: {(deposit.Hazard == DepositHazard.None ? "none" : deposit.Hazard.ToString())}");
            lines.Add($"Mining slots: {occupiedSlots}/{deposit.MiningSlotCount} in use");
            lines.Add($"Mining bonus: x{MiningYield.Multiplier(deposit.MiningSeconds):0.00}");
        }

        string Mine()
        {
            bool usedSelection = HasSelectedHarvester();
            int started = _mining.AssignHarvesters(_zone, _deposit, _selectedShips());
            if (started == 0)
            {
                return "No harvester available.";
            }
            return usedSelection ? $"{started} selected harvester(s) mining." : $"Called {started} harvester(s) to mine.";
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

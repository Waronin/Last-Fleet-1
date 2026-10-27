using System.Collections.Generic;
using LastFleet.Mining;
using LastFleet.ShipControl;
using LastFleet.Ships;
using LastFleet.Zones;

namespace LastFleet.Inspection
{
    /// <summary>
    /// The ship window: details of one selected ship, or one line per ship for a group, plus the
    /// hangar and squad commands that fit the selection.
    /// </summary>
    public sealed class ShipSelectionInspection : IInspectionTarget
    {
        readonly ShipSelection _selection;
        readonly ShipRegistry _registry;
        readonly HangarCommandInput _hangarCommands;

        public ShipSelectionInspection(ShipSelection selection, ShipRegistry registry, HangarCommandInput hangarCommands)
        {
            _selection = selection;
            _registry = registry;
            _hangarCommands = hangarCommands;
        }

        public string Title => _selection.Count == 1 ? _selection.Ships[0].name : $"{_selection.Count} ships selected";
        public bool IsValid => _selection.Count > 0;

        public void Describe(List<string> lines)
        {
            lines.Clear();
            if (_selection.Count == 1)
            {
                DescribeShip(_selection.Ships[0], lines);
                return;
            }
            foreach (ShipAgent ship in _selection.Ships)
            {
                lines.Add($"{ship.name}: {Hull(ship)}, {Fuel(ship)}, {Task(ship)}");
            }
        }

        public void CollectActions(List<InspectionAction> actions)
        {
            actions.Clear();
            int flyingSmall = 0;
            int docked = 0;
            bool sameClass = true;
            foreach (ShipAgent ship in _selection.Ships)
            {
                flyingSmall += !ship.IsCapital && !ship.IsDocked ? 1 : 0;
                docked += ship.IsDocked ? 1 : 0;
                sameClass &= ship.ClassName == _selection.Ships[0].ClassName;
            }

            actions.Add(new InspectionAction("Return to hangar", "H", flyingSmall > 0, "No small ship in flight.", () =>
            {
                _hangarCommands.ReturnToHangar(_selection.Ships);
                return null;
            }));
            actions.Add(new InspectionAction("Launch", "L", docked > 0, "No docked ship selected.", () =>
            {
                _hangarCommands.LaunchShips(_selection.Ships);
                return null;
            }));
            bool canForm = _selection.Count > 1 && sameClass;
            actions.Add(new InspectionAction("Form squad", "G", canForm, "Select several ships of one class.", () =>
                $"Formed {_registry.Squads.Form(_selection.Ships).Name}."));
        }

        void DescribeShip(ShipAgent ship, List<string> lines)
        {
            lines.Add($"{ship.ClassName} ({ship.Size})");
            lines.Add(Hull(ship));
            lines.Add(Fuel(ship));
            if (ship.Fuel.Tank.IsEmpty)
            {
                lines.Add("Out of fuel: thrusters only, no jumps");
            }
            lines.Add($"Task: {Task(ship)}");
            if (!ship.IsDocked)
            {
                lines.Add($"Motion: {ship.MotionPhase}, {ship.Speed * 0.1f:0.00} km/s");
            }
            lines.Add($"Zone: {ship.Zone.Record.PointOfInterest.Name}{(ship.IsDocked ? $", docked in {ship.DockedIn.Carrier.name}" : string.Empty)}");
            if (_registry.Squads.TryGetSquad(ship, out Squad squad))
            {
                lines.Add($"Squad: {squad.Name}");
            }
            if (ship.TryGetComponent(out HarvesterMining mining))
            {
                string cargo = mining.Cargo.IsEmpty ? "empty" : $"{mining.Cargo.Resource} {mining.Cargo.Amount:0}/{mining.Cargo.Capacity:0}";
                lines.Add($"Cargo: {cargo}");
            }
            if (ship.TryGetComponent(out Hangar hangar))
            {
                lines.Add($"Hangar: {hangar.DockedShips.Count}/{hangar.Capacity} docked{(hangar.LaunchQueueCount > 0 ? $", {hangar.LaunchQueueCount} launching" : string.Empty)}");
            }
            if (ship.TryGetComponent(out RawMaterialStorage storage))
            {
                RawMaterialStock stock = storage.Stock;
                lines.Add($"Stored: Ore {stock.AmountOf(ResourceKind.Ore):0}, Ice {stock.AmountOf(ResourceKind.Ice):0}, Gas {stock.AmountOf(ResourceKind.Gas):0}");
            }
        }

        static string Hull(ShipAgent ship)
        {
            return ship.TryGetComponent(out ShipDurability durability)
                ? $"Hull {durability.Hull.Current:0}/{durability.Hull.Maximum:0}"
                : "No hull";
        }

        static string Fuel(ShipAgent ship)
        {
            FuelTank tank = ship.Fuel.Tank;
            return $"Fuel {tank.Amount:0.0}/{tank.Capacity:0}";
        }

        static string Task(ShipAgent ship)
        {
            if (ship.TryGetComponent(out HarvesterMining mining) && mining.State != MiningState.Inactive)
            {
                return $"Mining ({mining.State})";
            }
            return ship.Activity.ToString();
        }
    }
}

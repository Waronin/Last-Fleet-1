using System.Collections.Generic;
using LastFleet.Mining;
using LastFleet.People;
using LastFleet.Shared.Text;
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
        static readonly GameText ReturnHint = GameText.Verbatim("H");
        static readonly GameText LaunchHint = GameText.Verbatim("L");
        static readonly GameText FormSquadHint = GameText.Verbatim("G");

        readonly ShipSelection _selection;
        readonly ShipRegistry _registry;
        readonly HangarCommandInput _hangarCommands;

        public ShipSelectionInspection(ShipSelection selection, ShipRegistry registry, HangarCommandInput hangarCommands)
        {
            _selection = selection;
            _registry = registry;
            _hangarCommands = hangarCommands;
        }

        public GameText Title => _selection.Count == 1
            ? _selection.Ships[0].DisplayName
            : InspectionTexts.ShipsSelected.Format(("count", _selection.Count));

        public bool IsValid => _selection.Count > 0;

        public void Describe(List<GameText> lines)
        {
            lines.Clear();
            if (_selection.Count == 1)
            {
                DescribeShip(_selection.Ships[0], lines);
                return;
            }
            foreach (ShipAgent ship in _selection.Ships)
            {
                lines.Add(InspectionTexts.ShipGroupLine.Format(("ship", ship.DisplayName), ("hull", Hull(ship)), ("fuel", Fuel(ship)), ("task", Task(ship))));
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

            actions.Add(new InspectionAction(InspectionTexts.ReturnToHangarAction, ReturnHint, flyingSmall > 0, InspectionTexts.NoSmallShipInFlight, () =>
            {
                _hangarCommands.ReturnToHangar(_selection.Ships);
                return null;
            }));
            actions.Add(new InspectionAction(InspectionTexts.LaunchAction, LaunchHint, docked > 0, InspectionTexts.NoDockedShipSelected, () =>
            {
                _hangarCommands.LaunchShips(_selection.Ships);
                return null;
            }));
            bool canForm = _selection.Count > 1 && sameClass;
            actions.Add(new InspectionAction(InspectionTexts.FormSquadAction, FormSquadHint, canForm, InspectionTexts.FormSquadNeedsOneClass, () =>
                InspectionTexts.SquadFormed.Format(("squad", _registry.Squads.Form(_selection.Ships).DisplayName))));
        }

        void DescribeShip(ShipAgent ship, List<GameText> lines)
        {
            lines.Add(InspectionTexts.ShipClass.Format(("class", ship.ClassDisplayName), ("size", ShipTexts.Sizes.For(ship.Size))));
            lines.Add(Hull(ship));
            lines.Add(Fuel(ship));
            if (ship.Fuel.Tank.IsEmpty)
            {
                lines.Add(InspectionTexts.OutOfFuel);
            }
            if (ship.TryGetComponent(out ShipPassengers passengers))
            {
                PassengerQuarters quarters = passengers.Quarters;
                lines.Add(PeopleTexts.PassengersLine.Format(("count", quarters.Count), ("capacity", quarters.Capacity)));
                if (quarters.IsOvercrowded)
                {
                    lines.Add(PeopleTexts.Overcrowded);
                }
            }
            lines.Add(InspectionTexts.TaskLine.Format(("task", Task(ship))));
            if (!ship.IsDocked)
            {
                lines.Add(InspectionTexts.Motion.Format(("phase", ShipTexts.MotionPhases.For(ship.MotionPhase)), ("speed", ship.Speed * 0.1f)));
            }
            string zone = ship.Zone.Record.PointOfInterest.Name;
            lines.Add(ship.IsDocked
                ? InspectionTexts.ZoneDockedLine.Format(("zone", zone), ("carrier", ship.DockedIn.Carrier.DisplayName))
                : InspectionTexts.ZoneLine.Format(("zone", zone)));
            if (_registry.Squads.TryGetSquad(ship, out Squad squad))
            {
                lines.Add(InspectionTexts.SquadLine.Format(("squad", squad.DisplayName)));
            }
            if (ship.TryGetComponent(out HarvesterMining mining))
            {
                GameText cargo = mining.Cargo.IsEmpty
                    ? InspectionTexts.CargoEmpty
                    : InspectionTexts.CargoAmount.Format(("resource", ZoneTexts.Resources.For(mining.Cargo.Resource)),
                        ("amount", mining.Cargo.Amount), ("capacity", mining.Cargo.Capacity));
                lines.Add(InspectionTexts.CargoLine.Format(("cargo", cargo)));
            }
            if (ship.TryGetComponent(out Hangar hangar))
            {
                lines.Add(InspectionTexts.HangarLine.Format(("docked", hangar.DockedShips.Count), ("capacity", hangar.Capacity),
                    ("launching", hangar.LaunchQueueCount)));
            }
            if (ship.TryGetComponent(out RawMaterialStorage storage))
            {
                RawMaterialStock stock = storage.Stock;
                lines.Add(InspectionTexts.StoredLine.Format(("ore", stock.AmountOf(ResourceKind.Ore)), ("ice", stock.AmountOf(ResourceKind.Ice)),
                    ("gas", stock.AmountOf(ResourceKind.Gas))));
            }
        }

        static GameText Hull(ShipAgent ship)
        {
            return ship.TryGetComponent(out ShipDurability durability)
                ? InspectionTexts.Hull.Format(("current", durability.Hull.Current), ("maximum", durability.Hull.Maximum))
                : InspectionTexts.NoHull;
        }

        static GameText Fuel(ShipAgent ship)
        {
            FuelTank tank = ship.Fuel.Tank;
            return InspectionTexts.Fuel.Format(("amount", tank.Amount), ("capacity", tank.Capacity));
        }

        static GameText Task(ShipAgent ship)
        {
            if (ship.TryGetComponent(out HarvesterMining mining) && mining.State != MiningState.Inactive)
            {
                return InspectionTexts.MiningTask.Format(("state", MiningTexts.States.For(mining.State)));
            }
            return ShipTexts.Activities.For(ship.Activity);
        }
    }
}

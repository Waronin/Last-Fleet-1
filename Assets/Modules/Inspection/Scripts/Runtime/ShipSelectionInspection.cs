using System;
using System.Collections.Generic;
using LastFleet.Combat;
using LastFleet.Mining;
using LastFleet.People;
using LastFleet.Repairs;
using LastFleet.Rewards;
using LastFleet.Shared.Text;
using LastFleet.ShipControl;
using LastFleet.Ships;
using LastFleet.Stations;
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
        readonly ShipRepairInspection _repairs;

        /// <param name="repairs">Optional; without it the window offers no repairs.</param>
        public ShipSelectionInspection(ShipSelection selection, ShipRegistry registry, HangarCommandInput hangarCommands, RepairService repairs)
        {
            _selection = selection;
            _registry = registry;
            _hangarCommands = hangarCommands;
            _repairs = repairs != null ? new ShipRepairInspection(repairs) : null;
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
            int loaded = 0;
            bool sameClass = true;
            foreach (ShipAgent ship in _selection.Ships)
            {
                flyingSmall += !ship.IsCapital && !ship.IsDocked ? 1 : 0;
                docked += ship.IsDocked ? 1 : 0;
                loaded += ship.TryGetComponent(out HarvesterMining mining) && !mining.Cargo.IsEmpty ? 1 : 0;
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
            // Shown only for harvesters: the way out when the fleet has no room for what they carry.
            if (loaded > 0)
            {
                actions.Add(new InspectionAction(InspectionTexts.JettisonCargoAction, null, true, null, JettisonCargo));
            }
            TankerRefuelingInspection.CollectActions(_selection.Ships, actions);
            _repairs?.CollectActions(_selection.Ships, actions);
            bool canForm = _selection.Count > 1 && sameClass;
            actions.Add(new InspectionAction(InspectionTexts.FormSquadAction, FormSquadHint, canForm, InspectionTexts.FormSquadNeedsOneClass, () =>
                InspectionTexts.SquadFormed.Format(("squad", _registry.Squads.Form(_selection.Ships).DisplayName))));
        }

        GameText JettisonCargo()
        {
            int ships = 0;
            foreach (ShipAgent ship in _selection.Ships)
            {
                if (ship.TryGetComponent(out HarvesterMining mining) && mining.JettisonCargo() > 0f)
                {
                    ships++;
                }
            }
            return InspectionTexts.CargoJettisoned.Format(("count", ships));
        }

        void DescribeShip(ShipAgent ship, List<GameText> lines)
        {
            lines.Add(InspectionTexts.ShipClass.Format(("class", ship.ClassDisplayName), ("size", ShipTexts.Sizes.For(ship.Size))));
            lines.Add(Hull(ship));
            _repairs?.Describe(ship, lines);
            if (ship.TryGetComponent(out ShipShield shield))
            {
                lines.Add(CombatTexts.ShieldLine.Format(("current", shield.Charge.Current), ("maximum", shield.Charge.Maximum)));
            }
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
            if (ship.Crew != null)
            {
                CrewComplement crew = ship.Crew.Complement;
                lines.Add(InspectionTexts.CrewLine.Format(("count", crew.Count), ("required", crew.Required), ("efficiency", crew.Efficiency)));
                if (ship.IsDerelict)
                {
                    lines.Add(InspectionTexts.Derelict);
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
            if (ship.TryGetComponent(out ShipRewardHold hold) && !hold.Hold.IsEmpty)
            {
                lines.Add(RewardTexts.HoldLine.Format(("finds", RewardTexts.DescribeAll(hold.Hold.Rewards))));
            }
            if (ship.TryGetComponent(out Hangar hangar))
            {
                lines.Add(InspectionTexts.HangarLine.Format(("docked", hangar.DockedShips.Count), ("capacity", hangar.Capacity),
                    ("launching", hangar.LaunchQueueCount)));
            }
            if (ship.TryGetComponent(out SupplyStorage storage))
            {
                AddStoredLines(storage.Stock, lines);
            }
        }

        /// <summary>One line per supply the ship can store, e.g. "Stored: Fuel 8000/20000".</summary>
        static void AddStoredLines(SupplyStock stock, List<GameText> lines)
        {
            foreach (Supply supply in (Supply[])Enum.GetValues(typeof(Supply)))
            {
                if (stock.CapacityOf(supply) > 0f)
                {
                    lines.Add(InspectionTexts.StoredLine.Format(("stock", MiningTexts.SupplyAmount.Format(("supply", MiningTexts.Supplies.For(supply)),
                        ("amount", stock.AmountOf(supply)), ("capacity", stock.CapacityOf(supply))))));
                }
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
            if (ship.TryGetComponent(out StationSearching searching) && searching.IsSearching)
            {
                return StationTexts.SearchingTask.Format(("station", searching.Station.Name));
            }
            if (ship.TryGetComponent(out StationDocking docking) && docking.State != DockingState.Inactive)
            {
                return StationTexts.DockingTask.Format(("state", StationTexts.DockingStates.For(docking.State)), ("station", docking.Station.Name));
            }
            if (ship.TryGetComponent(out FighterCombat fighter) && fighter.State == CombatState.Engaging && fighter.Target != null)
            {
                return CombatTexts.EngagingTask.Format(("target", fighter.Target.DisplayName));
            }
            if (fighter != null && fighter.State == CombatState.Escorting && fighter.Escort != null && !fighter.Escort.IsOver)
            {
                return CombatTexts.EscortingTask.Format(("target", fighter.Escort.DisplayName));
            }
            if (ship.TryGetComponent(out HarvesterMining mining) && mining.State != MiningState.Inactive)
            {
                return InspectionTexts.MiningTask.Format(("state", MiningTexts.States.For(mining.State)));
            }
            if (ship.TryGetComponent(out FuelTanker tanker) && tanker.IsRefueling)
            {
                return MiningTexts.RefuelingTask.Format(("target", tanker.Target.DisplayName));
            }
            return ShipTexts.Activities.For(ship.Activity);
        }
    }
}

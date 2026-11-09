using System.Collections.Generic;
using LastFleet.Mining;
using LastFleet.Shared.Text;
using LastFleet.Ships;

namespace LastFleet.Inspection
{
    /// <summary>
    /// The refueling part of the ship window, built like the repair part: the refueling state of a
    /// capital ship and the queue of a tanker, and the buttons to refuel, refuel first or cancel
    /// refueling of the selected capital ships. Refusals say why, e.g. that no tanker is in the zone.
    /// </summary>
    public sealed class TankerRefuelingInspection
    {
        readonly FleetRefueling _refueling;

        public TankerRefuelingInspection(ShipRegistry registry)
        {
            _refueling = new FleetRefueling(registry);
        }

        public void Describe(ShipAgent ship, List<GameText> lines)
        {
            if (_refueling.TryGetJob(ship, out FuelTanker jobTanker, out int position))
            {
                lines.Add(JobLine(jobTanker, position));
            }
            if (ship.TryGetComponent(out FuelTanker ownTanker) && ownTanker.Current != null)
            {
                lines.Add(MiningTexts.TankerLine.Format(("ship", ownTanker.Current.DisplayName), ("waiting", ownTanker.WaitingCount)));
            }
        }

        public void CollectActions(IReadOnlyList<ShipAgent> ships, List<InspectionAction> actions)
        {
            int capitals = 0;
            int orderable = 0;
            int noTanker = 0;
            int full = 0;
            int queued = 0;
            int behindOthers = 0;
            foreach (ShipAgent ship in ships)
            {
                if (!ship.IsCapital)
                {
                    continue;
                }
                capitals++;
                RefuelEligibility eligibility = _refueling.Check(ship);
                orderable += eligibility == RefuelEligibility.Eligible ? 1 : 0;
                noTanker += eligibility == RefuelEligibility.NoTankerInZone ? 1 : 0;
                full += eligibility == RefuelEligibility.TankFull ? 1 : 0;
                if (_refueling.TryGetJob(ship, out FuelTanker _, out int position))
                {
                    queued++;
                    behindOthers += position > 0 ? 1 : 0;
                }
            }
            // Small ships refuel in their hangar, so the buttons only show for capital ships.
            if (capitals == 0)
            {
                return;
            }

            GameText orderReason = noTanker > 0 ? MiningTexts.NoTankerInZone
                : full > 0 ? MiningTexts.TankFull
                : MiningTexts.AlreadyWaitingForFuel;
            actions.Add(new InspectionAction(MiningTexts.RefuelAction, null, orderable > 0, orderReason, () => Order(ships)));
            // Only offered once a selected ship waits at a tanker, to keep the window short.
            if (queued == 0)
            {
                return;
            }
            actions.Add(new InspectionAction(MiningTexts.RefuelFirstAction, null, behindOthers > 0, MiningTexts.AlreadyRefueledFirst, () => RefuelFirst(ships)));
            actions.Add(new InspectionAction(MiningTexts.CancelRefuelAction, null, true, null, () => Cancel(ships)));
        }

        static GameText JobLine(FuelTanker tanker, int position)
        {
            if (position > 0)
            {
                return MiningTexts.RefuelJobWaiting.Format(("position", position + 1), ("tanker", tanker.Ship.DisplayName));
            }
            return tanker.IsShortOfFuel
                ? MiningTexts.RefuelJobShortOfFuel.Format(("tanker", tanker.Ship.DisplayName))
                : MiningTexts.RefuelJobActive.Format(("tanker", tanker.Ship.DisplayName));
        }

        GameText Order(IReadOnlyList<ShipAgent> ships)
        {
            int count = 0;
            foreach (ShipAgent ship in ships)
            {
                count += ship.IsCapital && _refueling.Order(ship) == RefuelEligibility.Eligible ? 1 : 0;
            }
            return MiningTexts.RefuelOrdered.Format(("count", count));
        }

        GameText RefuelFirst(IReadOnlyList<ShipAgent> ships)
        {
            // Walk backwards so the first selected ship ends up at the very front.
            int count = 0;
            for (int index = ships.Count - 1; index >= 0; index--)
            {
                count += _refueling.RefuelFirst(ships[index]) ? 1 : 0;
            }
            return MiningTexts.RefuelMovedToFront.Format(("count", count));
        }

        GameText Cancel(IReadOnlyList<ShipAgent> ships)
        {
            int count = 0;
            foreach (ShipAgent ship in ships)
            {
                count += _refueling.Cancel(ship) ? 1 : 0;
            }
            return MiningTexts.RefuelCancelled.Format(("count", count));
        }
    }
}

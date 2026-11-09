using System.Collections.Generic;
using LastFleet.Repairs;
using LastFleet.Shared.Text;
using LastFleet.Ships;

namespace LastFleet.Inspection
{
    /// <summary>
    /// The repair part of the ship window: the state of a ship's repair, the work of a capital ship's
    /// shipyard, and the buttons to queue, put first or cancel repairs of the selected ships.
    /// </summary>
    public sealed class ShipRepairInspection
    {
        readonly RepairService _repairs;

        public ShipRepairInspection(RepairService repairs)
        {
            _repairs = repairs;
        }

        public void Describe(ShipAgent ship, List<GameText> lines)
        {
            if (_repairs.TryGetJob(ship, out Shipyard jobYard, out int position))
            {
                lines.Add(JobLine(ship, jobYard, position));
            }
            if (ship.IsCapital && _repairs.TryGetYard(ship, out Shipyard ownYard) && ownYard.Current != null)
            {
                lines.Add(RepairTexts.YardLine.Format(("ship", ownYard.Current.DisplayName), ("waiting", ownYard.WaitingCount)));
            }
        }

        public void CollectActions(IReadOnlyList<ShipAgent> ships, List<InspectionAction> actions)
        {
            int orderable = 0;
            int flyingDamaged = 0;
            int queued = 0;
            int behindOthers = 0;
            foreach (ShipAgent ship in ships)
            {
                RepairEligibility eligibility = _repairs.Check(ship);
                orderable += eligibility == RepairEligibility.Eligible ? 1 : 0;
                flyingDamaged += eligibility == RepairEligibility.NotInHangar ? 1 : 0;
                if (_repairs.TryGetJob(ship, out Shipyard _, out int position))
                {
                    queued++;
                    behindOthers += position > 0 ? 1 : 0;
                }
            }

            GameText orderReason = flyingDamaged > 0 ? RepairTexts.OnlyInHangar : RepairTexts.NothingToRepair;
            actions.Add(new InspectionAction(RepairTexts.RepairAction, null, orderable > 0, orderReason, () => Order(ships)));
            // Only offered once a selected ship waits in a shipyard, to keep the window short.
            if (queued == 0)
            {
                return;
            }
            actions.Add(new InspectionAction(RepairTexts.RepairFirstAction, null, behindOthers > 0, RepairTexts.AlreadyFirst, () => RepairFirst(ships)));
            actions.Add(new InspectionAction(RepairTexts.CancelAction, null, true, null, () => Cancel(ships)));
        }

        GameText JobLine(ShipAgent ship, Shipyard yard, int position)
        {
            if (position > 0)
            {
                return RepairTexts.JobWaiting.Format(("position", position + 1), ("yard", yard.Owner.DisplayName));
            }
            return yard.IsShortOfMetal
                ? RepairTexts.JobShortOfMetal
                : RepairTexts.JobActive.Format(("rate", _repairs.HullPerSecondFor(ship)));
        }

        GameText Order(IReadOnlyList<ShipAgent> ships)
        {
            int count = 0;
            foreach (ShipAgent ship in ships)
            {
                count += _repairs.Order(ship) == RepairEligibility.Eligible ? 1 : 0;
            }
            return RepairTexts.Queued.Format(("count", count));
        }

        GameText RepairFirst(IReadOnlyList<ShipAgent> ships)
        {
            // Walk backwards so the first selected ship ends up at the very front.
            int count = 0;
            for (int index = ships.Count - 1; index >= 0; index--)
            {
                count += _repairs.RepairFirst(ships[index]) ? 1 : 0;
            }
            return RepairTexts.MovedToFront.Format(("count", count));
        }

        GameText Cancel(IReadOnlyList<ShipAgent> ships)
        {
            int count = 0;
            foreach (ShipAgent ship in ships)
            {
                count += _repairs.Cancel(ship) ? 1 : 0;
            }
            return RepairTexts.Cancelled.Format(("count", count));
        }
    }
}

using System.Collections.Generic;
using LastFleet.Mining;
using LastFleet.Ships;

namespace LastFleet.Inspection
{
    /// <summary>
    /// The tanker part of the ship window. With a tanker selected it offers to refuel the other
    /// selected capital ships one after another, to fill the tanker's own tank, and to stop refueling.
    /// Right click on a capital ship does the same for a single target.
    /// </summary>
    public static class TankerRefuelingInspection
    {
        public static void CollectActions(IReadOnlyList<ShipAgent> ships, List<InspectionAction> actions)
        {
            int tankers = 0;
            int targets = 0;
            int refueling = 0;
            foreach (ShipAgent ship in ships)
            {
                if (ship.TryGetComponent(out FuelTanker tanker))
                {
                    tankers++;
                    refueling += tanker.IsRefueling ? 1 : 0;
                }
                else if (ship.IsCapital)
                {
                    targets++;
                }
            }
            if (tankers == 0)
            {
                return;
            }

            actions.Add(new InspectionAction(MiningTexts.RefuelAction, null, targets > 0, MiningTexts.RefuelNeedsTarget, () =>
            {
                RefuelOthers(ships);
                return null;
            }));
            actions.Add(new InspectionAction(MiningTexts.RefuelSelfAction, null, true, null, () =>
            {
                RefuelSelf(ships);
                return null;
            }));
            if (refueling > 0)
            {
                actions.Add(new InspectionAction(MiningTexts.StopRefuelingAction, null, true, null, () =>
                {
                    StopRefueling(ships);
                    return null;
                }));
            }
        }

        /// <summary>Every selected tanker refuels the other selected capital ships in selection order.</summary>
        static void RefuelOthers(IReadOnlyList<ShipAgent> ships)
        {
            foreach (ShipAgent ship in ships)
            {
                if (!ship.TryGetComponent(out FuelTanker tanker))
                {
                    continue;
                }
                bool append = false;
                foreach (ShipAgent target in ships)
                {
                    if (target.IsCapital && !target.TryGetComponent(out FuelTanker _) && tanker.Refuel(target, append))
                    {
                        append = true;
                    }
                }
            }
        }

        static void RefuelSelf(IReadOnlyList<ShipAgent> ships)
        {
            foreach (ShipAgent ship in ships)
            {
                if (ship.TryGetComponent(out FuelTanker tanker))
                {
                    tanker.Refuel(ship, false);
                }
            }
        }

        static void StopRefueling(IReadOnlyList<ShipAgent> ships)
        {
            foreach (ShipAgent ship in ships)
            {
                if (ship.TryGetComponent(out FuelTanker tanker))
                {
                    tanker.StopRefueling();
                }
            }
        }
    }
}

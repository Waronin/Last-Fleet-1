using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.ShipControl;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Inspection
{
    /// <summary>
    /// The automatic refueling part of the ship window: one button that steps the threshold of every
    /// selected small ship through off, 15, 30 and 50 %, and a line while a ship is on a refueling trip.
    /// </summary>
    public sealed class FuelReturnInspection
    {
        readonly FuelReturnService _service;

        public FuelReturnInspection(FuelReturnService service)
        {
            _service = service;
        }

        public void Describe(ShipAgent ship, List<GameText> lines)
        {
            FuelReturnState state = _service.StateOf(ship);
            if (state == FuelReturnState.Returning)
            {
                lines.Add(ShipControlTexts.ReturningToRefuelLine);
            }
            else if (state == FuelReturnState.Refueling)
            {
                lines.Add(ShipControlTexts.RefuelingLine);
            }
        }

        public void CollectActions(IReadOnlyList<ShipAgent> ships, List<InspectionAction> actions)
        {
            ShipAgent first = null;
            bool mixed = false;
            foreach (ShipAgent ship in ships)
            {
                if (!FuelReturnService.Applies(ship))
                {
                    continue;
                }
                if (first == null)
                {
                    first = ship;
                }
                else if (!Mathf.Approximately(_service.ThresholdOf(ship), _service.ThresholdOf(first)))
                {
                    mixed = true;
                }
            }
            if (first == null)
            {
                return;
            }

            // Mixed settings show as such; a click gives all of them the step after the first ship's.
            float current = _service.ThresholdOf(first);
            GameText label = mixed
                ? ShipControlTexts.AutoRefuelMixedAction
                : ShipControlTexts.AutoRefuelAction.Format(("percent", Percent(current)));
            actions.Add(new InspectionAction(label, null, true, null, () =>
            {
                float next = RefuelThresholds.Next(current);
                int count = _service.SetThreshold(ships, next);
                return ShipControlTexts.AutoRefuelSet.Format(("percent", Percent(next)), ("count", count));
            }));
        }

        static int Percent(float threshold)
        {
            return Mathf.RoundToInt(threshold * 100f);
        }
    }
}

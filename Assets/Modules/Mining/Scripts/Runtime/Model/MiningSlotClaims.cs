using System;
using System.Collections.Generic;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Which harvester works at which mining slot. A ship holds at most one slot; claiming a new
    /// one releases the old. Claims are session state and vanish with the ships.
    /// </summary>
    public sealed class MiningSlotClaims
    {
        readonly Dictionary<ResourceDeposit, ShipAgent[]> _slotsByDeposit = new Dictionary<ResourceDeposit, ShipAgent[]>();
        readonly Dictionary<ShipAgent, (ResourceDeposit Deposit, int Slot)> _claimByShip =
            new Dictionary<ShipAgent, (ResourceDeposit Deposit, int Slot)>();

        public int FreeSlotCount(ResourceDeposit deposit)
        {
            if (!_slotsByDeposit.TryGetValue(deposit, out ShipAgent[] slots))
            {
                return deposit.MiningSlotCount;
            }

            int free = 0;
            foreach (ShipAgent holder in slots)
            {
                if (holder == null)
                {
                    free++;
                }
            }
            return free;
        }

        /// <summary>Claims the free slot closest to a zone-local position. Depleted deposits have no slots.</summary>
        public bool TryClaim(ResourceDeposit deposit, ShipAgent ship, Vector3 fromLocalPosition, out int slot)
        {
            if (deposit == null)
            {
                throw new ArgumentNullException(nameof(deposit));
            }
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            slot = -1;
            if (deposit.IsDepleted)
            {
                return false;
            }
            if (_claimByShip.TryGetValue(ship, out (ResourceDeposit Deposit, int Slot) current) && current.Deposit == deposit)
            {
                slot = current.Slot;
                return true;
            }

            ShipAgent[] slots = SlotsOf(deposit);
            float bestDistanceSquared = float.MaxValue;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    continue;
                }

                float distanceSquared = (deposit.GetMiningSlotLocalPosition(i) - fromLocalPosition).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    slot = i;
                }
            }
            if (slot < 0)
            {
                return false;
            }

            Release(ship);
            slots[slot] = ship;
            _claimByShip[ship] = (deposit, slot);
            return true;
        }

        public void Release(ShipAgent ship)
        {
            if (!_claimByShip.TryGetValue(ship, out (ResourceDeposit Deposit, int Slot) claim))
            {
                return;
            }

            _claimByShip.Remove(ship);
            _slotsByDeposit[claim.Deposit][claim.Slot] = null;
        }

        ShipAgent[] SlotsOf(ResourceDeposit deposit)
        {
            if (!_slotsByDeposit.TryGetValue(deposit, out ShipAgent[] slots))
            {
                slots = new ShipAgent[deposit.MiningSlotCount];
                _slotsByDeposit.Add(deposit, slots);
            }
            return slots;
        }
    }
}

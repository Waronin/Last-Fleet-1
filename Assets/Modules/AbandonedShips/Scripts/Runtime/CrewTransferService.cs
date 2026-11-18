using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.AbandonedShips
{
    /// <summary>
    /// Brings small ships without crew back into service: one of the fleet's own whose last crew member died, or an
    /// abandoned one found adrift. A selected small ship with at least two aboard flies alongside and one person
    /// climbs over (see <see cref="CrewTransferRules"/>). An own ship simply flies again; a found ship joins the fleet
    /// as a squad of its own, homed on a carrier with room. The hangar fills the remaining posts as usual.
    /// </summary>
    public sealed class CrewTransferService : MonoBehaviour
    {
        [SerializeField] ShipRegistry _fleet;
        [Tooltip("The registry of the stranded side, where found ships wait.")]
        [SerializeField] ShipRegistry _stranded;
        [SerializeField] AbandonedShipBodies _abandoned;

        readonly List<Transfer> _transfers = new List<Transfer>();
        readonly List<ShipAgent> _candidates = new List<ShipAgent>();
        readonly HashSet<ShipAgent> _knownDerelicts = new HashSet<ShipAgent>();
        readonly List<ShipAgent> _gone = new List<ShipAgent>();

        public event Action<GameText> Notice;

        /// <summary>A small win for a success popup: a found ship joined the fleet.</summary>
        public event Action<GameText> Success;

        /// <summary>A found ship joined the fleet.</summary>
        public event Action<ShipAgent> ShipJoined;

        void Awake()
        {
            if (_fleet == null || _stranded == null || _abandoned == null)
            {
                Debug.LogError("CrewTransferService needs the fleet's and the stranded side's registries and the abandoned ship bodies.", this);
                enabled = false;
            }
        }

        /// <summary>A small ship without crew that a crew member could board: an own derelict or a found ship, not docked.</summary>
        public bool CanReceiveCrew(ShipAgent ship)
        {
            if (ship == null || ship.IsCapital || !ship.IsDerelict || ship.IsDocked)
            {
                return false;
            }
            return _fleet.Contains(ship) || _abandoned.TryGetRecord(ship, out AbandonedShip _);
        }

        /// <summary>The nearest ship that can receive crew around a screen point in the given zone.</summary>
        public bool TryPickReceiver(ZoneView zone, Camera camera, Vector2 screenPoint, float radiusPixels, out ShipAgent receiver)
        {
            receiver = null;
            float bestDistanceSquared = radiusPixels * radiusPixels;
            foreach (ShipRegistry side in new[] { _fleet, _stranded })
            {
                side.CollectShipsInZone(zone, _candidates);
                foreach (ShipAgent ship in _candidates)
                {
                    if (!CanReceiveCrew(ship))
                    {
                        continue;
                    }
                    Vector3 screen = camera.WorldToScreenPoint(ship.WorldPosition);
                    float distanceSquared = (new Vector2(screen.x, screen.y) - screenPoint).sqrMagnitude;
                    if (screen.z > 0f && distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        receiver = ship;
                    }
                }
            }
            return receiver != null;
        }

        /// <summary>Sends the nearest selected small ship that can spare a crew member to the receiver.</summary>
        public bool OrderTransfer(IReadOnlyList<ShipAgent> selection, ShipAgent receiver)
        {
            if (!CanReceiveCrew(receiver))
            {
                return false;
            }

            ShipAgent donor = null;
            float bestDistance = float.PositiveInfinity;
            foreach (ShipAgent ship in selection)
            {
                if (ship == receiver || ship.IsCapital || ship.IsDocked || ship.Crew == null || !CrewTransferRules.CanDonate(ship.Crew.Complement.Count))
                {
                    continue;
                }
                float distance = ship.Zone == receiver.Zone ? Vector3.Distance(ship.WorldPosition, receiver.WorldPosition) : float.MaxValue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    donor = ship;
                }
            }
            if (donor == null)
            {
                Notice?.Invoke(AbandonedShipTexts.NoDonor);
                return false;
            }

            CancelTransferOf(donor);
            donor.Order(ShipOrder.Follow(receiver, new Vector3(CrewTransferRules.SideOffsetUnits, 0f, 0f)), false);
            Transfer transfer = new Transfer(donor, receiver);
            donor.OrdersReplaced += transfer.OnDonorReordered;
            _transfers.Add(transfer);
            Notice?.Invoke(AbandonedShipTexts.TransferOrdered.Format(("donor", donor.DisplayName), ("ship", NameOf(receiver))));
            return true;
        }

        void Update()
        {
            AnnounceNewDerelicts();
            for (int i = _transfers.Count - 1; i >= 0; i--)
            {
                Transfer transfer = _transfers[i];
                if (transfer.IsCancelled || transfer.Donor == null || transfer.Donor.IsDerelict || !CrewTransferRules.CanDonate(transfer.Donor.Crew.Complement.Count))
                {
                    End(i, transfer.Receiver != null && transfer.Receiver.IsDerelict);
                    continue;
                }
                if (transfer.Receiver == null || !transfer.Receiver.IsDerelict)
                {
                    // Lost, or someone else got there first.
                    End(i, false);
                    continue;
                }
                if (transfer.Donor.Zone == transfer.Receiver.Zone &&
                    CrewTransferRules.IsInRange(Vector3.Distance(transfer.Donor.WorldPosition, transfer.Receiver.WorldPosition)))
                {
                    Complete(transfer);
                    End(i, false);
                }
            }
        }

        void Complete(Transfer transfer)
        {
            ShipAgent donor = transfer.Donor;
            ShipAgent receiver = transfer.Receiver;
            donor.OrdersReplaced -= transfer.OnDonorReordered;
            // The donor stops beside the ship it saved.
            donor.Order(ShipOrder.MoveTo(donor.Zone.Record.PointOfInterestIndex, donor.Zone.transform.InverseTransformPoint(donor.WorldPosition)), false);
            receiver.Crew.Complement.Add(donor.Crew.Complement.Remove(1));
            _knownDerelicts.Remove(receiver);

            if (!_abandoned.TryGetRecord(receiver, out AbandonedShip _))
            {
                Notice?.Invoke(AbandonedShipTexts.Recrewed.Format(("donor", donor.DisplayName), ("ship", receiver.DisplayName)));
                return;
            }

            _abandoned.Release(receiver);
            _stranded.TransferTo(receiver, _fleet);
            Hangar home = _fleet.FindHangarFor(receiver);
            _fleet.FormSquad(new[] { receiver }, home);
            Notice?.Invoke(home != null
                ? AbandonedShipTexts.Recovered.Format(("ship", receiver.DisplayName), ("home", home.Carrier.DisplayName))
                : AbandonedShipTexts.RecoveredWithoutHome.Format(("ship", receiver.DisplayName)));
            Success?.Invoke(ShipTexts.JoinedFleet.Format(("ship", receiver.DisplayName)));
            ShipJoined?.Invoke(receiver);
        }

        /// <summary>Tells the player once when one of the fleet's small ships loses its last crew member.</summary>
        void AnnounceNewDerelicts()
        {
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (!ship.IsCapital && ship.IsDerelict && _knownDerelicts.Add(ship))
                {
                    Notice?.Invoke(AbandonedShipTexts.CrewGone.Format(("ship", ship.DisplayName)));
                }
            }
            _gone.Clear();
            foreach (ShipAgent ship in _knownDerelicts)
            {
                if (ship == null || !ship.IsDerelict || !_fleet.Contains(ship))
                {
                    _gone.Add(ship);
                }
            }
            foreach (ShipAgent ship in _gone)
            {
                _knownDerelicts.Remove(ship);
            }
        }

        void CancelTransferOf(ShipAgent donor)
        {
            for (int i = _transfers.Count - 1; i >= 0; i--)
            {
                if (_transfers[i].Donor == donor)
                {
                    End(i, false);
                }
            }
        }

        void End(int index, bool tellPlayer)
        {
            Transfer transfer = _transfers[index];
            if (transfer.Donor != null)
            {
                transfer.Donor.OrdersReplaced -= transfer.OnDonorReordered;
            }
            _transfers.RemoveAt(index);
            if (tellPlayer)
            {
                Notice?.Invoke(AbandonedShipTexts.TransferAbandoned.Format(("ship", NameOf(transfer.Receiver))));
            }
        }

        /// <summary>A found ship is named by its class until it joins; the stranded side would repeat the fleet's numbers.</summary>
        GameText NameOf(ShipAgent ship)
        {
            return _abandoned.TryGetRecord(ship, out AbandonedShip _)
                ? AbandonedShipTexts.FoundShipName.Format(("class", ship.ClassDisplayName))
                : ship.DisplayName;
        }

        sealed class Transfer
        {
            public Transfer(ShipAgent donor, ShipAgent receiver)
            {
                Donor = donor;
                Receiver = receiver;
            }

            public ShipAgent Donor { get; }
            public ShipAgent Receiver { get; }

            /// <summary>The player gave the donor another order; it no longer flies to the receiver.</summary>
            public bool IsCancelled { get; private set; }

            public void OnDonorReordered(ShipAgent ship)
            {
                IsCancelled = true;
            }
        }
    }
}

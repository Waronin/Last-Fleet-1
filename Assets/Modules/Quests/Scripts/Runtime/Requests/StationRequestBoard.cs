using System;
using System.Collections.Generic;
using LastFleet.Dialogue;
using LastFleet.Mining;
using LastFleet.Rewards;
using LastFleet.Shared.Randomness;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// The small quests of the run: when a zone is revealed, its inhabited stations may ask for a delivery
    /// (<see cref="SupplyRequestRules"/>), told by hail or a docked crew. Once accepted, the next fleet ship
    /// docked there hands the goods over from the industrial ships' stores, as soon as the fleet has
    /// enough, and the station pays. Jumping to another system lets open requests expire.
    /// </summary>
    public sealed class StationRequestBoard : MonoBehaviour
    {
        const ulong RequestSalt = 9000UL;

        [SerializeField] ZoneWorld _zoneWorld;
        [SerializeField] ShipRegistry _fleet;
        [SerializeField] DialogueService _dialogue;
        [SerializeField] RewardDelivery _rewards;

        readonly List<StationRequestEntry> _entries = new List<StationRequestEntry>();
        readonly List<StationRequestEntry> _accepted = new List<StationRequestEntry>();
        FleetSupplies _supplies;
        SystemZones _system;

        /// <summary>Accepted requests waiting for their delivery, for the quest log.</summary>
        public IReadOnlyList<StationRequestEntry> Accepted => _accepted;

        public event Action<GameText> Notice;

        /// <summary>The fleet's stock of a supply, to show how far a request is covered.</summary>
        public float StockOf(Supply supply)
        {
            return Supplies.AmountOf(supply);
        }

        FleetSupplies Supplies => _supplies ??= new FleetSupplies(_fleet);

        void Awake()
        {
            if (_zoneWorld == null || _fleet == null || _dialogue == null || _rewards == null)
            {
                Debug.LogError("StationRequestBoard needs the zone world, the fleet's ship registry, the dialogue service and the reward delivery.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _zoneWorld.ZoneRevealed += OnZoneRevealed;
        }

        void OnDisable()
        {
            _zoneWorld.ZoneRevealed -= OnZoneRevealed;
        }

        void Update()
        {
            if (_zoneWorld.CurrentSystem != _system)
            {
                _system = _zoneWorld.CurrentSystem;
                ExpireOutside(_system);
            }

            _accepted.Clear();
            foreach (StationRequestEntry entry in _entries)
            {
                if (entry.Request.State == SupplyRequestState.Accepted)
                {
                    TryDeliver(entry);
                }
                if (entry.Request.State == SupplyRequestState.Accepted)
                {
                    _accepted.Add(entry);
                }
            }
        }

        void OnZoneRevealed(ZoneView zone)
        {
            IReadOnlyList<ZoneStation> stations = zone.Record.Contents.Stations;
            for (int i = 0; i < stations.Count; i++)
            {
                ZoneStation station = stations[i];
                // A station that already has someone to talk to, such as a quest giver, keeps them.
                if (station.Contact != null || station.IsDestroyed)
                {
                    continue;
                }
                DeterministicRandom random = new DeterministicRandom(DeterministicRandom.DeriveSeed(zone.Record.Seed, RequestSalt + (ulong)i));
                SupplyRequest request = SupplyRequestRules.Roll(random, station.Profile);
                if (request == null)
                {
                    continue;
                }
                SupplyRequestContact contact = new SupplyRequestContact(_dialogue, request, IsCovered);
                station.SetContact(contact);
                _entries.Add(new StationRequestEntry(station, request, contact, _zoneWorld.CurrentSystem));
            }
        }

        /// <summary>A docked fleet ship hands the goods over once the stores hold enough; the station pays at once.</summary>
        void TryDeliver(StationRequestEntry entry)
        {
            SupplyRequest request = entry.Request;
            if (!request.CanBeFulfilledFrom(Supplies.AmountOf(request.Wanted)) || !HasDockedShip(entry.Station))
            {
                return;
            }

            Supplies.Take(request.Wanted, request.WantedAmount);
            request.Fulfil();
            if (request.RewardKind == SupplyRequestRewardKind.UpgradeModule)
            {
                _rewards.Inventory.Add(ItemKind.UpgradeModule, 1);
            }
            else
            {
                float stored = Supplies.Store(request.RewardSupply, request.RewardAmount, null);
                if (stored < request.RewardAmount - 0.5f)
                {
                    Notice?.Invoke(QuestTexts.RequestRewardOverflow.Format(("station", entry.Station.Name),
                        ("lost", Mathf.RoundToInt(request.RewardAmount - stored)), ("supply", MiningTexts.Supplies.For(request.RewardSupply))));
                }
            }
            GameText reward = SupplyRequestContact.RewardText(request);
            Notice?.Invoke(QuestTexts.RequestFulfilled.Format(("station", entry.Station.Name), ("reward", reward)));
            _dialogue.Open(DialogueBuilder.Message(SupplyRequestContact.Speaker(entry.Station), QuestTexts.RequestThanks.Format(("reward", reward))));
            ReleaseContact(entry);
        }

        bool IsCovered(SupplyRequest request)
        {
            return Supplies.AmountOf(request.Wanted) >= request.WantedAmount;
        }

        bool HasDockedShip(ZoneStation station)
        {
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (ship.TryGetComponent(out StationDocking docking) && docking.State == DockingState.Docked && docking.Station == station &&
                    !ship.IsDerelict)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Requests only hold while the fleet is in their system; accepted ones are reported as lost.</summary>
        void ExpireOutside(SystemZones system)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                StationRequestEntry entry = _entries[i];
                if (entry.System == system && entry.Request.IsOpen)
                {
                    continue;
                }
                if (entry.Request.State == SupplyRequestState.Accepted)
                {
                    Notice?.Invoke(QuestTexts.RequestExpired.Format(("station", entry.Station.Name)));
                }
                entry.Request.Expire();
                ReleaseContact(entry);
                _entries.RemoveAt(i);
            }
        }

        static void ReleaseContact(StationRequestEntry entry)
        {
            if (entry.Station.Contact == entry.Contact)
            {
                entry.Station.SetContact(null);
            }
        }
    }
}

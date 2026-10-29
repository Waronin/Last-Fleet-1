using System;
using System.Collections.Generic;
using LastFleet.People;
using LastFleet.Rewards;
using LastFleet.Shared.Randomness;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Stations
{
    /// <summary>
    /// Keeps what is left to find aboard each station and hands the finds to the ships searching it:
    /// survivors take seats aboard, everything else goes into the ship's hold until it lands. Every
    /// docked ship at a station joins the search and draws from the same supply.
    /// </summary>
    public sealed class SearchService : MonoBehaviour
    {
        const ulong LootSalt = 5000UL;

        [SerializeField] DockingService _docking;

        readonly Dictionary<ZoneStation, StationLoot> _lootByStation = new Dictionary<ZoneStation, StationLoot>();
        readonly List<ShipAgent> _assigned = new List<ShipAgent>();

        /// <summary>Short feedback for the player about the search and its finds.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_docking == null)
            {
                Debug.LogError("SearchService needs the docking service.", this);
                enabled = false;
            }
        }

        /// <summary>The station's supply of finds, rolled from the zone seed when first needed.</summary>
        public StationLoot LootOf(ZoneView zone, ZoneStation station)
        {
            if (!_lootByStation.TryGetValue(station, out StationLoot loot))
            {
                int index = IndexOf(zone.Record.Contents.Stations, station);
                ulong seed = DeterministicRandom.DeriveSeed(zone.Record.Seed, LootSalt + (ulong)index);
                loot = new StationLoot(StationSearchRules.RollFinds(seed, station.Type, station.Profile.Condition));
                _lootByStation.Add(station, loot);
            }
            return loot;
        }

        /// <summary>Ships docked at the station; only they can search it.</summary>
        public void CollectDockedShips(ZoneStation station, List<ShipAgent> ships)
        {
            _docking.CollectAssignedShips(station, _assigned);
            ships.Clear();
            foreach (ShipAgent ship in _assigned)
            {
                if (ship.TryGetComponent(out StationDocking docking) && docking.State == DockingState.Docked)
                {
                    ships.Add(ship);
                }
            }
        }

        /// <summary>Sends the crews of every ship docked at the station to search it; returns feedback.</summary>
        public GameText RequestSearch(ZoneView zone, ZoneStation station)
        {
            switch (StationSearchRules.Blocker(station.Profile))
            {
                case SearchBlocker.Inhabited:
                    return StationTexts.SearchBlockedInhabited.Format(("station", station.Name));
                case SearchBlocker.ToxicAtmosphere:
                    return StationTexts.SearchBlockedToxic.Format(("station", station.Name));
                case SearchBlocker.Radiation:
                    return StationTexts.SearchBlockedRadiation.Format(("station", station.Name));
            }

            StationLoot loot = LootOf(zone, station);
            if (loot.IsEmpty)
            {
                return StationTexts.NothingLeft.Format(("station", station.Name));
            }

            List<ShipAgent> docked = new List<ShipAgent>();
            CollectDockedShips(station, docked);
            int started = 0;
            foreach (ShipAgent ship in docked)
            {
                if (ship.TryGetComponent(out StationSearching searching) && !ship.IsDerelict)
                {
                    searching.Begin(this, station);
                    started++;
                }
            }
            return started > 0
                ? StationTexts.SearchStarted.Format(("count", started), ("station", station.Name))
                : StationTexts.NoShipDocked.Format(("station", station.Name));
        }

        /// <summary>Called by a searching ship each time its crew turns something up.</summary>
        internal void TakeFind(StationSearching searching)
        {
            if (!_lootByStation.TryGetValue(searching.Station, out StationLoot loot) || !loot.TryPeek(out Reward next))
            {
                Finish(searching);
                return;
            }

            ShipAgent ship = searching.Ship;
            float taken = next.Kind == RewardKind.Survivors ? BoardSurvivors(ship, next) : Stow(ship, next);
            if (taken <= 0f)
            {
                // Survivors without a free seat wait for the next ship; the search goes on with the rest.
                loot.Postpone();
                if (OnlySurvivorsLeft(loot))
                {
                    Notice?.Invoke(StationTexts.NoSeatsForSurvivors.Format(("ship", ship.DisplayName), ("station", searching.Station.Name)));
                    searching.Stop();
                }
                return;
            }

            loot.Take(taken);
            Notice?.Invoke(StationTexts.Found.Format(("ship", ship.DisplayName), ("station", searching.Station.Name),
                ("reward", RewardTexts.Describe(next.WithAmount(taken)))));
            if (loot.IsEmpty)
            {
                Finish(searching);
            }
        }

        static float BoardSurvivors(ShipAgent ship, Reward survivors)
        {
            return ship.TryGetComponent(out ShipPassengers seats) ? seats.Quarters.Add(survivors.People) : 0f;
        }

        static float Stow(ShipAgent ship, Reward reward)
        {
            if (!ship.TryGetComponent(out ShipRewardHold hold))
            {
                return 0f;
            }
            hold.Hold.Add(reward);
            return reward.Amount;
        }

        static bool OnlySurvivorsLeft(StationLoot loot)
        {
            return loot.TryPeek(out Reward next) && next.Kind == RewardKind.Survivors && loot.RemainingCount == loot.CountOf(RewardKind.Survivors);
        }

        /// <summary>The station is empty: every ship searching it stops.</summary>
        void Finish(StationSearching searching)
        {
            ZoneStation station = searching.Station;
            Notice?.Invoke(StationTexts.SearchedCompletely.Format(("station", station.Name)));
            CollectDockedShips(station, _assigned);
            foreach (ShipAgent ship in _assigned)
            {
                if (ship.TryGetComponent(out StationSearching other) && other.Station == station)
                {
                    other.Stop();
                }
            }
            searching.Stop();
        }

        static int IndexOf(IReadOnlyList<ZoneStation> stations, ZoneStation station)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                if (stations[i] == station)
                {
                    return i;
                }
            }
            throw new ArgumentException($"{station.Name} is not in this zone.", nameof(station));
        }
    }
}

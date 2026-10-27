using System;
using System.Collections.Generic;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Mining
{
    /// <summary>
    /// Scene-wide mining services: hands mining slots to harvesters, finds the next deposit when one
    /// is full or exhausted (GDD: the ship looks for the next spot in the same zone) and finds where
    /// cargo is delivered.
    /// </summary>
    public sealed class MiningCoordinator : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;

        readonly MiningSlotClaims _claims = new MiningSlotClaims();
        readonly List<(ResourceDeposit Deposit, float DistanceSquared)> _candidates = new List<(ResourceDeposit Deposit, float DistanceSquared)>();
        readonly List<ShipAgent> _harvesters = new List<ShipAgent>();
        readonly List<ShipAgent> _harvesterCandidates = new List<ShipAgent>();

        public MiningSlotClaims Claims => _claims;

        /// <summary>Short feedback for the player, e.g. when a harvester runs out of work.</summary>
        public event Action<string> Notice;

        void Awake()
        {
            if (_registry == null)
            {
                Debug.LogError("MiningCoordinator needs the ship registry.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _registry.ShipSpawned += OnShipSpawned;
            _registry.ShipRemoved += _claims.Release;
            foreach (ShipAgent ship in _registry.Ships)
            {
                OnShipSpawned(ship);
            }
        }

        void OnDisable()
        {
            _registry.ShipSpawned -= OnShipSpawned;
            _registry.ShipRemoved -= _claims.Release;
        }

        /// <summary>
        /// Claims a slot, trying the preferred deposit first, then the nearest deposits of the preferred
        /// resource, then any deposit in the zone.
        /// </summary>
        public bool TryFindDeposit(ZoneView zone, ResourceDeposit preferred, ResourceKind preferredResource, ShipAgent ship,
            out ResourceDeposit deposit, out int slot)
        {
            Vector3 from = zone.transform.InverseTransformPoint(ship.WorldPosition);
            if (preferred != null && _claims.TryClaim(preferred, ship, from, out slot))
            {
                deposit = preferred;
                return true;
            }

            _candidates.Clear();
            foreach (ResourceDeposit candidate in zone.Record.Contents.Deposits)
            {
                if (!candidate.IsDepleted && _claims.FreeSlotCount(candidate) > 0)
                {
                    _candidates.Add((candidate, (candidate.LocalPosition - from).sqrMagnitude));
                }
            }
            _candidates.Sort((a, b) => a.DistanceSquared.CompareTo(b.DistanceSquared));

            for (int pass = 0; pass < 2; pass++)
            {
                foreach ((ResourceDeposit candidate, float _) in _candidates)
                {
                    bool matches = candidate.Resource == preferredResource;
                    if ((pass == 0) == matches && _claims.TryClaim(candidate, ship, from, out slot))
                    {
                        deposit = candidate;
                        return true;
                    }
                }
            }

            deposit = null;
            slot = -1;
            return false;
        }

        /// <summary>
        /// Puts harvesters to work on a deposit and returns how many started. Selected harvesters are
        /// used if there are any; otherwise free harvesters are called, then the nearest busy ones,
        /// as many as the deposit has free slots.
        /// </summary>
        public int AssignHarvesters(ZoneView zone, ResourceDeposit deposit, IReadOnlyList<ShipAgent> selectedShips)
        {
            _harvesters.Clear();
            foreach (ShipAgent ship in selectedShips)
            {
                if (ship.TryGetComponent(out HarvesterMining _))
                {
                    _harvesters.Add(ship);
                }
            }

            if (_harvesters.Count == 0)
            {
                _harvesterCandidates.Clear();
                foreach (ShipAgent ship in _registry.Ships)
                {
                    if (ship.TryGetComponent(out HarvesterMining mining) && mining.Deposit != deposit)
                    {
                        _harvesterCandidates.Add(ship);
                    }
                }
                int zoneIndex = zone.Record.PointOfInterestIndex;
                Vector2 zonePositionAU = zone.Record.PointOfInterest.PositionAU;
                ShipAvailability.PickFreeThenNearest(_harvesterCandidates, ShipAvailability.IsFree,
                    ship => ShipAvailability.TravelCost(ship, zoneIndex, zonePositionAU, deposit.LocalPosition),
                    _claims.FreeSlotCount(deposit), _harvesters);
            }

            int started = 0;
            foreach (ShipAgent harvester in _harvesters)
            {
                if (harvester.GetComponent<HarvesterMining>().StartMining(zone, deposit))
                {
                    started++;
                }
            }
            return started;
        }

        /// <summary>The home carrier's storage if it has room, otherwise any storage with room.</summary>
        public RawMaterialStorage FindStorage(ShipAgent ship, ResourceKind resource)
        {
            if (ship.HomeHangar != null && ship.HomeHangar.TryGetComponent(out RawMaterialStorage home) &&
                home.Stock.FreeSpaceFor(resource) > 0f)
            {
                return home;
            }

            foreach (ShipAgent candidate in _registry.Ships)
            {
                if (candidate.TryGetComponent(out RawMaterialStorage storage) && storage.Stock.FreeSpaceFor(resource) > 0f)
                {
                    return storage;
                }
            }
            return null;
        }

        public void Report(string message)
        {
            Notice?.Invoke(message);
        }

        void OnShipSpawned(ShipAgent ship)
        {
            if (ship.TryGetComponent(out HarvesterMining mining))
            {
                mining.Initialize(this);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.StarSystem;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Exploration
{
    /// <summary>
    /// Sends ships to find things out: a ship to scan a deposit, or a ship to explore an unknown zone.
    /// It picks a free small ship first, otherwise the nearest one, and never sends a second ship for
    /// the same deposit.
    /// </summary>
    public sealed class ScoutingService : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;

        readonly List<ShipAgent> _candidates = new List<ShipAgent>();
        readonly List<ShipAgent> _picked = new List<ShipAgent>(1);

        /// <summary>Short feedback for the player about dispatched and finished tasks.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_registry == null)
            {
                Debug.LogError("ScoutingService needs the ship registry.", this);
                enabled = false;
            }
        }

        /// <summary>The scanner currently assigned to the deposit, if any.</summary>
        public bool TryGetActiveScan(ResourceDeposit deposit, out ShipScanner scanner)
        {
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (ship.TryGetComponent(out scanner) && scanner.State != ScanTaskState.Inactive && scanner.Target == deposit)
                {
                    return true;
                }
            }
            scanner = null;
            return false;
        }

        public bool RequestScan(ZoneView zone, ResourceDeposit deposit)
        {
            if (deposit.IsScanned || TryGetActiveScan(deposit, out ShipScanner _))
            {
                return false;
            }

            _candidates.Clear();
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (!ship.IsCapital && ship.TryGetComponent(out ShipScanner _))
                {
                    _candidates.Add(ship);
                }
            }

            int zoneIndex = zone.Record.PointOfInterestIndex;
            Vector2 zonePositionAU = zone.Record.PointOfInterest.PositionAU;
            ShipAvailability.PickFreeThenNearest(_candidates, ShipAvailability.IsFree,
                ship => ShipAvailability.TravelCost(ship, zoneIndex, zonePositionAU, deposit.LocalPosition), 1, _picked);
            if (_picked.Count == 0)
            {
                Notice?.Invoke(ExplorationTexts.NoShipToScan);
                return false;
            }

            ShipScanner scanner = _picked[0].GetComponent<ShipScanner>();
            scanner.ScanCompleted -= OnScanCompleted;
            scanner.ScanCompleted += OnScanCompleted;
            scanner.BeginScan(zone, deposit);
            Notice?.Invoke(ExplorationTexts.ScanDispatched.Format(("ship", scanner.Ship.name)));
            return true;
        }

        /// <summary>Sends a small ship to the center of a zone; arriving there reveals the zone.</summary>
        public bool RequestExploration(int zoneIndex, PointOfInterest pointOfInterest)
        {
            _candidates.Clear();
            foreach (ShipAgent ship in _registry.Ships)
            {
                if (!ship.IsCapital)
                {
                    _candidates.Add(ship);
                }
            }

            ShipAvailability.PickFreeThenNearest(_candidates, ShipAvailability.IsFree,
                ship => ShipAvailability.TravelCost(ship, zoneIndex, pointOfInterest.PositionAU, Vector3.zero), 1, _picked);
            if (_picked.Count == 0)
            {
                Notice?.Invoke(ExplorationTexts.NoShipToExplore);
                return false;
            }

            _picked[0].Order(ShipOrder.MoveTo(zoneIndex, Vector3.zero), false);
            Notice?.Invoke(ExplorationTexts.ExplorationDispatched.Format(("ship", _picked[0].name), ("zone", pointOfInterest.Name)));
            return true;
        }

        void OnScanCompleted(ShipScanner scanner, ResourceDeposit deposit)
        {
            scanner.ScanCompleted -= OnScanCompleted;
            Notice?.Invoke(ExplorationTexts.ScanFinished.Format(("ship", scanner.Ship.name), ("size", ZoneTexts.SizeClasses.For(deposit.SizeClass)),
                ("shape", ZoneTexts.Shapes.For(deposit.Shape)), ("resource", ZoneTexts.Resources.For(deposit.Resource))));
        }
    }
}

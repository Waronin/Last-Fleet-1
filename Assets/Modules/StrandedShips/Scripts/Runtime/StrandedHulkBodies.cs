using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.StrandedShips
{
    /// <summary>
    /// Gives every stranded ship in a shown zone a hull in combat: a ship of the stranded side that never
    /// moves, stands in for the hulk's station and carries its hull (see <see cref="FactionRelations"/>: the
    /// Pronin hunt it like the fleet). Hits on it wear down the stranded hull and so undo repairs; repairs
    /// show on it. Shot to pieces, the hulk explodes and leaves wrecks like any capital ship, the survivors
    /// aboard die and docked ships cast off. A recovered hulk loses its stand-in; leaving the system removes
    /// the stand-ins with the zones, and coming back brings them back from the hull's state.
    /// </summary>
    public sealed class StrandedHulkBodies : MonoBehaviour
    {
        const float SpawnCheckIntervalSeconds = 0.5f;
        const float HullSyncTolerancePoints = 0.01f;

        [SerializeField] ZoneWorld _zoneWorld;
        [Tooltip("The registry of the stranded side.")]
        [SerializeField] ShipRegistry _stranded;
        [SerializeField] StrandedShipRecovery _recovery;
        [SerializeField] DockingService _docking;
        [SerializeField] ShipAgent _hulkPrefab;

        readonly Dictionary<ZoneStation, ShipAgent> _bodies = new Dictionary<ZoneStation, ShipAgent>();
        readonly Dictionary<ShipAgent, ZoneStation> _stations = new Dictionary<ShipAgent, ZoneStation>();
        readonly List<ZoneStation> _released = new List<ZoneStation>();
        readonly List<ShipAgent> _holders = new List<ShipAgent>();
        float _secondsUntilSpawnCheck;

        /// <summary>Short feedback for the player, e.g. a hulk destroyed with the survivors aboard.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_zoneWorld == null || _stranded == null || _recovery == null || _docking == null || _hulkPrefab == null)
            {
                Debug.LogError("StrandedHulkBodies needs the zone world, the stranded side's registry, recovery, docking and the hulk prefab.", this);
                enabled = false;
                return;
            }
            if (_stranded.Allegiance != Allegiance.Stranded)
            {
                Debug.LogError("StrandedHulkBodies needs the registry of the stranded side.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _stranded.ShipLost += OnHulkLost;
        }

        void OnDisable()
        {
            _stranded.ShipLost -= OnHulkLost;
        }

        /// <summary>The stand-in of a hulk while its zone is shown, e.g. to point at it in tests or quests.</summary>
        public bool TryGetBody(ZoneStation station, out ShipAgent body)
        {
            return _bodies.TryGetValue(station, out body) && body != null;
        }

        void Update()
        {
            ReleaseGoneBodies();
            SyncRepairs();

            _secondsUntilSpawnCheck -= Time.deltaTime;
            if (_secondsUntilSpawnCheck > 0f)
            {
                return;
            }
            _secondsUntilSpawnCheck = SpawnCheckIntervalSeconds;
            SpawnMissingBodies();
        }

        /// <summary>Drops stand-ins whose zone was cleared, and removes those of hulks that joined the fleet.</summary>
        void ReleaseGoneBodies()
        {
            _released.Clear();
            foreach (KeyValuePair<ZoneStation, ShipAgent> entry in _bodies)
            {
                if (entry.Value == null || entry.Key.IsGone)
                {
                    _released.Add(entry.Key);
                }
            }
            foreach (ZoneStation station in _released)
            {
                ShipAgent body = _bodies[station];
                _bodies.Remove(station);
                // The key is still the same C# object after Unity destroyed it.
                _stations.Remove(body);
                if (body != null)
                {
                    body.GetComponent<ShipDurability>().Damaged -= OnBodyDamaged;
                    // Gone without a loss: the registry drops it when its GameObject is destroyed.
                    Destroy(body.gameObject);
                }
            }
        }

        /// <summary>Repairs raise the stranded hull; the stand-in follows.</summary>
        void SyncRepairs()
        {
            foreach (KeyValuePair<ZoneStation, ShipAgent> entry in _bodies)
            {
                ShipHull hull = entry.Value.GetComponent<ShipDurability>().Hull;
                float missing = entry.Key.StrandedHull.Fraction * hull.Maximum - hull.Current;
                if (missing > HullSyncTolerancePoints)
                {
                    hull.Repair(missing);
                }
            }
        }

        void SpawnMissingBodies()
        {
            SystemZones system = _zoneWorld.CurrentSystem;
            if (system == null)
            {
                return;
            }
            foreach (ZoneRecord record in system.Zones)
            {
                if (!record.IsRevealed || !_zoneWorld.TryGetView(record.PointOfInterestIndex, out ZoneView zone))
                {
                    continue;
                }
                foreach (ZoneStation station in record.Contents.Stations)
                {
                    if (station.IsStrandedShip && !station.IsGone && !_bodies.ContainsKey(station))
                    {
                        Spawn(zone, station);
                    }
                }
            }
        }

        void Spawn(ZoneView zone, ZoneStation station)
        {
            StrandedRepairPlan plan = _recovery.PlanFor(station);
            if (plan == null)
            {
                return;
            }
            ShipAgent body = _stranded.Spawn(_hulkPrefab, zone, station.LocalPosition, station.HeadingDegrees);
            body.StandInFor(station);
            ShipDurability durability = body.GetComponent<ShipDurability>();
            durability.InitializeHull(plan.MaximumHullPoints, station.StrandedHull.Fraction * plan.MaximumHullPoints);
            durability.Damaged += OnBodyDamaged;
            _bodies.Add(station, body);
            _stations.Add(body, station);
        }

        void OnBodyDamaged(ShipDurability durability, float damage, GameText cause)
        {
            if (_stations.TryGetValue(durability.GetComponent<ShipAgent>(), out ZoneStation station))
            {
                station.StrandedHull.TakeDamage(damage / durability.Hull.Maximum);
            }
        }

        /// <summary>The hulk was shot to pieces: the stand-in leaves its wrecks and explosion, the station goes.</summary>
        void OnHulkLost(ShipAgent body, GameText cause)
        {
            if (!_stations.TryGetValue(body, out ZoneStation station))
            {
                return;
            }
            body.GetComponent<ShipDurability>().Damaged -= OnBodyDamaged;
            _stations.Remove(body);
            _bodies.Remove(station);

            _docking.CollectAssignedShips(station, _holders);
            foreach (ShipAgent holder in _holders)
            {
                holder.GetComponent<StationDocking>().Undock();
            }
            station.MarkDestroyed();
            int deaths = station.Residents.PerishAll();
            if (body.Zone != null)
            {
                body.Zone.RemoveStationView(station);
            }
            Notice?.Invoke(StrandedShipTexts.HulkDestroyed.Format(("station", station.Name), ("count", deaths), ("cause", cause)));
        }
    }
}

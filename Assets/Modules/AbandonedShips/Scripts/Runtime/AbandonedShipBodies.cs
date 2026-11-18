using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.AbandonedShips
{
    /// <summary>
    /// Puts the abandoned small ships of every shown zone into space as real ships of the stranded side, without
    /// crew, damaged and nearly dry (see <see cref="AbandonedShip"/>). The Pronin hunt them like stranded hulks.
    /// Hits are written back to the record; leaving the system removes the ships with their zones, and coming back
    /// brings them back as they were. A recovered ship is handed to the fleet and no longer kept here.
    /// </summary>
    public sealed class AbandonedShipBodies : MonoBehaviour
    {
        const float SpawnCheckIntervalSeconds = 0.5f;

        [SerializeField] ZoneWorld _zoneWorld;
        [Tooltip("The registry of the stranded side.")]
        [SerializeField] ShipRegistry _stranded;
        [SerializeField] ShipAgent _shuttlePrefab;
        [SerializeField] ShipAgent _harvesterPrefab;
        [SerializeField] ShipAgent _fighterPrefab;

        readonly Dictionary<AbandonedShip, ShipAgent> _bodies = new Dictionary<AbandonedShip, ShipAgent>();
        readonly Dictionary<ShipAgent, AbandonedShip> _records = new Dictionary<ShipAgent, AbandonedShip>();
        readonly HashSet<AbandonedShip> _announced = new HashSet<AbandonedShip>();
        readonly List<AbandonedShip> _released = new List<AbandonedShip>();
        float _secondsUntilSpawnCheck;

        /// <summary>Short feedback for the player, e.g. an abandoned ship spotted in a zone.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_zoneWorld == null || _stranded == null || _shuttlePrefab == null || _harvesterPrefab == null || _fighterPrefab == null)
            {
                Debug.LogError("AbandonedShipBodies needs the zone world, the stranded side's registry and the small ship prefabs.", this);
                enabled = false;
                return;
            }
            if (_stranded.Allegiance != Allegiance.Stranded)
            {
                Debug.LogError("AbandonedShipBodies needs the registry of the stranded side.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _stranded.ShipLost += OnShipLost;
        }

        void OnDisable()
        {
            _stranded.ShipLost -= OnShipLost;
        }

        /// <summary>The record behind a ship of the stranded side, if it is an abandoned small ship.</summary>
        public bool TryGetRecord(ShipAgent ship, out AbandonedShip record)
        {
            return _records.TryGetValue(ship, out record);
        }

        /// <summary>The ship has a crew now: it leaves the stranded side and is no longer kept here.</summary>
        public void Release(ShipAgent ship)
        {
            if (!_records.TryGetValue(ship, out AbandonedShip record))
            {
                throw new ArgumentException($"{ship.name} is not an abandoned ship of a shown zone.", nameof(ship));
            }
            Forget(record, ship);
            record.MarkRecovered();
        }

        void Update()
        {
            ReleaseGoneBodies();
            _secondsUntilSpawnCheck -= Time.deltaTime;
            if (_secondsUntilSpawnCheck > 0f)
            {
                return;
            }
            _secondsUntilSpawnCheck = SpawnCheckIntervalSeconds;
            SpawnMissingBodies();
        }

        /// <summary>Forgets ships destroyed with their zone, e.g. when the fleet left the system.</summary>
        void ReleaseGoneBodies()
        {
            _released.Clear();
            foreach (KeyValuePair<AbandonedShip, ShipAgent> entry in _bodies)
            {
                if (entry.Value == null)
                {
                    _released.Add(entry.Key);
                }
            }
            foreach (AbandonedShip record in _released)
            {
                // The key is still the same C# object after Unity destroyed the ship.
                Forget(record, _bodies[record]);
            }
        }

        void SpawnMissingBodies()
        {
            SystemZones system = _zoneWorld.CurrentSystem;
            if (system == null)
            {
                return;
            }
            foreach (ZoneRecord zoneRecord in system.Zones)
            {
                if (!zoneRecord.IsRevealed || !_zoneWorld.TryGetView(zoneRecord.PointOfInterestIndex, out ZoneView zone))
                {
                    continue;
                }
                foreach (AbandonedShip record in zoneRecord.Contents.AbandonedShips)
                {
                    if (!record.IsGone && !_bodies.ContainsKey(record))
                    {
                        Spawn(zone, record);
                    }
                }
            }
        }

        void Spawn(ZoneView zone, AbandonedShip record)
        {
            ShipAgent ship = _stranded.Spawn(PrefabFor(record.Class), zone, record.LocalPosition, record.HeadingDegrees);
            ShipCrew crew = ship.Crew;
            if (crew != null)
            {
                crew.Complement.Remove(crew.Complement.Count);
            }
            ShipDurability durability = ship.GetComponent<ShipDurability>();
            durability.InitializeHull(durability.MaximumHull, record.HullFraction * durability.MaximumHull);
            durability.Damaged += OnDamaged;
            if (ship.Fuel != null)
            {
                FuelTank tank = ship.Fuel.Tank;
                tank.Drain(Mathf.Max(0f, tank.Amount - record.FuelFraction * tank.Capacity));
            }
            _bodies.Add(record, ship);
            _records.Add(ship, record);

            if (_announced.Add(record))
            {
                Notice?.Invoke(AbandonedShipTexts.Found.Format(("class", ship.ClassDisplayName), ("zone", zone.Record.PointOfInterest.Name)));
            }
        }

        ShipAgent PrefabFor(AbandonedShipClass shipClass)
        {
            return shipClass switch
            {
                AbandonedShipClass.Shuttle => _shuttlePrefab,
                AbandonedShipClass.Harvester => _harvesterPrefab,
                AbandonedShipClass.Fighter => _fighterPrefab,
                _ => throw new ArgumentOutOfRangeException(nameof(shipClass), shipClass, null)
            };
        }

        void OnDamaged(ShipDurability durability, float damage, GameText cause)
        {
            if (_records.TryGetValue(durability.GetComponent<ShipAgent>(), out AbandonedShip record))
            {
                record.SetHullFraction(durability.Hull.Current / durability.Hull.Maximum);
            }
        }

        /// <summary>Shot to pieces before anyone came: its wreckage stays, the record is gone for good.</summary>
        void OnShipLost(ShipAgent ship, GameText cause)
        {
            if (_records.TryGetValue(ship, out AbandonedShip record))
            {
                Forget(record, ship);
                record.MarkDestroyed();
            }
        }

        void Forget(AbandonedShip record, ShipAgent ship)
        {
            _bodies.Remove(record);
            _records.Remove(ship);
            if (ship != null)
            {
                ship.GetComponent<ShipDurability>().Damaged -= OnDamaged;
            }
        }
    }
}

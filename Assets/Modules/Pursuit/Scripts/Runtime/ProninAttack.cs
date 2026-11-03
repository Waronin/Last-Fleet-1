using System;
using System.Collections.Generic;
using LastFleet.Combat;
using LastFleet.Shared.Randomness;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Pursuit
{
    /// <summary>
    /// The Pronin attack when the pursuit countdown runs out: a carrier arrives in a random zone of
    /// the system and launches a wave of fighters that each go for a random ship of the fleet; more
    /// waves follow while it lives. After the strike delay it hunts the fleet through the system: it
    /// jumps to the zone with the most of the fleet's capital ships, also to where they are jumping,
    /// and joins in with its guns. A new or stopped countdown ends the attack's control of the carrier.
    /// </summary>
    public sealed class ProninAttack : MonoBehaviour
    {
        const float ArrivalDistanceFraction = 0.6f;
        const float StrikeStandoffUnits = 120f;
        const float StrikeMaximumRadiusFraction = 0.9f;
        // Within this distance of the capital ships' center the carrier's guns (250) reach most of them.
        const float EngageRangeUnits = 200f;
        const float PursuitCheckIntervalSeconds = 2f;

        [SerializeField] PursuitClock _pursuit;
        [SerializeField] ZoneWorld _zoneWorld;
        [SerializeField] ShipRegistry _fleet;
        [SerializeField] ShipRegistry _pronin;
        [SerializeField] ShipAgent _carrierPrefab;
        [SerializeField] ShipAgent _fighterPrefab;
        [SerializeField, Min(1)] int _fightersPerWave = 6;
        [Tooltip("Waves stop topping up once this many of the carrier's fighters are alive.")]
        [SerializeField, Min(1)] int _maximumFighters = 12;

        readonly List<int> _capitalShipZones = new List<int>();
        readonly List<ShipAgent> _fighters = new List<ShipAgent>();
        DeterministicRandom _random;
        ShipAgent _carrier;
        Hangar _hangar;
        CarrierSchedule _schedule;

        public event Action<GameText> Notice;

        void Awake()
        {
            if (_pursuit == null || _zoneWorld == null || _fleet == null || _pronin == null || _carrierPrefab == null || _fighterPrefab == null)
            {
                Debug.LogError("ProninAttack needs the pursuit clock, the zone world, both ship registries and the carrier and fighter prefabs.", this);
                enabled = false;
                return;
            }
            if (!_carrierPrefab.TryGetComponent(out Hangar carrierHangar) || carrierHangar.Capacity < _fightersPerWave)
            {
                Debug.LogError($"The Pronin carrier prefab needs a hangar for {_fightersPerWave} fighters.", this);
                enabled = false;
                return;
            }
            _random = new DeterministicRandom((ulong)Environment.TickCount);
        }

        void OnEnable()
        {
            _pursuit.EnemyArrived += Arrive;
            _pursuit.Reset += EndAttack;
        }

        void OnDisable()
        {
            _pursuit.EnemyArrived -= Arrive;
            _pursuit.Reset -= EndAttack;
        }

        void Update()
        {
            if (_schedule == null)
            {
                return;
            }
            // Unity's overloaded equality treats a destroyed carrier as null.
            if (_carrier == null)
            {
                EndAttack();
                return;
            }

            CarrierDuties duties = _schedule.Advance(Time.deltaTime);
            if ((duties & CarrierDuties.LaunchWave) != 0)
            {
                LaunchWave();
            }
            if ((duties & CarrierDuties.CheckPursuit) != 0)
            {
                Pursue();
            }
        }

        void Arrive()
        {
            SystemZones system = _zoneWorld.CurrentSystem;
            if (system == null)
            {
                return;
            }

            int zoneIndex = ProninAttackRules.ArrivalZone(system.Zones.Count, FindStrikeZone(), _random);
            ZoneView zone = _zoneWorld.Reveal(zoneIndex);

            // Towards the zone's edge, facing the center.
            Vector2 direction = ProninDirection();
            Vector3 position = new Vector3(direction.x, 0f, direction.y) * (zone.Zone.RadiusUnits * ArrivalDistanceFraction);
            _carrier = _pronin.Spawn(_carrierPrefab, zone, position, ShipMotion.DirectionToHeading(-direction));
            _hangar = _carrier.GetComponent<Hangar>();
            _fighters.Clear();
            _schedule = new CarrierSchedule(_pursuit.Settings.CarrierStrikeDelaySeconds, _pursuit.Settings.FighterWaveIntervalSeconds, PursuitCheckIntervalSeconds);

            Notice?.Invoke(PursuitTexts.CarrierArrived.Format(("ship", _carrier.DisplayName), ("zone", zone.Record.PointOfInterest.Name)));
            LaunchWave();
        }

        /// <summary>Fills the hangar with new fighters and sends them out, up to the limit of living fighters.</summary>
        void LaunchWave()
        {
            _fighters.RemoveAll(fighter => fighter == null);
            int count = Math.Min(_fightersPerWave, Math.Min(_maximumFighters - _fighters.Count, _hangar.Capacity - _hangar.DockedShips.Count));
            if (count <= 0)
            {
                return;
            }

            Squad wave = _pronin.SpawnSquadDocked(_fighterPrefab, _hangar, count);
            foreach (ShipAgent fighter in wave.Members)
            {
                if (fighter.TryGetComponent(out AutonomousHunting hunting))
                {
                    hunting.SetTargetChoice(HuntTargetChoice.Random);
                }
                _hangar.RequestLaunch(fighter);
                _fighters.Add(fighter);
            }
            Notice?.Invoke(PursuitTexts.CarrierWave.Format(("ship", _carrier.DisplayName), ("count", count)));
        }

        /// <summary>Follows the fleet's capital ships through the system, also into zones they are still jumping to.</summary>
        void Pursue()
        {
            int strikeZoneIndex = FindStrikeZone();
            if (strikeZoneIndex < 0 || !_zoneWorld.TryGetView(strikeZoneIndex, out ZoneView zone))
            {
                return;
            }

            bool hasTargets = TryGetCapitalShipCenter(zone, out Vector2 center);
            float distance = hasTargets && _carrier.Zone == zone ? Vector2.Distance(center, Planar(zone, _carrier.WorldPosition)) : 0f;
            bool isIdle = _carrier.Activity == ShipActivity.Idle;
            if (!ProninAttackRules.NeedsNewCourse(strikeZoneIndex, DestinationZoneIndex(_carrier), isIdle, distance, EngageRangeUnits))
            {
                return;
            }

            bool changesZone = DestinationZoneIndex(_carrier) != strikeZoneIndex;
            Vector2 travel = _carrier.Zone == zone ? center - Planar(zone, _carrier.WorldPosition) : _carrier.JumpDirection(strikeZoneIndex);
            Vector2 point = ProninAttackRules.StrikePoint(center, travel, StrikeStandoffUnits, zone.Zone.RadiusUnits * StrikeMaximumRadiusFraction);
            _carrier.Order(ShipOrder.MoveTo(strikeZoneIndex, new Vector3(point.x, 0f, point.y)), false);
            if (changesZone)
            {
                Notice?.Invoke(PursuitTexts.CarrierStrike.Format(("ship", _carrier.DisplayName), ("zone", zone.Record.PointOfInterest.Name)));
            }
        }

        void EndAttack()
        {
            _schedule = null;
            _carrier = null;
            _hangar = null;
            _fighters.Clear();
        }

        int FindStrikeZone()
        {
            _capitalShipZones.Clear();
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (ship.IsCapital && ship.Zone != null)
                {
                    _capitalShipZones.Add(DestinationZoneIndex(ship));
                }
            }
            return ProninAttackRules.StrikeZone(_capitalShipZones);
        }

        /// <summary>The zone a ship is jumping to, or the zone it is in.</summary>
        static int DestinationZoneIndex(ShipAgent ship)
        {
            return ship.TargetZoneIndex >= 0 ? ship.TargetZoneIndex : ship.Zone.Record.PointOfInterestIndex;
        }

        /// <summary>Center of the fleet's capital ships flying in the zone; false while none has arrived yet.</summary>
        bool TryGetCapitalShipCenter(ZoneView zone, out Vector2 center)
        {
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (ship.IsCapital && ship.Zone == zone)
                {
                    sum += Planar(zone, ship.WorldPosition);
                    count++;
                }
            }
            center = count > 0 ? sum / count : Vector2.zero;
            return count > 0;
        }

        Vector2 ProninDirection()
        {
            float angleRadians = _random.AngleDegrees() * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
        }

        static Vector2 Planar(ZoneView zone, Vector3 worldPosition)
        {
            Vector3 local = zone.transform.InverseTransformPoint(worldPosition);
            return new Vector2(local.x, local.z);
        }
    }
}

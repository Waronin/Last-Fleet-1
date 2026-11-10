using System;
using System.Collections.Generic;
using LastFleet.Mining;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.TacticalWeapons
{
    /// <summary>
    /// EMP missiles of the fleet's warships. A missile flies to a point of its launcher's zone and
    /// stuns every ship around it, of both sides, longest at the center (<see cref="EmpBlast"/>).
    /// Warships build new missiles from metal and fuel in the fleet's stores, paid when the build starts.
    /// </summary>
    public sealed class EmpService : MonoBehaviour
    {
        [SerializeField] ShipRegistry _fleet;
        [SerializeField] ShipRegistry _pronin;
        [SerializeField] EmpMissile _missilePrefab;
        [SerializeField] EmpBlastEffect _blastPrefab;
        [Tooltip("20 km: 1 unit is 100 m.")]
        [SerializeField, Min(1f)] float _blastRadiusUnits = 200f;
        [SerializeField, Min(0f)] float _proninStunSeconds = 10f;
        [Tooltip("Human ships recover faster.")]
        [SerializeField, Min(0f)] float _humanStunSeconds = 7f;
        [SerializeField, Min(1f)] float _missileSpeedUnitsPerSecond = 80f;

        readonly List<ShipAgent> _shipsInZone = new List<ShipAgent>();
        FleetSupplies _supplies;
        EmpBlast _blast;

        /// <summary>Short feedback for the player: launches, detonations and finished missiles.</summary>
        public event Action<GameText> Notice;

        public float BlastRadiusUnits => _blastRadiusUnits;
        FleetSupplies Supplies => _supplies ??= new FleetSupplies(_fleet);
        EmpBlast Blast => _blast ??= new EmpBlast(_blastRadiusUnits, _proninStunSeconds, _humanStunSeconds);

        void Awake()
        {
            if (_fleet == null || _pronin == null || _missilePrefab == null || _blastPrefab == null)
            {
                Debug.LogError("EmpService needs both ship registries and the missile and blast prefabs.", this);
                enabled = false;
            }
        }

        public LaunchReadiness CheckLaunch(EmpLauncher launcher)
        {
            if (launcher.Ship.Zone == null || launcher.Ship.IsDocked)
            {
                return LaunchReadiness.NotInZone;
            }
            if (launcher.Ship.IsStunned)
            {
                return LaunchReadiness.Stunned;
            }
            return launcher.Magazine.Count > 0 ? LaunchReadiness.Ready : LaunchReadiness.NoMissile;
        }

        /// <summary>Fires a missile at a point of the launcher's zone; returns why not otherwise.</summary>
        public LaunchReadiness Launch(EmpLauncher launcher, Vector3 targetWorldPoint)
        {
            LaunchReadiness readiness = CheckLaunch(launcher);
            if (readiness != LaunchReadiness.Ready || !launcher.Magazine.TryTake())
            {
                return readiness == LaunchReadiness.Ready ? LaunchReadiness.NoMissile : readiness;
            }

            ZoneView zone = launcher.Ship.Zone;
            Vector3 target = zone.transform.InverseTransformPoint(targetWorldPoint);
            target.y = 0f;
            EmpMissile missile = Instantiate(_missilePrefab, launcher.Ship.WorldPosition, Quaternion.identity, zone.transform);
            missile.Launch(target, _missileSpeedUnitsPerSecond, arrived => Detonate(zone, arrived.transform.position));
            Notice?.Invoke(TacticalWeaponTexts.Launched.Format(("ship", launcher.Ship.DisplayName)));
            return LaunchReadiness.Ready;
        }

        public BuildReadiness CheckBuild(EmpLauncher launcher)
        {
            MissileMagazine magazine = launcher.Magazine;
            if (magazine.IsBuilding)
            {
                return BuildReadiness.AlreadyBuilding;
            }
            if (magazine.IsFull)
            {
                return BuildReadiness.MagazineFull;
            }
            bool affordable = Supplies.AmountOf(Supply.Metal) >= launcher.MetalPerMissile &&
                              Supplies.AmountOf(Supply.Fuel) >= launcher.FuelPerMissile;
            return affordable ? BuildReadiness.Ready : BuildReadiness.NotEnoughSupplies;
        }

        /// <summary>Pays for a missile and starts building it; returns why not otherwise.</summary>
        public BuildReadiness OrderBuild(EmpLauncher launcher)
        {
            BuildReadiness readiness = CheckBuild(launcher);
            if (readiness != BuildReadiness.Ready)
            {
                return readiness;
            }
            Supplies.Take(Supply.Metal, launcher.MetalPerMissile);
            Supplies.Take(Supply.Fuel, launcher.FuelPerMissile);
            launcher.Magazine.StartBuild();
            return readiness;
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (ship.TryGetComponent(out EmpLauncher launcher) && launcher.Magazine.Tick(deltaTime))
                {
                    MissileMagazine magazine = launcher.Magazine;
                    Notice?.Invoke(TacticalWeaponTexts.Built.Format(("ship", ship.DisplayName), ("count", magazine.Count),
                        ("capacity", magazine.Capacity)));
                }
            }
        }

        void Detonate(ZoneView zone, Vector3 worldPoint)
        {
            if (zone == null)
            {
                return;
            }
            int enemies = StunShips(_pronin, zone, worldPoint);
            int own = StunShips(_fleet, zone, worldPoint);
            EmpBlastEffect effect = Instantiate(_blastPrefab, worldPoint, Quaternion.identity, zone.transform);
            effect.Play(_blastRadiusUnits);
            Notice?.Invoke(TacticalWeaponTexts.Detonated.Format(("enemies", enemies), ("own", own)));
        }

        int StunShips(ShipRegistry registry, ZoneView zone, Vector3 worldPoint)
        {
            int stunned = 0;
            registry.CollectShipsInZone(zone, _shipsInZone);
            foreach (ShipAgent ship in _shipsInZone)
            {
                Vector3 offset = ship.WorldPosition - worldPoint;
                float distance = new Vector2(offset.x, offset.z).magnitude;
                float seconds = Blast.StunSecondsFor(registry.Allegiance, distance);
                if (seconds > 0f)
                {
                    ship.Stun(seconds);
                    stunned++;
                }
            }
            return stunned;
        }
    }
}

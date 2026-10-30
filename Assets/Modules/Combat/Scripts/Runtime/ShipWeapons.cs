using System;
using System.Collections.Generic;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Weapons of a ship, fighter or capital ship alike (GDD: capital ships fire automatically). They
    /// fire in bursts at the focus target when it is in range and inside the firing arc, otherwise at
    /// the nearest enemy that is, so a ship defends itself even without orders. Forward guns therefore
    /// only fire while the ship points at an enemy. A hit is rolled when the shot is fired and lands
    /// when its round arrives; how a shot looks is up to a view such as <see cref="ProjectileShotView"/>.
    /// Rounds still in flight are lost when the ship lands or is destroyed.
    /// </summary>
    [RequireComponent(typeof(ShipAgent))]
    public sealed class ShipWeapons : MonoBehaviour
    {
        [SerializeField] WeaponSettings _weapon = new WeaponSettings();

        readonly List<(ShipAgent Target, float SecondsLeft)> _hitsInFlight = new List<(ShipAgent Target, float SecondsLeft)>();
        ShipAgent _ship;
        CombatService _combat;
        WeaponCycle _cycle;

        public float RangeUnits => _weapon.RangeUnits;
        public float ProjectileSpeedUnitsPerSecond => _weapon.ProjectileSpeedUnitsPerSecond;

        /// <summary>The ship the weapons prefer, e.g. the target of an attack order; may be null.</summary>
        public ShipAgent FocusTarget { get; private set; }

        /// <summary>The ship the weapons aimed at last; null when there was nothing to fire at.</summary>
        public ShipAgent CurrentTarget { get; private set; }

        /// <summary>A shot was fired at the target; true if it will hit.</summary>
        public event Action<ShipAgent, bool> Fired;

        WeaponCycle Cycle => _cycle ??= new WeaponCycle(_weapon);

        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
        }

        void OnDisable()
        {
            _hitsInFlight.Clear();
        }

        public void Initialize(CombatService combat)
        {
            _combat = combat;
        }

        public void SetFocusTarget(ShipAgent target)
        {
            FocusTarget = target;
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            Cycle.Tick(deltaTime);
            LandHits(deltaTime);
            if (_combat == null || _ship.IsDocked || _ship.IsDerelict || !Cycle.IsReady)
            {
                return;
            }

            Vector2 forward = Forward();
            CurrentTarget = CanFireAt(FocusTarget, forward)
                ? FocusTarget
                : _combat.FindNearestHostile(_ship, RangeUnits, forward, _weapon.FiringArcDegrees);
            if (CurrentTarget == null)
            {
                return;
            }

            float distance = CombatService.PlanarDistance(_ship, CurrentTarget);
            ShotResult result = Cycle.TryFire(distance, UnityEngine.Random.value);
            if (result == ShotResult.NotFired)
            {
                return;
            }
            Fired?.Invoke(CurrentTarget, result == ShotResult.Hit);
            if (result == ShotResult.Hit)
            {
                _hitsInFlight.Add((CurrentTarget, _weapon.FlightSeconds(distance)));
            }
        }

        /// <summary>Deals the damage of rounds that arrived; a target that is gone meanwhile is spared.</summary>
        void LandHits(float deltaTime)
        {
            for (int i = _hitsInFlight.Count - 1; i >= 0; i--)
            {
                (ShipAgent target, float secondsLeft) = _hitsInFlight[i];
                secondsLeft -= deltaTime;
                if (secondsLeft > 0f)
                {
                    _hitsInFlight[i] = (target, secondsLeft);
                    continue;
                }
                _hitsInFlight.RemoveAt(i);
                if (_combat != null && CombatService.IsHostileInZone(_ship, target))
                {
                    _combat.DealHit(_ship, target, _weapon.DamagePerHit);
                }
            }
        }

        bool CanFireAt(ShipAgent target, Vector2 forward)
        {
            return CombatService.IsHostileInZone(_ship, target) && CombatService.PlanarDistance(_ship, target) <= RangeUnits &&
                   _weapon.IsInArc(forward, ToTarget(target));
        }

        Vector2 Forward()
        {
            Vector3 forward = transform.forward;
            return new Vector2(forward.x, forward.z);
        }

        Vector2 ToTarget(ShipAgent target)
        {
            Vector3 offset = target.WorldPosition - _ship.WorldPosition;
            return new Vector2(offset.x, offset.z);
        }
    }
}

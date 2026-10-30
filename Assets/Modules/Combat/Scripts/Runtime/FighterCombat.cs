using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    public enum CombatState
    {
        Inactive,

        /// <summary>Flying attack runs at an enemy.</summary>
        Engaging
    }

    /// <summary>
    /// Attack behavior of a small armed ship (GDD: a fighter on an enemy attacks it). After one order
    /// it flies attack runs at the target, across zones if needed: in at run speed with its forward
    /// guns firing, past the target, out to the break-off distance and about for the next pass. Ships
    /// pass on alternating sides, so a squad splits around its target. After a kill it takes on the
    /// next enemy nearby. It stops when nothing is left nearby, the ship lands or the player gives another
    /// order. Without orders the ship defends itself against enemies in weapon range. Stances
    /// (aggressive, defensive, escort, retreat) will build on this.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(ShipWeapons))]
    public sealed class FighterCombat : MonoBehaviour, IShipAutomation
    {
        const float DefenseCheckIntervalSeconds = 0.5f;

        [Tooltip("Speed of an attack run; slower runs give the guns more time on target.")]
        [SerializeField, Min(1f)] float _attackRunSpeedUnitsPerSecond = 30f;
        [Tooltip("How far beside the target a run passes.")]
        [SerializeField, Min(0f)] float _passOffsetUnits = 3f;
        [Tooltip("How far past the target a run flies on before the ship comes about.")]
        [SerializeField, Min(0f)] float _breakOffUnits = 25f;
        [Tooltip("After a kill, the nearest enemy within this distance is engaged next; beyond the weapon range.")]
        [SerializeField, Min(0f)] float _followUpRadiusUnits = 300f;
        [Tooltip("A ship without orders attacks enemies that come within weapon range.")]
        [SerializeField] bool _defendWhenIdle = true;

        ShipAgent _ship;
        ShipWeapons _weapons;
        CombatService _combat;
        bool _isIssuingOrder;
        float _secondsUntilDefenseCheck;

        public CombatState State { get; private set; }
        public ShipAgent Target { get; private set; }
        public bool IsAutomationActive => State != CombatState.Inactive;

        // Subscribed for the object's whole life: docked ships are inactive but must still react.
        void Awake()
        {
            _ship = GetComponent<ShipAgent>();
            _weapons = GetComponent<ShipWeapons>();
            _ship.OrdersReplaced += OnOrdersReplaced;
            _ship.DockedInHangar += OnDockedInHangar;
            _ship.JumpRefusedForFuel += OnJumpRefusedForFuel;
        }

        void OnDestroy()
        {
            _ship.OrdersReplaced -= OnOrdersReplaced;
            _ship.DockedInHangar -= OnDockedInHangar;
            _ship.JumpRefusedForFuel -= OnJumpRefusedForFuel;
        }

        public void Initialize(CombatService combat)
        {
            _combat = combat;
        }

        /// <summary>Starts attack runs at an enemy; returns false for a derelict ship or a ship that is not an enemy.</summary>
        public bool Engage(ShipAgent target)
        {
            if (_combat == null)
            {
                Debug.LogError($"{name} has no combat service.", this);
                return false;
            }
            if (_ship.IsDerelict || !IsEnemy(target))
            {
                return false;
            }

            Target = target;
            State = CombatState.Engaging;
            _weapons.SetFocusTarget(target);
            _isIssuingOrder = true;
            _ship.Order(ShipOrder.Intercept(target, _attackRunSpeedUnitsPerSecond, PassOffset(), _breakOffUnits), false);
            _isIssuingOrder = false;
            return true;
        }

        /// <summary>
        /// Odd and even numbers pass on opposite sides, and at one, two or three times the offset, so
        /// ships of a squad on the same target fly their own lines instead of one on top of the other.
        /// </summary>
        float PassOffset()
        {
            int serial = _ship.SerialNumber;
            float side = serial % 2 == 0 ? 1f : -1f;
            int lane = serial / 2 % 3;
            return side * _passOffsetUnits * (1 + lane);
        }

        public void Stop()
        {
            State = CombatState.Inactive;
            Target = null;
            _weapons.SetFocusTarget(null);
        }

        void Update()
        {
            if (State != CombatState.Engaging)
            {
                DefendWhenIdle();
                return;
            }
            if (IsEnemy(Target) && !Target.IsDocked)
            {
                return;
            }

            // The target is gone, landed or changed sides: take on the next enemy nearby, or stand down.
            ShipAgent next = _combat.FindNearestHostile(_ship, _followUpRadiusUnits);
            if (next == null || !Engage(next))
            {
                Stop();
            }
        }

        /// <summary>
        /// Forward guns cannot hit what the ship does not face, so a fighter waiting without orders turns
        /// on an enemy within weapon range by itself. Ships busy with an order keep to it: the player
        /// decides, e.g. when fighters are sent home.
        /// </summary>
        void DefendWhenIdle()
        {
            if (!_defendWhenIdle || _combat == null || _ship.IsDocked || !ShipAvailability.IsFree(_ship))
            {
                return;
            }

            _secondsUntilDefenseCheck -= Time.deltaTime;
            if (_secondsUntilDefenseCheck > 0f)
            {
                return;
            }
            _secondsUntilDefenseCheck = DefenseCheckIntervalSeconds;

            ShipAgent attacker = _combat.FindNearestHostile(_ship, _weapons.RangeUnits);
            if (attacker != null)
            {
                Engage(attacker);
            }
        }

        bool IsEnemy(ShipAgent target)
        {
            // Unity's overloaded equality treats a destroyed ship as null.
            return target != null && target != _ship && target.Allegiance != _ship.Allegiance;
        }

        void OnOrdersReplaced(ShipAgent ship)
        {
            if (!_isIssuingOrder)
            {
                Stop();
            }
        }

        void OnDockedInHangar(ShipAgent ship, Hangar hangar)
        {
            Stop();
        }

        // The pursuit was dropped for lack of fuel; chasing on would ask for the same jump again.
        void OnJumpRefusedForFuel(ShipAgent ship, int targetZoneIndex, float requiredFuel)
        {
            Stop();
        }
    }
}

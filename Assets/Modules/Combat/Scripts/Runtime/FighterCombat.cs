using System;
using System.Collections.Generic;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    public enum CombatState
    {
        Inactive,

        /// <summary>Flying attack runs at an enemy.</summary>
        Engaging,

        /// <summary>Keeping station next to a protected ship or squad, ready to go for its attackers.</summary>
        Escorting
    }

    /// <summary>
    /// Combat behavior of a small armed ship (GDD: a fighter on an enemy attacks it; escort stance).
    /// Attack: attack runs at the target, across zones if needed: in at run speed with its forward guns
    /// firing, past the target, out to the break-off distance and about for the next pass. Ships pass on
    /// alternating sides, so a squad splits around its target. After a kill it takes on the next enemy
    /// nearby. Escort: keep station around a ship or squad, attack whatever comes near it, but never
    /// chase further than the leash; then fall back to the escort. Both end when the player gives
    /// another order or the ship lands. Without orders the ship defends itself against enemies in range.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(ShipWeapons))]
    public sealed class FighterCombat : MonoBehaviour, IShipAutomation, IResumableAutomation
    {
        const float CheckIntervalSeconds = 0.5f;

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
        [Tooltip("Escort: distance kept from the protected ship's hull.")]
        [SerializeField, Min(1f)] float _escortDistanceUnits = 8f;
        [Tooltip("Escort: enemies this close to a protected ship are attacked.")]
        [SerializeField, Min(1f)] float _guardRadiusUnits = 150f;
        [Tooltip("Escort: a chase that gets this far from the protected ship is broken off.")]
        [SerializeField, Min(1f)] float _leashUnits = 250f;

        readonly List<ShipAgent> _guarded = new List<ShipAgent>();
        ShipAgent _ship;
        ShipWeapons _weapons;
        CombatService _combat;
        EscortDuty _escort;
        ShipAgent _followedAnchor;
        bool _isIssuingOrder;
        float _secondsUntilCheck;

        public CombatState State { get; private set; }
        public ShipAgent Target { get; private set; }

        /// <summary>The escort duty, kept while the ship breaks away to fight; null without one.</summary>
        public EscortDuty Escort => _escort;

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

        /// <summary>A player's attack order: ends any escort and starts attack runs at the enemy.</summary>
        public bool Attack(ShipAgent target)
        {
            _escort = null;
            return Engage(target);
        }

        /// <summary>
        /// Starts escorting a ship, or its squad when one is given, from a slot at the given bearing.
        /// Returns false for a derelict ship or a ship that is not on this ship's side.
        /// </summary>
        public bool StartEscort(ShipAgent protectedShip, Squad squad, float slotAngleDegrees)
        {
            if (!HasCombatService() || _ship.IsDerelict || protectedShip == null || protectedShip == _ship ||
                protectedShip.Allegiance != _ship.Allegiance)
            {
                return false;
            }

            _escort = new EscortDuty(protectedShip, squad, slotAngleDegrees);
            ResumeEscort();
            return true;
        }

        /// <summary>Stops attack and escort; the escort is taken up again later, or the attack on the same enemy if it still lives.</summary>
        public Action Suspend()
        {
            if (_escort != null && _escort.Lead != null)
            {
                Squad squad = _escort.Squad;
                ShipAgent protectedShip = _escort.ProtectedShip;
                float slotAngle = _escort.SlotAngleDegrees;
                Stop();
                return () =>
                {
                    // The squad may have lost its first leader meanwhile; protect whoever leads it now.
                    ShipAgent lead = squad != null ? squad.Leader : protectedShip;
                    if (lead != null)
                    {
                        StartEscort(lead, squad, slotAngle);
                    }
                };
            }
            if (State == CombatState.Engaging && Target != null)
            {
                ShipAgent target = Target;
                Stop();
                return () =>
                {
                    if (target != null)
                    {
                        Attack(target);
                    }
                };
            }
            return null;
        }

        /// <summary>Ends attack and escort; the ship stays where it is.</summary>
        public void Stop()
        {
            State = CombatState.Inactive;
            Target = null;
            _escort = null;
            _followedAnchor = null;
            _weapons.SetFocusTarget(null);
        }

        void Update()
        {
            switch (State)
            {
                case CombatState.Inactive:
                    DefendWhenIdle();
                    break;
                case CombatState.Engaging:
                    UpdateEngagement();
                    break;
                case CombatState.Escorting:
                    UpdateEscort();
                    break;
            }
        }

        /// <summary>Starts attack runs at an enemy; keeps an escort duty to return to afterwards.</summary>
        bool Engage(ShipAgent target)
        {
            if (!HasCombatService() || _ship.IsDerelict || !IsEnemy(target))
            {
                return false;
            }

            Target = target;
            State = CombatState.Engaging;
            _followedAnchor = null;
            _weapons.SetFocusTarget(target);
            Issue(ShipOrder.Intercept(target, _attackRunSpeedUnitsPerSecond, PassOffset(), _breakOffUnits));
            return true;
        }

        void UpdateEngagement()
        {
            bool targetLeft = !IsEnemy(Target) || Target.IsDocked;
            if (_escort != null)
            {
                ShipAgent anchor = _escort.Anchor;
                bool strayedTooFar = anchor != null && CombatService.PlanarDistance(_ship, anchor) > _leashUnits;
                if (targetLeft || strayedTooFar)
                {
                    ResumeEscort();
                }
                return;
            }
            if (!targetLeft)
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

        /// <summary>Back to the protected ship, unless an attacker is already near it.</summary>
        void ResumeEscort()
        {
            if (_escort == null || _escort.IsOver)
            {
                Stop();
                return;
            }

            Target = null;
            _weapons.SetFocusTarget(null);
            State = CombatState.Escorting;
            _secondsUntilCheck = 0f;
            FollowAnchor();
        }

        void UpdateEscort()
        {
            if (!TickCheck())
            {
                return;
            }
            if (_escort.IsOver)
            {
                Stop();
                return;
            }
            if (_escort.Anchor != _followedAnchor)
            {
                // The protected ship landed or launched, or the squad has a new leader.
                FollowAnchor();
            }

            ShipAgent attacker = FindAttackerOfGuarded();
            if (attacker != null)
            {
                Engage(attacker);
            }
        }

        ShipAgent FindAttackerOfGuarded()
        {
            _escort.CollectGuarded(_guarded);
            ShipAgent nearest = null;
            float bestDistance = float.PositiveInfinity;
            foreach (ShipAgent guarded in _guarded)
            {
                ShipAgent candidate = _combat.FindNearestHostileNear(guarded, guarded.WorldPosition, _guardRadiusUnits);
                if (candidate == null)
                {
                    continue;
                }
                float distance = CombatService.PlanarDistance(_ship, candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = candidate;
                }
            }
            return nearest;
        }

        void FollowAnchor()
        {
            ShipAgent anchor = _escort.Anchor;
            _followedAnchor = anchor;
            if (anchor != null && anchor != _ship)
            {
                Issue(ShipOrder.Follow(anchor, _escort.SlotOffset(anchor, _escortDistanceUnits)));
            }
        }

        /// <summary>
        /// Forward guns cannot hit what the ship does not face, so a fighter waiting without orders turns
        /// on an enemy within weapon range by itself. Ships busy with an order keep to it: the player
        /// decides, e.g. when fighters are sent home.
        /// </summary>
        void DefendWhenIdle()
        {
            if (!_defendWhenIdle || _combat == null || _ship.IsDocked || !ShipAvailability.IsFree(_ship) || !TickCheck())
            {
                return;
            }

            ShipAgent attacker = _combat.FindNearestHostile(_ship, _weapons.RangeUnits);
            if (attacker != null)
            {
                Engage(attacker);
            }
        }

        bool TickCheck()
        {
            _secondsUntilCheck -= Time.deltaTime;
            if (_secondsUntilCheck > 0f)
            {
                return false;
            }
            _secondsUntilCheck = CheckIntervalSeconds;
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

        void Issue(ShipOrder order)
        {
            _isIssuingOrder = true;
            _ship.Order(order, false);
            _isIssuingOrder = false;
        }

        bool HasCombatService()
        {
            if (_combat == null)
            {
                Debug.LogError($"{name} has no combat service.", this);
                return false;
            }
            return true;
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

        // The chase was dropped for lack of fuel; chasing on would ask for the same jump again.
        void OnJumpRefusedForFuel(ShipAgent ship, int targetZoneIndex, float requiredFuel)
        {
            Stop();
        }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Galaxy;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.FleetJump
{
    /// <summary>
    /// The fleet's large jump to another star system (GDD: The jump). It plans a jump to a system in
    /// range, charges the drive after the player's confirmation (no launches meanwhile, landing still
    /// works) and then carries it out: every capital ship pays from its own tank, ships that cannot
    /// pay and small ships outside a hangar of a jumping ship are lost. Placing the fleet in the new
    /// system is up to whoever owns the run, through <see cref="Jumped"/>; it then calls
    /// <see cref="ArrivedIn"/>, which starts the drive's cooldown.
    /// </summary>
    public sealed class FleetJumpService : MonoBehaviour
    {
        [SerializeField] FleetJumpSettings _settings = new FleetJumpSettings();
        [SerializeField] ShipRegistry _fleet;

        readonly List<ShipAgent> _jumpers = new List<ShipAgent>();
        GalaxyLayout _galaxy;
        FleetJumpDrive _drive;

        /// <summary>The drive, or null before the fleet arrived anywhere.</summary>
        public FleetJumpDrive Drive => _galaxy != null ? _drive : null;
        public int CurrentSystemIndex { get; private set; } = -1;
        public GalaxyLayout Galaxy => _galaxy;

        /// <summary>Name of the system the drive charges towards, or null.</summary>
        public string ChargingTargetName => Drive != null && _drive.TargetSystemIndex >= 0 ? _galaxy.Systems[_drive.TargetSystemIndex].Name : null;

        public event Action<GameText> Notice;

        /// <summary>
        /// The jump happened: the given capital ships left their zones for the target system and wait
        /// to be placed there with <see cref="ShipAgent.ArriveAfterFleetJump"/>.
        /// </summary>
        public event Action<int, IReadOnlyList<ShipAgent>> Jumped;

        void Awake()
        {
            if (_fleet == null)
            {
                Debug.LogError("FleetJumpService needs the fleet's ship registry.", this);
                enabled = false;
                return;
            }
            try
            {
                _drive = new FleetJumpDrive(_settings);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"FleetJumpService has invalid settings: {exception.Message}", this);
                enabled = false;
            }
        }

        /// <summary>The fleet is in a system, at the start of a run or after a jump; the drive cools down.</summary>
        public void ArrivedIn(GalaxyLayout galaxy, int systemIndex)
        {
            _galaxy = galaxy ?? throw new ArgumentNullException(nameof(galaxy));
            CurrentSystemIndex = systemIndex;
            _drive.StartCooldown();
            HoldLaunches(false);
        }

        public bool IsInRange(int systemIndex)
        {
            if (_galaxy == null || systemIndex == CurrentSystemIndex)
            {
                return false;
            }
            foreach (int neighbour in _galaxy.GetNeighbours(CurrentSystemIndex))
            {
                if (neighbour == systemIndex)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>What a jump to the system would do right now.</summary>
        public FleetJumpPlan Plan(int targetSystemIndex)
        {
            if (_galaxy == null)
            {
                throw new InvalidOperationException("The fleet has not arrived in a system yet.");
            }

            float distance = _galaxy.DistanceLightYears(CurrentSystemIndex, targetSystemIndex);
            FleetJumpPlan plan = new FleetJumpPlan(targetSystemIndex, _galaxy.Systems[targetSystemIndex].Name, distance);
            List<Hangar> jumpingHangars = new List<Hangar>();
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (!ship.IsCapital)
                {
                    continue;
                }
                float cost = FleetJumpCost.ForShip(ship.Fuel.Tank.Capacity, distance, _galaxy.JumpRangeLightYears, _settings.FullRangeTankFraction);
                if (ship.Fuel.Tank.Amount + 1e-3f >= cost)
                {
                    float fullRangeCost = FleetJumpCost.ForShip(ship.Fuel.Tank.Capacity, _galaxy.JumpRangeLightYears, _galaxy.JumpRangeLightYears, _settings.FullRangeTankFraction);
                    plan.AddJumping(ship, cost, FleetJumpCost.JumpsLeft(ship.Fuel.Tank.Amount - cost, fullRangeCost));
                    if (ship.TryGetComponent(out Hangar hangar))
                    {
                        jumpingHangars.Add(hangar);
                    }
                }
                else
                {
                    plan.AddOutOfFuel(ship, cost);
                }
            }

            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (!ship.IsCapital && !jumpingHangars.Contains(ship.DockedIn))
                {
                    plan.AddSmallShipLeftBehind(ship);
                }
            }
            return plan;
        }

        /// <summary>Why a jump to the system cannot start right now, or null when it can be planned and confirmed.</summary>
        public GameText BlockedReason(int targetSystemIndex)
        {
            if (Drive == null)
            {
                return FleetJumpTexts.JumpFailed;
            }
            if (!IsInRange(targetSystemIndex))
            {
                return FleetJumpTexts.OutOfRange.Format(("system", _galaxy.Systems[targetSystemIndex].Name));
            }
            return _drive.State switch
            {
                FleetJumpState.CoolingDown => FleetJumpTexts.StillCoolingDown.Format(("remaining", ClockFormat.MinutesSeconds(_drive.CooldownRemainingSeconds))),
                FleetJumpState.Charging => FleetJumpTexts.ChargeInProgress,
                _ => null
            };
        }

        /// <summary>Starts charging towards a system after the player confirmed the plan.</summary>
        public bool TryStartCharge(int targetSystemIndex)
        {
            GameText blocked = BlockedReason(targetSystemIndex);
            if (blocked != null)
            {
                Notice?.Invoke(blocked);
                return false;
            }
            if (!Plan(targetSystemIndex).CanJump || !_drive.TryStartCharge(targetSystemIndex))
            {
                Notice?.Invoke(FleetJumpTexts.JumpFailed);
                return false;
            }

            HoldLaunches(true);
            Notice?.Invoke(FleetJumpTexts.ChargeStarted.Format(("system", _galaxy.Systems[targetSystemIndex].Name),
                ("remaining", ClockFormat.MinutesSeconds(_drive.ChargeRemainingSeconds))));
            return true;
        }

        public void CancelCharge()
        {
            if (Drive == null || _drive.State != FleetJumpState.Charging)
            {
                return;
            }
            _drive.CancelCharge();
            HoldLaunches(false);
            Notice?.Invoke(FleetJumpTexts.ChargeCancelled);
        }

        /// <summary>Developer shortcut to skip the cooldown or the charge.</summary>
        public void SkipAhead(float seconds)
        {
            Advance(seconds);
        }

        void Update()
        {
            Advance(Time.deltaTime);
        }

        void Advance(float deltaSeconds)
        {
            if (Drive != null && _drive.Advance(deltaSeconds))
            {
                Execute(_drive.TargetSystemIndex);
            }
        }

        /// <summary>The charge is complete; plans again, since tanks and hangars changed while it charged.</summary>
        void Execute(int targetSystemIndex)
        {
            FleetJumpPlan plan = Plan(targetSystemIndex);
            if (!plan.CanJump)
            {
                HoldLaunches(false);
                _drive.CancelCharge();
                Notice?.Invoke(FleetJumpTexts.JumpFailed);
                return;
            }

            foreach (ShipAgent ship in plan.SmallShipsLeftBehind)
            {
                // Ships docked in a carrier that stays behind are already gone with it.
                if (ship != null && _fleet.Contains(ship))
                {
                    _fleet.Abandon(ship, FleetJumpTexts.LeftBehindCause);
                }
            }
            foreach (FleetJumpPassage passage in plan.OutOfFuel)
            {
                _fleet.Abandon(passage.Ship, FleetJumpTexts.LeftBehindCause);
            }

            _jumpers.Clear();
            foreach (FleetJumpPassage passage in plan.Jumping)
            {
                passage.Ship.Fuel.Tank.Drain(passage.FuelCost);
                // Orders of docked ships point into the old system; they must not launch with them after the jump.
                if (passage.Ship.TryGetComponent(out Hangar hangar))
                {
                    hangar.CancelDockedOrders();
                }
                // Out of the old zone, which the new system's zones replace.
                passage.Ship.transform.SetParent(null, true);
                _jumpers.Add(passage.Ship);
            }
            HoldLaunches(false);

            Notice?.Invoke(FleetJumpTexts.Jumped.Format(("system", plan.TargetName), ("count", _jumpers.Count)));
            Jumped?.Invoke(targetSystemIndex, _jumpers);
        }

        void HoldLaunches(bool held)
        {
            foreach (Hangar hangar in _fleet.Hangars)
            {
                hangar.LaunchesHeld = held;
            }
        }
    }
}

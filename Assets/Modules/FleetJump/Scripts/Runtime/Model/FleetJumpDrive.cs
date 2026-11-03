using System;

namespace LastFleet.FleetJump
{
    public enum FleetJumpState
    {
        /// <summary>After an arrival: the drive cannot charge yet.</summary>
        CoolingDown,
        Ready,
        Charging
    }

    /// <summary>
    /// The fleet's large jump drive as a timeline (GDD: The jump): it cools down after each arrival,
    /// then charges towards one target once the player confirms, and the charge can be cancelled.
    /// Plain time in, completion out.
    /// </summary>
    public sealed class FleetJumpDrive
    {
        readonly float _cooldownSeconds;
        readonly float _chargeSeconds;

        public FleetJumpDrive(FleetJumpSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            settings.Validate();
            _cooldownSeconds = settings.CooldownSeconds;
            _chargeSeconds = settings.ChargeSeconds;
            TargetSystemIndex = -1;
            StartCooldown();
        }

        public FleetJumpState State { get; private set; }
        public float CooldownRemainingSeconds { get; private set; }
        public float ChargeRemainingSeconds { get; private set; }

        /// <summary>The system the drive charges towards, or -1.</summary>
        public int TargetSystemIndex { get; private set; }

        /// <summary>After an arrival in a system; also ends a running charge.</summary>
        public void StartCooldown()
        {
            CooldownRemainingSeconds = _cooldownSeconds;
            ChargeRemainingSeconds = 0f;
            TargetSystemIndex = -1;
            State = _cooldownSeconds > 0f ? FleetJumpState.CoolingDown : FleetJumpState.Ready;
        }

        /// <summary>Begins charging towards a system; only a ready drive can charge.</summary>
        public bool TryStartCharge(int targetSystemIndex)
        {
            if (targetSystemIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetSystemIndex), "A jump needs a target system.");
            }
            if (State != FleetJumpState.Ready)
            {
                return false;
            }
            TargetSystemIndex = targetSystemIndex;
            ChargeRemainingSeconds = _chargeSeconds;
            State = FleetJumpState.Charging;
            return true;
        }

        /// <summary>Stops a running charge; the drive stays ready.</summary>
        public void CancelCharge()
        {
            if (State != FleetJumpState.Charging)
            {
                return;
            }
            TargetSystemIndex = -1;
            ChargeRemainingSeconds = 0f;
            State = FleetJumpState.Ready;
        }

        /// <summary>Lets time pass; returns true in the step in which a charge completes.</summary>
        public bool Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Time cannot run backwards.");
            }

            switch (State)
            {
                case FleetJumpState.CoolingDown:
                    CooldownRemainingSeconds = Math.Max(0f, CooldownRemainingSeconds - deltaSeconds);
                    if (CooldownRemainingSeconds <= 0f)
                    {
                        State = FleetJumpState.Ready;
                    }
                    return false;
                case FleetJumpState.Charging:
                    ChargeRemainingSeconds = Math.Max(0f, ChargeRemainingSeconds - deltaSeconds);
                    return ChargeRemainingSeconds <= 0f;
                default:
                    return false;
            }
        }
    }
}

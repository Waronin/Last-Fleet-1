using System;

namespace LastFleet.Pursuit
{
    /// <summary>Where the countdown to the enemy's arrival stands; phases only move forward.</summary>
    public enum PursuitPhase
    {
        Counting,
        /// <summary>Shortly before the attack.</summary>
        Warning,
        Arrived
    }

    /// <summary>
    /// Countdown to the Pronin's arrival in the current system (GDD: game loop, early warning): one
    /// fixed time, with a warning shortly before it runs out. Plain time in, phase out: time
    /// compression later only feeds it more seconds.
    /// </summary>
    public sealed class PursuitCountdown
    {
        public PursuitCountdown(PursuitSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            settings.Validate();

            ArrivalSeconds = settings.ArrivalSeconds;
            WarningSeconds = Math.Max(0f, ArrivalSeconds - settings.WarningLeadSeconds);
            Phase = PhaseAt(0f);
        }

        public float ElapsedSeconds { get; private set; }
        public PursuitPhase Phase { get; private set; }
        public float ArrivalSeconds { get; }
        public float WarningSeconds { get; }
        public float SecondsUntilArrival => Math.Max(0f, ArrivalSeconds - ElapsedSeconds);

        /// <summary>Lets time pass; returns true when the phase changed. A long step may skip the warning.</summary>
        public bool Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Time cannot run backwards.");
            }
            if (Phase == PursuitPhase.Arrived)
            {
                return false;
            }

            ElapsedSeconds += deltaSeconds;
            PursuitPhase previous = Phase;
            Phase = PhaseAt(ElapsedSeconds);
            return Phase != previous;
        }

        PursuitPhase PhaseAt(float elapsedSeconds)
        {
            if (elapsedSeconds >= ArrivalSeconds)
            {
                return PursuitPhase.Arrived;
            }
            return elapsedSeconds >= WarningSeconds ? PursuitPhase.Warning : PursuitPhase.Counting;
        }
    }
}

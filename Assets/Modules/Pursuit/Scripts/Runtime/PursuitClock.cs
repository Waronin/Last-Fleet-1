using System;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.Pursuit
{
    /// <summary>
    /// Runs the countdown to the Pronin's arrival in the current system on game time (scaled, so time
    /// compression speeds it up) and announces its phases. Whoever owns the run starts it on arrival
    /// in a system and stops it on leaving; what the arrival brings is up to the listeners.
    /// </summary>
    public sealed class PursuitClock : MonoBehaviour
    {
        [SerializeField] PursuitSettings _settings = new PursuitSettings();

        /// <summary>The running countdown, or null while the fleet is in no system.</summary>
        public PursuitCountdown Countdown { get; private set; }

        public PursuitSettings Settings => _settings;

        public event Action<GameText> Notice;

        /// <summary>The countdown ran out: the enemy found the fleet.</summary>
        public event Action EnemyArrived;

        /// <summary>The countdown was stopped or restarted; whatever its arrival started is over.</summary>
        public event Action Reset;

        void Awake()
        {
            try
            {
                _settings.Validate();
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"PursuitClock has invalid settings: {exception.Message}", this);
                enabled = false;
            }
        }

        public void Begin()
        {
            Reset?.Invoke();
            Countdown = new PursuitCountdown(_settings);
            Notice?.Invoke(PursuitTexts.CountdownStarted.Format(("remaining", ClockFormat.MinutesSeconds(Countdown.SecondsUntilArrival))));
        }

        public void Stop()
        {
            Countdown = null;
            Reset?.Invoke();
        }

        /// <summary>Developer shortcut to reach the arrival without waiting the full countdown.</summary>
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
            if (Countdown == null || !Countdown.Advance(deltaSeconds))
            {
                return;
            }

            if (Countdown.Phase == PursuitPhase.Warning)
            {
                Notice?.Invoke(PursuitTexts.WarningIssued.Format(("remaining", ClockFormat.MinutesSeconds(Countdown.SecondsUntilArrival))));
            }
            else if (Countdown.Phase == PursuitPhase.Arrived)
            {
                EnemyArrived?.Invoke();
            }
        }
    }
}

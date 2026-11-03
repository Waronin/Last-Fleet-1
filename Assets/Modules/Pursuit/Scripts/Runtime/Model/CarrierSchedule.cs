using System;

namespace LastFleet.Pursuit
{
    /// <summary>What is due for the Pronin carrier after a stretch of time.</summary>
    [Flags]
    public enum CarrierDuties
    {
        None = 0,
        /// <summary>The strike delay is over: from now on the carrier hunts the fleet's capital ships.</summary>
        StartHunt = 1,
        /// <summary>Time for the next wave of fighters.</summary>
        LaunchWave = 2,
        /// <summary>Time to check where the fleet's capital ships are and change course if needed.</summary>
        CheckPursuit = 4
    }

    /// <summary>
    /// Timing of a Pronin carrier after its arrival: the first wave launches on arrival, further waves
    /// follow at a fixed interval; after the strike delay it hunts the fleet and checks its course
    /// regularly. Plain time in, duties out.
    /// </summary>
    public sealed class CarrierSchedule
    {
        readonly float _waveIntervalSeconds;
        readonly float _pursuitCheckIntervalSeconds;
        float _secondsUntilHunt;
        float _secondsUntilWave;
        float _secondsUntilPursuitCheck;

        public CarrierSchedule(float strikeDelaySeconds, float waveIntervalSeconds, float pursuitCheckIntervalSeconds)
        {
            if (strikeDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(strikeDelaySeconds), "The strike delay must not be negative.");
            }
            if (waveIntervalSeconds <= 0f || pursuitCheckIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(waveIntervalSeconds), "Wave and pursuit intervals must be positive.");
            }

            _waveIntervalSeconds = waveIntervalSeconds;
            _pursuitCheckIntervalSeconds = pursuitCheckIntervalSeconds;
            _secondsUntilHunt = strikeDelaySeconds;
            _secondsUntilWave = waveIntervalSeconds;
        }

        public bool IsHunting { get; private set; }

        /// <summary>
        /// Lets time pass and returns what became due. Each duty is reported at most once per call, even
        /// after a long step, so a frame hitch never launches several waves at once.
        /// </summary>
        public CarrierDuties Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Time cannot run backwards.");
            }

            CarrierDuties duties = CarrierDuties.None;
            _secondsUntilWave -= deltaSeconds;
            if (_secondsUntilWave <= 0f)
            {
                duties |= CarrierDuties.LaunchWave;
                _secondsUntilWave = Math.Max(_secondsUntilWave + _waveIntervalSeconds, 0f);
            }

            if (!IsHunting)
            {
                _secondsUntilHunt -= deltaSeconds;
                if (_secondsUntilHunt > 0f)
                {
                    return duties;
                }
                IsHunting = true;
                _secondsUntilPursuitCheck = _pursuitCheckIntervalSeconds;
                return duties | CarrierDuties.StartHunt | CarrierDuties.CheckPursuit;
            }

            _secondsUntilPursuitCheck -= deltaSeconds;
            if (_secondsUntilPursuitCheck <= 0f)
            {
                duties |= CarrierDuties.CheckPursuit;
                _secondsUntilPursuitCheck = Math.Max(_secondsUntilPursuitCheck + _pursuitCheckIntervalSeconds, 0f);
            }
            return duties;
        }
    }
}

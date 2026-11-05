using System;

namespace LastFleet.Quests
{
    /// <summary>
    /// The radiation a ship's crew has taken, from 0 (clean) to 1 (the limit). It builds up while the
    /// ship is exposed and wears off when it is not. At the limit, further exposure kills one crew member
    /// per interval, so a ship has to leave now and then to recover.
    /// </summary>
    public sealed class RadiationDose
    {
        readonly float _secondsToLimit;
        readonly float _secondsToRecover;
        readonly float _deathIntervalSeconds;
        float _secondsAtLimit;

        public RadiationDose(float secondsToLimit, float secondsToRecover, float deathIntervalSeconds)
        {
            _secondsToLimit = Positive(secondsToLimit, nameof(secondsToLimit));
            _secondsToRecover = Positive(secondsToRecover, nameof(secondsToRecover));
            _deathIntervalSeconds = Positive(deathIntervalSeconds, nameof(deathIntervalSeconds));
        }

        /// <summary>0 is clean, 1 the limit above which crew members die.</summary>
        public float Level { get; private set; }

        public bool IsAtLimit => Level >= 1f;
        public bool IsClean => Level <= 0f;

        /// <summary>Exposure for the given time; returns how many crew members die of it.</summary>
        public int Expose(float deltaSeconds)
        {
            NotNegative(deltaSeconds);
            float toLimit = (1f - Level) * _secondsToLimit;
            if (deltaSeconds < toLimit)
            {
                Level += deltaSeconds / _secondsToLimit;
                return 0;
            }

            Level = 1f;
            _secondsAtLimit += deltaSeconds - toLimit;
            int deaths = (int)(_secondsAtLimit / _deathIntervalSeconds);
            _secondsAtLimit -= deaths * _deathIntervalSeconds;
            return deaths;
        }

        /// <summary>Time away from the radiation; the dose wears off.</summary>
        public void Recover(float deltaSeconds)
        {
            NotNegative(deltaSeconds);
            Level = Math.Max(0f, Level - deltaSeconds / _secondsToRecover);
            if (Level < 1f)
            {
                _secondsAtLimit = 0f;
            }
        }

        static float Positive(float value, string name)
        {
            if (value <= 0f || float.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(name, "Radiation timings must be positive.");
            }
            return value;
        }

        static void NotNegative(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Time cannot run backwards.");
            }
        }
    }
}

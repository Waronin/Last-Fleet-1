using System;

namespace LastFleet.Quests
{
    /// <summary>
    /// A quest countdown on game time, e.g. a fuse or a time to hold a station. Plain time in,
    /// "ran out" out, once; like the pursuit countdown, time compression only feeds it more seconds.
    /// </summary>
    public sealed class QuestTimer
    {
        public QuestTimer(float durationSeconds)
        {
            if (durationSeconds <= 0f || float.IsNaN(durationSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "A quest timer needs a positive duration.");
            }
            DurationSeconds = durationSeconds;
        }

        public float DurationSeconds { get; }
        public float ElapsedSeconds { get; private set; }
        public float RemainingSeconds => Math.Max(0f, DurationSeconds - ElapsedSeconds);
        public bool HasRunOut => ElapsedSeconds >= DurationSeconds;

        /// <summary>Lets time pass; returns true only in the step in which the timer runs out.</summary>
        public bool Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Time cannot run backwards.");
            }
            if (HasRunOut)
            {
                return false;
            }
            ElapsedSeconds += deltaSeconds;
            return HasRunOut;
        }
    }
}

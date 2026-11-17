using System;
using LastFleet.Shared.Text;

namespace LastFleet.Tutorial
{
    /// <summary>
    /// One step of a tutorial: a radio call that explains something and an objective the game checks.
    /// The step is done when its condition holds, when its time is up, or when the sequence is advanced
    /// from outside (e.g. the fleet jumped, or the player skipped the step).
    /// </summary>
    public sealed class TutorialStep
    {
        readonly Func<bool> _isDone;
        readonly Action _began;

        /// <param name="isDone">Checked every frame while the step is current; null waits for an outside advance.</param>
        /// <param name="maximumSeconds">The step ends by itself after this long; 0 waits without limit.</param>
        /// <param name="began">Runs when the step becomes current, before its radio call is shown.</param>
        public TutorialStep(GameText speaker, GameText line, GameText objective, Func<bool> isDone, float maximumSeconds = 0f,
            Action began = null)
        {
            if (maximumSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSeconds), "The time limit cannot be negative.");
            }
            Speaker = speaker ?? throw new ArgumentNullException(nameof(speaker));
            Line = line ?? throw new ArgumentNullException(nameof(line));
            Objective = objective ?? throw new ArgumentNullException(nameof(objective));
            MaximumSeconds = maximumSeconds;
            _isDone = isDone;
            _began = began;
        }

        public GameText Speaker { get; }
        public GameText Line { get; }
        public GameText Objective { get; }
        public float MaximumSeconds { get; }
        public bool HasTimeLimit => MaximumSeconds > 0f;

        internal bool IsDone()
        {
            return _isDone != null && _isDone();
        }

        internal void Begin()
        {
            _began?.Invoke();
        }
    }
}

using System;
using LastFleet.Shared.Text;

namespace LastFleet.RunEnd
{
    /// <summary>What the end screen tells about a finished run.</summary>
    public sealed class RunSummary
    {
        public RunSummary(RunOutcome outcome, GameText reason, int people, int ships, float elapsedSeconds)
        {
            if (people < 0 || ships < 0 || elapsedSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(people), "Counts and time cannot be negative.");
            }
            Outcome = outcome;
            Reason = reason;
            People = people;
            Ships = ships;
            ElapsedSeconds = elapsedSeconds;
        }

        public RunOutcome Outcome { get; }

        /// <summary>Why the run ended, e.g. the chain that won it; may be null.</summary>
        public GameText Reason { get; }

        /// <summary>People alive in the fleet at the end, crews and passengers.</summary>
        public int People { get; }

        public int Ships { get; }

        /// <summary>Game time from the start of the run.</summary>
        public float ElapsedSeconds { get; }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;

namespace LastFleet.RunEnd
{
    /// <summary>
    /// What the end screen tells about a finished run: the outcome, why, the quest chain that won it, and the account
    /// with its points.
    /// </summary>
    public sealed class RunSummary
    {
        RunSummary(RunOutcome outcome, GameText reason, string winningChainId, GameText winningChainTitle, int survivors, int ships,
            float elapsedSeconds, RunTally tally, RunScoreRules rules)
        {
            if (survivors < 0 || ships < 0 || elapsedSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(survivors), "Counts and time cannot be negative.");
            }
            if (tally == null)
            {
                throw new ArgumentNullException(nameof(tally));
            }
            Outcome = outcome;
            Reason = reason;
            WinningChainId = winningChainId;
            WinningChainTitle = winningChainTitle;
            Survivors = survivors;
            Ships = ships;
            ElapsedSeconds = elapsedSeconds;
            Tally = tally.Copy();
            List<RunEndLine> lines = RunScore.BuildLines(outcome, survivors, Tally, rules);
            Lines = lines;
            TotalPoints = RunScore.Total(lines);
        }

        /// <summary>A won run: the quest chain that won it, and what is left of the fleet.</summary>
        public static RunSummary Won(string winningChainId, GameText winningChainTitle, int survivors, int ships, float elapsedSeconds,
            RunTally tally, RunScoreRules rules)
        {
            if (string.IsNullOrWhiteSpace(winningChainId))
            {
                throw new ArgumentException("A won run needs the id of the quest chain that won it.", nameof(winningChainId));
            }
            if (winningChainTitle == null)
            {
                throw new ArgumentNullException(nameof(winningChainTitle));
            }
            return new RunSummary(RunOutcome.Won, null, winningChainId, winningChainTitle, survivors, ships, elapsedSeconds, tally, rules);
        }

        /// <summary>A lost run: nobody and nothing is left, so no survivors and no ships count.</summary>
        public static RunSummary Lost(GameText reason, float elapsedSeconds, RunTally tally, RunScoreRules rules)
        {
            return new RunSummary(RunOutcome.Lost, reason, null, null, 0, 0, elapsedSeconds, tally, rules);
        }

        public RunOutcome Outcome { get; }

        /// <summary>Why a run was lost; null for a won run, which names its quest chain instead.</summary>
        public GameText Reason { get; }

        /// <summary>The id of the quest chain that won the run; null for a lost run.</summary>
        public string WinningChainId { get; }

        /// <summary>The title of the quest chain that won the run; null for a lost run.</summary>
        public GameText WinningChainTitle { get; }

        /// <summary>People alive in the fleet at the end, crews and passengers; none after a defeat.</summary>
        public int Survivors { get; }

        /// <summary>Ships left in the fleet at the end.</summary>
        public int Ships { get; }

        /// <summary>Game time from the start of the run.</summary>
        public float ElapsedSeconds { get; }

        /// <summary>A copy of the run's counts, frozen at the end.</summary>
        public RunTally Tally { get; }

        /// <summary>The account in display order.</summary>
        public IReadOnlyList<RunEndLine> Lines { get; }

        public int TotalPoints { get; }
    }
}

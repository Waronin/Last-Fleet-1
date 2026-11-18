using System;
using System.Collections.Generic;

namespace LastFleet.RunEnd
{
    /// <summary>Everything the end screen shows: the run's summary, how it compares, and the latest runs including this one.</summary>
    public sealed class RunEndReport
    {
        public RunEndReport(RunSummary summary, RunComparison comparison, IReadOnlyList<RunRecord> latestRuns)
        {
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
            LatestRuns = latestRuns ?? throw new ArgumentNullException(nameof(latestRuns));
        }

        public RunSummary Summary { get; }

        public RunComparison Comparison { get; }

        /// <summary>Newest first; the first one is this run.</summary>
        public IReadOnlyList<RunRecord> LatestRuns { get; }
    }
}

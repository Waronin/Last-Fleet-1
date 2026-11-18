using System;
using System.Collections.Generic;

namespace LastFleet.RunEnd
{
    /// <summary>How one line of the account compares with earlier runs.</summary>
    public readonly struct RunLineComparison
    {
        public RunLineComparison(int? changeSinceLastRun, bool isImprovement, bool isRecord)
        {
            ChangeSinceLastRun = changeSinceLastRun;
            IsImprovement = isImprovement;
            IsRecord = isRecord;
        }

        /// <summary>The count minus the last run's; null without an earlier run.</summary>
        public int? ChangeSinceLastRun { get; }

        /// <summary>Better than the last run: more achievements or fewer losses.</summary>
        public bool IsImprovement { get; }

        /// <summary>Better than every earlier run.</summary>
        public bool IsRecord { get; }
    }

    /// <summary>
    /// Compares a finished run with all earlier ones, wins and losses together: each line against the last run and
    /// against the best so far, and the total against the best total.
    /// </summary>
    public sealed class RunComparison
    {
        readonly Dictionary<RunMetric, RunLineComparison> _lines = new Dictionary<RunMetric, RunLineComparison>();

        RunComparison()
        {
        }

        public bool HasEarlierRuns { get; private set; }

        /// <summary>The best total of the earlier runs; null without one.</summary>
        public int? BestEarlierTotal { get; private set; }

        public bool IsNewBestTotal { get; private set; }

        /// <param name="earlierRuns">Runs before this one, oldest first.</param>
        public static RunComparison Against(IReadOnlyList<RunRecord> earlierRuns, RunSummary current)
        {
            if (earlierRuns == null)
            {
                throw new ArgumentNullException(nameof(earlierRuns));
            }
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            RunComparison comparison = new RunComparison { HasEarlierRuns = earlierRuns.Count > 0 };
            if (!comparison.HasEarlierRuns)
            {
                return comparison;
            }

            RunRecord last = earlierRuns[earlierRuns.Count - 1];
            int bestTotal = int.MinValue;
            foreach (RunRecord run in earlierRuns)
            {
                bestTotal = Math.Max(bestTotal, run.TotalPoints);
            }
            comparison.BestEarlierTotal = bestTotal;
            comparison.IsNewBestTotal = current.TotalPoints > bestTotal;

            foreach (RunEndLine line in current.Lines)
            {
                if (line.Metric == RunMetric.Victory)
                {
                    continue;
                }
                bool lowerIsBetter = RunMetrics.IsLowerBetter(line.Metric);
                int change = line.Count - last.ValueOf(line.Metric);
                bool isImprovement = lowerIsBetter ? change < 0 : change > 0;
                comparison._lines[line.Metric] = new RunLineComparison(change, isImprovement, BeatsAll(line, earlierRuns, lowerIsBetter));
            }
            return comparison;
        }

        /// <summary>The comparison of one line; empty for lines without one, e.g. the victory bonus.</summary>
        public RunLineComparison For(RunMetric metric)
        {
            return _lines.TryGetValue(metric, out RunLineComparison line) ? line : default;
        }

        static bool BeatsAll(RunEndLine line, IReadOnlyList<RunRecord> earlierRuns, bool lowerIsBetter)
        {
            foreach (RunRecord run in earlierRuns)
            {
                int earlier = run.ValueOf(line.Metric);
                if (lowerIsBetter ? line.Count >= earlier : line.Count <= earlier)
                {
                    return false;
                }
            }
            return true;
        }
    }
}

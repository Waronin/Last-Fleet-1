using System;
using LastFleet.Shared.Text;

namespace LastFleet.RunEnd
{
    /// <summary>One line of the end screen's account: what it counts and, for achievements, the points it brings.</summary>
    public sealed class RunEndLine
    {
        RunEndLine(RunMetric metric, GameText label, int count, int points, bool scores)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Counts cannot be negative.");
            }
            Metric = metric;
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Count = count;
            Points = points;
            Scores = scores;
        }

        public RunMetric Metric { get; }

        public GameText Label { get; }

        /// <summary>How many were counted, compared with earlier runs.</summary>
        public int Count { get; }

        /// <summary>Points of this line; 0 for lines that do not score.</summary>
        public int Points { get; }

        /// <summary>False for losses, which are shown without points.</summary>
        public bool Scores { get; }

        public static RunEndLine Scoring(RunMetric metric, GameText label, int count, int points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), "Only achievements score; points cannot be negative.");
            }
            return new RunEndLine(metric, label, count, points, true);
        }

        public static RunEndLine Fact(RunMetric metric, GameText label, int count)
        {
            return new RunEndLine(metric, label, count, 0, false);
        }
    }
}

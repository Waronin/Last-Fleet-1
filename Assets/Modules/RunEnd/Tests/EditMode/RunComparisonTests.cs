using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using NUnit.Framework;

namespace LastFleet.RunEnd.Tests
{
    public class RunComparisonTests
    {
        static readonly RunScoreRules Rules = new RunScoreRules(1, 1, 50, 500, 200, 5000);
        static readonly DateTime Finished = new DateTime(2026, 11, 18, 10, 0, 0, DateTimeKind.Utc);

        static RunSummary Lost(int rescued, int peopleLost)
        {
            RunTally tally = new RunTally();
            tally.AddRescued(rescued);
            tally.AddPeopleLost(peopleLost);
            return RunSummary.Lost(null, 60f, tally, Rules);
        }

        static RunRecord Record(RunSummary summary)
        {
            return RunRecord.From(summary, Finished);
        }

        [Test]
        public void TheFirstRun_HasNothingToCompareWith()
        {
            RunComparison comparison = RunComparison.Against(new List<RunRecord>(), Lost(100, 10));

            Assert.IsFalse(comparison.HasEarlierRuns);
            Assert.IsNull(comparison.BestEarlierTotal);
            Assert.IsNull(comparison.For(RunMetric.PeopleRescued).ChangeSinceLastRun);
        }

        [Test]
        public void Lines_CompareWithTheLastRun_AndRecordsWithTheBestSoFar()
        {
            List<RunRecord> earlier = new List<RunRecord> { Record(Lost(500, 50)), Record(Lost(100, 30)) };

            RunComparison comparison = RunComparison.Against(earlier, Lost(300, 20));

            RunLineComparison rescued = comparison.For(RunMetric.PeopleRescued);
            Assert.AreEqual(200, rescued.ChangeSinceLastRun);
            Assert.IsTrue(rescued.IsImprovement);
            Assert.IsFalse(rescued.IsRecord, "500 were rescued two runs ago.");

            RunLineComparison peopleLost = comparison.For(RunMetric.PeopleLost);
            Assert.AreEqual(-10, peopleLost.ChangeSinceLastRun);
            Assert.IsTrue(peopleLost.IsImprovement, "Fewer losses are better.");
            Assert.IsTrue(peopleLost.IsRecord, "Fewer people lost than in any earlier run.");

            Assert.AreEqual(500, comparison.BestEarlierTotal);
            Assert.IsFalse(comparison.IsNewBestTotal);
        }

        [Test]
        public void WinsAndLosses_CompareTogether()
        {
            List<RunRecord> earlier = new List<RunRecord> { Record(Lost(800, 0)) };
            RunSummary win = RunSummary.Won("meltdown", GameText.Verbatim("Meltdown"), 10, 3, 60f, new RunTally(), Rules);

            RunComparison comparison = RunComparison.Against(earlier, win);

            Assert.IsTrue(comparison.IsNewBestTotal);
            Assert.IsFalse(comparison.For(RunMetric.PeopleRescued).IsImprovement);
            Assert.IsNull(comparison.For(RunMetric.Victory).ChangeSinceLastRun, "The victory bonus is not compared.");
        }

        [Test]
        public void ATie_IsNoRecord()
        {
            List<RunRecord> earlier = new List<RunRecord> { Record(Lost(100, 10)) };

            RunComparison comparison = RunComparison.Against(earlier, Lost(100, 10));

            Assert.AreEqual(0, comparison.For(RunMetric.PeopleRescued).ChangeSinceLastRun);
            Assert.IsFalse(comparison.For(RunMetric.PeopleRescued).IsRecord);
            Assert.IsFalse(comparison.IsNewBestTotal);
        }
    }
}

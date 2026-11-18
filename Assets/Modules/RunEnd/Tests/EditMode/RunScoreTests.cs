using System.Collections.Generic;
using LastFleet.Shared.Text;
using NUnit.Framework;

namespace LastFleet.RunEnd.Tests
{
    public class RunScoreTests
    {
        static readonly RunScoreRules Rules = new RunScoreRules(1, 1, 50, 500, 200, 5000);

        static RunTally CreateTally()
        {
            RunTally tally = new RunTally();
            tally.AddRescued(300);
            tally.AddRescued(40);
            tally.AddShipGained(false);
            tally.AddShipGained(true);
            tally.AddClue();
            tally.AddClue();
            tally.AddPeopleLost(120);
            tally.AddShipLost(false);
            tally.AddShipLost(true);
            return tally;
        }

        [Test]
        public void AWonRun_ScoresAchievementsAndTheVictoryBonus_AndNamesItsChain()
        {
            RunSummary summary = RunSummary.Won("meltdown", GameText.Verbatim("Meltdown"), 7000, 9, 600f, CreateTally(), Rules);

            // 340 rescued + 7000 survivors + 50 + 500 + 2 * 200 clues + 5000 victory.
            Assert.AreEqual(340 + 7000 + 50 + 500 + 400 + 5000, summary.TotalPoints);
            Assert.AreEqual(8, summary.Lines.Count);
            Assert.AreEqual("meltdown", summary.WinningChainId);
            Assert.IsNull(summary.Reason);
        }

        [Test]
        public void Losses_AreListed_WithoutTakingPoints()
        {
            List<RunEndLine> lines = RunScore.BuildLines(RunOutcome.Lost, 0, CreateTally(), Rules);

            RunEndLine peopleLost = lines[lines.Count - 2];
            RunEndLine shipsLost = lines[lines.Count - 1];
            Assert.IsFalse(peopleLost.Scores);
            Assert.IsFalse(shipsLost.Scores);
            Assert.AreEqual(120, peopleLost.Count);
            Assert.AreEqual(0, peopleLost.Points + shipsLost.Points);
        }

        [Test]
        public void ALostRun_HasNoVictoryBonus_NoSurvivors_AndStillScoresWhatWasSaved()
        {
            RunSummary summary = RunSummary.Lost(GameText.Verbatim("Supernova"), 600f, CreateTally(), Rules);

            Assert.AreEqual(340 + 50 + 500 + 400, summary.TotalPoints);
            Assert.AreEqual(7, summary.Lines.Count);
            Assert.AreEqual(0, summary.Survivors);
            Assert.IsNull(summary.WinningChainTitle);
        }

        [Test]
        public void TheSummary_KeepsItsOwnCopyOfTheTally()
        {
            RunTally tally = CreateTally();
            RunSummary summary = RunSummary.Lost(null, 1f, tally, Rules);

            tally.Reset();

            Assert.AreEqual(340, summary.Tally.PeopleRescued);
            Assert.AreEqual(2, summary.Tally.ShipsLost);
        }

        [Test]
        public void NegativeCountsAndPoints_AndAWinWithoutChain_AreRejected()
        {
            RunTally tally = new RunTally();

            Assert.Throws<System.ArgumentOutOfRangeException>(() => tally.AddRescued(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new RunScoreRules(-1, 1, 1, 1, 1, 1));
            Assert.Throws<System.ArgumentException>(() => RunSummary.Won("", GameText.Verbatim("x"), 0, 0, 0f, tally, Rules));
        }
    }
}

using System;
using System.IO;
using LastFleet.Shared.Text;
using NUnit.Framework;

namespace LastFleet.RunEnd.Tests
{
    public class RunHistoryStoreTests
    {
        static readonly RunScoreRules Rules = new RunScoreRules(1, 1, 50, 500, 200, 5000);

        string _directory;
        string _path;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "LastFleetRunHistoryTests", Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, RunHistoryStore.DefaultFileName);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void AMissingFile_IsAnEmptyHistory()
        {
            RunHistory history = new RunHistoryStore(_path).Load();

            Assert.AreEqual(0, history.Runs.Count);
        }

        [Test]
        public void SavedRuns_LoadAgain_WithTheirCountsAndWinningChain()
        {
            RunHistoryStore store = new RunHistoryStore(_path);
            RunTally tally = new RunTally();
            tally.AddRescued(340);
            tally.AddShipLost(true);
            RunHistory history = new RunHistory();
            DateTime finished = new DateTime(2026, 11, 18, 9, 30, 0, DateTimeKind.Utc);
            history.Add(RunRecord.From(RunSummary.Lost(GameText.Verbatim("x"), 60f, tally, Rules), finished));
            history.Add(RunRecord.From(RunSummary.Won("meltdown", new GameTextKey("Quests", "chain.meltdown").Format(), 900, 7, 1200f, tally, Rules),
                finished.AddHours(1)));

            store.Save(history);
            store.Save(history);
            RunHistory loaded = store.Load();

            Assert.AreEqual(2, loaded.Runs.Count);
            RunRecord won = loaded.Runs[1];
            Assert.AreEqual(RunOutcome.Won, won.Outcome);
            Assert.AreEqual("meltdown", won.WinningChainId);
            Assert.AreEqual("chain.meltdown", won.WinningChainTitle.Key.Entry);
            Assert.AreEqual(340, won.ValueOf(RunMetric.PeopleRescued));
            Assert.AreEqual(900, won.ValueOf(RunMetric.Survivors));
            Assert.AreEqual(finished.AddHours(1), won.FinishedAtUtc);
            Assert.IsNull(loaded.Runs[0].WinningChainTitle);
            Assert.AreEqual("meltdown", loaded.Newest(5)[0].WinningChainId);
            Assert.IsFalse(File.Exists(_path + ".tmp"));
        }

        [Test]
        public void AnUnreadableFile_IsReported_AndCanBeSetAside()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_path, "{ \"_version\": 1, \"_runs\": [ { \"_outcome\": \"Exploded\" } ] }");
            RunHistoryStore store = new RunHistoryStore(_path);

            Assert.Throws<InvalidDataException>(() => store.Load());
            string aside = store.SetAside(new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc));

            Assert.IsTrue(File.Exists(aside));
            Assert.IsFalse(File.Exists(_path));
            Assert.AreEqual(0, store.Load().Runs.Count);
        }

        [Test]
        public void AHistoryFromANewerBuild_IsNotUsed()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_path, "{ \"_version\": 99, \"_runs\": [] }");

            Assert.Throws<InvalidDataException>(() => new RunHistoryStore(_path).Load());
        }
    }
}

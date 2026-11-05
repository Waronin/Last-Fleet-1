using System;
using LastFleet.Quests.Meltdown;
using LastFleet.Shared.Text;
using LastFleet.Zones;
using NUnit.Framework;

namespace LastFleet.Quests.Tests
{
    public class QuestChainTests
    {
        static readonly GameTextKey Text = new GameTextKey("Test", "text");

        [Test]
        public void CompleteStep_MovesOnUntilTheLastStepCompletesTheChain()
        {
            QuestChain chain = new QuestChain(Chain(QuestFailure.EndsChain, QuestFailure.EndsChain));
            chain.PlaceCurrentStep(3);

            Assert.IsTrue(chain.CompleteStep());
            Assert.AreEqual(2, chain.StepNumber);
            Assert.IsFalse(chain.IsPlaced, "The next step is placed by the director.");

            chain.PlaceCurrentStep(5);
            Assert.IsFalse(chain.CompleteStep());
            Assert.AreEqual(QuestChainState.Completed, chain.State);
        }

        [Test]
        public void FailStep_FollowsTheStepsRule()
        {
            QuestChain retrying = new QuestChain(Chain(QuestFailure.RetryStep));
            retrying.PlaceCurrentStep(1);
            Assert.AreEqual(QuestFailure.RetryStep, retrying.FailStep());
            Assert.IsTrue(retrying.IsActive);
            Assert.AreEqual(2, retrying.Attempt);
            Assert.AreEqual(1, retrying.StepSystemIndex);

            QuestChain ending = new QuestChain(Chain(QuestFailure.EndsChain));
            ending.PlaceCurrentStep(1);
            Assert.AreEqual(QuestFailure.EndsChain, ending.FailStep());
            Assert.AreEqual(QuestChainState.Failed, ending.State);
        }

        [Test]
        public void UnplacedStep_CannotFinish()
        {
            QuestChain chain = new QuestChain(Chain(QuestFailure.EndsChain));

            Assert.Throws<InvalidOperationException>(() => chain.CompleteStep());
            Assert.Throws<InvalidOperationException>(() => chain.FailStep());
        }

        [Test]
        public void Timer_RunsOutOnce()
        {
            QuestTimer timer = new QuestTimer(30f);

            Assert.IsFalse(timer.Advance(29f));
            Assert.AreEqual(1f, timer.RemainingSeconds, 1e-4f);
            Assert.IsTrue(timer.Advance(2f));
            Assert.IsFalse(timer.Advance(1f));
            Assert.AreEqual(0f, timer.RemainingSeconds);
        }

        [Test]
        public void Meltdown_HasFivePlayableSteps_AndWinsTheRun()
        {
            QuestChainDefinition meltdown = MeltdownQuest.Create();

            Assert.AreEqual(5, meltdown.Steps.Count);
            Assert.IsInstanceOf<SearchTrapStep>(meltdown.Steps[0].CreateStep());
            Assert.IsInstanceOf<EscortStep>(meltdown.Steps[1].CreateStep());
            Assert.IsInstanceOf<GuardedDepositStep>(meltdown.Steps[2].CreateStep());
            Assert.IsInstanceOf<StationProcessingStep>(meltdown.Steps[3].CreateStep());
            Assert.IsInstanceOf<SupernovaStep>(meltdown.Steps[4].CreateStep());
            Assert.IsTrue(meltdown.WinsRun);
        }

        [Test]
        public void Supernova_NeedsAHoldThatConsumesItsItem()
        {
            QuestItem fuel = new QuestItem("test.fuel", new GameTextKey("Quests", "test.fuel"));
            QuestItem product = new QuestItem("test.product", new GameTextKey("Quests", "test.product"));
            QuestStationSpec station = new QuestStationSpec("Array", StationType.Industrial,
                new StationProfile(StationAtmosphere.Breathable, 0, StationAttitude.None, StationCondition.Damaged, StationRadiation.None));
            GameTextKey work = new GameTextKey("Quests", "test.work");

            Assert.DoesNotThrow(() => new SupernovaSettings(
                new StationProcessingSettings(station, fuel, null, work, 60f, null, _ => null, null, null), 30f, null, null));
            Assert.Throws<ArgumentException>(() => new SupernovaSettings(
                new StationProcessingSettings(station, fuel, product, work, 60f, null, _ => null, null, null), 30f, null, null));
        }

        static QuestChainDefinition Chain(params QuestFailure[] failures)
        {
            QuestStepDefinition[] steps = new QuestStepDefinition[failures.Length];
            for (int i = 0; i < failures.Length; i++)
            {
                steps[i] = new QuestStepDefinition(Text, Text, failures[i], null);
            }
            return new QuestChainDefinition("test", Text, steps);
        }
    }
}

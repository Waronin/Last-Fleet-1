using System;
using LastFleet.Quests.Meltdown;
using LastFleet.Shared.Text;
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
        public void Meltdown_HasFiveStepsOfWhichTheFirstTwoArePlayable()
        {
            QuestChainDefinition meltdown = MeltdownQuest.Create();

            Assert.AreEqual(5, meltdown.Steps.Count);
            Assert.IsTrue(meltdown.Steps[0].IsPlayable);
            Assert.IsTrue(meltdown.Steps[1].IsPlayable);
            Assert.IsFalse(meltdown.Steps[2].IsPlayable);
            Assert.IsInstanceOf<SearchTrapStep>(meltdown.Steps[0].CreateStep());
            Assert.IsInstanceOf<EscortStep>(meltdown.Steps[1].CreateStep());
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

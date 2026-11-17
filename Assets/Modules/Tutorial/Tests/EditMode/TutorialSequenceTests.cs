using System.Collections.Generic;
using LastFleet.Shared.Text;
using NUnit.Framework;

namespace LastFleet.Tutorial.Tests
{
    public class TutorialSequenceTests
    {
        static readonly GameText Text = GameText.Verbatim("text");

        static TutorialStep Step(System.Func<bool> isDone, float maximumSeconds = 0f, System.Action began = null)
        {
            return new TutorialStep(Text, Text, Text, isDone, maximumSeconds, began);
        }

        [Test]
        public void Start_BeginsTheFirstStep()
        {
            List<string> log = new List<string>();
            TutorialStep first = Step(() => false, began: () => log.Add("first"));
            TutorialSequence sequence = new TutorialSequence(new[] { first, Step(() => false) });
            sequence.StepStarted += step => log.Add("started");

            sequence.Start();

            Assert.AreSame(first, sequence.Current);
            Assert.AreEqual(1, sequence.CurrentNumber);
            CollectionAssert.AreEqual(new[] { "first", "started" }, log);
        }

        [Test]
        public void Tick_MovesOnWhenTheConditionHolds()
        {
            bool done = false;
            TutorialStep second = Step(() => false);
            TutorialSequence sequence = new TutorialSequence(new[] { Step(() => done), second });
            sequence.Start();

            sequence.Tick(1f);
            Assert.AreEqual(1, sequence.CurrentNumber);

            done = true;
            sequence.Tick(1f);
            Assert.AreSame(second, sequence.Current);
        }

        [Test]
        public void Tick_EndsAtMostOneStep_SoEveryStepIsAnnounced()
        {
            TutorialSequence sequence = new TutorialSequence(new[] { Step(() => true), Step(() => true), Step(() => true) });
            sequence.Start();

            sequence.Tick(0.1f);

            Assert.AreEqual(2, sequence.CurrentNumber);
        }

        [Test]
        public void Tick_EndsATimedStepWhenItsTimeIsUp()
        {
            TutorialSequence sequence = new TutorialSequence(new[] { Step(() => false, 5f), Step(() => false) });
            sequence.Start();

            sequence.Tick(4.9f);
            Assert.AreEqual(1, sequence.CurrentNumber);

            sequence.Tick(0.2f);
            Assert.AreEqual(2, sequence.CurrentNumber);
            Assert.AreEqual(0f, sequence.StepSeconds);
        }

        [Test]
        public void StepWithoutCondition_WaitsForAdvance()
        {
            bool finished = false;
            TutorialSequence sequence = new TutorialSequence(new[] { Step(null) });
            sequence.Finished += () => finished = true;
            sequence.Start();

            sequence.Tick(1000f);
            Assert.IsFalse(sequence.IsFinished);

            sequence.Advance();
            Assert.IsTrue(sequence.IsFinished);
            Assert.IsTrue(finished);
            Assert.IsNull(sequence.Current);
        }

        [Test]
        public void TickAndAdvance_DoNothingAfterTheEnd()
        {
            int finishedCount = 0;
            TutorialSequence sequence = new TutorialSequence(new[] { Step(() => true) });
            sequence.Finished += () => finishedCount++;
            sequence.Start();
            sequence.Tick(1f);

            sequence.Tick(1f);
            sequence.Advance();

            Assert.AreEqual(1, finishedCount);
        }

        [Test]
        public void Start_Twice_Throws()
        {
            TutorialSequence sequence = new TutorialSequence(new[] { Step(() => false) });
            sequence.Start();

            Assert.Throws<System.InvalidOperationException>(sequence.Start);
        }

        [Test]
        public void Constructor_RejectsEmptyOrMissingSteps()
        {
            Assert.Throws<System.ArgumentException>(() => new TutorialSequence(new TutorialStep[0]));
            Assert.Throws<System.ArgumentException>(() => new TutorialSequence(new TutorialStep[] { null }));
        }
    }
}

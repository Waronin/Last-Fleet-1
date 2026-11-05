using System;

namespace LastFleet.Quests
{
    public enum QuestChainState
    {
        Active,
        Completed,
        Failed
    }

    /// <summary>
    /// Progress of one quest chain in a run: the current step, the system it takes place in and whether
    /// the chain is still going. Plain rules without Unity, so they can be tested on their own.
    /// </summary>
    public sealed class QuestChain
    {
        public QuestChain(QuestChainDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            StepSystemIndex = -1;
        }

        public QuestChainDefinition Definition { get; }
        public QuestChainState State { get; private set; }
        public bool IsActive => State == QuestChainState.Active;
        public int StepIndex { get; private set; }

        /// <summary>1-based, for players.</summary>
        public int StepNumber => StepIndex + 1;

        public int StepCount => Definition.Steps.Count;
        public QuestStepDefinition CurrentStep => Definition.Steps[StepIndex];

        /// <summary>The system of the current step; -1 until the step is placed.</summary>
        public int StepSystemIndex { get; private set; }

        public bool IsPlaced => StepSystemIndex >= 0;

        /// <summary>How often the current step was started; above 1 after a retry.</summary>
        public int Attempt { get; private set; }

        public void PlaceCurrentStep(int systemIndex)
        {
            RequireActive();
            if (systemIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemIndex));
            }
            StepSystemIndex = systemIndex;
            Attempt = 1;
        }

        /// <summary>The current step is done; returns true if another follows, which still has to be placed.</summary>
        public bool CompleteStep()
        {
            RequirePlaced();
            if (StepIndex + 1 >= StepCount)
            {
                State = QuestChainState.Completed;
                return false;
            }

            StepIndex++;
            StepSystemIndex = -1;
            Attempt = 0;
            return true;
        }

        /// <summary>The current step failed; applies its failure rule and returns it.</summary>
        public QuestFailure FailStep()
        {
            RequirePlaced();
            QuestFailure failure = CurrentStep.Failure;
            if (failure == QuestFailure.EndsChain)
            {
                State = QuestChainState.Failed;
            }
            else
            {
                Attempt++;
            }
            return failure;
        }

        void RequireActive()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException($"Quest chain '{Definition.Id}' is {State}.");
            }
        }

        void RequirePlaced()
        {
            RequireActive();
            if (!IsPlaced)
            {
                throw new InvalidOperationException($"Step {StepNumber} of '{Definition.Id}' has no system yet.");
            }
        }
    }
}

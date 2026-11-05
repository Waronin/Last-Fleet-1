using System;
using LastFleet.Shared.Text;

namespace LastFleet.Quests
{
    /// <summary>What a failed step does to its chain.</summary>
    public enum QuestFailure
    {
        /// <summary>The chain is over for this run, e.g. the person to rescue is dead.</summary>
        EndsChain,

        /// <summary>The step starts again from the beginning, in the same system.</summary>
        RetryStep
    }

    /// <summary>
    /// One step of a quest chain as written by its author: what the player is told, what failure means
    /// and how to make the step's logic. A step without logic is shown but cannot be played yet.
    /// </summary>
    public sealed class QuestStepDefinition
    {
        public QuestStepDefinition(GameTextKey title, GameTextKey objective, QuestFailure failure, Func<IQuestStep> createStep)
        {
            Title = title;
            Objective = objective;
            Failure = failure;
            CreateStep = createStep;
        }

        public GameText Title { get; }

        /// <summary>What to do, for the quest log.</summary>
        public GameText Objective { get; }

        public QuestFailure Failure { get; }

        /// <summary>A fresh instance of the step's logic per attempt; null while the step is only written down.</summary>
        public Func<IQuestStep> CreateStep { get; }

        public bool IsPlayable => CreateStep != null;
    }
}

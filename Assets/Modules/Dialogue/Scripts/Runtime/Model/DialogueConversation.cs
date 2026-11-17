using System;

namespace LastFleet.Dialogue
{
    /// <summary>
    /// A dialogue being held: where it stands and how the player moves on. Choosing runs the
    /// choice's consequence before the next node shows, so the consequence can already be seen there.
    /// </summary>
    public sealed class DialogueConversation
    {
        public DialogueConversation(DialogueScript script)
        {
            Script = script ?? throw new ArgumentNullException(nameof(script));
            MoveTo(script.Start.Id);
        }

        public DialogueScript Script { get; }

        /// <summary>The node shown now; null once the conversation is over.</summary>
        public DialogueNode Current { get; private set; }

        public bool IsFinished => Current == null;

        /// <summary>Seconds left to answer the current node; infinite when it has no time limit.</summary>
        public float SecondsLeft { get; private set; }

        /// <summary>
        /// Lets time pass on the current node. When its time limit runs out, its default choice is taken, consequence
        /// included; returns true then.
        /// </summary>
        public bool Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Time cannot run backwards.");
            }
            if (IsFinished || !Current.HasTimeLimit)
            {
                return false;
            }

            SecondsLeft -= deltaSeconds;
            if (SecondsLeft > 0f)
            {
                return false;
            }
            Choose(Current.DefaultChoiceIndex);
            return true;
        }

        /// <summary>Moves on from a node without choices.</summary>
        public void Continue()
        {
            RequireOpen();
            if (Current.HasChoices)
            {
                throw new InvalidOperationException($"Node '{Current.Id}' waits for a choice.");
            }
            MoveTo(Current.NextNodeId);
        }

        public void Choose(int choiceIndex)
        {
            RequireOpen();
            if (choiceIndex < 0 || choiceIndex >= Current.Choices.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(choiceIndex), choiceIndex, $"Node '{Current.Id}' has {Current.Choices.Count} choice(s).");
            }

            DialogueChoice choice = Current.Choices[choiceIndex];
            choice.Chosen?.Invoke();
            MoveTo(choice.NextNodeId);
        }

        /// <summary>Ends the conversation without an answer, e.g. when the speaker is gone.</summary>
        public void Abort()
        {
            Current = null;
        }

        void MoveTo(string nodeId)
        {
            Current = nodeId != null ? Script.GetNode(nodeId) : null;
            SecondsLeft = Current != null && Current.HasTimeLimit ? Current.TimeLimitSeconds : float.PositiveInfinity;
        }

        void RequireOpen()
        {
            if (IsFinished)
            {
                throw new InvalidOperationException("The conversation is over.");
            }
        }
    }
}

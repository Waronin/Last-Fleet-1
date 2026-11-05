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
            Current = script.Start;
        }

        public DialogueScript Script { get; }

        /// <summary>The node shown now; null once the conversation is over.</summary>
        public DialogueNode Current { get; private set; }

        public bool IsFinished => Current == null;

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

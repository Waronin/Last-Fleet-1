using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;

namespace LastFleet.Dialogue
{
    /// <summary>
    /// An answer the player can give. It leads to another node or ends the conversation, and may
    /// carry a consequence, e.g. accepting a quest, which runs when it is chosen.
    /// </summary>
    public sealed class DialogueChoice
    {
        public DialogueChoice(GameText label, string nextNodeId, Action chosen)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            NextNodeId = nextNodeId;
            Chosen = chosen;
        }

        public GameText Label { get; }

        /// <summary>The node that follows; null ends the conversation.</summary>
        public string NextNodeId { get; }

        /// <summary>Consequence of the choice, or null.</summary>
        public Action Chosen { get; }
    }

    /// <summary>
    /// One thing said in a conversation: who says it, what they say and how the player can answer.
    /// A node without choices is simply continued, to <see cref="NextNodeId"/> or to the end.
    /// </summary>
    public sealed class DialogueNode
    {
        readonly DialogueChoice[] _choices;

        public DialogueNode(string id, GameText speaker, GameText line, IReadOnlyList<DialogueChoice> choices, string nextNodeId)
            : this(id, speaker, line, choices, nextNodeId, 0f, -1)
        {
        }

        /// <param name="timeLimitSeconds">0 to wait for the player; otherwise the default choice is taken when the time runs out.</param>
        /// <param name="defaultChoiceIndex">The answer given for the player when the time runs out (GDD: events), -1 without a limit.</param>
        public DialogueNode(string id, GameText speaker, GameText line, IReadOnlyList<DialogueChoice> choices, string nextNodeId,
            float timeLimitSeconds, int defaultChoiceIndex)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A dialogue node needs an id.", nameof(id));
            }
            if (choices != null && choices.Count > 0 && nextNodeId != null)
            {
                throw new ArgumentException($"Node '{id}' has choices; they decide what follows, not a next node.", nameof(nextNodeId));
            }

            Id = id;
            Speaker = speaker ?? throw new ArgumentNullException(nameof(speaker));
            Line = line ?? throw new ArgumentNullException(nameof(line));
            _choices = choices != null ? new List<DialogueChoice>(choices).ToArray() : Array.Empty<DialogueChoice>();
            NextNodeId = nextNodeId;
            if (timeLimitSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeLimitSeconds), "A time limit cannot be negative.");
            }
            if (timeLimitSeconds > 0f && (defaultChoiceIndex < 0 || defaultChoiceIndex >= _choices.Length))
            {
                throw new ArgumentException($"Node '{id}' has a time limit, so it needs one of its choices as the default.", nameof(defaultChoiceIndex));
            }
            TimeLimitSeconds = timeLimitSeconds;
            DefaultChoiceIndex = timeLimitSeconds > 0f ? defaultChoiceIndex : -1;
        }

        public string Id { get; }
        public GameText Speaker { get; }
        public GameText Line { get; }
        public IReadOnlyList<DialogueChoice> Choices => _choices;
        public bool HasChoices => _choices.Length > 0;

        /// <summary>For nodes without choices: what follows when the player continues; null ends the conversation.</summary>
        public string NextNodeId { get; }

        /// <summary>Seconds the player has to answer; 0 waits for the player.</summary>
        public float TimeLimitSeconds { get; }

        public bool HasTimeLimit => TimeLimitSeconds > 0f;

        /// <summary>The answer taken when the time runs out; -1 without a time limit.</summary>
        public int DefaultChoiceIndex { get; }
    }
}

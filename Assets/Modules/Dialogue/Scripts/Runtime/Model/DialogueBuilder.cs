using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;

namespace LastFleet.Dialogue
{
    /// <summary>
    /// Writes a dialogue in reading order. Each <see cref="Say"/> starts a node; the choices and the
    /// next node given after it belong to that node:
    /// <code>
    /// new DialogueBuilder()
    ///     .Say("ask", scientist, Texts.Ask)
    ///         .Choice(Texts.Accept, "thanks", onAccepted)
    ///         .Choice(Texts.Decline, null)
    ///     .Say("thanks", scientist, Texts.Thanks)
    ///     .Build();
    /// </code>
    /// </summary>
    public sealed class DialogueBuilder
    {
        readonly List<DialogueNode> _nodes = new List<DialogueNode>();
        readonly List<DialogueChoice> _choices = new List<DialogueChoice>();
        string _id;
        GameText _speaker;
        GameText _line;
        string _nextNodeId;
        float _timeLimitSeconds;
        int _defaultChoiceIndex = -1;

        /// <summary>A single line, e.g. a radio message: one node that ends when continued.</summary>
        public static DialogueScript Message(GameText speaker, GameText line)
        {
            return new DialogueBuilder().Say("message", speaker, line).Build();
        }

        public DialogueBuilder Say(string id, GameText speaker, GameText line)
        {
            CloseNode();
            _id = id;
            _speaker = speaker;
            _line = line;
            return this;
        }

        /// <summary>An answer to the current node; null as next node ends the conversation.</summary>
        public DialogueBuilder Choice(GameText label, string nextNodeId, Action chosen = null)
        {
            RequireNode();
            _choices.Add(new DialogueChoice(label, nextNodeId, chosen));
            return this;
        }

        /// <summary>
        /// The player has this long to answer the current node, else the choice at this index is taken for them
        /// (GDD: events have a time limit and fall back to a default). Give it after the node's choices.
        /// </summary>
        public DialogueBuilder TimeLimit(float seconds, int defaultChoiceIndex)
        {
            RequireNode();
            _timeLimitSeconds = seconds;
            _defaultChoiceIndex = defaultChoiceIndex;
            return this;
        }

        /// <summary>For a node without choices: the node that follows when the player continues.</summary>
        public DialogueBuilder Then(string nextNodeId)
        {
            RequireNode();
            _nextNodeId = nextNodeId;
            return this;
        }

        public DialogueScript Build()
        {
            CloseNode();
            return new DialogueScript(_nodes);
        }

        void RequireNode()
        {
            if (_id == null)
            {
                throw new InvalidOperationException("Start a node with Say before adding choices or a next node.");
            }
        }

        void CloseNode()
        {
            if (_id == null)
            {
                return;
            }
            _nodes.Add(new DialogueNode(_id, _speaker, _line, _choices, _nextNodeId, _timeLimitSeconds, _defaultChoiceIndex));
            _choices.Clear();
            _id = null;
            _speaker = null;
            _line = null;
            _nextNodeId = null;
            _timeLimitSeconds = 0f;
            _defaultChoiceIndex = -1;
        }
    }
}

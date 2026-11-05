using System;
using System.Collections.Generic;

namespace LastFleet.Dialogue
{
    /// <summary>
    /// A conversation as a small graph of nodes, starting at the first one. Every reference between
    /// nodes is checked on construction, so a broken script fails when it is built, not mid-conversation.
    /// Build it with <see cref="DialogueBuilder"/>.
    /// </summary>
    public sealed class DialogueScript
    {
        readonly Dictionary<string, DialogueNode> _nodesById = new Dictionary<string, DialogueNode>();

        public DialogueScript(IReadOnlyList<DialogueNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("A dialogue needs at least one node.", nameof(nodes));
            }

            foreach (DialogueNode node in nodes)
            {
                if (!_nodesById.TryAdd(node.Id, node))
                {
                    throw new ArgumentException($"The node id '{node.Id}' is used twice.", nameof(nodes));
                }
            }
            foreach (DialogueNode node in nodes)
            {
                Validate(node.Id, node.NextNodeId);
                foreach (DialogueChoice choice in node.Choices)
                {
                    Validate(node.Id, choice.NextNodeId);
                }
            }
            Start = nodes[0];
        }

        public DialogueNode Start { get; }
        public int NodeCount => _nodesById.Count;

        public DialogueNode GetNode(string id)
        {
            if (id == null || !_nodesById.TryGetValue(id, out DialogueNode node))
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "No such node in this dialogue.");
            }
            return node;
        }

        void Validate(string fromId, string targetId)
        {
            if (targetId != null && !_nodesById.ContainsKey(targetId))
            {
                throw new ArgumentException($"Node '{fromId}' leads to the unknown node '{targetId}'.");
            }
        }
    }
}

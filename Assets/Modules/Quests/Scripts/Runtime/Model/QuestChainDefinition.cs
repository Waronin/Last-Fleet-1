using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;

namespace LastFleet.Quests
{
    /// <summary>
    /// A quest chain (GDD: quest lines): steps played one after another, each in its own star system.
    /// Definitions are plain data built in code (see <c>MeltdownQuest</c>); progress lives in <see cref="QuestChain"/>.
    /// </summary>
    public sealed class QuestChainDefinition
    {
        readonly QuestStepDefinition[] _steps;

        public QuestChainDefinition(string id, GameTextKey title, IReadOnlyList<QuestStepDefinition> steps, bool winsRun = false)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A quest chain needs an id.", nameof(id));
            }
            if (steps == null || steps.Count == 0)
            {
                throw new ArgumentException($"Quest chain '{id}' needs at least one step.", nameof(steps));
            }

            Id = id;
            Title = title;
            WinsRun = winsRun;
            _steps = new List<QuestStepDefinition>(steps).ToArray();
        }

        public string Id { get; }
        public GameText Title { get; }
        public IReadOnlyList<QuestStepDefinition> Steps => _steps;

        /// <summary>Completing this chain wins the run (GDD: an end scenario).</summary>
        public bool WinsRun { get; }
    }
}

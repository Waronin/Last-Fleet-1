using System;
using LastFleet.Shared.Text;

namespace LastFleet.Quests
{
    /// <summary>
    /// A thing a quest chain needs carried from step to step, such as a mineral sample. It is not part
    /// of the fleet inventory: it rides aboard one ship (<see cref="QuestCargo"/>) and is lost with it.
    /// </summary>
    public sealed class QuestItem
    {
        public QuestItem(string id, GameTextKey name)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("A quest item needs an id.", nameof(id));
            }
            Id = id;
            Name = name;
        }

        public string Id { get; }
        public GameText Name { get; }
    }
}

using System;
using LastFleet.Dialogue;
using LastFleet.Shared.Text;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// A quest giver aboard a station: a hail opens their conversation instead of the plain answer,
    /// as often as the player hails, until the step says there is nothing more to talk about.
    /// </summary>
    public sealed class QuestStationContact : IStationContact
    {
        readonly DialogueService _dialogue;
        readonly Func<DialogueScript> _conversation;

        public QuestStationContact(DialogueService dialogue, Func<DialogueScript> conversation)
        {
            _dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
            _conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
            HasSomethingToSay = true;
        }

        public bool HasSomethingToSay { get; private set; }

        public void FallSilent()
        {
            HasSomethingToSay = false;
        }

        public GameText Answer(ZoneStation station)
        {
            _dialogue.Open(_conversation());
            return QuestTexts.ContactAnswered.Format(("station", station.Name));
        }
    }
}

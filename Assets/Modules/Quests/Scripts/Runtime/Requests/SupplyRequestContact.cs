using System;
using LastFleet.Dialogue;
using LastFleet.Mining;
using LastFleet.Shared.Text;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// The people of a station with a request: a hail or a docked crew hears their offer and can accept it;
    /// later calls remind the player what they are waiting for. Silent once the request is fulfilled or expired.
    /// </summary>
    public sealed class SupplyRequestContact : IStationContact
    {
        readonly DialogueService _dialogue;
        readonly SupplyRequest _request;
        readonly Func<SupplyRequest, bool> _isCovered;

        /// <param name="isCovered">Whether the fleet has enough in stock; then a docked crew just hands it over.</param>
        public SupplyRequestContact(DialogueService dialogue, SupplyRequest request, Func<SupplyRequest, bool> isCovered)
        {
            _isCovered = isCovered ?? throw new ArgumentNullException(nameof(isCovered));
            _dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public bool HasSomethingToSay => _request.IsOpen;

        public GameText Answer(ZoneStation station)
        {
            if (_request.State == SupplyRequestState.Accepted && _isCovered(_request))
            {
                // Nothing to remind of: the goods are handed over as soon as a ship is docked.
                return null;
            }
            _dialogue.Open(_request.State == SupplyRequestState.Offered ? Offer(station) : Reminder(station));
            return QuestTexts.ContactAnswered.Format(("station", station.Name));
        }

        /// <summary>What the station pays, e.g. "450 fuel" or "an upgrade module".</summary>
        public static GameText RewardText(SupplyRequest request)
        {
            return request.RewardKind == SupplyRequestRewardKind.UpgradeModule
                ? QuestTexts.RequestRewardModule
                : QuestTexts.RequestAmount.Format(("amount", request.RewardAmount), ("supply", MiningTexts.Supplies.For(request.RewardSupply)));
        }

        public static GameText WantedText(SupplyRequest request)
        {
            return QuestTexts.RequestAmount.Format(("amount", request.WantedAmount), ("supply", MiningTexts.Supplies.For(request.Wanted)));
        }

        public static GameText Speaker(ZoneStation station)
        {
            return QuestTexts.ResidentsSpeaker.Format(("station", station.Name));
        }

        DialogueScript Offer(ZoneStation station)
        {
            GameText speaker = Speaker(station);
            return new DialogueBuilder()
                .Say("offer", speaker, QuestTexts.RequestOffer.Format(("wanted", WantedText(_request)), ("reward", RewardText(_request))))
                .Choice(QuestTexts.RequestAccept, "accepted", _request.Accept)
                .Choice(QuestTexts.RequestDecline, "declined")
                .Say("accepted", speaker, QuestTexts.RequestAcceptedReply)
                .Say("declined", speaker, QuestTexts.RequestDeclinedReply)
                .Build();
        }

        DialogueScript Reminder(ZoneStation station)
        {
            return DialogueBuilder.Message(Speaker(station), QuestTexts.RequestWaiting.Format(("wanted", WantedText(_request))));
        }
    }
}

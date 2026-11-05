using LastFleet.Shared.Text;

namespace LastFleet.Zones
{
    /// <summary>
    /// Someone aboard a station who answers a hail with more than its attitude, such as a quest giver
    /// opening a conversation. Zones only know the contract; quests and dialogues fill it in.
    /// </summary>
    public interface IStationContact
    {
        /// <summary>False once the contact has nothing (more) to say; a hail then gets the plain answer.</summary>
        bool HasSomethingToSay { get; }

        /// <summary>Answers the hail, e.g. by opening a dialogue; returns short feedback for the player, or null.</summary>
        GameText Answer(ZoneStation station);
    }
}

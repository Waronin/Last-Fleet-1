using LastFleet.Shared.Text;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// The logic of one quest step attempt. The director creates it when the step is placed in its
    /// system and drives it; the step reports back through its <see cref="QuestStepContext"/> with
    /// <see cref="QuestStepContext.Complete"/> or <see cref="QuestStepContext.Fail"/>. Reusable step
    /// types (see the Steps folder) take their stations, texts and numbers as parameters.
    /// </summary>
    public interface IQuestStep
    {
        /// <summary>The step became active; the fleet may be anywhere in the galaxy.</summary>
        void Begin(QuestStepContext context);

        /// <summary>The fleet is in the step's system: on arrival, or right after Begin if it is already there.</summary>
        void OnSystemEntered(SystemZones system);

        /// <summary>The fleet jumped out of the step's system; its zone views are gone.</summary>
        void OnSystemLeft();

        /// <summary>Game time passes, also while the fleet is elsewhere.</summary>
        void Tick(float deltaSeconds);

        /// <summary>Last call of this attempt (done, failed or run over): release subscriptions and contacts.</summary>
        void End();

        /// <summary>A live line for the quest log, e.g. a countdown; null when there is nothing to add.</summary>
        GameText Status { get; }
    }
}

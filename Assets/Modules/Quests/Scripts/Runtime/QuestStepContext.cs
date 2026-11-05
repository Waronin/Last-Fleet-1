using System;
using LastFleet.Dialogue;
using LastFleet.Shared.Randomness;
using LastFleet.Shared.Text;
using LastFleet.Zones;

namespace LastFleet.Quests
{
    /// <summary>
    /// What a step attempt knows about its place in the run and how it reports back. Reports after the
    /// attempt is over (a late event, a second call) are ignored, so a step cannot finish twice.
    /// </summary>
    public sealed class QuestStepContext
    {
        readonly Action<QuestStepContext> _completed;
        readonly Action<QuestStepContext, GameText> _failed;
        readonly Action<GameText> _notice;
        readonly Action<QuestStepContext> _zoneMarked;

        internal QuestStepContext(QuestChain chain, QuestServices services, QuestCargo cargo, ulong seed, Action<QuestStepContext> completed,
            Action<QuestStepContext, GameText> failed, Action<GameText> notice, Action<QuestStepContext> zoneMarked)
        {
            Chain = chain;
            SystemIndex = chain.StepSystemIndex;
            Services = services;
            Cargo = cargo;
            Seed = seed;
            _completed = completed;
            _failed = failed;
            _notice = notice;
            _zoneMarked = zoneMarked;
            MarkedZoneIndex = -1;
        }

        public QuestChain Chain { get; }

        /// <summary>The star system the step takes place in.</summary>
        public int SystemIndex { get; }

        public QuestServices Services { get; }

        /// <summary>Quest items aboard ships of the fleet; they outlast steps, so one step can hand them to the next.</summary>
        public QuestCargo Cargo { get; }

        /// <summary>Seed of this attempt, for the step's own random choices such as where its station lies.</summary>
        public ulong Seed { get; }

        public bool IsOver { get; private set; }

        /// <summary>The zone of the step's system where the player has to go, or -1 while there is none.</summary>
        public int MarkedZoneIndex { get; private set; }

        /// <summary>Shows the player where the step takes place: on the system map and in the quest log.</summary>
        public void MarkZone(int pointOfInterestIndex)
        {
            if (IsOver || pointOfInterestIndex == MarkedZoneIndex)
            {
                return;
            }
            MarkedZoneIndex = pointOfInterestIndex;
            _zoneMarked(this);
        }

        /// <summary>The view of a zone of the step's system: only while the fleet is there and the zone is shown.</summary>
        public bool TryGetZoneView(int pointOfInterestIndex, out ZoneView view)
        {
            ZoneWorld world = Services.ZoneWorld;
            if (world.CurrentSystem != null && world.CurrentSystem.SystemIndex == SystemIndex)
            {
                return world.TryGetView(pointOfInterestIndex, out view);
            }
            view = null;
            return false;
        }

        public DeterministicRandom CreateRandom(ulong salt)
        {
            return new DeterministicRandom(DeterministicRandom.DeriveSeed(Seed, salt));
        }

        public void Complete()
        {
            if (IsOver)
            {
                return;
            }
            IsOver = true;
            _completed(this);
        }

        /// <summary>The step failed; the reason is told to the player, then the step's failure rule applies.</summary>
        public void Fail(GameText reason)
        {
            if (IsOver)
            {
                return;
            }
            IsOver = true;
            _failed(this, reason);
        }

        public void Notify(GameText message)
        {
            _notice(message);
        }

        /// <summary>Opens a conversation or radio call; it queues behind the one already shown.</summary>
        public void Say(DialogueScript script)
        {
            Services.Dialogue.Open(script);
        }
    }
}

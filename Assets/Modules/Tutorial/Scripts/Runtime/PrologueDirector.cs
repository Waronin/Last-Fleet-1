using System;
using System.Collections.Generic;
using LastFleet.Dialogue;
using LastFleet.Mining;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Tutorial
{
    /// <summary>
    /// The optional prologue (GDD: Tutorial) in the start system. Offered as a radio call at the start of
    /// a run. Phase 1, civilian fleet only: explore a station, dock, search or hail it, mine and deliver.
    /// Phase 2: a few Pronin fighters jump in, the military follows shortly after, the fight is easy.
    /// It ends with the first fleet jump. The run owner reacts to the events: it brings in the military
    /// ships, lifts the jump hold and starts the normal game loop when the prologue is skipped or done.
    /// </summary>
    public sealed class PrologueDirector : MonoBehaviour
    {
        const float RaidDistanceFraction = 0.6f;

        [SerializeField] DialogueService _dialogue;
        [SerializeField] ShipRegistry _fleet;
        [SerializeField] ShipRegistry _pronin;
        [SerializeField] ZoneWorld _zoneWorld;
        [SerializeField] SearchService _search;
        [SerializeField] MiningCoordinator _mining;
        [SerializeField] ShipAgent _proninFighterPrefab;
        [SerializeField, Min(1)] int _raidFighterCount = 3;
        [Tooltip("Seconds after the raid until the military jumps in.")]
        [SerializeField, Min(1f)] float _militaryDelaySeconds = 20f;

        readonly List<ShipAgent> _raiders = new List<ShipAgent>();
        PrologueConditions _conditions;
        TutorialSequence _sequence;
        bool _isOffered;

        /// <summary>The running prologue, or null when none is running.</summary>
        public TutorialSequence Sequence => IsRunning ? _sequence : null;
        public bool IsRunning => _sequence != null && _sequence.HasStarted && !_sequence.IsFinished;

        /// <summary>The player skipped the prologue at the offer or ended it early: start the normal game.</summary>
        public event Action Skipped;

        /// <summary>The cruiser and its fighters should jump in now.</summary>
        public event Action MilitaryRequested;

        /// <summary>The last step began: the fleet may charge the jump drive.</summary>
        public event Action JumpCleared;

        /// <summary>A line for the notice feed.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_dialogue == null || _fleet == null || _pronin == null || _zoneWorld == null || _search == null || _mining == null ||
                _proninFighterPrefab == null)
            {
                Debug.LogError("PrologueDirector needs the dialogue service, both ship registries, the zone world, search, mining and the Pronin fighter prefab.", this);
                enabled = false;
                return;
            }
            _conditions = new PrologueConditions(_fleet, _zoneWorld, _search, _mining);
        }

        void Update()
        {
            if (IsRunning)
            {
                _sequence.Tick(Time.deltaTime);
            }
        }

        /// <summary>Asks at the start of a run whether to play the prologue; call after the fleet is in the start system.</summary>
        public void Offer()
        {
            Cancel();
            _isOffered = true;
            GameText captain = PrologueTexts.LinerCaptain;
            _dialogue.Open(new DialogueBuilder()
                .Say("offer", captain, PrologueTexts.OfferLine)
                .Choice(PrologueTexts.OfferPlay, null, Play)
                .Choice(PrologueTexts.OfferSkip, null, Skip)
                .Build());
        }

        /// <summary>Drops the prologue without events, e.g. when a new run starts.</summary>
        public void Cancel()
        {
            _isOffered = false;
            _sequence = null;
            _raiders.Clear();
        }

        /// <summary>The player ends the prologue early; the normal game starts where the fleet is.</summary>
        public void EndEarly()
        {
            if (!IsRunning)
            {
                return;
            }
            Cancel();
            Skipped?.Invoke();
        }

        /// <summary>The player skips the current step.</summary>
        public void SkipStep()
        {
            if (IsRunning)
            {
                _sequence.Advance();
            }
        }

        /// <summary>
        /// The fleet jumped. Returns true when this finished the prologue, so the run owner starts the
        /// normal game in the target system.
        /// </summary>
        public bool CompleteOnFleetJump()
        {
            if (!IsRunning)
            {
                return false;
            }
            // The drive is held until the last step, so a jump always comes from there.
            while (IsRunning)
            {
                _sequence.Advance();
            }
            return true;
        }

        void Play()
        {
            if (!_isOffered)
            {
                return;
            }
            _isOffered = false;
            _sequence = CreateSequence();
            _sequence.StepStarted += OnStepStarted;
            _sequence.Finished += OnFinished;
            _sequence.Start();
        }

        void Skip()
        {
            if (!_isOffered)
            {
                return;
            }
            _isOffered = false;
            Skipped?.Invoke();
            _dialogue.Open(DialogueBuilder.Message(PrologueTexts.Admiral, PrologueTexts.SkippedLine));
        }

        TutorialSequence CreateSequence()
        {
            GameText captain = PrologueTexts.LinerCaptain;
            GameText admiral = PrologueTexts.Admiral;
            return new TutorialSequence(new[]
            {
                new TutorialStep(captain, PrologueTexts.ExploreLine, PrologueTexts.ExploreObjective, _conditions.IsStationZoneRevealed),
                new TutorialStep(captain, PrologueTexts.DockLine, PrologueTexts.DockObjective, _conditions.WasStationVisited),
                new TutorialStep(captain, PrologueTexts.SearchLine, PrologueTexts.SearchObjective, _conditions.WasStationSearchedOrHailed),
                new TutorialStep(captain, PrologueTexts.MineLine, PrologueTexts.MineObjective, _conditions.AreHarvestersMining),
                new TutorialStep(captain, PrologueTexts.DeliverLine, PrologueTexts.DeliverObjective, _conditions.HasDeliveredSupplies,
                    began: _conditions.RememberSupplies),
                // The military always takes its time, so the raid is felt even with every small ship at home.
                new TutorialStep(captain, PrologueTexts.RaidLine, PrologueTexts.RaidObjective, null, _militaryDelaySeconds, LaunchRaid),
                new TutorialStep(admiral, PrologueTexts.MilitaryLine, PrologueTexts.MilitaryObjective, _conditions.AreFightersLaunched,
                    began: () => MilitaryRequested?.Invoke()),
                new TutorialStep(admiral, PrologueTexts.FightLine, PrologueTexts.FightObjective, AreRaidersGone),
                new TutorialStep(admiral, PrologueTexts.JumpLine, PrologueTexts.JumpObjective, null,
                    began: () => JumpCleared?.Invoke())
            });
        }

        void OnStepStarted(TutorialStep step)
        {
            _dialogue.Open(DialogueBuilder.Message(step.Speaker, step.Line));
        }

        void OnFinished()
        {
            _raiders.Clear();
            _dialogue.Open(DialogueBuilder.Message(PrologueTexts.LinerCaptain, PrologueTexts.FarewellLine));
        }

        /// <summary>Pronin fighters at the edge of the jump point zone, where the fleet arrived; they hunt on their own.</summary>
        void LaunchRaid()
        {
            SystemZones system = _zoneWorld.CurrentSystem;
            if (system == null || !_zoneWorld.TryGetView(system.Layout.ArrivalPointOfInterestIndex, out ZoneView zone))
            {
                Debug.LogError("The prologue raid found no jump point zone to arrive in.", this);
                return;
            }

            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 position = new Vector3(direction.x, 0f, direction.y) * (zone.Zone.RadiusUnits * RaidDistanceFraction);
            Squad raid = _pronin.SpawnSquad(_proninFighterPrefab, zone, position, ShipMotion.DirectionToHeading(-direction), _raidFighterCount);
            _raiders.Clear();
            _raiders.AddRange(raid.Members);
            Notice?.Invoke(PrologueTexts.RaidNotice.Format(("count", _raiders.Count)));
        }

        bool AreRaidersGone()
        {
            foreach (ShipAgent raider in _raiders)
            {
                // Unity's overloaded equality treats a destroyed ship as null.
                if (raider != null && _pronin.Contains(raider))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

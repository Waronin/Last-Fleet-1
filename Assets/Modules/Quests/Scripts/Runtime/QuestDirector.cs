using System;
using System.Collections.Generic;
using LastFleet.Combat;
using LastFleet.Dialogue;
using LastFleet.Galaxy;
using LastFleet.Shared.Randomness;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Stations;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// Runs the quest chains of a run (GDD: quests). At the start of a run every opening chain gets its
    /// first step in the start system; when a step is done, the next one is placed in another system
    /// (<see cref="QuestSystemPicker"/>) and the galaxy map and jump warning show it. Whoever owns the
    /// run calls <see cref="BeginRun"/> and <see cref="SystemEntered"/>; the director drives the step
    /// logic and applies each step's failure rule.
    /// </summary>
    public sealed class QuestDirector : MonoBehaviour
    {
        const ulong StepSeedSalt = 7000UL;
        const ulong PlacementSalt = 7100UL;

        [SerializeField] ShipRegistry _fleet;
        [SerializeField] ShipRegistry _pronin;
        [SerializeField] ZoneWorld _zoneWorld;
        [SerializeField] SearchService _search;
        [SerializeField] DialogueService _dialogue;
        [SerializeField] ShipAgent _shuttlePrefab;
        [SerializeField] ShipAgent _proninFighterPrefab;
        [Tooltip("Fighters of a station that guards something, on the enemy side but without hunting the fleet.")]
        [SerializeField] ShipAgent _guardFighterPrefab;
        [SerializeField] ExplosionEffect _explosionPrefab;

        readonly List<QuestChain> _chains = new List<QuestChain>();
        readonly Dictionary<QuestChain, ActiveStep> _activeSteps = new Dictionary<QuestChain, ActiveStep>();
        readonly List<ActiveStep> _ticking = new List<ActiveStep>();
        readonly QuestCargo _cargo = new QuestCargo();
        QuestServices _services;
        GalaxyLayout _galaxy;
        RunZoneRegistry _runZones;
        SystemZones _currentSystem;

        public IReadOnlyList<QuestChain> Chains => _chains;

        /// <summary>Quest items aboard ships of the fleet.</summary>
        public QuestCargo Cargo => _cargo;

        public event Action<GameText> Notice;

        /// <summary>A step was placed, finished or failed: the systems with a quest marker changed.</summary>
        public event Action MarkersChanged;

        void Awake()
        {
            if (_fleet == null || _pronin == null || _zoneWorld == null || _search == null || _dialogue == null ||
                _shuttlePrefab == null || _proninFighterPrefab == null || _guardFighterPrefab == null || _explosionPrefab == null)
            {
                Debug.LogError("QuestDirector needs both ship registries, the zone world, the search and dialogue services and the shuttle, " +
                               "Pronin fighter, guard fighter and explosion prefabs.", this);
                enabled = false;
                return;
            }
            _services = new QuestServices(_fleet, _pronin, _zoneWorld, _search, _dialogue,
                new QuestPrefabs(_shuttlePrefab, _proninFighterPrefab, _guardFighterPrefab, _explosionPrefab));
        }

        void OnEnable()
        {
            _fleet.ShipRemoved += _cargo.ShipRemoved;
        }

        void OnDisable()
        {
            _fleet.ShipRemoved -= _cargo.ShipRemoved;
            EndAllSteps();
        }

        /// <summary>Starts the given chains for a new run, each with its first step in the start system.</summary>
        public void BeginRun(GalaxyLayout galaxy, RunZoneRegistry runZones, IReadOnlyList<QuestChainDefinition> openingChains)
        {
            _galaxy = galaxy ?? throw new ArgumentNullException(nameof(galaxy));
            _runZones = runZones ?? throw new ArgumentNullException(nameof(runZones));
            EndAllSteps();
            _chains.Clear();
            _currentSystem = null;
            _dialogue.Clear();
            _cargo.Clear();

            foreach (QuestChainDefinition definition in openingChains)
            {
                QuestChain chain = new QuestChain(definition);
                _chains.Add(chain);
                PlaceStep(chain, galaxy.StartSystemIndex);
            }
            MarkersChanged?.Invoke();
        }

        /// <summary>
        /// The fleet arrived in a system; call after its zones are shown, so quest stations in revealed
        /// zones appear at once. Steps of the system left behind are told first.
        /// </summary>
        public void SystemEntered(SystemZones system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }
            if (_currentSystem != null && _currentSystem.SystemIndex != system.SystemIndex)
            {
                foreach (ActiveStep active in SnapshotOfSteps())
                {
                    if (active.Context.SystemIndex == _currentSystem.SystemIndex && !active.Context.IsOver)
                    {
                        active.Step.OnSystemLeft();
                    }
                }
            }

            _currentSystem = system;
            foreach (ActiveStep active in SnapshotOfSteps())
            {
                if (active.Context.SystemIndex == system.SystemIndex && !active.Context.IsOver)
                {
                    active.Step.OnSystemEntered(system);
                }
            }
        }

        /// <summary>The label of the active quest step in the system, e.g. "Meltdown – step 2/5".</summary>
        public bool TryGetMarker(int systemIndex, out GameText marker)
        {
            foreach (QuestChain chain in _chains)
            {
                if (chain.IsActive && chain.StepSystemIndex == systemIndex)
                {
                    marker = QuestTexts.Marker.Format(("chain", chain.Definition.Title), ("step", chain.StepNumber), ("count", chain.StepCount));
                    return true;
                }
            }
            marker = null;
            return false;
        }

        public void CollectMarkedSystems(List<int> systemIndices)
        {
            systemIndices.Clear();
            foreach (QuestChain chain in _chains)
            {
                if (chain.IsActive && chain.IsPlaced && !systemIndices.Contains(chain.StepSystemIndex))
                {
                    systemIndices.Add(chain.StepSystemIndex);
                }
            }
        }

        /// <summary>Zones of the system where an active step wants the player, for the system map.</summary>
        public void CollectMarkedZones(int systemIndex, List<int> pointOfInterestIndices)
        {
            pointOfInterestIndices.Clear();
            foreach (ActiveStep active in _ticking)
            {
                int zone = active.Context.MarkedZoneIndex;
                if (active.Context.SystemIndex == systemIndex && zone >= 0 && !pointOfInterestIndices.Contains(zone))
                {
                    pointOfInterestIndices.Add(zone);
                }
            }
        }

        /// <summary>Name of the zone the chain's current step points to, or null while it points nowhere yet.</summary>
        public string MarkedZoneName(QuestChain chain)
        {
            if (!_activeSteps.TryGetValue(chain, out ActiveStep active) || active.Context.MarkedZoneIndex < 0 ||
                !_runZones.TryGetSystem(active.Context.SystemIndex, out SystemZones system))
            {
                return null;
            }
            return system.Zones[active.Context.MarkedZoneIndex].PointOfInterest.Name;
        }

        /// <summary>The live line of the chain's current step, e.g. a countdown, or null.</summary>
        public GameText StatusOf(QuestChain chain)
        {
            return _activeSteps.TryGetValue(chain, out ActiveStep active) ? active.Step.Status : null;
        }

        public string SystemName(int systemIndex)
        {
            return _galaxy != null && systemIndex >= 0 ? _galaxy.Systems[systemIndex].Name : string.Empty;
        }

        public bool IsFleetIn(int systemIndex)
        {
            return _currentSystem != null && _currentSystem.SystemIndex == systemIndex;
        }

        void Update()
        {
            if (_ticking.Count == 0)
            {
                return;
            }
            float deltaSeconds = Time.deltaTime;
            foreach (ActiveStep active in SnapshotOfSteps())
            {
                if (!active.Context.IsOver)
                {
                    active.Step.Tick(deltaSeconds);
                }
            }
        }

        void PlaceStep(QuestChain chain, int systemIndex)
        {
            chain.PlaceCurrentStep(systemIndex);
            StartAttempt(chain);
        }

        void StartAttempt(QuestChain chain)
        {
            QuestStepDefinition definition = chain.CurrentStep;
            if (!definition.IsPlayable)
            {
                return;
            }

            int chainIndex = _chains.IndexOf(chain);
            ulong seed = DeterministicRandom.DeriveSeed(_galaxy.Seed,
                StepSeedSalt + (ulong)chainIndex * 1000UL + (ulong)chain.StepIndex * 10UL + (ulong)chain.Attempt);
            QuestStepContext context = new QuestStepContext(chain, _services, _cargo, seed, OnStepCompleted, OnStepFailed, RaiseNotice,
                _ => MarkersChanged?.Invoke());
            ActiveStep active = new ActiveStep(definition.CreateStep(), context);
            _activeSteps[chain] = active;
            _ticking.Add(active);

            active.Step.Begin(context);
            if (!context.IsOver && IsFleetIn(chain.StepSystemIndex))
            {
                active.Step.OnSystemEntered(_currentSystem);
            }
        }

        void OnStepCompleted(QuestStepContext context)
        {
            QuestChain chain = context.Chain;
            EndAttempt(chain);
            GameText title = chain.CurrentStep.Title;
            if (!chain.CompleteStep())
            {
                RaiseNotice(QuestTexts.ChainCompleted.Format(("chain", chain.Definition.Title)));
                MarkersChanged?.Invoke();
                return;
            }

            RaiseNotice(QuestTexts.StepCompleted.Format(("chain", chain.Definition.Title), ("title", title)));
            ulong placementSeed = DeterministicRandom.DeriveSeed(_galaxy.Seed, PlacementSalt + (ulong)_chains.IndexOf(chain) * 100UL + (ulong)chain.StepIndex);
            int next = QuestSystemPicker.Pick(_galaxy, context.SystemIndex, IsVisited, placementSeed);
            PlaceStep(chain, next);
            RaiseNotice(QuestTexts.StepPlaced.Format(("chain", chain.Definition.Title), ("step", chain.StepNumber),
                ("title", chain.CurrentStep.Title), ("system", GameText.Verbatim(SystemName(next)))));
            MarkersChanged?.Invoke();
        }

        void OnStepFailed(QuestStepContext context, GameText reason)
        {
            QuestChain chain = context.Chain;
            EndAttempt(chain);
            GameText title = chain.CurrentStep.Title;
            if (chain.FailStep() == QuestFailure.EndsChain)
            {
                RaiseNotice(QuestTexts.ChainFailed.Format(("chain", chain.Definition.Title), ("reason", reason)));
                MarkersChanged?.Invoke();
                return;
            }

            RaiseNotice(QuestTexts.StepRetry.Format(("chain", chain.Definition.Title), ("title", title), ("reason", reason)));
            StartAttempt(chain);
            MarkersChanged?.Invoke();
        }

        void EndAttempt(QuestChain chain)
        {
            if (_activeSteps.TryGetValue(chain, out ActiveStep active))
            {
                _activeSteps.Remove(chain);
                _ticking.Remove(active);
                active.Step.End();
            }
        }

        void EndAllSteps()
        {
            foreach (ActiveStep active in SnapshotOfSteps())
            {
                active.Step.End();
            }
            _activeSteps.Clear();
            _ticking.Clear();
        }

        bool IsVisited(int systemIndex)
        {
            return _runZones.TryGetSystem(systemIndex, out SystemZones _);
        }

        void RaiseNotice(GameText message)
        {
            Notice?.Invoke(message);
        }

        /// <summary>Steps may finish while they are driven, which changes the list.</summary>
        ActiveStep[] SnapshotOfSteps()
        {
            return _ticking.ToArray();
        }

        sealed class ActiveStep
        {
            public ActiveStep(IQuestStep step, QuestStepContext context)
            {
                Step = step;
                Context = context;
            }

            public IQuestStep Step { get; }
            public QuestStepContext Context { get; }
        }
    }
}

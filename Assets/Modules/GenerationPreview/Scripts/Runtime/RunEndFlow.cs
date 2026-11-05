using LastFleet.Dialogue;
using LastFleet.People;
using LastFleet.Quests;
using LastFleet.RunEnd;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.GenerationPreview
{
    /// <summary>
    /// Ends the run when the quests say so: a won run waits until the last radio call is closed, then the
    /// end screen shows what is left of the fleet and the game stops behind it. A lost run means the fleet
    /// is gone. The end screen's button starts the next run.
    /// </summary>
    public sealed class RunEndFlow : MonoBehaviour
    {
        [SerializeField] GenerationPreviewController _controller;
        [SerializeField] QuestDirector _quests;
        [SerializeField] DialogueService _dialogue;
        [SerializeField] ShipRegistry _fleet;
        [SerializeField] FleetPopulation _population;
        [SerializeField] RunEndWindow _window;

        RunSummary _pending;
        float _runStartTime;

        void Awake()
        {
            if (_controller == null || _quests == null || _dialogue == null || _fleet == null || _population == null || _window == null)
            {
                Debug.LogError("RunEndFlow needs the preview controller, quest director, dialogue service, fleet registry, population and end screen.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _quests.RunWon += OnRunWon;
            _quests.RunLost += OnRunLost;
            _controller.RunStarted += OnRunStarted;
            _window.NewRunRequested += OnNewRunRequested;
        }

        void OnDisable()
        {
            _quests.RunWon -= OnRunWon;
            _quests.RunLost -= OnRunLost;
            _controller.RunStarted -= OnRunStarted;
            _window.NewRunRequested -= OnNewRunRequested;
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (_pending == null || _dialogue.Current != null)
            {
                return;
            }
            _window.Show(_pending);
            _pending = null;
            Time.timeScale = 0f;
        }

        void OnRunStarted()
        {
            _runStartTime = Time.time;
            _pending = null;
        }

        void OnRunWon(QuestChain chain)
        {
            _pending = new RunSummary(RunOutcome.Won, QuestTexts.ChainCompleted.Format(("chain", chain.Definition.Title)),
                _population.TotalPeople, _fleet.Ships.Count, Time.time - _runStartTime);
        }

        /// <summary>The fleet went down: nobody and nothing is left.</summary>
        void OnRunLost(GameText reason)
        {
            _dialogue.Clear();
            _pending = new RunSummary(RunOutcome.Lost, reason, 0, 0, Time.time - _runStartTime);
        }

        void OnNewRunRequested()
        {
            _window.Close();
            Time.timeScale = 1f;
            _controller.StartNextRun();
        }
    }
}

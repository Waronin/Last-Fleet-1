using System;
using System.Collections.Generic;
using System.IO;
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
    /// Ends the run: a won run when the quests say so, after the last radio call is closed; a lost run when a quest
    /// step says so or the fleet's last capital ship falls (GDD). The run's account is saved to the run history at
    /// once and compared with the earlier runs; the end screen shows both and the game stops behind it. The end
    /// screen's button starts the next run.
    /// </summary>
    public sealed class RunEndFlow : MonoBehaviour
    {
        const int LatestRunsShown = 5;

        [SerializeField] GenerationPreviewController _controller;
        [SerializeField] QuestDirector _quests;
        [SerializeField] DialogueService _dialogue;
        [SerializeField] ShipRegistry _fleet;
        [SerializeField] FleetPopulation _population;
        [SerializeField] RunEndWindow _window;
        [SerializeField] RunTallyRecorder _tally;
        [SerializeField] CapitalShipLossWatch _lossWatch;
        [SerializeField] RunScoreRules _scoreRules = new RunScoreRules();

        RunHistoryStore _store;
        RunHistory _history;
        // False when the history file exists but could not be read: saving would overwrite earlier runs.
        bool _canSaveHistory = true;
        RunEndReport _pending;
        float _runStartTime;

        void Awake()
        {
            if (_controller == null || _quests == null || _dialogue == null || _fleet == null || _population == null || _window == null ||
                _tally == null || _lossWatch == null)
            {
                Debug.LogError("RunEndFlow needs the preview controller, quest director, dialogue service, fleet registry, population, " +
                               "end screen, run tally and capital ship loss watch.", this);
                enabled = false;
                return;
            }
            _store = new RunHistoryStore(Path.Combine(Application.persistentDataPath, RunHistoryStore.DefaultFileName));
        }

        void OnEnable()
        {
            _quests.RunWon += OnRunWon;
            _quests.RunLost += OnRunLost;
            _lossWatch.FleetDestroyed += OnFleetDestroyed;
            _controller.RunStarted += OnRunStarted;
            _window.NewRunRequested += OnNewRunRequested;
        }

        void OnDisable()
        {
            _quests.RunWon -= OnRunWon;
            _quests.RunLost -= OnRunLost;
            _lossWatch.FleetDestroyed -= OnFleetDestroyed;
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
            _lossWatch.ResetWatch();
        }

        bool IsRunOver => _pending != null || _window.IsOpen;

        float ElapsedSeconds => Time.time - _runStartTime;

        void OnRunWon(QuestChain chain)
        {
            if (IsRunOver)
            {
                return;
            }
            Finish(RunSummary.Won(chain.Definition.Id, chain.Definition.Title, _population.TotalPeople, _fleet.Ships.Count, ElapsedSeconds,
                _tally.Tally, _scoreRules));
        }

        void OnRunLost(GameText reason)
        {
            Lose(reason);
        }

        void OnFleetDestroyed()
        {
            Lose(RunEndTexts.CapitalShipsLost.Format());
        }

        void Lose(GameText reason)
        {
            if (IsRunOver)
            {
                return;
            }
            _dialogue.Clear();
            Finish(RunSummary.Lost(reason, ElapsedSeconds, _tally.Tally, _scoreRules));
        }

        /// <summary>Compares with the earlier runs, then saves this one right away, before the screen even shows.</summary>
        void Finish(RunSummary summary)
        {
            RunHistory history = History;
            List<RunRecord> earlierRuns = new List<RunRecord>(history.Runs);
            RunComparison comparison = RunComparison.Against(earlierRuns, summary);
            history.Add(RunRecord.From(summary, DateTime.UtcNow));
            SaveHistory(history);
            _pending = new RunEndReport(summary, comparison, history.Newest(LatestRunsShown));
        }

        RunHistory History => _history ??= LoadHistory();

        RunHistory LoadHistory()
        {
            try
            {
                return _store.Load();
            }
            catch (InvalidDataException exception)
            {
                return SetAsideUnreadableHistory(exception);
            }
            catch (IOException exception)
            {
                _canSaveHistory = false;
                Debug.LogError($"The run history '{_store.Path}' could not be read; this session will not save runs. {exception.Message}", this);
                return new RunHistory();
            }
            catch (UnauthorizedAccessException exception)
            {
                _canSaveHistory = false;
                Debug.LogError($"The run history '{_store.Path}' could not be read; this session will not save runs. {exception.Message}", this);
                return new RunHistory();
            }
        }

        /// <summary>An unusable file is kept for inspection instead of being overwritten; the history starts over.</summary>
        RunHistory SetAsideUnreadableHistory(InvalidDataException problem)
        {
            try
            {
                string aside = _store.SetAside(DateTime.UtcNow);
                Debug.LogError($"{problem.Message} It was moved to '{aside}' and a new run history begins.", this);
            }
            catch (IOException exception)
            {
                _canSaveHistory = false;
                Debug.LogError($"{problem.Message} It could not be moved aside ({exception.Message}); this session will not save runs.", this);
            }
            return new RunHistory();
        }

        void SaveHistory(RunHistory history)
        {
            if (!_canSaveHistory)
            {
                return;
            }
            try
            {
                _store.Save(history);
            }
            catch (IOException exception)
            {
                Debug.LogError($"The run history could not be saved to '{_store.Path}': {exception.Message}", this);
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogError($"The run history could not be saved to '{_store.Path}': {exception.Message}", this);
            }
        }

        void OnNewRunRequested()
        {
            _window.Close();
            Time.timeScale = 1f;
            _controller.StartNextRun();
        }
    }
}

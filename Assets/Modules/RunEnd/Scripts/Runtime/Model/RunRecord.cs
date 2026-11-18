using System;
using System.Globalization;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.RunEnd
{
    /// <summary>
    /// A finished run as saved in the run history: the account's counts and points, enough to compare later runs with
    /// it and to list it. Field names are the JSON format; rename them only with a new history version.
    /// </summary>
    [Serializable]
    public sealed class RunRecord
    {
        [SerializeField] string _finishedAtUtc;
        [SerializeField] string _outcome;
        [SerializeField] string _winningChainId;
        [SerializeField] string _winningChainTitleTable;
        [SerializeField] string _winningChainTitleEntry;
        [SerializeField] int _totalPoints;
        [SerializeField] int _peopleRescued;
        [SerializeField] int _survivors;
        [SerializeField] int _smallShipsGained;
        [SerializeField] int _capitalShipsGained;
        [SerializeField] int _cluesFound;
        [SerializeField] int _peopleLost;
        [SerializeField] int _shipsLost;
        [SerializeField] int _shipsAtEnd;
        [SerializeField] float _elapsedSeconds;

        public static RunRecord From(RunSummary summary, DateTime finishedAtUtc)
        {
            if (summary == null)
            {
                throw new ArgumentNullException(nameof(summary));
            }
            if (finishedAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Runs are saved with UTC times.", nameof(finishedAtUtc));
            }
            RunTally tally = summary.Tally;
            GameText chainTitle = summary.WinningChainTitle;
            bool hasTitleKey = chainTitle != null && !chainTitle.IsVerbatim;
            return new RunRecord
            {
                _finishedAtUtc = finishedAtUtc.ToString("o", CultureInfo.InvariantCulture),
                _outcome = summary.Outcome.ToString(),
                _winningChainId = summary.WinningChainId ?? string.Empty,
                _winningChainTitleTable = hasTitleKey ? chainTitle.Key.Table : string.Empty,
                _winningChainTitleEntry = hasTitleKey ? chainTitle.Key.Entry : string.Empty,
                _totalPoints = summary.TotalPoints,
                _peopleRescued = tally.PeopleRescued,
                _survivors = summary.Survivors,
                _smallShipsGained = tally.SmallShipsGained,
                _capitalShipsGained = tally.CapitalShipsGained,
                _cluesFound = tally.CluesFound,
                _peopleLost = tally.PeopleLost,
                _shipsLost = tally.ShipsLost,
                _shipsAtEnd = summary.Ships,
                _elapsedSeconds = summary.ElapsedSeconds
            };
        }

        public DateTime FinishedAtUtc => DateTime.Parse(_finishedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        public RunOutcome Outcome => (RunOutcome)Enum.Parse(typeof(RunOutcome), _outcome);

        public string WinningChainId => string.IsNullOrEmpty(_winningChainId) ? null : _winningChainId;

        /// <summary>The title of the winning quest chain in the current language; null for a lost run.</summary>
        public GameText WinningChainTitle => string.IsNullOrEmpty(_winningChainTitleTable) || string.IsNullOrEmpty(_winningChainTitleEntry)
            ? null
            : new GameTextKey(_winningChainTitleTable, _winningChainTitleEntry).Format();

        public int TotalPoints => _totalPoints;

        public float ElapsedSeconds => _elapsedSeconds;

        public int ValueOf(RunMetric metric)
        {
            return metric switch
            {
                RunMetric.PeopleRescued => _peopleRescued,
                RunMetric.Survivors => _survivors,
                RunMetric.SmallShipsGained => _smallShipsGained,
                RunMetric.CapitalShipsGained => _capitalShipsGained,
                RunMetric.CluesFound => _cluesFound,
                RunMetric.Victory => Outcome == RunOutcome.Won ? 1 : 0,
                RunMetric.PeopleLost => _peopleLost,
                RunMetric.ShipsLost => _shipsLost,
                _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null)
            };
        }

        /// <summary>Checks a record read from disk; returns why it cannot be used, or null.</summary>
        public string FindProblem()
        {
            if (!DateTime.TryParse(_finishedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime _))
            {
                return $"unreadable finish time '{_finishedAtUtc}'";
            }
            if (!Enum.TryParse(_outcome, out RunOutcome _))
            {
                return $"unknown outcome '{_outcome}'";
            }
            if (_totalPoints < 0 || _peopleRescued < 0 || _survivors < 0 || _smallShipsGained < 0 || _capitalShipsGained < 0 ||
                _cluesFound < 0 || _peopleLost < 0 || _shipsLost < 0 || _shipsAtEnd < 0 || _elapsedSeconds < 0f)
            {
                return "negative counts";
            }
            return null;
        }
    }
}

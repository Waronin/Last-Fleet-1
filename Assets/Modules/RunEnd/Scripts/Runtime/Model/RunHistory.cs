using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFleet.RunEnd
{
    /// <summary>Every finished run, oldest first, as saved between sessions. Kept without limit (agreed with the designer).</summary>
    [Serializable]
    public sealed class RunHistory
    {
        /// <summary>The format this build writes; a file with a higher version came from a newer build.</summary>
        public const int CurrentVersion = 1;

        [SerializeField] int _version = CurrentVersion;
        [SerializeField] List<RunRecord> _runs = new List<RunRecord>();

        public int Version => _version;

        public IReadOnlyList<RunRecord> Runs => _runs;

        public void Add(RunRecord record)
        {
            _runs.Add(record ?? throw new ArgumentNullException(nameof(record)));
        }

        /// <summary>The newest runs first, at most <paramref name="count"/>.</summary>
        public List<RunRecord> Newest(int count)
        {
            List<RunRecord> newest = new List<RunRecord>();
            for (int i = _runs.Count - 1; i >= 0 && newest.Count < count; i--)
            {
                newest.Add(_runs[i]);
            }
            return newest;
        }

        /// <summary>Checks a history read from disk; returns why it cannot be used, or null.</summary>
        public string FindProblem()
        {
            if (_version < 1 || _version > CurrentVersion)
            {
                return $"unsupported version {_version}";
            }
            if (_runs == null)
            {
                return "no run list";
            }
            for (int i = 0; i < _runs.Count; i++)
            {
                string problem = _runs[i] == null ? "missing record" : _runs[i].FindProblem();
                if (problem != null)
                {
                    return $"run {i}: {problem}";
                }
            }
            return null;
        }
    }
}

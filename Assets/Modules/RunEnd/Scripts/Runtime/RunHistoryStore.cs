using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace LastFleet.RunEnd
{
    /// <summary>
    /// Reads and writes the run history as a JSON file. A missing file is an empty history. A file that cannot be
    /// read as a history is reported, never overwritten silently: the caller decides whether to set it aside.
    /// </summary>
    public sealed class RunHistoryStore
    {
        public const string DefaultFileName = "run-history.json";

        readonly string _path;

        public RunHistoryStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The run history needs a file path.", nameof(path));
            }
            _path = path;
        }

        public string Path => _path;

        /// <exception cref="InvalidDataException">The file is not a run history this build can use.</exception>
        /// <exception cref="IOException">The file exists but cannot be read.</exception>
        public RunHistory Load()
        {
            if (!File.Exists(_path))
            {
                return new RunHistory();
            }

            string json = File.ReadAllText(_path);
            RunHistory history;
            try
            {
                history = JsonUtility.FromJson<RunHistory>(json);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException($"The run history '{_path}' is not valid JSON.", exception);
            }
            string problem = history == null ? "empty file" : history.FindProblem();
            if (problem != null)
            {
                throw new InvalidDataException($"The run history '{_path}' cannot be used: {problem}.");
            }
            return history;
        }

        /// <summary>Writes to a temporary file first, so a failed write leaves the old history intact.</summary>
        public void Save(RunHistory history)
        {
            if (history == null)
            {
                throw new ArgumentNullException(nameof(history));
            }
            string directory = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(history, true));
            if (File.Exists(_path))
            {
                File.Replace(temporaryPath, _path, null);
            }
            else
            {
                File.Move(temporaryPath, _path);
            }
        }

        /// <summary>Moves an unusable file out of the way, keeping it for inspection; returns where it went.</summary>
        public string SetAside(DateTime nowUtc)
        {
            string stamp = nowUtc.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string aside = System.IO.Path.ChangeExtension(_path, $".unreadable-{stamp}.json");
            File.Move(_path, aside);
            return aside;
        }
    }
}

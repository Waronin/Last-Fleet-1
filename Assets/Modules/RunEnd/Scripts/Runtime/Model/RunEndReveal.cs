using System;

namespace LastFleet.RunEnd
{
    /// <summary>
    /// Timing of the end screen: the account's lines appear one after another, then the total counts up.
    /// The player can skip to the end at any time.
    /// </summary>
    public sealed class RunEndReveal
    {
        readonly int _lineCount;
        readonly float _secondsPerLine;
        readonly float _countUpSeconds;
        float _elapsedSeconds;

        public RunEndReveal(int lineCount, float secondsPerLine, float countUpSeconds)
        {
            if (lineCount < 0 || secondsPerLine <= 0f || countUpSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(lineCount), "The reveal needs lines to show and positive durations.");
            }
            _lineCount = lineCount;
            _secondsPerLine = secondsPerLine;
            _countUpSeconds = countUpSeconds;
        }

        float LinesSeconds => _lineCount * _secondsPerLine;

        public bool IsComplete => _elapsedSeconds >= LinesSeconds + _countUpSeconds;

        /// <summary>Lines shown so far; the first appears at once.</summary>
        public int VisibleLineCount => Math.Min(_lineCount, (int)(_elapsedSeconds / _secondsPerLine) + 1);

        /// <summary>The total starts counting once every line is shown.</summary>
        public bool IsTotalVisible => _elapsedSeconds >= LinesSeconds;

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Time cannot run backwards.");
            }
            _elapsedSeconds += deltaSeconds;
        }

        public void Skip()
        {
            _elapsedSeconds = Math.Max(_elapsedSeconds, LinesSeconds + _countUpSeconds);
        }

        /// <summary>The part of the total to show while it counts up.</summary>
        public int DisplayedTotal(int total)
        {
            if (!IsTotalVisible)
            {
                return 0;
            }
            float progress = Math.Min(1f, (_elapsedSeconds - LinesSeconds) / _countUpSeconds);
            return (int)Math.Round(total * progress);
        }
    }
}

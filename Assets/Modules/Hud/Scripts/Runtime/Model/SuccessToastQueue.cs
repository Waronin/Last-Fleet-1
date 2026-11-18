using System;
using System.Collections.Generic;

namespace LastFleet.Hud
{
    /// <summary>One success message on screen and how long it has been shown.</summary>
    public sealed class SuccessToast
    {
        public SuccessToast(string message)
        {
            Message = message;
        }

        public string Message { get; }
        public float AgeSeconds { get; internal set; }
    }

    /// <summary>
    /// Timing of the success popups: a few at once, each for a fixed time with a short fade in and out; more wait
    /// their turn. Runs on plain seconds, so the view decides whether game time compression applies.
    /// </summary>
    public sealed class SuccessToastQueue
    {
        /// <summary>A burst of wins (e.g. several ferries landing) waits at most this deep; the oldest waiting ones give way.</summary>
        public const int MaximumWaiting = 8;

        readonly int _maximumVisible;
        readonly float _displaySeconds;
        readonly float _fadeSeconds;
        readonly List<SuccessToast> _visible = new List<SuccessToast>();
        readonly Queue<SuccessToast> _waiting = new Queue<SuccessToast>();

        public SuccessToastQueue(int maximumVisible, float displaySeconds, float fadeSeconds)
        {
            if (maximumVisible < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumVisible), "At least one toast must fit on screen.");
            }
            if (displaySeconds <= 0f || fadeSeconds < 0f || fadeSeconds * 2f > displaySeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeSeconds), "A toast needs a positive display time that holds its fade in and out.");
            }
            _maximumVisible = maximumVisible;
            _displaySeconds = displaySeconds;
            _fadeSeconds = fadeSeconds;
        }

        /// <summary>The toasts on screen, oldest first.</summary>
        public IReadOnlyList<SuccessToast> Visible => _visible;

        public int WaitingCount => _waiting.Count;

        public void Enqueue(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("A success toast needs a message.", nameof(message));
            }
            if (_waiting.Count == MaximumWaiting)
            {
                _waiting.Dequeue();
            }
            _waiting.Enqueue(new SuccessToast(message));
            ShowWaiting();
        }

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Time cannot run backwards.");
            }
            foreach (SuccessToast toast in _visible)
            {
                toast.AgeSeconds += deltaSeconds;
            }
            _visible.RemoveAll(toast => toast.AgeSeconds >= _displaySeconds);
            ShowWaiting();
        }

        /// <summary>0 to 1: fades in at the start and out at the end of the display time.</summary>
        public float OpacityOf(SuccessToast toast)
        {
            if (_fadeSeconds <= 0f)
            {
                return 1f;
            }
            float fadeIn = toast.AgeSeconds / _fadeSeconds;
            float fadeOut = (_displaySeconds - toast.AgeSeconds) / _fadeSeconds;
            return Math.Clamp(Math.Min(fadeIn, fadeOut), 0f, 1f);
        }

        void ShowWaiting()
        {
            while (_visible.Count < _maximumVisible && _waiting.Count > 0)
            {
                _visible.Add(_waiting.Dequeue());
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace LastFleet.Tutorial
{
    /// <summary>
    /// Runs tutorial steps in order on game time. At most one step ends per tick, so every step gets
    /// its radio call even when the player already did what it asks.
    /// </summary>
    public sealed class TutorialSequence
    {
        const int NotStarted = -1;

        readonly List<TutorialStep> _steps;
        int _index = NotStarted;
        float _stepSeconds;

        public TutorialSequence(IEnumerable<TutorialStep> steps)
        {
            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }
            _steps = new List<TutorialStep>(steps);
            if (_steps.Count == 0)
            {
                throw new ArgumentException("A tutorial needs at least one step.", nameof(steps));
            }
            if (_steps.Contains(null))
            {
                throw new ArgumentException("A tutorial step is missing.", nameof(steps));
            }
        }

        /// <summary>The step to do now; null before the start and after the end.</summary>
        public TutorialStep Current => _index >= 0 && _index < _steps.Count ? _steps[_index] : null;

        /// <summary>1-based number of the current step.</summary>
        public int CurrentNumber => _index + 1;
        public int StepCount => _steps.Count;
        public bool HasStarted => _index != NotStarted;
        public bool IsFinished => _index >= _steps.Count;

        /// <summary>Seconds of game time since the current step began.</summary>
        public float StepSeconds => _stepSeconds;

        public event Action<TutorialStep> StepStarted;
        public event Action Finished;

        public void Start()
        {
            if (HasStarted)
            {
                throw new InvalidOperationException("The tutorial has already started.");
            }
            Enter(0);
        }

        /// <summary>Checks the current step and moves on when it is done or its time is up.</summary>
        public void Tick(float deltaSeconds)
        {
            TutorialStep step = Current;
            if (step == null)
            {
                return;
            }
            _stepSeconds += deltaSeconds;
            if (step.IsDone() || (step.HasTimeLimit && _stepSeconds >= step.MaximumSeconds))
            {
                Enter(_index + 1);
            }
        }

        /// <summary>Ends the current step from outside, e.g. when the player skips it.</summary>
        public void Advance()
        {
            if (Current != null)
            {
                Enter(_index + 1);
            }
        }

        void Enter(int index)
        {
            _index = index;
            _stepSeconds = 0f;
            TutorialStep step = Current;
            if (step == null)
            {
                Finished?.Invoke();
                return;
            }
            step.Begin();
            StepStarted?.Invoke(step);
        }
    }
}

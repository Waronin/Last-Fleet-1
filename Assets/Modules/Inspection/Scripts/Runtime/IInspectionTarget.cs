using System;
using System.Collections.Generic;

namespace LastFleet.Inspection
{
    /// <summary>
    /// Something the player can select to see details and interactions, such as a deposit or a
    /// point of interest. New kinds (stations, wrecks, ...) only need a new implementation.
    /// </summary>
    public interface IInspectionTarget
    {
        string Title { get; }

        /// <summary>False once the target no longer exists, e.g. after leaving its zone.</summary>
        bool IsValid { get; }

        /// <summary>Fills current information lines; called every frame, so values stay live.</summary>
        void Describe(List<string> lines);

        void CollectActions(List<InspectionAction> actions);
    }

    /// <summary>An interaction offered for a target. Execute returns feedback for the player.</summary>
    public readonly struct InspectionAction
    {
        public InspectionAction(string label, string shortcutHint, bool isEnabled, string disabledReason, Func<string> execute)
        {
            Label = label;
            ShortcutHint = shortcutHint;
            IsEnabled = isEnabled;
            DisabledReason = disabledReason;
            Execute = execute;
        }

        public string Label { get; }

        /// <summary>The faster way to do the same, shown on the button so players learn it; may be null.</summary>
        public string ShortcutHint { get; }

        public bool IsEnabled { get; }
        public string DisabledReason { get; }
        public Func<string> Execute { get; }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;

namespace LastFleet.Inspection
{
    /// <summary>
    /// Something the player can select to see details and interactions, such as a deposit or a
    /// point of interest. New kinds (stations, wrecks, ...) only need a new implementation.
    /// All texts are <see cref="GameText"/>, so targets stay free of any language.
    /// </summary>
    public interface IInspectionTarget
    {
        GameText Title { get; }

        /// <summary>False once the target no longer exists, e.g. after leaving its zone.</summary>
        bool IsValid { get; }

        /// <summary>Fills current information lines; called every frame, so values stay live.</summary>
        void Describe(List<GameText> lines);

        void CollectActions(List<InspectionAction> actions);
    }

    /// <summary>An interaction offered for a target. Execute returns feedback for the player, or null.</summary>
    public readonly struct InspectionAction
    {
        public InspectionAction(GameText label, GameText shortcutHint, bool isEnabled, GameText disabledReason, Func<GameText> execute)
        {
            Label = label;
            ShortcutHint = shortcutHint;
            IsEnabled = isEnabled;
            DisabledReason = disabledReason;
            Execute = execute;
        }

        public GameText Label { get; }

        /// <summary>The faster way to do the same, shown on the button so players learn it; may be null.</summary>
        public GameText ShortcutHint { get; }

        public bool IsEnabled { get; }
        public GameText DisabledReason { get; }
        public Func<GameText> Execute { get; }
    }
}

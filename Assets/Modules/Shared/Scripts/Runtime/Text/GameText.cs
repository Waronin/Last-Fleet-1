using System;
using System.Collections.Generic;

namespace LastFleet.Shared.Text
{
    /// <summary>A named value for a placeholder of a text. Values may be numbers, names or other texts.</summary>
    public readonly struct GameTextArgument
    {
        public GameTextArgument(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("An argument needs a name.", nameof(name));
            }
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public object Value { get; }

        public static implicit operator GameTextArgument((string Name, object Value) argument)
        {
            return new GameTextArgument(argument.Name, argument.Value);
        }
    }

    /// <summary>
    /// A player-facing text that is not yet in any language: a key plus arguments. Gameplay code passes
    /// these around; only the presentation resolves them for the selected language. This keeps rules
    /// and tests free of display strings.
    /// </summary>
    public sealed class GameText
    {
        static readonly GameTextArgument[] NoArguments = Array.Empty<GameTextArgument>();

        readonly GameTextArgument[] _arguments;

        public GameText(GameTextKey key, params GameTextArgument[] arguments)
        {
            Key = key;
            _arguments = arguments ?? NoArguments;
        }

        GameText(string verbatimText)
        {
            VerbatimText = verbatimText ?? string.Empty;
            _arguments = NoArguments;
        }

        public GameTextKey Key { get; }
        public IReadOnlyList<GameTextArgument> Arguments => _arguments;

        /// <summary>Set only for verbatim texts, which skip localization.</summary>
        public string VerbatimText { get; }

        public bool IsVerbatim => VerbatimText != null;

        /// <summary>
        /// Text shown as it is: proper names such as ship and system names, and developer tools. Any
        /// other player-facing text uses a key, so it can be translated.
        /// </summary>
        public static GameText Verbatim(string text)
        {
            return new GameText(text);
        }

        public static implicit operator GameText(GameTextKey key)
        {
            return new GameText(key, NoArguments);
        }

        public override string ToString()
        {
            return IsVerbatim ? VerbatimText : Key.ToString();
        }
    }
}

using System.Collections.Generic;
using LastFleet.Shared.Text;
using UnityEngine.Localization.Settings;

namespace LastFleet.Localization
{
    /// <summary>
    /// Turns a <see cref="GameText"/> into a string in the selected language. Entries are Smart
    /// Strings: placeholders are named after the arguments, e.g. {ship} or {amount:0.0}, and numbers
    /// follow the culture of the selected language. Arguments that are texts themselves are resolved
    /// first, so a resource name inside a sentence is translated too.
    /// </summary>
    public static class GameTextResolver
    {
        public static string Resolve(GameText text)
        {
            if (text == null)
            {
                return string.Empty;
            }
            if (text.IsVerbatim)
            {
                return text.VerbatimText;
            }

            Dictionary<string, object> arguments = new Dictionary<string, object>(text.Arguments.Count);
            foreach (GameTextArgument argument in text.Arguments)
            {
                arguments[argument.Name] = argument.Value is GameText nested ? Resolve(nested) : argument.Value;
            }
            // Loads the table synchronously on first use; the prototype UI needs the string in the same frame.
            return LocalizationSettings.StringDatabase.GetLocalizedString(text.Key.Table, text.Key.Entry, new object[] { arguments });
        }
    }
}

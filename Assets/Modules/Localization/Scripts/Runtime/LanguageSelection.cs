using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace LastFleet.Localization
{
    /// <summary>Switches the language at runtime; the settings menu will use this later.</summary>
    public static class LanguageSelection
    {
        public static string SelectedLanguageName => LocalizationSettings.SelectedLocale != null
            ? LocalizationSettings.SelectedLocale.Identifier.Code
            : string.Empty;

        /// <summary>Selects the next available language and returns its code.</summary>
        public static string SelectNext()
        {
            List<Locale> locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales.Count == 0)
            {
                return SelectedLanguageName;
            }

            int current = locales.IndexOf(LocalizationSettings.SelectedLocale);
            LocalizationSettings.SelectedLocale = locales[(current + 1) % locales.Count];
            return SelectedLanguageName;
        }
    }
}

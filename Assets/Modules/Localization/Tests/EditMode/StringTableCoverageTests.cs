using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using LastFleet.Shared.Text;
using NUnit.Framework;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace LastFleet.Localization.Tests
{
    /// <summary>
    /// Guards the string tables against the code: every text key declared in a LastFleet assembly
    /// exists in every language, translations use the same placeholders, and each entry formats.
    /// </summary>
    public class StringTableCoverageTests
    {
        static readonly Regex PlaceholderName = new Regex(@"\{([A-Za-z]+)", RegexOptions.Compiled);

        [Test]
        public void ProjectHasEnglishAndGerman()
        {
            List<string> codes = new List<string>();
            foreach (Locale locale in LocalizationEditorSettings.GetLocales())
            {
                codes.Add(locale.Identifier.Code);
            }

            CollectionAssert.IsSupersetOf(codes, new string[] { "en", "de" });
        }

        [Test]
        public void EveryDeclaredKey_HasATextInEveryLanguage()
        {
            List<string> missing = new List<string>();
            List<GameTextKey> keys = CollectDeclaredKeys();
            foreach (GameTextKey key in keys)
            {
                foreach (Locale locale in LocalizationEditorSettings.GetLocales())
                {
                    if (string.IsNullOrWhiteSpace(Lookup(key, locale)))
                    {
                        missing.Add($"{key} [{locale.Identifier.Code}]");
                    }
                }
            }

            Assert.Greater(keys.Count, 0, "No text keys were found; the reflection scan is broken.");
            Assert.IsEmpty(missing, "Missing texts:\n" + string.Join("\n", missing));
        }

        [Test]
        public void Translations_UseTheSamePlaceholders()
        {
            List<string> mismatches = new List<string>();
            foreach (GameTextKey key in CollectDeclaredKeys())
            {
                HashSet<string> english = Placeholders(Lookup(key, Locale("en")));
                foreach (Locale locale in LocalizationEditorSettings.GetLocales())
                {
                    if (!english.SetEquals(Placeholders(Lookup(key, locale))))
                    {
                        mismatches.Add($"{key} [{locale.Identifier.Code}]");
                    }
                }
            }

            Assert.IsEmpty(mismatches, "Placeholders differ from English:\n" + string.Join("\n", mismatches));
        }

        [Test]
        public void EveryEntry_FormatsWithSampleArguments()
        {
            List<string> failures = new List<string>();
            foreach (GameTextKey key in CollectDeclaredKeys())
            {
                foreach (Locale locale in LocalizationEditorSettings.GetLocales())
                {
                    string format = Lookup(key, locale);
                    if (format == null)
                    {
                        continue;
                    }

                    // Numbers fit every placeholder: names, counts, conditions and plurals.
                    Dictionary<string, object> arguments = new Dictionary<string, object>();
                    foreach (string name in Placeholders(format))
                    {
                        arguments[name] = 2;
                    }
                    try
                    {
                        string result = LocalizationSettings.StringDatabase.SmartFormatter.Format(
                            new CultureInfo(locale.Identifier.Code), format, arguments);
                        if (result.Contains("{") || result.Contains("}"))
                        {
                            failures.Add($"{key} [{locale.Identifier.Code}]: unresolved braces in \"{result}\"");
                        }
                    }
                    catch (Exception exception)
                    {
                        failures.Add($"{key} [{locale.Identifier.Code}]: {exception.Message}");
                    }
                }
            }

            Assert.IsEmpty(failures, "Entries that do not format:\n" + string.Join("\n", failures));
        }

        static List<GameTextKey> CollectDeclaredKeys()
        {
            List<GameTextKey> keys = new List<GameTextKey>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (!name.StartsWith("LastFleet.", StringComparison.Ordinal) || name.Contains(".Tests"))
                {
                    continue;
                }
                foreach (Type type in assembly.GetTypes())
                {
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (typeof(IGameTextKeySource).IsAssignableFrom(field.FieldType) && field.GetValue(null) is IGameTextKeySource source)
                        {
                            keys.AddRange(source.Keys);
                        }
                    }
                }
            }
            return keys;
        }

        static string Lookup(GameTextKey key, Locale locale)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(key.Table);
            if (collection == null || !(collection.GetTable(locale.Identifier) is StringTable table))
            {
                return null;
            }
            StringTableEntry entry = table.GetEntry(key.Entry);
            return entry?.Value;
        }

        static Locale Locale(string code)
        {
            foreach (Locale locale in LocalizationEditorSettings.GetLocales())
            {
                if (locale.Identifier.Code == code)
                {
                    return locale;
                }
            }
            throw new InvalidOperationException($"Locale {code} is missing.");
        }

        static HashSet<string> Placeholders(string format)
        {
            HashSet<string> names = new HashSet<string>();
            if (format == null)
            {
                return names;
            }
            foreach (Match match in PlaceholderName.Matches(format))
            {
                names.Add(match.Groups[1].Value);
            }
            return names;
        }
    }
}

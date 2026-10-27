using System.Collections.Generic;
using System.Text;
using LastFleet.Shared.Randomness;

namespace LastFleet.Galaxy
{
    /// <summary>
    /// Builds unique, pronounceable star system names from syllables. Names are proper nouns and are
    /// therefore not localized.
    /// </summary>
    public sealed class SystemNameGenerator
    {
        const int MaximumAttemptsPerName = 20;

        static readonly string[] Syllables =
        {
            "ka", "ve", "tor", "an", "is", "mar", "ul", "rha", "sel", "dun", "ori", "ty", "bel", "xe", "no",
            "cas", "pha", "len", "qui", "ra", "zen", "ath", "mo", "ser", "vi", "lyr", "eth", "gal", "dra", "sun"
        };

        static readonly string[] Suffixes = { "", "", "", " Prime", " Major", " Minor" };

        readonly DeterministicRandom _random;
        readonly HashSet<string> _usedNames = new HashSet<string>();

        public SystemNameGenerator(DeterministicRandom random)
        {
            _random = random;
        }

        public string NextUniqueName()
        {
            for (int attempt = 0; attempt < MaximumAttemptsPerName; attempt++)
            {
                string candidate = BuildName();
                if (_usedNames.Add(candidate))
                {
                    return candidate;
                }
            }

            // Fall back to a numbered variant; the syllable space is large, so this is rare.
            string baseName = BuildName();
            int number = 2;
            string numbered = $"{baseName} {number}";
            while (!_usedNames.Add(numbered))
            {
                number++;
                numbered = $"{baseName} {number}";
            }
            return numbered;
        }

        string BuildName()
        {
            int syllableCount = _random.Range(2, 4);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < syllableCount; i++)
            {
                builder.Append(Syllables[_random.Range(0, Syllables.Length)]);
            }
            builder[0] = char.ToUpperInvariant(builder[0]);
            builder.Append(Suffixes[_random.Range(0, Suffixes.Length)]);
            return builder.ToString();
        }
    }
}

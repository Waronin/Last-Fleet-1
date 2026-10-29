using System;
using System.Collections.Generic;

namespace LastFleet.Shared.Text
{
    /// <summary>
    /// Display names for a fixed set of identifiers that live in data rather than in an enum, such as
    /// ship classes, one entry per name called "{prefix}.{name}". Unknown names are rejected, so a new
    /// identifier has to be declared here and gets checked by the key coverage test.
    /// </summary>
    public sealed class NamedTextKeys : IGameTextKeySource
    {
        readonly Dictionary<string, GameTextKey> _keys = new Dictionary<string, GameTextKey>();

        public NamedTextKeys(string table, string prefix, params string[] names)
        {
            foreach (string name in names)
            {
                _keys[name] = new GameTextKey(table, $"{prefix}.{name}");
            }
        }

        public IEnumerable<GameTextKey> Keys => _keys.Values;
        public IEnumerable<string> Names => _keys.Keys;

        public bool Contains(string name)
        {
            return name != null && _keys.ContainsKey(name);
        }

        public GameText For(string name)
        {
            if (name == null || !_keys.TryGetValue(name, out GameTextKey key))
            {
                throw new ArgumentOutOfRangeException(nameof(name), name, "No display name is declared for this identifier.");
            }
            return key;
        }
    }
}

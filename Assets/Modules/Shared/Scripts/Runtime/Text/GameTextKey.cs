using System;
using System.Collections.Generic;

namespace LastFleet.Shared.Text
{
    /// <summary>
    /// Names one player-facing text: a string table and an entry in it (GDD: all texts are managed with
    /// Unity Localization, not written into the code). Modules declare their keys as static fields of a
    /// texts class, so tests can check that every key exists in every language.
    /// </summary>
    public readonly struct GameTextKey : IEquatable<GameTextKey>, IGameTextKeySource
    {
        public GameTextKey(string table, string entry)
        {
            if (string.IsNullOrWhiteSpace(table))
            {
                throw new ArgumentException("A text key needs a table.", nameof(table));
            }
            if (string.IsNullOrWhiteSpace(entry))
            {
                throw new ArgumentException("A text key needs an entry.", nameof(entry));
            }
            Table = table;
            Entry = entry;
        }

        public string Table { get; }
        public string Entry { get; }

        IEnumerable<GameTextKey> IGameTextKeySource.Keys
        {
            get { yield return this; }
        }

        /// <summary>The text with named arguments for its placeholders, e.g. ("ship", ship.name) for {ship}.</summary>
        public GameText Format(params GameTextArgument[] arguments)
        {
            return new GameText(this, arguments);
        }

        public bool Equals(GameTextKey other)
        {
            return Table == other.Table && Entry == other.Entry;
        }

        public override bool Equals(object obj)
        {
            return obj is GameTextKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Table, Entry);
        }

        public override string ToString()
        {
            return $"{Table}/{Entry}";
        }
    }
}

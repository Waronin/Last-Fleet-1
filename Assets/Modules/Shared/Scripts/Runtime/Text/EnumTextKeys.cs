using System;
using System.Collections.Generic;

namespace LastFleet.Shared.Text
{
    /// <summary>
    /// Display names for the values of an enum, one entry per value named "{prefix}.{Value}". Renaming
    /// a value renames its entry; the key coverage test then reports the missing text.
    /// </summary>
    public sealed class EnumTextKeys<TEnum> : IGameTextKeySource where TEnum : struct, Enum
    {
        readonly Dictionary<TEnum, GameTextKey> _keys = new Dictionary<TEnum, GameTextKey>();

        public EnumTextKeys(string table, string prefix)
        {
            foreach (TEnum value in (TEnum[])Enum.GetValues(typeof(TEnum)))
            {
                _keys[value] = new GameTextKey(table, $"{prefix}.{value}");
            }
        }

        public IEnumerable<GameTextKey> Keys => _keys.Values;

        public GameText For(TEnum value)
        {
            if (!_keys.TryGetValue(value, out GameTextKey key))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"{typeof(TEnum).Name} has no such value.");
            }
            return key;
        }
    }
}

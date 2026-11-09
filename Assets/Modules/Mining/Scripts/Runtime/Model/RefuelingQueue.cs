using System;
using System.Collections.Generic;

namespace LastFleet.Mining
{
    /// <summary>
    /// The ships a tanker was ordered to refuel, in order. The tanker works on the first one; Shift
    /// appends further ships behind it, a plain order replaces the list. A ship is listed once.
    /// </summary>
    public sealed class RefuelingQueue<T> where T : class
    {
        readonly List<T> _targets = new List<T>();

        public IReadOnlyList<T> Targets => _targets;
        public T Current => _targets.Count > 0 ? _targets[0] : null;
        public bool IsEmpty => _targets.Count == 0;

        /// <summary>Lists the target; returns false if it was listed already (with append) and nothing changed.</summary>
        public bool Add(T target, bool append)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!append)
            {
                _targets.Clear();
            }
            else if (_targets.Contains(target))
            {
                return false;
            }
            _targets.Add(target);
            return true;
        }

        /// <summary>Drops the target wherever it stands, e.g. when it is full or was destroyed.</summary>
        public bool Remove(T target)
        {
            return _targets.Remove(target);
        }

        public void Clear()
        {
            _targets.Clear();
        }
    }
}

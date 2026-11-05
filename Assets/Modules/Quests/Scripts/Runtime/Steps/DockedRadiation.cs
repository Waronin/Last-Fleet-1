using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;

namespace LastFleet.Quests
{
    /// <summary>
    /// Radiation doses of the fleet ships that dock at a radioactive quest station: it builds up while a
    /// ship is docked there and wears off elsewhere; at the limit, crew members die. The player is warned
    /// once per stay when a docked ship passes the warning level.
    /// </summary>
    public sealed class DockedRadiation
    {
        readonly Dictionary<ShipAgent, RadiationDose> _doses = new Dictionary<ShipAgent, RadiationDose>();
        readonly HashSet<ShipAgent> _warned = new HashSet<ShipAgent>();
        readonly List<ShipAgent> _clean = new List<ShipAgent>();
        readonly Func<RadiationDose> _createDose;
        readonly float _warningLevel;
        readonly GameText _cause;

        public DockedRadiation(Func<RadiationDose> createDose, float warningLevel, GameText cause)
        {
            _createDose = createDose ?? throw new ArgumentNullException(nameof(createDose));
            _warningLevel = warningLevel;
            _cause = cause;
        }

        /// <summary>The highest dose among the docked ships that still have a crew, 0 to 1.</summary>
        public float HighestDockedLevel { get; private set; }

        /// <summary>Lets time pass; <paramref name="warn"/> runs for a docked ship that just passed the warning level.</summary>
        public void Tick(float deltaSeconds, IReadOnlyList<ShipAgent> docked, Action<ShipAgent> warn)
        {
            HighestDockedLevel = 0f;
            foreach (ShipAgent ship in docked)
            {
                if (!_doses.TryGetValue(ship, out RadiationDose dose))
                {
                    dose = _createDose();
                    _doses.Add(ship, dose);
                }
                int deaths = dose.Expose(deltaSeconds);
                if (deaths > 0 && ship.Crew != null)
                {
                    ship.Crew.Kill(deaths, _cause);
                }
                if (dose.Level >= _warningLevel && _warned.Add(ship))
                {
                    warn?.Invoke(ship);
                }
                if (!ship.IsDerelict)
                {
                    HighestDockedLevel = Math.Max(HighestDockedLevel, dose.Level);
                }
            }

            _clean.Clear();
            foreach (KeyValuePair<ShipAgent, RadiationDose> entry in _doses)
            {
                if (Contains(docked, entry.Key))
                {
                    continue;
                }
                _warned.Remove(entry.Key);
                entry.Value.Recover(deltaSeconds);
                // Unity's overloaded equality treats a destroyed ship as null.
                if (entry.Value.IsClean || entry.Key == null)
                {
                    _clean.Add(entry.Key);
                }
            }
            foreach (ShipAgent ship in _clean)
            {
                _doses.Remove(ship);
            }
        }

        public float LevelOf(ShipAgent ship)
        {
            return _doses.TryGetValue(ship, out RadiationDose dose) ? dose.Level : 0f;
        }

        static bool Contains(IReadOnlyList<ShipAgent> ships, ShipAgent ship)
        {
            for (int i = 0; i < ships.Count; i++)
            {
                if (ReferenceEquals(ships[i], ship))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

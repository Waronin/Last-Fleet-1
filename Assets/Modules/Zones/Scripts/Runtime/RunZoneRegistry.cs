using System;
using System.Collections.Generic;
using LastFleet.StarSystem;

namespace LastFleet.Zones
{
    /// <summary>
    /// Zone state of every visited star system for the duration of a run. Returning to a system
    /// reuses the stored state, so revealed zones and depleted deposits stay as the player left them
    /// and systems cannot be farmed by backtracking.
    /// </summary>
    public sealed class RunZoneRegistry
    {
        readonly Dictionary<int, SystemZones> _systems = new Dictionary<int, SystemZones>();

        public int VisitedSystemCount => _systems.Count;

        public bool TryGetSystem(int systemIndex, out SystemZones zones)
        {
            return _systems.TryGetValue(systemIndex, out zones);
        }

        /// <summary>Registers a system on its first visit. Use <see cref="TryGetSystem"/> for later visits.</summary>
        public SystemZones RegisterSystem(int systemIndex, StarSystemLayout layout)
        {
            if (_systems.ContainsKey(systemIndex))
            {
                throw new InvalidOperationException($"System {systemIndex} is already registered for this run.");
            }

            SystemZones zones = new SystemZones(systemIndex, layout);
            _systems.Add(systemIndex, zones);
            return zones;
        }
    }
}

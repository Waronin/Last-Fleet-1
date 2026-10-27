using System;
using System.Collections.Generic;

namespace LastFleet.Ships
{
    /// <summary>
    /// Slots and launch queue of a hangar, without Unity lifecycle so the rules stay testable.
    /// Ships launch one at a time; the interval is the GDD's launch speed (upgradeable later).
    /// </summary>
    public sealed class HangarBay
    {
        readonly List<ShipAgent> _docked = new List<ShipAgent>();
        readonly List<ShipAgent> _launchQueue = new List<ShipAgent>();
        readonly float _launchIntervalSeconds;
        float _cooldownSeconds;

        public HangarBay(int capacity, float launchIntervalSeconds)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "A hangar needs at least one slot.");
            }
            if (launchIntervalSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(launchIntervalSeconds), "Launch interval must not be negative.");
            }

            Capacity = capacity;
            _launchIntervalSeconds = launchIntervalSeconds;
        }

        public int Capacity { get; }
        public IReadOnlyList<ShipAgent> Docked => _docked;
        public int LaunchQueueCount => _launchQueue.Count;
        public bool HasSpace => _docked.Count < Capacity;

        public bool TryDock(ShipAgent ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }
            if (_docked.Contains(ship))
            {
                return true;
            }
            if (!HasSpace)
            {
                return false;
            }

            _docked.Add(ship);
            return true;
        }

        /// <summary>Queues a docked ship for launch; ships that are not docked or already queued are ignored.</summary>
        public void RequestLaunch(ShipAgent ship)
        {
            if (_docked.Contains(ship) && !_launchQueue.Contains(ship))
            {
                _launchQueue.Add(ship);
            }
        }

        /// <summary>Advances the launch timer and returns the ship that leaves the bay now, if any.</summary>
        public ShipAgent Tick(float deltaTime, bool canLaunch)
        {
            _cooldownSeconds = Math.Max(0f, _cooldownSeconds - deltaTime);
            // Unity's overloaded equality treats destroyed ships as null.
            _docked.RemoveAll(ship => ship == null);
            _launchQueue.RemoveAll(ship => ship == null);

            if (!canLaunch || _cooldownSeconds > 0f || _launchQueue.Count == 0)
            {
                return null;
            }

            ShipAgent launching = _launchQueue[0];
            _launchQueue.RemoveAt(0);
            _docked.Remove(launching);
            _cooldownSeconds = _launchIntervalSeconds;
            return launching;
        }

        public void Remove(ShipAgent ship)
        {
            _docked.Remove(ship);
            _launchQueue.Remove(ship);
        }
    }
}

using System;
using System.Collections.Generic;

namespace LastFleet.Ships
{
    /// <summary>
    /// Owns all squads. Every ship belongs to at most one squad; forming a new squad takes ships out
    /// of their old ones, and squads that become empty are dissolved (GDD: regroup after losses).
    /// </summary>
    public sealed class SquadRoster
    {
        readonly List<Squad> _squads = new List<Squad>();
        readonly Dictionary<ShipAgent, Squad> _squadOfShip = new Dictionary<ShipAgent, Squad>();
        readonly Dictionary<string, int> _lastNumberOfClass = new Dictionary<string, int>();

        public IReadOnlyList<Squad> Squads => _squads;

        public event Action Changed;

        /// <summary>Creates a squad from ships of one class, numbered per class.</summary>
        public Squad Form(IReadOnlyList<ShipAgent> ships)
        {
            if (ships == null || ships.Count == 0)
            {
                throw new ArgumentException("A squad needs at least one ship.", nameof(ships));
            }

            string className = ships[0].ClassName;
            foreach (ShipAgent ship in ships)
            {
                if (ship.ClassName != className)
                {
                    throw new ArgumentException($"A squad holds one ship class; got {className} and {ship.ClassName}.", nameof(ships));
                }
            }

            _lastNumberOfClass.TryGetValue(className, out int lastNumber);
            _lastNumberOfClass[className] = lastNumber + 1;
            Squad squad = new Squad(lastNumber + 1, className);
            foreach (ShipAgent ship in ships)
            {
                Detach(ship);
                squad.Add(ship);
                _squadOfShip[ship] = squad;
            }
            _squads.Add(squad);
            Changed?.Invoke();
            return squad;
        }

        public bool TryGetSquad(ShipAgent ship, out Squad squad)
        {
            return _squadOfShip.TryGetValue(ship, out squad);
        }

        /// <summary>Removes a lost ship from its squad.</summary>
        public void Remove(ShipAgent ship)
        {
            if (Detach(ship))
            {
                Changed?.Invoke();
            }
        }

        bool Detach(ShipAgent ship)
        {
            if (!_squadOfShip.TryGetValue(ship, out Squad squad))
            {
                return false;
            }

            _squadOfShip.Remove(ship);
            squad.Remove(ship);
            if (squad.Members.Count == 0)
            {
                _squads.Remove(squad);
            }
            return true;
        }
    }
}

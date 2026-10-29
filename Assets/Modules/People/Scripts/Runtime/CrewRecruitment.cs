using System.Collections.Generic;
using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.People
{
    /// <summary>
    /// Fills empty crew posts of docked small ships with passengers of their carrier, one person at a
    /// time (GDD: crews are people of the fleet; replacements take time). Docked ships are inactive,
    /// so the work happens here rather than on the ships.
    /// </summary>
    public sealed class CrewRecruitment : MonoBehaviour
    {
        [SerializeField] ShipRegistry _registry;
        [Tooltip("Time to bring one new crew member aboard a docked ship.")]
        [SerializeField, Min(0.1f)] float _secondsPerCrewMember = 10f;

        readonly Dictionary<ShipCrew, float> _progressSeconds = new Dictionary<ShipCrew, float>();
        readonly List<ShipCrew> _finished = new List<ShipCrew>();

        void Awake()
        {
            if (_registry == null)
            {
                Debug.LogError("CrewRecruitment needs the ship registry.", this);
                enabled = false;
            }
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (ShipAgent ship in _registry.Ships)
            {
                ShipCrew crew = ship.Crew;
                if (crew == null || crew.Complement.IsFull || !ship.IsDocked ||
                    !ship.DockedIn.TryGetComponent(out ShipPassengers carrier) || carrier.Quarters.Count == 0)
                {
                    continue;
                }

                _progressSeconds.TryGetValue(crew, out float progress);
                progress += deltaTime;
                if (progress >= _secondsPerCrewMember)
                {
                    progress -= _secondsPerCrewMember;
                    crew.Complement.Add(carrier.Quarters.Remove(1));
                }
                _progressSeconds[crew] = progress;
            }

            // Forget ships that are full, gone or no longer docked, so a new gap starts from zero.
            _finished.Clear();
            foreach (KeyValuePair<ShipCrew, float> entry in _progressSeconds)
            {
                if (entry.Key == null || entry.Key.Complement.IsFull || !entry.Key.Ship.IsDocked)
                {
                    _finished.Add(entry.Key);
                }
            }
            foreach (ShipCrew crew in _finished)
            {
                _progressSeconds.Remove(crew);
            }
        }
    }
}

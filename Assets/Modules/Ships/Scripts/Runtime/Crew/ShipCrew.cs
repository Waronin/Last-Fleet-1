using System;
using LastFleet.Shared.Text;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// The crew of a small ship. Fewer crew members make the ship work more slowly; an empty ship is a
    /// derelict that takes no orders (GDD: in the worst case the shuttle cannot be flown back). Crew
    /// die with their ship; hazards such as radiation take crew members one by one.
    /// </summary>
    [RequireComponent(typeof(ShipAgent), typeof(ShipDurability))]
    public sealed class ShipCrew : MonoBehaviour
    {
        [SerializeField, Min(1)] int _requiredCrew = 2;

        CrewComplement _complement;
        ShipAgent _ship;
        ShipDurability _durability;

        public CrewComplement Complement => _complement ??= new CrewComplement(_requiredCrew, _requiredCrew);
        public ShipAgent Ship => _ship != null ? _ship : _ship = GetComponent<ShipAgent>();
        public float Efficiency => Complement.Efficiency;
        public bool IsDerelict => Complement.IsEmpty;

        /// <summary>Crew members died aboard: the crew, the number and the cause.</summary>
        public event Action<ShipCrew, int, GameText> CrewLost;

        /// <summary>Everyone of the crew died with the ship: the crew and the number.</summary>
        public event Action<ShipCrew, int> LostWithShip;

        /// <summary>The last crew member died; the ship is a derelict from now on.</summary>
        public event Action<ShipCrew> BecameDerelict;

        // Subscribed for the object's whole life, like the hull it listens to.
        void Awake()
        {
            _durability = GetComponent<ShipDurability>();
            _durability.DestroyedBy += OnDestroyedBy;
        }

        void OnDestroy()
        {
            _durability.DestroyedBy -= OnDestroyedBy;
        }

        /// <summary>Kills up to the given number of crew members and returns how many died.</summary>
        public int Kill(int people, GameText cause)
        {
            int removed = Complement.Remove(people);
            if (removed == 0)
            {
                return 0;
            }

            CrewLost?.Invoke(this, removed, cause);
            if (Complement.IsEmpty)
            {
                BecameDerelict?.Invoke(this);
            }
            return removed;
        }

        void OnDestroyedBy(ShipAgent ship, GameText cause)
        {
            int lost = Complement.Remove(Complement.Count);
            if (lost > 0)
            {
                LostWithShip?.Invoke(this, lost);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Spawns the ships of one side in the current star system and keeps track of them, their squads
    /// and hangars. Each side has its own registry, so systems that work for the player's fleet never
    /// see enemy ships. <see cref="ShipRemoved"/> means "no longer ours": the ship was destroyed or
    /// changed sides; losses are reported separately by <see cref="ShipLost"/>.
    /// </summary>
    public sealed class ShipRegistry : MonoBehaviour
    {
        [SerializeField] Allegiance _allegiance = Allegiance.HumanFleet;
        [SerializeField] ZoneWorld _zoneWorld;
        [SerializeField] Camera _camera;
        [SerializeField, Min(0.5f)] float _formationSpacingUnits = 6f;

        readonly List<ShipAgent> _ships = new List<ShipAgent>();
        readonly List<Hangar> _hangars = new List<Hangar>();
        readonly SquadRoster _squads = new SquadRoster();
        readonly Dictionary<string, int> _spawnedPerClass = new Dictionary<string, int>();
        readonly List<ShipDurability> _damageTargets = new List<ShipDurability>();

        public Allegiance Allegiance => _allegiance;
        public IReadOnlyList<ShipAgent> Ships => _ships;
        public IReadOnlyList<Hangar> Hangars => _hangars;
        public SquadRoster Squads => _squads;
        public float FormationSpacingUnits => _formationSpacingUnits;

        public event Action<ShipAgent> ShipSpawned;
        public event Action<ShipAgent> ShipRemoved;

        /// <summary>A ship was destroyed by damage; the text names the cause.</summary>
        public event Action<ShipAgent, GameText> ShipLost;

        /// <summary>A ship could not start a zone jump: target zone index and the fuel it would have cost.</summary>
        public event Action<ShipAgent, int, float> JumpRefusedForFuel;

        /// <summary>Damages every ship flying within a radius of a world point, e.g. for an explosion.</summary>
        public int DamageShipsNear(ZoneView zone, Vector3 worldPoint, float radiusUnits, float damage, GameText cause)
        {
            _damageTargets.Clear();
            foreach (ShipAgent ship in _ships)
            {
                if (!ship.IsDocked && ship.Zone == zone && (ship.transform.position - worldPoint).sqrMagnitude <= radiusUnits * radiusUnits &&
                    ship.TryGetComponent(out ShipDurability durability))
                {
                    _damageTargets.Add(durability);
                }
            }
            // Separate pass: destroyed ships leave the list during the loop above otherwise.
            foreach (ShipDurability target in _damageTargets)
            {
                target.ApplyDamage(damage, cause);
            }
            return _damageTargets.Count;
        }

        void Awake()
        {
            if (_zoneWorld == null || _camera == null)
            {
                Debug.LogError("ShipRegistry needs the zone world and the camera.", this);
            }
        }

        public ShipAgent Spawn(ShipAgent prefab, ZoneView zone, Vector3 localPosition, float headingDegrees)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }
            if (zone == null)
            {
                throw new ArgumentNullException(nameof(zone));
            }

            ShipAgent ship = Instantiate(prefab, zone.transform);
            ship.Initialize(_zoneWorld, zone, localPosition, headingDegrees);
            if (ship.TryGetComponent(out ShipPresentation presentation))
            {
                presentation.Initialize(_camera);
            }
            Register(ship);
            return ship;
        }

        public bool Contains(ShipAgent ship)
        {
            return _ships.Contains(ship);
        }

        /// <summary>
        /// Hands a ship over to another side, e.g. when it is captured or a stranded ship joins the
        /// fleet. It keeps flying where it is, but loses its orders, its squad and its number: the new
        /// side numbers it among its own ships. Ships docked in a transferred carrier stay where they are.
        /// </summary>
        public void TransferTo(ShipAgent ship, ShipRegistry newOwner)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }
            if (newOwner == null)
            {
                throw new ArgumentNullException(nameof(newOwner));
            }
            if (newOwner == this)
            {
                throw new ArgumentException("A ship cannot be transferred to the registry that owns it.", nameof(newOwner));
            }
            if (!_ships.Contains(ship))
            {
                throw new InvalidOperationException($"{ship.name} does not belong to this registry.");
            }

            Unregister(ship);
            ship.CancelOrders();
            ship.SetHomeHangar(null);
            newOwner.Register(ship);
        }

        /// <summary>Takes a ship into this registry: numbers it, gives it this side and reports it.</summary>
        internal void Register(ShipAgent ship)
        {
            _spawnedPerClass.TryGetValue(ship.ClassName, out int spawned);
            _spawnedPerClass[ship.ClassName] = spawned + 1;
            ship.AssignSerialNumber(spawned + 1);
            ship.SetAllegiance(_allegiance);
            if (ship.TryGetComponent(out ShipPresentation presentation))
            {
                presentation.RefreshBracket();
            }
            if (ship.TryGetComponent(out Hangar hangar))
            {
                _hangars.Add(hangar);
            }
            if (ship.TryGetComponent(out ShipDurability durability))
            {
                durability.DestroyedBy += OnShipDestroyedBy;
            }

            ship.Destroyed += OnShipDestroyed;
            ship.JumpRefusedForFuel += OnJumpRefusedForFuel;
            _ships.Add(ship);
            ShipSpawned?.Invoke(ship);
        }

        /// <summary>Spawns ships in wedge formation around a lead position and forms them into a squad.</summary>
        public Squad SpawnSquad(ShipAgent prefab, ZoneView zone, Vector3 leaderLocalPosition, float headingDegrees, int count)
        {
            ValidateSquadSize(count);
            Vector2 facing = ShipMotion.HeadingToDirection(headingDegrees);
            List<ShipAgent> members = new List<ShipAgent>(count);
            for (int i = 0; i < count; i++)
            {
                Vector3 position = leaderLocalPosition + SquadFormation.ZoneOffset(i, _formationSpacingUnits, facing);
                members.Add(Spawn(prefab, zone, position, headingDegrees));
            }
            return _squads.Form(members);
        }

        /// <summary>Spawns a squad inside a hangar, which becomes the squad's home hangar.</summary>
        public Squad SpawnSquadDocked(ShipAgent prefab, Hangar hangar, int count)
        {
            ValidateSquadSize(count);
            List<ShipAgent> members = new List<ShipAgent>(count);
            for (int i = 0; i < count; i++)
            {
                ZoneView zone = hangar.Carrier.Zone;
                ShipAgent ship = Spawn(prefab, zone, zone.transform.InverseTransformPoint(hangar.BayWorldPosition), hangar.Carrier.HeadingDegrees);
                ship.SetHomeHangar(hangar);
                if (!hangar.TryDock(ship))
                {
                    Destroy(ship.gameObject);
                    throw new InvalidOperationException($"{hangar.Carrier.name} has no room for {count} more ships.");
                }
                members.Add(ship);
            }
            return _squads.Form(members);
        }

        /// <summary>Where a small ship should land: home hangar first, else any hangar with room.</summary>
        public Hangar FindHangarFor(ShipAgent ship)
        {
            return HangarSelection.FindFor(ship, _hangars);
        }

        /// <summary>Ships flying in the given zone; docked ships are not in the zone itself.</summary>
        public void CollectShipsInZone(ZoneView zone, List<ShipAgent> result)
        {
            result.Clear();
            foreach (ShipAgent ship in _ships)
            {
                if (!ship.IsDocked && ship.Zone == zone)
                {
                    result.Add(ship);
                }
            }
        }

        static void ValidateSquadSize(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "A squad needs at least one ship.");
            }
        }

        void OnShipDestroyedBy(ShipAgent ship, GameText cause)
        {
            ShipLost?.Invoke(ship, cause);
        }

        void OnJumpRefusedForFuel(ShipAgent ship, int targetZoneIndex, float requiredFuel)
        {
            JumpRefusedForFuel?.Invoke(ship, targetZoneIndex, requiredFuel);
        }

        // Ships are children of their zone view or carrier, so leaving a system destroys them with the zones.
        void OnShipDestroyed(ShipAgent ship)
        {
            Unregister(ship);
        }

        void Unregister(ShipAgent ship)
        {
            ship.Destroyed -= OnShipDestroyed;
            ship.JumpRefusedForFuel -= OnJumpRefusedForFuel;
            if (ship.TryGetComponent(out ShipDurability durability))
            {
                durability.DestroyedBy -= OnShipDestroyedBy;
            }
            _ships.Remove(ship);
            _squads.Remove(ship);
            if (ship.TryGetComponent(out Hangar hangar))
            {
                _hangars.Remove(hangar);
            }
            ShipRemoved?.Invoke(ship);
        }
    }
}

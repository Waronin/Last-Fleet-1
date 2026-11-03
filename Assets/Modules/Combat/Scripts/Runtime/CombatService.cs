using System;
using System.Collections.Generic;
using LastFleet.Shared.Text;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>
    /// Scene-wide combat services between the player's fleet and the Pronin: finds enemies for
    /// weapons and hunters, resolves hits through shields into hull damage and carries out attack
    /// orders. It is the only place that knows both sides' registries.
    /// </summary>
    public sealed class CombatService : MonoBehaviour
    {
        [SerializeField] ShipRegistry _fleet;
        [SerializeField] ShipRegistry _pronin;
        [Tooltip("Optional. Rocks, stations and capital ships give cover against weapon fire.")]
        [SerializeField] ObstacleField _obstacles;

        readonly List<FighterCombat> _escorts = new List<FighterCombat>();
        readonly List<ShipAgent> _candidates = new List<ShipAgent>();

        /// <summary>Short feedback for the player, e.g. when an enemy is shot down.</summary>
        public event Action<GameText> Notice;

        void Awake()
        {
            if (_fleet == null || _pronin == null || _fleet.Allegiance == _pronin.Allegiance)
            {
                Debug.LogError("CombatService needs the fleet's and the Pronin's ship registries.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            Subscribe(_fleet);
            Subscribe(_pronin);
            _pronin.ShipLost += OnEnemyLost;
        }

        void OnDisable()
        {
            Unsubscribe(_fleet);
            Unsubscribe(_pronin);
            _pronin.ShipLost -= OnEnemyLost;
        }

        /// <summary>Distance on the zone plane between two ships; docked ships sit at their carrier's bay.</summary>
        public static float PlanarDistance(ShipAgent a, ShipAgent b)
        {
            Vector3 offset = a.WorldPosition - b.WorldPosition;
            return new Vector2(offset.x, offset.z).magnitude;
        }

        /// <summary>Whether a ship is an enemy of another and flies in the same zone.</summary>
        public static bool IsHostileInZone(ShipAgent ship, ShipAgent other)
        {
            // Unity's overloaded equality treats a destroyed ship as null.
            return other != null && !other.IsDocked && other.Allegiance != ship.Allegiance && other.Zone == ship.Zone;
        }

        public ShipRegistry OpponentsOf(ShipAgent ship)
        {
            return ship.Allegiance == _fleet.Allegiance ? _pronin : _fleet;
        }

        /// <summary>
        /// Whether nothing blocks the line from the shooter to the target (GDD: cover in asteroid
        /// fields). Rocks, stations and capital ships other than the two block it.
        /// </summary>
        public bool HasLineOfFire(ShipAgent shooter, ShipAgent target)
        {
            return _obstacles == null || !_obstacles.IsLineBlocked(shooter, target);
        }

        /// <summary>The nearest enemy flying in the ship's zone within the distance, in cover or not, or null.</summary>
        public ShipAgent FindNearestHostile(ShipAgent ship, float maximumDistanceUnits)
        {
            return FindNearestHostileNear(ship, ship.WorldPosition, maximumDistanceUnits);
        }

        /// <summary>The enemy of a ship that is nearest to a point in the ship's zone within the distance, or null.</summary>
        public ShipAgent FindNearestHostileNear(ShipAgent ship, Vector3 worldPoint, float maximumDistanceUnits)
        {
            ShipAgent nearest = null;
            float bestDistanceSquared = maximumDistanceUnits * maximumDistanceUnits;
            foreach (ShipAgent candidate in OpponentsOf(ship).Ships)
            {
                if (!IsHostileInZone(ship, candidate))
                {
                    continue;
                }
                Vector3 offset = candidate.WorldPosition - worldPoint;
                float distanceSquared = offset.x * offset.x + offset.z * offset.z;
                if (distanceSquared <= bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    nearest = candidate;
                }
            }
            return nearest;
        }

        /// <summary>
        /// The nearest enemy the ship's weapons can fire at: within the distance, inside the cone around
        /// the forward direction and not in cover; or null.
        /// </summary>
        public ShipAgent FindTargetInSight(ShipAgent ship, float maximumDistanceUnits, Vector2 forward, float arcDegrees)
        {
            ShipAgent nearest = null;
            float bestDistance = maximumDistanceUnits;
            foreach (ShipAgent candidate in OpponentsOf(ship).Ships)
            {
                if (!IsHostileInZone(ship, candidate))
                {
                    continue;
                }
                Vector3 offset = candidate.WorldPosition - ship.WorldPosition;
                float distance = PlanarDistance(ship, candidate);
                if (distance > bestDistance || !WeaponSettings.IsInArc(forward, new Vector2(offset.x, offset.z), arcDegrees) ||
                    !HasLineOfFire(ship, candidate))
                {
                    continue;
                }
                bestDistance = distance;
                nearest = candidate;
            }
            return nearest;
        }

        /// <summary>The enemy an autonomous hunter goes for anywhere in the system (see <see cref="HuntingRules"/>), or null.</summary>
        public ShipAgent FindHuntTarget(ShipAgent hunter)
        {
            ShipAgent best = null;
            float bestCost = float.PositiveInfinity;
            IReadOnlyList<ShipAgent> prey = OpponentsOf(hunter).Ships;
            foreach (ShipAgent candidate in prey)
            {
                if (candidate.IsDocked || candidate.Zone == null)
                {
                    continue;
                }

                ZoneView zone = candidate.Zone;
                Vector3 local = zone.transform.InverseTransformPoint(candidate.WorldPosition);
                float travel = ShipAvailability.TravelCost(hunter, zone.Record.PointOfInterestIndex, zone.Record.PointOfInterest.PositionAU, local);
                float cost = HuntingRules.Cost(candidate.IsCapital, CountEscorts(candidate, prey), travel);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = candidate;
                }
            }
            return best;
        }

        /// <summary>A random enemy of the hunter flying anywhere in the system, small or capital, or null.</summary>
        public ShipAgent FindRandomHuntTarget(ShipAgent hunter)
        {
            _candidates.Clear();
            foreach (ShipAgent candidate in OpponentsOf(hunter).Ships)
            {
                if (!candidate.IsDocked && candidate.Zone != null)
                {
                    _candidates.Add(candidate);
                }
            }
            return _candidates.Count > 0 ? _candidates[UnityEngine.Random.Range(0, _candidates.Count)] : null;
        }

        /// <summary>A weapon hit: the shield takes what it can, the rest goes to the hull.</summary>
        public void DealHit(ShipAgent attacker, ShipAgent target, float damage)
        {
            if (target == null || !target.TryGetComponent(out ShipDurability durability))
            {
                return;
            }

            float throughShield = target.TryGetComponent(out ShipShield shield) ? shield.Absorb(damage) : damage;
            if (throughShield > 0f)
            {
                durability.ApplyDamage(throughShield, CombatTexts.WeaponFireCause.Format(("attacker", attacker.DisplayName)));
            }
        }

        /// <summary>The enemy ship nearest to a screen point within the radius in the zone, or false.</summary>
        public bool TryPickHostile(ZoneView zone, Camera camera, Vector2 pointer, float radiusPixels, out ShipAgent hostile)
        {
            hostile = null;
            float bestDistanceSquared = radiusPixels * radiusPixels;
            foreach (ShipAgent ship in _pronin.Ships)
            {
                if (ship.IsDocked || ship.Zone != zone)
                {
                    continue;
                }
                Vector3 screen = camera.WorldToScreenPoint(ship.transform.position);
                float distanceSquared = (new Vector2(screen.x, screen.y) - pointer).sqrMagnitude;
                if (screen.z > 0f && distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    hostile = ship;
                }
            }
            return hostile != null;
        }

        /// <summary>
        /// Small armed ships chase the target; capital ships, which keep their position, focus their
        /// weapons on it. Returns how many ships took the order.
        /// </summary>
        public int OrderAttack(IReadOnlyList<ShipAgent> ships, ShipAgent target)
        {
            int attacking = 0;
            foreach (ShipAgent ship in ships)
            {
                if (ship.TryGetComponent(out FighterCombat fighter))
                {
                    if (fighter.Attack(target))
                    {
                        attacking++;
                    }
                }
                else if (ship.TryGetComponent(out ShipWeapons weapons))
                {
                    weapons.SetFocusTarget(target);
                    attacking++;
                }
            }

            Notice?.Invoke(attacking == 0
                ? CombatTexts.NoAttackers
                : CombatTexts.AttackOrdered.Format(("count", attacking), ("target", target.DisplayName)));
            return attacking;
        }

        /// <summary>Whether any of the ships can take an escort order, so a click on a friendly ship means escort.</summary>
        public static bool CanEscort(IReadOnlyList<ShipAgent> ships, ShipAgent protectedShip)
        {
            foreach (ShipAgent ship in ships)
            {
                if (ship != protectedShip && ship.TryGetComponent(out FighterCombat _))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The fighters among the ships escort a friendly ship, or its whole squad if it flies in one,
        /// spread evenly around it. Members of that squad do not escort themselves. Returns how many
        /// ships took the order.
        /// </summary>
        public int OrderEscort(IReadOnlyList<ShipAgent> ships, ShipAgent protectedShip)
        {
            RegistryOf(protectedShip).Squads.TryGetSquad(protectedShip, out Squad squad);
            _escorts.Clear();
            foreach (ShipAgent ship in ships)
            {
                bool protectsItself = ship == protectedShip || (squad != null && RegistryOf(ship).Squads.TryGetSquad(ship, out Squad own) && own == squad);
                if (!protectsItself && ship.TryGetComponent(out FighterCombat fighter))
                {
                    _escorts.Add(fighter);
                }
            }

            int escorting = 0;
            for (int i = 0; i < _escorts.Count; i++)
            {
                if (_escorts[i].StartEscort(protectedShip, squad, 360f * i / _escorts.Count))
                {
                    escorting++;
                }
            }
            GameText protectedName = squad != null ? squad.DisplayName : protectedShip.DisplayName;
            Notice?.Invoke(escorting == 0
                ? CombatTexts.NoEscorts
                : CombatTexts.EscortOrdered.Format(("count", escorting), ("target", protectedName)));
            return escorting;
        }

        ShipRegistry RegistryOf(ShipAgent ship)
        {
            return ship.Allegiance == _fleet.Allegiance ? _fleet : _pronin;
        }

        static int CountEscorts(ShipAgent ship, IReadOnlyList<ShipAgent> side)
        {
            int escorts = 0;
            foreach (ShipAgent other in side)
            {
                if (other != ship && !other.IsDocked && other.Zone == ship.Zone &&
                    PlanarDistance(ship, other) <= HuntingRules.EscortRadiusUnits)
                {
                    escorts++;
                }
            }
            return escorts;
        }

        void Subscribe(ShipRegistry registry)
        {
            registry.ShipSpawned += OnShipSpawned;
            foreach (ShipAgent ship in registry.Ships)
            {
                OnShipSpawned(ship);
            }
        }

        void Unsubscribe(ShipRegistry registry)
        {
            registry.ShipSpawned -= OnShipSpawned;
        }

        // Also runs for ships that change sides, so they work for the new side's service too.
        void OnShipSpawned(ShipAgent ship)
        {
            if (ship.TryGetComponent(out ShipWeapons weapons))
            {
                weapons.Initialize(this);
                weapons.SetFocusTarget(null);
            }
            if (ship.TryGetComponent(out FighterCombat fighter))
            {
                fighter.Initialize(this);
            }
            if (ship.TryGetComponent(out AutonomousHunting hunting))
            {
                hunting.Initialize(this);
            }
        }

        void OnEnemyLost(ShipAgent ship, GameText cause)
        {
            Notice?.Invoke(ShipTexts.ShipDestroyed.Format(("ship", ship.DisplayName), ("cause", cause)));
        }
    }
}

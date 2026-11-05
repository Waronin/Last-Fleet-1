using System.Collections.Generic;
using LastFleet.Combat;
using LastFleet.Ships;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Quests
{
    /// <summary>
    /// A station's fighters that defend a place, e.g. a sacred rock. They launch from the station, go for
    /// fleet ships flying near the place, miners first, and fly back to it once nobody is left there.
    /// They never leave to hunt elsewhere. They are on the enemy side, so the fleet's guns and orders
    /// treat them like any enemy.
    /// </summary>
    public sealed class DepositGuards
    {
        const float LaunchClearanceUnits = 6f;
        const float ReturnToleranceUnits = 8f;
        const float OrderIntervalSeconds = 0.5f;

        readonly List<ShipAgent> _guards = new List<ShipAgent>();
        readonly ShipRegistry _fleet;
        readonly float _guardRadiusUnits;
        ZoneView _zone;
        Vector3 _postLocal;
        float _secondsUntilOrders;

        public DepositGuards(ShipRegistry fleet, float guardRadiusUnits)
        {
            _fleet = fleet;
            _guardRadiusUnits = guardRadiusUnits;
        }

        /// <summary>Guards still flying in the zone they defend.</summary>
        public int AliveCount
        {
            get
            {
                Prune();
                return _guards.Count;
            }
        }

        /// <summary>Launches the guards from the station towards the place they defend and sends them at an intruder.</summary>
        public void Launch(ShipRegistry side, ShipAgent prefab, int count, ZoneView zone, ZoneStation station, Vector3 postLocal, ShipAgent intruder)
        {
            _zone = zone;
            _postLocal = postLocal;
            Vector3 toPost = postLocal - station.LocalPosition;
            toPost.y = 0f;
            Vector3 direction = toPost.sqrMagnitude > 1e-6f ? toPost.normalized : Vector3.forward;
            Vector3 launch = station.LocalPosition + direction * (station.FootprintRadiusUnits + LaunchClearanceUnits);
            Squad squad = side.SpawnSquad(prefab, zone, launch, ShipMotion.DirectionToHeading(new Vector2(direction.x, direction.z)), count);
            foreach (ShipAgent guard in squad.Members)
            {
                _guards.Add(guard);
                if (intruder != null && guard.TryGetComponent(out FighterCombat combat))
                {
                    combat.Attack(intruder);
                }
            }
            _secondsUntilOrders = OrderIntervalSeconds;
        }

        /// <summary>Keeps the guards on their place; call every frame while the zone is shown.</summary>
        public void Tick(float deltaSeconds, ShipAgent preferredTarget)
        {
            _secondsUntilOrders -= deltaSeconds;
            if (_secondsUntilOrders > 0f || AliveCount == 0)
            {
                return;
            }
            _secondsUntilOrders = OrderIntervalSeconds;

            ShipAgent intruder = IsIntruder(preferredTarget) ? preferredTarget : FindNearestIntruder();
            foreach (ShipAgent guard in _guards)
            {
                if (guard.IsDerelict || !guard.TryGetComponent(out FighterCombat combat))
                {
                    continue;
                }
                if (combat.State == CombatState.Engaging && IsIntruder(combat.Target))
                {
                    continue;
                }
                if (intruder != null)
                {
                    combat.Attack(intruder);
                    continue;
                }
                if (combat.State == CombatState.Engaging)
                {
                    combat.Stop();
                }
                ReturnToPost(guard);
            }
        }

        /// <summary>Lets the guards be: they stop fighting and wait at their place.</summary>
        public void StandDown()
        {
            Prune();
            foreach (ShipAgent guard in _guards)
            {
                if (guard.TryGetComponent(out FighterCombat combat))
                {
                    combat.Stop();
                }
                ReturnToPost(guard);
            }
            _guards.Clear();
        }

        void ReturnToPost(ShipAgent guard)
        {
            Vector3 offset = _zone.transform.InverseTransformPoint(guard.WorldPosition) - _postLocal;
            offset.y = 0f;
            if (offset.magnitude > ReturnToleranceUnits && guard.Activity == ShipActivity.Idle && guard.QueuedOrderCount == 0)
            {
                guard.Order(ShipOrder.MoveTo(_zone.Record.PointOfInterestIndex, _postLocal), false);
            }
        }

        /// <summary>A fleet ship flying near the place.</summary>
        bool IsIntruder(ShipAgent ship)
        {
            // Unity's overloaded equality treats a destroyed ship as null.
            if (ship == null || ship.IsDocked || ship.Zone != _zone || !_fleet.Contains(ship))
            {
                return false;
            }
            Vector3 offset = _zone.transform.InverseTransformPoint(ship.WorldPosition) - _postLocal;
            return new Vector2(offset.x, offset.z).magnitude <= _guardRadiusUnits;
        }

        ShipAgent FindNearestIntruder()
        {
            ShipAgent nearest = null;
            float bestDistance = float.MaxValue;
            foreach (ShipAgent ship in _fleet.Ships)
            {
                if (!IsIntruder(ship))
                {
                    continue;
                }
                Vector3 offset = _zone.transform.InverseTransformPoint(ship.WorldPosition) - _postLocal;
                float distance = new Vector2(offset.x, offset.z).magnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = ship;
                }
            }
            return nearest;
        }

        /// <summary>Drops destroyed guards and those whose zone is gone, e.g. after the fleet left the system.</summary>
        void Prune()
        {
            if (_zone == null)
            {
                _guards.Clear();
                return;
            }
            _guards.RemoveAll(guard => guard == null || guard.Zone != _zone);
        }
    }
}

using System.Collections.Generic;
using LastFleet.Zones;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// The obstacles of each zone for flying and for cover: rocks and wrecks that are not used up,
    /// stations, and capital ships of every side. Gathered at most once per frame and zone, since
    /// every ship and every weapon asks for them.
    /// </summary>
    public sealed class ObstacleField : MonoBehaviour
    {
        [Tooltip("Registries of every side; their capital ships are obstacles.")]
        [SerializeField] ShipRegistry[] _registries = new ShipRegistry[0];

        readonly Dictionary<ZoneView, List<Obstacle>> _obstaclesByZone = new Dictionary<ZoneView, List<Obstacle>>();
        readonly Dictionary<ZoneView, int> _gatheredInFrame = new Dictionary<ZoneView, int>();

        void Awake()
        {
            if (_registries.Length == 0)
            {
                Debug.LogError("ObstacleField needs the ship registries of every side.", this);
            }
        }

        public IReadOnlyList<Obstacle> In(ZoneView zone)
        {
            if (!_obstaclesByZone.TryGetValue(zone, out List<Obstacle> obstacles))
            {
                obstacles = new List<Obstacle>();
                _obstaclesByZone.Add(zone, obstacles);
            }
            if (_gatheredInFrame.TryGetValue(zone, out int frame) && frame == Time.frameCount)
            {
                return obstacles;
            }

            _gatheredInFrame[zone] = Time.frameCount;
            Gather(zone, obstacles);
            return obstacles;
        }

        /// <summary>Whether something blocks the line between two ships in their zone (GDD: cover).</summary>
        public bool IsLineBlocked(ShipAgent shooter, ShipAgent target)
        {
            ZoneView zone = shooter.Zone;
            if (zone == null)
            {
                return false;
            }
            return ObstacleAvoidance.IsLineBlocked(PlanePosition(zone, shooter.WorldPosition), PlanePosition(zone, target.WorldPosition),
                In(zone), shooter, target);
        }

        public static Vector2 PlanePosition(ZoneView zone, Vector3 worldPosition)
        {
            Vector3 local = zone.transform.InverseTransformPoint(worldPosition);
            return new Vector2(local.x, local.z);
        }

        void Gather(ZoneView zone, List<Obstacle> obstacles)
        {
            obstacles.Clear();
            ZoneContents contents = zone.Record.Contents;
            foreach (ResourceDeposit deposit in contents.Deposits)
            {
                if (!deposit.IsDepleted)
                {
                    obstacles.Add(new Obstacle(new Vector2(deposit.LocalPosition.x, deposit.LocalPosition.z), deposit.RadiusUnits, deposit));
                }
            }
            foreach (ZoneStation station in contents.Stations)
            {
                obstacles.Add(new Obstacle(new Vector2(station.LocalPosition.x, station.LocalPosition.z), station.FootprintRadiusUnits, station));
            }
            foreach (ShipRegistry registry in _registries)
            {
                foreach (ShipAgent ship in registry.Ships)
                {
                    if (ship.ObstacleRadiusUnits > 0f && !ship.IsDocked && ship.Zone == zone)
                    {
                        obstacles.Add(new Obstacle(PlanePosition(zone, ship.WorldPosition), ship.ObstacleRadiusUnits, ship));
                    }
                }
            }
        }
    }
}

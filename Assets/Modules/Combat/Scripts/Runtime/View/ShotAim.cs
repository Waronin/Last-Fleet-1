using LastFleet.Ships;
using UnityEngine;

namespace LastFleet.Combat
{
    /// <summary>Where a shot's visual ends: on the target for a hit, beside it on the zone plane for a miss.</summary>
    public static class ShotAim
    {
        public static Vector3 Point(ShipAgent target, bool hit, float missOffsetUnits)
        {
            Vector3 point = target.WorldPosition;
            if (hit)
            {
                return point;
            }
            Vector2 offset = Random.insideUnitCircle.normalized * missOffsetUnits;
            return point + new Vector3(offset.x, 0f, offset.y);
        }
    }
}

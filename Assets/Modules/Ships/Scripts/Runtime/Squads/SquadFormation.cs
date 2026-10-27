using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Wedge formation: the leader in front, followers alternating left and right, one row further
    /// back each. Slots are in formation space (x right, y forward) and rotated by the facing.
    /// </summary>
    public static class SquadFormation
    {
        public static Vector2 SlotOffset(int slotIndex, float spacingUnits)
        {
            if (slotIndex <= 0)
            {
                return Vector2.zero;
            }

            int row = (slotIndex + 1) / 2;
            float side = slotIndex % 2 == 1 ? -1f : 1f;
            return new Vector2(side * row * spacingUnits, -row * spacingUnits);
        }

        /// <summary>Slot offset on the zone plane (x, z) for a formation facing the given direction.</summary>
        public static Vector3 ZoneOffset(int slotIndex, float spacingUnits, Vector2 facing)
        {
            Vector2 forward = facing.sqrMagnitude > 1e-8f ? facing.normalized : Vector2.up;
            Vector2 right = new Vector2(forward.y, -forward.x);
            Vector2 slot = SlotOffset(slotIndex, spacingUnits);
            Vector2 offset = right * slot.x + forward * slot.y;
            return new Vector3(offset.x, 0f, offset.y);
        }
    }
}

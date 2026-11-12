using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFleet.Ships
{
    /// <summary>
    /// Where the pieces of a broken-up ship come to rest on the zone plane: one piece stays where the
    /// ship was; several spread evenly around it, alternating between the full and a nearer distance,
    /// so the field reads as debris and the pieces leave each other room for salvage slots.
    /// </summary>
    public static class WreckScatter
    {
        const float NearDistanceShare = 0.55f;

        /// <summary>Fills the offsets from the ship's position, one per piece.</summary>
        public static void Offsets(int pieceCount, float radiusUnits, float startAngleDegrees, List<Vector2> offsets)
        {
            if (pieceCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pieceCount), "A wreck has at least one piece.");
            }
            if (radiusUnits < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radiusUnits), "The scatter radius cannot be negative.");
            }

            offsets.Clear();
            if (pieceCount == 1)
            {
                offsets.Add(Vector2.zero);
                return;
            }
            float stepDegrees = 360f / pieceCount;
            for (int i = 0; i < pieceCount; i++)
            {
                float angle = (startAngleDegrees + i * stepDegrees) * Mathf.Deg2Rad;
                float distance = radiusUnits * (i % 2 == 0 ? 1f : NearDistanceShare);
                offsets.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance);
            }
        }
    }
}

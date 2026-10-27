using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using UnityEngine;

namespace LastFleet.Zones
{
    /// <summary>
    /// Finds free spots for objects in a zone: inside the usable radius, outside the arrival area at
    /// the center and without overlapping anything placed before.
    /// </summary>
    internal sealed class ZonePlacement
    {
        readonly DeterministicRandom _random;
        readonly float _maximumRadiusUnits;
        readonly float _arrivalClearanceUnits;
        readonly float _minimumGapUnits;
        readonly float _verticalSpreadUnits;
        readonly int _attemptsPerObject;
        readonly List<(Vector3 Position, float Radius)> _occupied = new List<(Vector3 Position, float Radius)>();

        public ZonePlacement(DeterministicRandom random, float maximumRadiusUnits, float arrivalClearanceUnits,
            float minimumGapUnits, float verticalSpreadUnits, int attemptsPerObject)
        {
            _random = random;
            _maximumRadiusUnits = maximumRadiusUnits;
            _arrivalClearanceUnits = arrivalClearanceUnits;
            _minimumGapUnits = minimumGapUnits;
            _verticalSpreadUnits = verticalSpreadUnits;
            _attemptsPerObject = attemptsPerObject;
        }

        public bool TryPlace(float radiusUnits, out Vector3 position)
        {
            float innerRadius = _arrivalClearanceUnits + radiusUnits;
            float outerRadius = _maximumRadiusUnits - radiusUnits;
            position = default;
            if (innerRadius >= outerRadius)
            {
                return false;
            }

            for (int attempt = 0; attempt < _attemptsPerObject; attempt++)
            {
                // Interpolating the squared radius spreads objects evenly over the ring area.
                float distance = Mathf.Sqrt(Mathf.Lerp(innerRadius * innerRadius, outerRadius * outerRadius, _random.NextFloat()));
                float angle = _random.Range(0f, Mathf.PI * 2f);
                Vector3 candidate = new Vector3(Mathf.Cos(angle) * distance,
                    _random.Range(-_verticalSpreadUnits, _verticalSpreadUnits), Mathf.Sin(angle) * distance);

                if (Overlaps(candidate, radiusUnits))
                {
                    continue;
                }

                _occupied.Add((candidate, radiusUnits));
                position = candidate;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Compares distances on the zone plane: ships fly on the plane, so objects must not stack
        /// above each other, or a mining slot could end up under a neighbouring rock.
        /// </summary>
        bool Overlaps(Vector3 candidate, float radiusUnits)
        {
            foreach ((Vector3 occupiedPosition, float occupiedRadius) in _occupied)
            {
                float minimumDistance = radiusUnits + occupiedRadius + _minimumGapUnits;
                Vector2 planarOffset = new Vector2(occupiedPosition.x - candidate.x, occupiedPosition.z - candidate.z);
                if (planarOffset.sqrMagnitude < minimumDistance * minimumDistance)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

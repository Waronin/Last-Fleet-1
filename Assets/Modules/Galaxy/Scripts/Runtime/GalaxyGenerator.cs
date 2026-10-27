using System;
using System.Collections.Generic;
using LastFleet.Shared.Randomness;
using UnityEngine;

namespace LastFleet.Galaxy
{
    /// <summary>
    /// Generates a galaxy from a seed (GDD: Space). Systems grow outward from the start system:
    /// every new system is placed within jump range of an existing one, so every system is reachable
    /// from the start by construction. The start system lies near the rim of the disc.
    /// </summary>
    public static class GalaxyGenerator
    {
        const ulong PositionSalt = 1UL;
        const ulong NameSalt = 2UL;
        const ulong SystemSeedSalt = 3UL;
        const float StartRadiusFraction = 0.85f;
        const float MaximumStepFraction = 0.95f;
        const float VerticalDirectionScale = 0.3f;

        public static GalaxyLayout Generate(ulong seed, GalaxySettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            settings.Validate();

            List<Vector3> positions = PlacePositions(seed, settings);
            DeterministicRandom nameRandom = new DeterministicRandom(DeterministicRandom.DeriveSeed(seed, NameSalt));
            SystemNameGenerator nameGenerator = new SystemNameGenerator(nameRandom);
            ulong systemSeedBase = DeterministicRandom.DeriveSeed(seed, SystemSeedSalt);

            List<StarSystemNode> systems = new List<StarSystemNode>(positions.Count);
            for (int i = 0; i < positions.Count; i++)
            {
                ulong systemSeed = DeterministicRandom.DeriveSeed(systemSeedBase, (ulong)i);
                systems.Add(new StarSystemNode(i, nameGenerator.NextUniqueName(), positions[i], systemSeed));
            }

            return new GalaxyLayout(seed, settings.JumpRangeLightYears, systems, 0);
        }

        static List<Vector3> PlacePositions(ulong seed, GalaxySettings settings)
        {
            DeterministicRandom random = new DeterministicRandom(DeterministicRandom.DeriveSeed(seed, PositionSalt));
            List<Vector3> positions = new List<Vector3>(settings.SystemCount);

            float startAngle = random.Range(0f, Mathf.PI * 2f);
            float startRadius = settings.RadiusLightYears * StartRadiusFraction;
            positions.Add(new Vector3(Mathf.Cos(startAngle) * startRadius, 0f, Mathf.Sin(startAngle) * startRadius));

            float maximumStep = settings.JumpRangeLightYears * MaximumStepFraction;
            int remainingAttempts = settings.SystemCount * settings.PlacementAttemptsPerSystem;
            while (positions.Count < settings.SystemCount && remainingAttempts > 0)
            {
                remainingAttempts--;

                Vector3 parent = positions[random.Range(0, positions.Count)];
                Vector3 direction = random.InsideUnitSphere();
                direction.y *= VerticalDirectionScale;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                Vector3 candidate = parent + direction.normalized * random.Range(settings.MinimumSpacingLightYears, maximumStep);
                if (IsInsideDisc(candidate, settings) && !IsTooClose(candidate, positions, settings.MinimumSpacingLightYears))
                {
                    positions.Add(candidate);
                }
            }

            return positions;
        }

        static bool IsInsideDisc(Vector3 position, GalaxySettings settings)
        {
            Vector2 planar = new Vector2(position.x, position.z);
            return planar.sqrMagnitude <= settings.RadiusLightYears * settings.RadiusLightYears &&
                   Mathf.Abs(position.y) <= settings.ThicknessLightYears * 0.5f;
        }

        static bool IsTooClose(Vector3 candidate, List<Vector3> positions, float minimumSpacing)
        {
            float minimumSpacingSquared = minimumSpacing * minimumSpacing;
            foreach (Vector3 position in positions)
            {
                if ((position - candidate).sqrMagnitude < minimumSpacingSquared)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

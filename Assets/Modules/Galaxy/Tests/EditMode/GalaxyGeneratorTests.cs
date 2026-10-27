using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RangeAttribute = NUnit.Framework.RangeAttribute;

namespace LastFleet.Galaxy.Tests
{
    public class GalaxyGeneratorTests
    {
        static readonly GalaxySettings DefaultSettings = new GalaxySettings();

        [Test]
        public void SameSeed_ProducesIdenticalGalaxy()
        {
            GalaxyLayout first = GalaxyGenerator.Generate(1234UL, DefaultSettings);
            GalaxyLayout second = GalaxyGenerator.Generate(1234UL, DefaultSettings);

            Assert.AreEqual(first.Systems.Count, second.Systems.Count);
            for (int i = 0; i < first.Systems.Count; i++)
            {
                Assert.AreEqual(first.Systems[i].Name, second.Systems[i].Name);
                Assert.AreEqual(first.Systems[i].PositionLightYears, second.Systems[i].PositionLightYears);
                Assert.AreEqual(first.Systems[i].SystemSeed, second.Systems[i].SystemSeed);
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentGalaxies()
        {
            GalaxyLayout first = GalaxyGenerator.Generate(1UL, DefaultSettings);
            GalaxyLayout second = GalaxyGenerator.Generate(2UL, DefaultSettings);

            Assert.AreNotEqual(first.Systems[0].PositionLightYears, second.Systems[0].PositionLightYears);
        }

        [Test]
        public void DefaultSettings_ReachRequestedSystemCount([Range(1, 20)] int seed)
        {
            GalaxyLayout galaxy = GalaxyGenerator.Generate((ulong)seed, DefaultSettings);

            Assert.AreEqual(DefaultSettings.SystemCount, galaxy.Systems.Count);
        }

        [Test]
        public void EverySystem_IsReachableFromStart([Range(1, 50)] int seed)
        {
            GalaxyLayout galaxy = GalaxyGenerator.Generate((ulong)seed, DefaultSettings);

            HashSet<int> visited = new HashSet<int> { galaxy.StartSystemIndex };
            Queue<int> open = new Queue<int>();
            open.Enqueue(galaxy.StartSystemIndex);
            while (open.Count > 0)
            {
                foreach (int neighbour in galaxy.GetNeighbours(open.Dequeue()))
                {
                    if (visited.Add(neighbour))
                    {
                        open.Enqueue(neighbour);
                    }
                }
            }

            Assert.AreEqual(galaxy.Systems.Count, visited.Count);
        }

        [Test]
        public void Systems_RespectMinimumSpacingAndDiscBounds()
        {
            GalaxyLayout galaxy = GalaxyGenerator.Generate(99UL, DefaultSettings);
            float radius = DefaultSettings.RadiusLightYears;
            float halfThickness = DefaultSettings.ThicknessLightYears * 0.5f;

            for (int i = 0; i < galaxy.Systems.Count; i++)
            {
                Vector3 position = galaxy.Systems[i].PositionLightYears;
                Assert.LessOrEqual(new Vector2(position.x, position.z).magnitude, radius + 0.001f);
                Assert.LessOrEqual(Mathf.Abs(position.y), halfThickness + 0.001f);

                for (int j = i + 1; j < galaxy.Systems.Count; j++)
                {
                    Assert.GreaterOrEqual(galaxy.DistanceLightYears(i, j), DefaultSettings.MinimumSpacingLightYears - 0.001f);
                }
            }
        }

        [Test]
        public void Neighbours_AreWithinJumpRangeAndSymmetric()
        {
            GalaxyLayout galaxy = GalaxyGenerator.Generate(7UL, DefaultSettings);

            for (int i = 0; i < galaxy.Systems.Count; i++)
            {
                foreach (int neighbour in galaxy.GetNeighbours(i))
                {
                    Assert.LessOrEqual(galaxy.DistanceLightYears(i, neighbour), galaxy.JumpRangeLightYears + 0.001f);
                    CollectionAssert.Contains(galaxy.GetNeighbours(neighbour), i);
                }
            }
        }

        [Test]
        public void SystemNames_AreUnique()
        {
            GalaxyLayout galaxy = GalaxyGenerator.Generate(5UL, DefaultSettings);
            HashSet<string> names = new HashSet<string>();

            foreach (StarSystemNode system in galaxy.Systems)
            {
                Assert.IsTrue(names.Add(system.Name), $"Duplicate name {system.Name}");
            }
        }

        [Test]
        public void SpacingNotSmallerThanJumpRange_IsRejected()
        {
            GalaxySettings invalid = new GalaxySettings(50, 200f, 20f, 40f, 40f, 10);

            Assert.Throws<ArgumentException>(() => GalaxyGenerator.Generate(1UL, invalid));
        }
    }
}

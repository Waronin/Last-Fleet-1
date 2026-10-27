using LastFleet.Mining;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Mining.Tests
{
    public class HullClampPoseTests
    {
        [Test]
        public void Ship_SitsOnTheSurfaceFacingIt_BellyToTheRock()
        {
            Vector3 rock = new Vector3(10f, 6f, 0f);
            Vector3 ship = new Vector3(14f, 0f, 0f);

            HullClampPose.Compute(rock, 3f, ship, Vector3.forward, 0.3f, out Vector3 position, out Quaternion rotation);

            Vector3 normal = (ship - rock).normalized;
            Assert.AreEqual(3.3f, Vector3.Distance(position, rock), 0.001f);
            Assert.AreEqual(0f, Vector3.Angle(position - rock, normal), 0.01f, "Clamps on the side that faces the ship.");
            Assert.AreEqual(0f, Vector3.Angle(rotation * Vector3.up, normal), 0.01f, "Belly points to the rock.");
        }

        [Test]
        public void Heading_IsKeptWhereTheSurfaceAllows()
        {
            HullClampPose.Compute(Vector3.zero, 2f, new Vector3(5f, 0f, 0f), new Vector3(0f, 0f, 1f), 0.3f,
                out Vector3 _, out Quaternion rotation);

            Assert.AreEqual(0f, Vector3.Angle(rotation * Vector3.forward, Vector3.forward), 0.01f);
        }

        [Test]
        public void ForwardAlongTheNormal_StillGivesAValidPose()
        {
            HullClampPose.Compute(Vector3.zero, 2f, new Vector3(0f, 0f, 5f), Vector3.forward, 0.3f,
                out Vector3 position, out Quaternion rotation);

            Assert.AreEqual(2.3f, position.magnitude, 0.001f);
            Assert.AreEqual(0f, Vector3.Angle(rotation * Vector3.up, Vector3.forward), 0.01f);
        }
    }
}

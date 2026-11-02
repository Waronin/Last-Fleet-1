using LastFleet.Mining;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Mining.Tests
{
    public class HullClampPoseTests
    {
        [Test]
        public void Ship_SitsOnTheUpperSideFacingItsSlot_EvenBelowAFloatingRock()
        {
            Vector3 rock = new Vector3(10f, 6f, 0f);
            Vector3 slot = new Vector3(14f, 0f, 0f);

            HullClampPose.Compute(rock, 3f, 3f, slot, Vector3.forward, 0.3f, out Vector3 position, out Quaternion rotation);

            Vector3 offset = position - rock;
            Assert.AreEqual(3.3f, offset.magnitude, 0.001f);
            Assert.Greater(offset.y, 0f, "The clamp point lies above the rock center, in view of the camera.");
            Assert.Greater(offset.x, 0f, "It faces the mining slot.");
            Assert.AreEqual(HullClampPose.ClampElevationDegrees, Vector3.Angle(offset, Vector3.right), 0.01f);
            Assert.AreEqual(0f, Vector3.Angle(rotation * Vector3.up, offset), 0.01f, "Belly points to the rock.");
        }

        [Test]
        public void FlatGasCloud_ClampsOnItsSurfaceAlongItsNormal()
        {
            HullClampPose.Compute(Vector3.zero, 4f, 1f, new Vector3(10f, 0f, 0f), Vector3.forward, 0f,
                out Vector3 position, out Quaternion rotation);

            float onEllipsoid = (position.x * position.x + position.z * position.z) / 16f + position.y * position.y;
            Assert.AreEqual(1f, onEllipsoid, 0.001f, "The point lies on the cloud's surface.");
            Vector3 normal = new Vector3(position.x / 16f, position.y, position.z / 16f).normalized;
            Assert.AreEqual(0f, Vector3.Angle(rotation * Vector3.up, normal), 0.01f);
        }

        [Test]
        public void Heading_IsKeptWhereTheSurfaceAllows()
        {
            HullClampPose.Compute(Vector3.zero, 2f, 2f, new Vector3(5f, 0f, 0f), new Vector3(0f, 0f, 1f), 0.3f,
                out Vector3 _, out Quaternion rotation);

            Assert.AreEqual(0f, Vector3.Angle(rotation * Vector3.forward, Vector3.forward), 0.01f);
        }

        [Test]
        public void SlotStraightBelowTheCenter_StillGivesAValidPose()
        {
            HullClampPose.Compute(new Vector3(0f, 5f, 0f), 2f, 2f, Vector3.zero, Vector3.up, 0.3f,
                out Vector3 position, out Quaternion rotation);

            Assert.AreEqual(2.3f, Vector3.Distance(position, new Vector3(0f, 5f, 0f)), 0.001f);
            Assert.Greater(position.y, 5f);
            Assert.IsFalse(float.IsNaN(rotation.x));
        }
    }
}

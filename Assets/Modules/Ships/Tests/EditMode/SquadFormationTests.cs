using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Ships.Tests
{
    public class SquadFormationTests
    {
        const float Spacing = 6f;

        [Test]
        public void Leader_TakesTheTargetPoint()
        {
            Assert.AreEqual(Vector3.zero, SquadFormation.ZoneOffset(0, Spacing, new Vector2(0.3f, 0.7f)));
        }

        [Test]
        public void Slots_KeepAtLeastTheSpacing()
        {
            const int slotCount = 12;
            for (int i = 0; i < slotCount; i++)
            {
                for (int j = i + 1; j < slotCount; j++)
                {
                    float distance = Vector2.Distance(SquadFormation.SlotOffset(i, Spacing), SquadFormation.SlotOffset(j, Spacing));
                    Assert.GreaterOrEqual(distance, Spacing - 0.001f, $"Slots {i} and {j} are too close.");
                }
            }
        }

        [Test]
        public void Followers_TrailBehindTheFacing()
        {
            Vector2 facing = new Vector2(1f, 0f);
            for (int i = 1; i < 7; i++)
            {
                Vector3 offset = SquadFormation.ZoneOffset(i, Spacing, facing);
                Assert.Less(offset.x, 0f, $"Slot {i} should be behind the leader.");
            }

            Vector3 left = SquadFormation.ZoneOffset(1, Spacing, facing);
            Vector3 right = SquadFormation.ZoneOffset(2, Spacing, facing);
            Assert.AreEqual(-left.z, right.z, 0.001f);
        }
    }
}

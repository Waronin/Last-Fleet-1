using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Ships.Tests
{
    public class InterceptRunTests
    {
        const float BreakOff = 20f;

        [Test]
        public void AimPoint_LiesBesideTheTarget_OnTheSideThatTheOffsetPicks()
        {
            Vector2 target = new Vector2(0f, 50f);

            Assert.AreEqual(new Vector2(4f, 50f), InterceptRun.AimPoint(Vector2.zero, target, 4f));
            Assert.AreEqual(new Vector2(-4f, 50f), InterceptRun.AimPoint(Vector2.zero, target, -4f));
        }

        [Test]
        public void HoldsCourse_FromPassingTheTargetUntilBreakOff()
        {
            Vector2 forward = Vector2.up;

            Assert.IsFalse(InterceptRun.HoldsCourse(false, forward, new Vector2(0f, 10f), BreakOff), "target ahead");
            Assert.IsTrue(InterceptRun.HoldsCourse(false, forward, new Vector2(0f, -1f), BreakOff), "just passed");
            Assert.IsTrue(InterceptRun.HoldsCourse(true, -forward, new Vector2(0f, -10f), BreakOff), "still inside break-off while turning");
            Assert.IsFalse(InterceptRun.HoldsCourse(true, forward, new Vector2(0f, -BreakOff), BreakOff), "broke off: come about");
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LastFleet.Ships.Tests
{
    public class ShipMotionTests
    {
        const float StepSeconds = 1f / 60f;
        const float MaximumSimulatedSeconds = 300f;
        static readonly ShipMotionSettings Settings = new ShipMotionSettings();

        [Test]
        public void ShortMove_UsesThrustersOnly()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            motion.SetDestination(new Vector2(10f, 5f));

            HashSet<ShipMotionPhase> phases = Simulate(motion);

            Assert.IsTrue(motion.HasArrived);
            Assert.That(phases, Has.No.Member(ShipMotionPhase.Burning));
            Assert.That(phases, Has.No.Member(ShipMotionPhase.Flipping));
            Assert.AreEqual(new Vector2(10f, 5f), motion.Position);
        }

        [Test]
        public void LongMove_TurnsBurnsFlipsAndBrakes()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            Vector2 destination = new Vector2(-300f, 200f);
            motion.SetDestination(destination);

            HashSet<ShipMotionPhase> phases = Simulate(motion);

            Assert.IsTrue(motion.HasArrived);
            CollectionAssert.IsSubsetOf(
                new ShipMotionPhase[] { ShipMotionPhase.Aligning, ShipMotionPhase.Burning, ShipMotionPhase.Flipping, ShipMotionPhase.Braking },
                phases);
            Assert.AreEqual(0f, motion.Speed);
        }

        [Test]
        public void LongMove_DoesNotOvershootFarAndRespectsTopSpeed()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 90f);
            Vector2 destination = new Vector2(0f, 400f);
            motion.SetDestination(destination);

            float elapsed = 0f;
            float furthestBeyond = 0f;
            while (!motion.HasArrived && elapsed < MaximumSimulatedSeconds)
            {
                motion.Step(StepSeconds);
                elapsed += StepSeconds;
                Assert.LessOrEqual(motion.Speed, Settings.MaximumSpeed + 0.001f);
                furthestBeyond = Mathf.Max(furthestBeyond, motion.Position.y - destination.y);
            }

            Assert.IsTrue(motion.HasArrived, "Ship did not arrive in time.");
            Assert.Less(furthestBeyond, Settings.ManeuverRangeUnits);
        }

        [Test]
        public void ExitAtStoppingDistance_ArrivesWithKeptMomentum()
        {
            // Mirrors a zone jump exit: full speed towards the arrival point, already facing it.
            ShipMotion charging = new ShipMotion(Settings, Vector2.zero, 0f);
            charging.SetCourse(Vector2.up);
            for (float t = 0f; t < 10f; t += StepSeconds)
            {
                charging.Step(StepSeconds);
            }
            Assert.Greater(charging.Speed, Settings.MaximumSpeed * 0.9f);

            Vector2 arrival = new Vector2(0f, 5000f);
            charging.Teleport(arrival - Vector2.up * charging.StoppingDistance(charging.Speed));
            charging.SetDestination(arrival);
            float elapsed = 0f;
            float furthestBeyond = 0f;
            while (!charging.HasArrived && elapsed < MaximumSimulatedSeconds)
            {
                charging.Step(StepSeconds);
                elapsed += StepSeconds;
                furthestBeyond = Mathf.Max(furthestBeyond, charging.Position.y - arrival.y);
            }

            Assert.IsTrue(charging.HasArrived);
            Assert.Less(furthestBeyond, Settings.ManeuverRangeUnits);
        }

        [Test]
        public void NewDestinationMidFlight_StillArrives()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            motion.SetDestination(new Vector2(0f, 400f));
            for (float t = 0f; t < 8f; t += StepSeconds)
            {
                motion.Step(StepSeconds);
            }

            Vector2 reversed = new Vector2(50f, -100f);
            motion.SetDestination(reversed);
            Simulate(motion);

            Assert.IsTrue(motion.HasArrived);
            Assert.AreEqual(reversed, motion.Position);
        }

        [Test]
        public void MovingTarget_IsReachedByRetargeting()
        {
            // Like a harvester approaching the landing bay of a slowly moving carrier.
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            Vector2 target = new Vector2(200f, 100f);
            Vector2 targetVelocity = new Vector2(-1.5f, 0.5f);
            motion.SetDestination(target);

            float elapsed = 0f;
            bool reached = false;
            while (elapsed < MaximumSimulatedSeconds && !reached)
            {
                target += targetVelocity * StepSeconds;
                motion.RetargetDestination(target);
                motion.Step(StepSeconds);
                elapsed += StepSeconds;
                reached = Vector2.Distance(motion.Position, target) <= 3f;
            }

            Assert.IsTrue(reached, $"Ship did not reach the moving target; last phase {motion.Phase}.");
        }

        [Test]
        public void Cruising_AcceleratesAlongCourseToTopSpeed()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            motion.SetCourse(new Vector2(1f, 0f));
            for (float t = 0f; t < 10f; t += StepSeconds)
            {
                motion.Step(StepSeconds);
            }

            Assert.AreEqual(Settings.MaximumSpeed, motion.Speed, 0.01f);
            Assert.AreEqual(1f, motion.Velocity.normalized.x, 0.01f);
            Assert.AreEqual(ShipMotionPhase.Cruising, motion.Phase);
        }

        static HashSet<ShipMotionPhase> Simulate(ShipMotion motion)
        {
            HashSet<ShipMotionPhase> phases = new HashSet<ShipMotionPhase>();
            float elapsed = 0f;
            while (!motion.HasArrived && elapsed < MaximumSimulatedSeconds)
            {
                phases.Add(motion.Phase);
                motion.Step(StepSeconds);
                elapsed += StepSeconds;
            }
            Assert.IsTrue(motion.HasArrived, $"Ship did not arrive within {MaximumSimulatedSeconds} s; last phase {motion.Phase}.");
            return phases;
        }
    }
}

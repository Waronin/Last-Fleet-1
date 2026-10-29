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

        [Test]
        public void WithoutMainDrive_LongMoveUsesThrustersOnly()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            motion.MainDriveAvailable = false;
            Vector2 destination = new Vector2(-300f, 200f);
            motion.SetDestination(destination);

            float elapsed = 0f;
            while (!motion.HasArrived && elapsed < MaximumSimulatedSeconds)
            {
                motion.Step(StepSeconds);
                elapsed += StepSeconds;
                Assert.IsFalse(motion.MainDriveActive);
                Assert.LessOrEqual(motion.Speed, Settings.ThrusterMaximumSpeed + 0.001f);
            }

            Assert.IsTrue(motion.HasArrived, "Ship did not crawl to its destination in time.");
            Assert.AreEqual(destination, motion.Position);
        }

        [Test]
        public void MainDriveLostMidBurn_SlowsDownWithThrustersAndStillArrives()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            Vector2 destination = new Vector2(0f, 800f);
            motion.SetDestination(destination);
            while (motion.Phase != ShipMotionPhase.Burning || motion.Speed < Settings.MaximumSpeed * 0.5f)
            {
                motion.Step(StepSeconds);
            }

            motion.MainDriveAvailable = false;
            float elapsed = 0f;
            while (!motion.HasArrived && elapsed < MaximumSimulatedSeconds)
            {
                motion.Step(StepSeconds);
                elapsed += StepSeconds;
                Assert.IsFalse(motion.MainDriveActive);
            }

            Assert.IsTrue(motion.HasArrived, $"Ship did not arrive; last phase {motion.Phase}.");
        }

        [Test]
        public void WithoutMainDrive_CourseDoesNotAccelerate()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            motion.MainDriveAvailable = false;
            motion.SetCourse(Vector2.up);

            for (float t = 0f; t < 5f; t += StepSeconds)
            {
                motion.Step(StepSeconds);
            }

            Assert.AreEqual(0f, motion.Speed, 1e-4f);
        }

        [Test]
        public void PreciseManeuver_CreepsOnThrusters_AndEndsOnItsHeading()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 90f);
            const float maximumSpeed = 0.8f;
            Vector2 destination = new Vector2(0f, 4f);
            motion.SetPreciseDestination(destination, maximumSpeed, 0f);

            float elapsed = 0f;
            while (!motion.HasArrived && elapsed < MaximumSimulatedSeconds)
            {
                motion.Step(StepSeconds);
                elapsed += StepSeconds;
                Assert.IsFalse(motion.MainDriveActive, "A maneuver never lights the main drive.");
                Assert.LessOrEqual(motion.Speed, maximumSpeed + 0.001f);
            }

            Assert.IsTrue(motion.HasArrived);
            Assert.AreEqual(destination, motion.Position);
            Assert.AreEqual(0f, motion.HeadingDegrees, 0.001f);
            Assert.Greater(elapsed, (4f - Settings.ArrivalToleranceUnits) / maximumSpeed, "Creeping at 0.8 units per second takes its time.");
        }

        [Test]
        public void PreciseManeuver_BacksAwayWithoutTurning()
        {
            // Docked facing +z; backing out to -z keeps the nose on the port.
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            motion.SetPreciseDestination(new Vector2(0f, -4f), 1.2f, 0f);

            float elapsed = 0f;
            while (!motion.HasArrived && elapsed < MaximumSimulatedSeconds)
            {
                motion.Step(StepSeconds);
                elapsed += StepSeconds;
                Assert.AreEqual(0f, Mathf.DeltaAngle(motion.HeadingDegrees, 0f), 0.001f);
            }

            Assert.IsTrue(motion.HasArrived);
        }

        [Test]
        public void NormalMove_ClearsAPreciseManeuver()
        {
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 0f);
            motion.SetPreciseDestination(new Vector2(0f, 500f), 0.8f, 0f);
            motion.SetDestination(new Vector2(0f, 500f));

            HashSet<ShipMotionPhase> phases = Simulate(motion);

            CollectionAssert.Contains(phases, ShipMotionPhase.Burning, "A normal move burns again after a maneuver.");
        }

        [Test]
        public void PreciseManeuver_SettlesSoftly_WithoutSnappingTheLastMeters()
        {
            const float maximumSpeed = 0.8f;
            ShipMotion motion = new ShipMotion(Settings, Vector2.zero, 30f);
            motion.SetPreciseDestination(new Vector2(0f, 4f), maximumSpeed, 0f);

            float elapsed = 0f;
            float largestStep = 0f;
            float lastSpeed = 0f;
            while (!motion.HasArrived && elapsed < MaximumSimulatedSeconds)
            {
                Vector2 before = motion.Position;
                lastSpeed = motion.Speed;
                motion.Step(StepSeconds);
                elapsed += StepSeconds;
                largestStep = Mathf.Max(largestStep, (motion.Position - before).magnitude);
            }

            Assert.IsTrue(motion.HasArrived);
            // At most one frame of creeping plus a centimeter-scale settle, never the old 50 m snap.
            Assert.LessOrEqual(largestStep, maximumSpeed * StepSeconds + 0.02f);
            Assert.Less(lastSpeed, 0.1f, "The ship has almost stopped before it touches the port.");
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

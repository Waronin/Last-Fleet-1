using System;
using NUnit.Framework;

namespace LastFleet.FleetJump.Tests
{
    public class FleetJumpDriveTests
    {
        static FleetJumpDrive Create(float cooldown = 300f, float charge = 30f)
        {
            return new FleetJumpDrive(new FleetJumpSettings(cooldown, charge, 0.3f));
        }

        [Test]
        public void Drive_CoolsDown_ThenIsReady()
        {
            FleetJumpDrive drive = Create();
            Assert.AreEqual(FleetJumpState.CoolingDown, drive.State);
            Assert.IsFalse(drive.TryStartCharge(4));

            drive.Advance(299f);
            Assert.AreEqual(FleetJumpState.CoolingDown, drive.State);
            drive.Advance(1f);
            Assert.AreEqual(FleetJumpState.Ready, drive.State);
        }

        [Test]
        public void Charge_CompletesOnceAfterTheChargeTime()
        {
            FleetJumpDrive drive = Create(cooldown: 0f);
            Assert.IsTrue(drive.TryStartCharge(4));
            Assert.AreEqual(4, drive.TargetSystemIndex);
            Assert.IsFalse(drive.Advance(29f));
            Assert.IsTrue(drive.Advance(1f));
        }

        [Test]
        public void CancelledCharge_KeepsTheDriveReady()
        {
            FleetJumpDrive drive = Create(cooldown: 0f);
            drive.TryStartCharge(4);
            drive.Advance(20f);
            drive.CancelCharge();

            Assert.AreEqual(FleetJumpState.Ready, drive.State);
            Assert.AreEqual(-1, drive.TargetSystemIndex);
            Assert.IsFalse(drive.Advance(100f));
        }

        [Test]
        public void Arrival_RestartsTheCooldown_AndEndsACharge()
        {
            FleetJumpDrive drive = Create(cooldown: 0f);
            drive.TryStartCharge(4);
            drive.StartCooldown();
            Assert.AreEqual(FleetJumpState.Ready, drive.State);

            FleetJumpDrive cooling = Create();
            cooling.Advance(300f);
            cooling.StartCooldown();
            Assert.AreEqual(FleetJumpState.CoolingDown, cooling.State);
            Assert.AreEqual(300f, cooling.CooldownRemainingSeconds);
        }

        [Test]
        public void ChargingAgain_IsRefused()
        {
            FleetJumpDrive drive = Create(cooldown: 0f);
            drive.TryStartCharge(4);
            Assert.IsFalse(drive.TryStartCharge(5));
            Assert.AreEqual(4, drive.TargetSystemIndex);
        }

        [Test]
        public void InvalidInput_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new FleetJumpDrive(new FleetJumpSettings(-1f, 30f, 0.3f)));
            Assert.Throws<ArgumentException>(() => new FleetJumpDrive(new FleetJumpSettings(300f, 30f, 0f)));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create().Advance(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create().TryStartCharge(-1));
        }
    }
}

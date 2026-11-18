using NUnit.Framework;

namespace LastFleet.AbandonedShips.Tests
{
    public class CrewTransferRulesTests
    {
        [Test]
        public void ADonor_KeepsOneToFly()
        {
            Assert.IsFalse(CrewTransferRules.CanDonate(0));
            Assert.IsFalse(CrewTransferRules.CanDonate(1), "Giving the last person would make the donor a derelict.");
            Assert.IsTrue(CrewTransferRules.CanDonate(2));
        }

        [Test]
        public void TheCrewMemberCrossesOver_OnlyCloseBy()
        {
            Assert.IsTrue(CrewTransferRules.IsInRange(CrewTransferRules.SideOffsetUnits));
            Assert.IsTrue(CrewTransferRules.IsInRange(CrewTransferRules.TransferRangeUnits));
            Assert.IsFalse(CrewTransferRules.IsInRange(CrewTransferRules.TransferRangeUnits + 0.1f));
        }
    }
}

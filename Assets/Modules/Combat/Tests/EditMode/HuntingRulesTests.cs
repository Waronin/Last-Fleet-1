using NUnit.Framework;

namespace LastFleet.Combat.Tests
{
    public class HuntingRulesTests
    {
        [Test]
        public void SmallShips_ComeBeforeCapitalShipsEvenFarAway()
        {
            float farSmall = HuntingRules.Cost(false, 0, 200000f);
            float nearCapital = HuntingRules.Cost(true, 0, 10f);

            Assert.Less(farSmall, nearCapital);
        }

        [Test]
        public void IsolatedShips_ComeBeforeEscortedOnesAtTheSameDistance()
        {
            Assert.Less(HuntingRules.Cost(false, 0, 50f), HuntingRules.Cost(false, 2, 50f));
        }

        [Test]
        public void NearerShips_ComeFirstOtherwise()
        {
            Assert.Less(HuntingRules.Cost(false, 1, 20f), HuntingRules.Cost(false, 1, 30f));
        }
    }
}

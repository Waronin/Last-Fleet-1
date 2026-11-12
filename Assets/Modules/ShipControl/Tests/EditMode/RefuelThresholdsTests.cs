using NUnit.Framework;

namespace LastFleet.ShipControl.Tests
{
    public class RefuelThresholdsTests
    {
        [Test]
        public void Next_CyclesOffFifteenThirtyFifty()
        {
            Assert.AreEqual(0.15f, RefuelThresholds.Next(RefuelThresholds.Off), 1e-5f);
            Assert.AreEqual(0.3f, RefuelThresholds.Next(0.15f), 1e-5f);
            Assert.AreEqual(0.5f, RefuelThresholds.Next(0.3f), 1e-5f);
            Assert.AreEqual(RefuelThresholds.Off, RefuelThresholds.Next(0.5f), "After the highest step it is off again.");
        }

        [Test]
        public void Next_StartsOverForAnUnknownValue()
        {
            Assert.AreEqual(RefuelThresholds.Off, RefuelThresholds.Next(0.42f));
        }

        [Test]
        public void ShouldReturn_OnlyBelowAnActiveThreshold()
        {
            Assert.IsTrue(RefuelThresholds.ShouldReturn(0.29f, 0.3f));
            Assert.IsFalse(RefuelThresholds.ShouldReturn(0.3f, 0.3f), "At the threshold the ship keeps going.");
            Assert.IsFalse(RefuelThresholds.ShouldReturn(0f, RefuelThresholds.Off), "Off never sends a ship home.");
        }

        [Test]
        public void SmallShips_StartAtThirtyPercent()
        {
            Assert.AreEqual(0.3f, RefuelThresholds.Default, 1e-5f);
        }
    }
}

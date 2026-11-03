using LastFleet.Shared.Text;
using NUnit.Framework;

namespace LastFleet.Shared.Tests
{
    public class ClockFormatTests
    {
        [TestCase(0f, "0:00")]
        [TestCase(0.2f, "0:01")]
        [TestCase(59.5f, "1:00")]
        [TestCase(780f, "13:00")]
        [TestCase(-5f, "0:00")]
        public void MinutesSeconds_RoundsUp(float seconds, string expected)
        {
            Assert.AreEqual(expected, ClockFormat.MinutesSeconds(seconds));
        }
    }
}

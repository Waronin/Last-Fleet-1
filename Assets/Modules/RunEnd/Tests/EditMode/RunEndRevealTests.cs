using NUnit.Framework;

namespace LastFleet.RunEnd.Tests
{
    public class RunEndRevealTests
    {
        [Test]
        public void Lines_AppearOneAfterAnother_ThenTheTotalCountsUp()
        {
            RunEndReveal reveal = new RunEndReveal(4, 0.5f, 1f);

            Assert.AreEqual(1, reveal.VisibleLineCount, "The first line shows at once.");
            Assert.IsFalse(reveal.IsTotalVisible);

            reveal.Advance(1.25f);
            Assert.AreEqual(3, reveal.VisibleLineCount);

            reveal.Advance(0.75f);
            Assert.AreEqual(4, reveal.VisibleLineCount);
            Assert.IsTrue(reveal.IsTotalVisible);
            Assert.AreEqual(0, reveal.DisplayedTotal(1000));

            reveal.Advance(0.5f);
            Assert.AreEqual(500, reveal.DisplayedTotal(1000));
            Assert.IsFalse(reveal.IsComplete);

            reveal.Advance(0.5f);
            Assert.AreEqual(1000, reveal.DisplayedTotal(1000));
            Assert.IsTrue(reveal.IsComplete);
        }

        [Test]
        public void Skip_ShowsEverythingAtOnce()
        {
            RunEndReveal reveal = new RunEndReveal(6, 0.5f, 1f);

            reveal.Skip();

            Assert.IsTrue(reveal.IsComplete);
            Assert.AreEqual(6, reveal.VisibleLineCount);
            Assert.AreEqual(1234, reveal.DisplayedTotal(1234));
        }
    }
}

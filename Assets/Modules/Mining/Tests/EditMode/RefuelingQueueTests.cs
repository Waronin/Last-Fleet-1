using LastFleet.Mining;
using NUnit.Framework;

namespace LastFleet.Mining.Tests
{
    public sealed class RefuelingQueueTests
    {
        [Test]
        public void PlainOrder_ReplacesTheTargets()
        {
            RefuelingQueue<string> queue = new RefuelingQueue<string>();
            queue.Add("liner", false);
            queue.Add("refinery", true);

            Assert.IsTrue(queue.Add("cruiser", false));

            Assert.AreEqual(1, queue.Targets.Count);
            Assert.AreEqual("cruiser", queue.Current);
        }

        [Test]
        public void AppendedOrder_WaitsBehindTheCurrentTarget()
        {
            RefuelingQueue<string> queue = new RefuelingQueue<string>();
            queue.Add("liner", false);
            queue.Add("refinery", true);

            Assert.AreEqual("liner", queue.Current);
            queue.Remove("liner");
            Assert.AreEqual("refinery", queue.Current, "The next target follows once the first is full.");
        }

        [Test]
        public void AppendingAListedTarget_ChangesNothing()
        {
            RefuelingQueue<string> queue = new RefuelingQueue<string>();
            queue.Add("liner", false);
            queue.Add("refinery", true);

            Assert.IsFalse(queue.Add("liner", true));
            Assert.AreEqual(2, queue.Targets.Count);
        }

        [Test]
        public void RemovingTheLastTarget_EndsTheRefueling()
        {
            RefuelingQueue<string> queue = new RefuelingQueue<string>();
            queue.Add("liner", false);

            queue.Remove("liner");

            Assert.IsTrue(queue.IsEmpty);
            Assert.IsNull(queue.Current);
        }
    }
}

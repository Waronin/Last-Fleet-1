using LastFleet.Mining;
using NUnit.Framework;

namespace LastFleet.Mining.Tests
{
    public sealed class RefuelingQueueTests
    {
        [Test]
        public void Enqueue_KeepsTheOrderAndRefuelsOnlyTheFirst()
        {
            RefuelingQueue<string> queue = new RefuelingQueue<string>();
            queue.Enqueue("liner");
            queue.Enqueue("refinery");

            Assert.AreEqual("liner", queue.Current);
            Assert.AreEqual(1, queue.PositionOf("refinery"));
        }

        [Test]
        public void Enqueue_ListsAShipOnce()
        {
            RefuelingQueue<string> queue = new RefuelingQueue<string>();
            queue.Enqueue("liner");

            Assert.IsFalse(queue.Enqueue("liner"));
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void MoveToFront_LetsAWaitingShipGoNext()
        {
            RefuelingQueue<string> queue = new RefuelingQueue<string>();
            queue.Enqueue("liner");
            queue.Enqueue("refinery");
            queue.Enqueue("cruiser");

            Assert.IsTrue(queue.MoveToFront("cruiser"));
            Assert.AreEqual("cruiser", queue.Current);
            Assert.AreEqual(1, queue.PositionOf("liner"), "The others keep their order behind it.");
            Assert.IsFalse(queue.MoveToFront("cruiser"), "Already first.");
            Assert.IsFalse(queue.MoveToFront("tanker"), "Not queued.");
        }

        [Test]
        public void Remove_LetsTheNextShipFollow()
        {
            RefuelingQueue<string> queue = new RefuelingQueue<string>();
            queue.Enqueue("liner");
            queue.Enqueue("refinery");

            Assert.IsTrue(queue.Remove("liner"));
            Assert.AreEqual("refinery", queue.Current);
            Assert.IsFalse(queue.Remove("liner"));
        }

        [Test]
        public void RemoveAll_KeepsTheOrderOfTheRest()
        {
            RefuelingQueue<string> queue = new RefuelingQueue<string>();
            queue.Enqueue("liner");
            queue.Enqueue("refinery");
            queue.Enqueue("cruiser");

            Assert.AreEqual(1, queue.RemoveAll(ship => ship == "refinery"));
            Assert.AreEqual("liner", queue.Current);
            Assert.AreEqual(1, queue.PositionOf("cruiser"));
        }
    }
}

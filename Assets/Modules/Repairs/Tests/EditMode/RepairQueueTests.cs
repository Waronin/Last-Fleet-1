using NUnit.Framework;

namespace LastFleet.Repairs.Tests
{
    public class RepairQueueTests
    {
        sealed class Ship
        {
        }

        [Test]
        public void Enqueue_KeepsThePlayersOrderAndRefusesDuplicates()
        {
            RepairQueue<Ship> queue = new RepairQueue<Ship>();
            Ship first = new Ship();
            Ship second = new Ship();

            Assert.IsNull(queue.Current);
            Assert.IsTrue(queue.Enqueue(first));
            Assert.IsTrue(queue.Enqueue(second));
            Assert.IsFalse(queue.Enqueue(first), "A ship waits only once.");

            Assert.AreSame(first, queue.Current);
            Assert.AreEqual(1, queue.PositionOf(second));
            Assert.AreEqual(2, queue.Count);
        }

        [Test]
        public void MoveToFront_LetsAWaitingShipGoNext()
        {
            RepairQueue<Ship> queue = new RepairQueue<Ship>();
            Ship first = new Ship();
            Ship second = new Ship();
            Ship third = new Ship();
            queue.Enqueue(first);
            queue.Enqueue(second);
            queue.Enqueue(third);

            Assert.IsTrue(queue.MoveToFront(third));
            Assert.IsFalse(queue.MoveToFront(third), "Already first.");
            Assert.IsFalse(queue.MoveToFront(new Ship()), "Not queued.");

            Assert.AreSame(third, queue.Ships[0]);
            Assert.AreSame(first, queue.Ships[1]);
            Assert.AreSame(second, queue.Ships[2]);
        }

        [Test]
        public void Remove_AndRemoveAll_KeepTheOrderOfTheRest()
        {
            RepairQueue<Ship> queue = new RepairQueue<Ship>();
            Ship first = new Ship();
            Ship second = new Ship();
            Ship third = new Ship();
            queue.Enqueue(first);
            queue.Enqueue(second);
            queue.Enqueue(third);

            Assert.IsTrue(queue.Remove(first));
            Assert.IsFalse(queue.Remove(first));
            Assert.AreSame(second, queue.Current);

            Assert.AreEqual(1, queue.RemoveAll(ship => ship == second));
            Assert.AreSame(third, queue.Current);
            Assert.AreEqual(1, queue.Count);
        }
    }
}

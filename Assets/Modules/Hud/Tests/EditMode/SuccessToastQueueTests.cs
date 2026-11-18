using NUnit.Framework;

namespace LastFleet.Hud.Tests
{
    public class SuccessToastQueueTests
    {
        static SuccessToastQueue Create()
        {
            return new SuccessToastQueue(2, 4f, 0.5f);
        }

        [Test]
        public void Toasts_ShowAtOnce_UpToTheLimit_AndTheRestWait()
        {
            SuccessToastQueue queue = Create();

            queue.Enqueue("a");
            queue.Enqueue("b");
            queue.Enqueue("c");

            Assert.AreEqual(2, queue.Visible.Count);
            Assert.AreEqual("a", queue.Visible[0].Message);
            Assert.AreEqual(1, queue.WaitingCount);
        }

        [Test]
        public void ExpiredToasts_MakeRoomForWaitingOnes()
        {
            SuccessToastQueue queue = Create();
            queue.Enqueue("a");
            queue.Advance(2f);
            queue.Enqueue("b");
            queue.Enqueue("c");

            queue.Advance(2f);

            Assert.AreEqual(2, queue.Visible.Count);
            Assert.AreEqual("b", queue.Visible[0].Message);
            Assert.AreEqual("c", queue.Visible[1].Message);
            Assert.AreEqual(0f, queue.Visible[1].AgeSeconds, 1e-4f, "A waiting toast starts its time when it shows.");
        }

        [Test]
        public void Opacity_FadesInAndOut()
        {
            SuccessToastQueue queue = Create();
            queue.Enqueue("a");
            SuccessToast toast = queue.Visible[0];

            Assert.AreEqual(0f, queue.OpacityOf(toast), 1e-4f);
            queue.Advance(0.25f);
            Assert.AreEqual(0.5f, queue.OpacityOf(toast), 1e-4f);
            queue.Advance(1f);
            Assert.AreEqual(1f, queue.OpacityOf(toast), 1e-4f);
            queue.Advance(2.5f);
            Assert.AreEqual(0.5f, queue.OpacityOf(toast), 1e-4f);
        }

        [Test]
        public void ABurst_KeepsOnlyTheNewestWaiting()
        {
            SuccessToastQueue queue = new SuccessToastQueue(1, 4f, 0.5f);
            for (int i = 0; i <= SuccessToastQueue.MaximumWaiting + 1; i++)
            {
                queue.Enqueue(i.ToString());
            }

            Assert.AreEqual(SuccessToastQueue.MaximumWaiting, queue.WaitingCount);
            queue.Advance(4f);
            Assert.AreEqual("2", queue.Visible[0].Message, "The oldest waiting toast gave way.");
        }

        [Test]
        public void InvalidTiming_IsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SuccessToastQueue(0, 4f, 0.5f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SuccessToastQueue(1, 0.5f, 0.5f));
        }
    }
}

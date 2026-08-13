using NUnit.Framework;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class RingBufferTests
    {
        [Test]
        public void Write_BelowCapacity_IncrementsCount()
        {
            var buf = new RingBuffer<int>(5);
            buf.Write(1);
            buf.Write(2);
            buf.Write(3);

            Assert.AreEqual(3, buf.Count);
            Assert.AreEqual(5, buf.Capacity);
        }

        [Test]
        public void Write_BeyondCapacity_OverwritesOldest()
        {
            var buf = new RingBuffer<int>(3);
            buf.Write(1);
            buf.Write(2);
            buf.Write(3);
            buf.Write(4);

            Assert.AreEqual(3, buf.Count);
            var all = buf.GetAll();
            Assert.AreEqual(new[] { 2, 3, 4 }, all);
        }

        [Test]
        public void GetAll_ReturnsChronologicalOrder()
        {
            var buf = new RingBuffer<int>(5);
            buf.Write(10);
            buf.Write(20);
            buf.Write(30);

            var all = buf.GetAll();
            Assert.AreEqual(new[] { 10, 20, 30 }, all);
        }

        [Test]
        public void GetAll_AfterWrap_ReturnsCorrectOrder()
        {
            var buf = new RingBuffer<int>(3);
            buf.Write(1);
            buf.Write(2);
            buf.Write(3);
            buf.Write(4);
            buf.Write(5);

            var all = buf.GetAll();
            Assert.AreEqual(new[] { 3, 4, 5 }, all);
        }

        [Test]
        public void Freeze_PreventsWrite()
        {
            var buf = new RingBuffer<int>(5);
            buf.Write(1);
            buf.Write(2);
            buf.Freeze();

            Assert.IsTrue(buf.IsFrozen);
            buf.Write(3);

            Assert.AreEqual(2, buf.Count);
            var all = buf.GetAll();
            Assert.AreEqual(new[] { 1, 2 }, all);
        }

        [Test]
        public void Unfreeze_ResumesWrite()
        {
            var buf = new RingBuffer<int>(5);
            buf.Write(1);
            buf.Freeze();
            buf.Unfreeze();
            buf.Write(2);

            Assert.IsFalse(buf.IsFrozen);
            Assert.AreEqual(2, buf.Count);
            Assert.AreEqual(new[] { 1, 2 }, buf.GetAll());
        }

        [Test]
        public void GetRange_ReturnsCorrectSubset()
        {
            var buf = new RingBuffer<int>(5);
            buf.Write(10);
            buf.Write(20);
            buf.Write(30);
            buf.Write(40);

            var range = buf.GetRange(1, 2);
            Assert.AreEqual(new[] { 20, 30 }, range);
        }

        [Test]
        public void GetLast_ReturnsMostRecentN()
        {
            var buf = new RingBuffer<int>(5);
            buf.Write(10);
            buf.Write(20);
            buf.Write(30);

            var last = buf.GetLast(2);
            Assert.AreEqual(new[] { 20, 30 }, last);
        }

        [Test]
        public void GetLast_AfterWrap_ReturnsCorrectItems()
        {
            var buf = new RingBuffer<int>(3);
            buf.Write(1);
            buf.Write(2);
            buf.Write(3);
            buf.Write(4);
            buf.Write(5);

            var last = buf.GetLast(2);
            Assert.AreEqual(new[] { 4, 5 }, last);
        }

        [Test]
        public void Clear_ResetsBuffer()
        {
            var buf = new RingBuffer<int>(5);
            buf.Write(1);
            buf.Write(2);
            buf.Clear();

            Assert.AreEqual(0, buf.Count);
            Assert.IsFalse(buf.IsFrozen);
            CollectionAssert.IsEmpty(buf.GetAll());
        }

        [Test]
        public void Write_DefaultStruct_DoesNotThrow()
        {
            var buf = new RingBuffer<RuntimeEvent>(4);
            buf.Write(default);
            buf.Write(RuntimeEvent.Create(1, 100, 42, -1));

            Assert.AreEqual(2, buf.Count);
            var all = buf.GetAll();
            Assert.IsFalse(all[0].IsValid);
            Assert.IsTrue(all[1].IsValid);
        }

        [Test]
        public void GetRange_InvalidStart_ReturnsEmpty()
        {
            var buf = new RingBuffer<int>(5);
            buf.Write(1);

            var range = buf.GetRange(10, 2);
            CollectionAssert.IsEmpty(range);
        }
    }
}

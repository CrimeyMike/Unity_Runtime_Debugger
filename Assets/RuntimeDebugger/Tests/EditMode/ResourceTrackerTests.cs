using NUnit.Framework;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class ResourceTrackerTests
    {
        private ResourceTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _tracker = new ResourceTracker(64);
        }

        [Test]
        public void RecordLoadStart_AddsRecord()
        {
            int taskId = _tracker.RecordLoadStart("Assets/Sprites/icon.png", 1, 1, 100);

            Assert.IsTrue(taskId > 0);
            Assert.AreEqual(1, _tracker.PendingLoadCount);
            Assert.AreEqual(1, _tracker.RecordCount);
        }

        [Test]
        public void RecordLoadComplete_RemovesFromPending()
        {
            int taskId = _tracker.RecordLoadStart("Assets/icon.png", 1, 1, 100);
            _tracker.RecordLoadComplete(taskId, "Assets/icon.png", 3, 300);

            Assert.AreEqual(0, _tracker.PendingLoadCount);
            Assert.AreEqual(1, _tracker.ActiveHandleCount);
        }

        [Test]
        public void RecordLoadFail_RemovesFromPending()
        {
            int taskId = _tracker.RecordLoadStart("Assets/icon.png", 1, 1, 100);
            _tracker.RecordLoadFail(taskId, "Assets/icon.png", 3, 300);

            Assert.AreEqual(0, _tracker.PendingLoadCount);
            Assert.AreEqual(0, _tracker.ActiveHandleCount);
        }

        [Test]
        public void RecordRelease_DecrementsActiveHandles()
        {
            int taskId = _tracker.RecordLoadStart("Assets/icon.png", 1, 1, 100);
            _tracker.RecordLoadComplete(taskId, "Assets/icon.png", 2, 200);
            _tracker.RecordRelease("Assets/icon.png", 5, 500);

            Assert.AreEqual(0, _tracker.ActiveHandleCount);
        }

        [Test]
        public void IsLoading_True_AfterLoadStart()
        {
            _tracker.RecordLoadStart("Assets/icon.png", 1, 1, 100);
            Assert.IsTrue(_tracker.IsLoading("Assets/icon.png"));
        }

        [Test]
        public void IsLoading_False_AfterLoadComplete()
        {
            int taskId = _tracker.RecordLoadStart("Assets/icon.png", 1, 1, 100);
            _tracker.RecordLoadComplete(taskId, "Assets/icon.png", 2, 200);
            Assert.IsFalse(_tracker.IsLoading("Assets/icon.png"));
        }

        [Test]
        public void GetDuplicateLoads_DetectsRepeatedLoads()
        {
            _tracker.RecordLoadStart("Assets/icon.png", 1, 1, 100);
            _tracker.RecordLoadComplete(1, "Assets/icon.png", 2, 200);
            _tracker.RecordLoadStart("Assets/icon.png", 3, 3, 300);
            _tracker.RecordLoadComplete(2, "Assets/icon.png", 4, 400);
            _tracker.RecordLoadStart("Assets/icon.png", 5, 5, 500);

            var duplicates = _tracker.GetDuplicateLoads(10);
            Assert.AreEqual(2, duplicates.Count); // 2nd and 3rd loads are duplicates
        }

        [Test]
        public void GetDuplicateLoads_NoDuplicates_ReturnsEmpty()
        {
            _tracker.RecordLoadStart("Assets/a.png", 1, 1, 100);
            _tracker.RecordLoadComplete(1, "Assets/a.png", 2, 200);
            _tracker.RecordLoadStart("Assets/b.png", 3, 3, 300);

            var duplicates = _tracker.GetDuplicateLoads(10);
            Assert.AreEqual(0, duplicates.Count);
        }

        [Test]
        public void Clear_ResetsTracker()
        {
            _tracker.RecordLoadStart("Assets/icon.png", 1, 1, 100);
            _tracker.Clear();

            Assert.AreEqual(0, _tracker.PendingLoadCount);
            Assert.AreEqual(0, _tracker.ActiveHandleCount);
            Assert.AreEqual(0, _tracker.RecordCount);
        }

        [Test]
        public void Freeze_PreventsNewRecords()
        {
            _tracker.Freeze();
            _tracker.RecordLoadStart("Assets/icon.png", 1, 1, 100);

            Assert.AreEqual(0, _tracker.RecordCount);
        }

        [Test]
        public void MultipleAssets_TrackedIndependently()
        {
            _tracker.RecordLoadStart("Assets/a.png", 1, 1, 100);
            _tracker.RecordLoadStart("Assets/b.png", 2, 1, 100);

            Assert.AreEqual(2, _tracker.PendingLoadCount);
            Assert.IsTrue(_tracker.IsLoading("Assets/a.png"));
            Assert.IsTrue(_tracker.IsLoading("Assets/b.png"));
        }

        [Test]
        public void FullLoadCycle_RecordsAllOperations()
        {
            int taskId = _tracker.RecordLoadStart("Assets/icon.png", 1, 1, 100);
            _tracker.RecordLoadComplete(taskId, "Assets/icon.png", 2, 200);
            _tracker.RecordRelease("Assets/icon.png", 5, 500);

            var records = _tracker.GetRecords();
            Assert.AreEqual(3, records.Length);
            Assert.AreEqual(ResourceOperation.LoadStart, records[0].OperationEnum);
            Assert.AreEqual(ResourceOperation.LoadComplete, records[1].OperationEnum);
            Assert.AreEqual(ResourceOperation.Release, records[2].OperationEnum);
        }
    }
}

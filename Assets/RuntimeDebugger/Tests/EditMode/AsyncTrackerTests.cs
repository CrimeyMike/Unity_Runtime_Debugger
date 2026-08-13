using NUnit.Framework;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class AsyncTrackerTests
    {
        private AsyncTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _tracker = new AsyncTracker(64);
        }

        [Test]
        public void StartTask_AssignsUniqueId()
        {
            int id1 = _tracker.StartTask("Load.A", 1, 1, 100);
            int id2 = _tracker.StartTask("Load.B", 2, 1, 110);
            // id1 and id2 still unique

            Assert.AreEqual(1, id1);
            Assert.AreEqual(2, id2);
            Assert.AreEqual(2, _tracker.ActiveTaskCount);
        }

        [Test]
        public void Complete_RemovesFromActive()
        {
            int id = _tracker.StartTask("Load.A", 1, 1, 100);
            _tracker.Complete(id, 3, 300);

            Assert.AreEqual(0, _tracker.ActiveTaskCount);
            Assert.IsFalse(_tracker.IsActive(id));
        }

        [Test]
        public void Complete_RecordsInBuffer()
        {
            int id = _tracker.StartTask("Load.A", 1, 1, 100);
            _tracker.Complete(id, 3, 300);

            var records = _tracker.GetRecords();
            Assert.AreEqual(1, records.Length);
            Assert.AreEqual(AsyncStatus.Completed, records[0].StatusEnum);
            Assert.AreEqual(300, records[0].CompleteMs);
        }

        [Test]
        public void Cancel_RecordsCancelledStatus()
        {
            int id = _tracker.StartTask("Load.A", 1, 1, 100);
            _tracker.Cancel(id, 3, 300);

            var records = _tracker.GetRecords();
            Assert.AreEqual(AsyncStatus.Cancelled, records[0].StatusEnum);
        }

        [Test]
        public void Fail_RecordsFailedStatus()
        {
            int id = _tracker.StartTask("Load.A", 1, 1, 100);
            _tracker.Fail(id, 3, 300);

            var records = _tracker.GetRecords();
            Assert.AreEqual(AsyncStatus.Failed, records[0].StatusEnum);
        }

        [Test]
        public void NotifyOwnerDestroyed_MarksActiveTasks()
        {
            int ownerObj = 10;
            int taskId = _tracker.StartTask("Load.A", ownerObj, 1, 100);

            _tracker.NotifyOwnerDestroyed(ownerObj, 5, 500);

            // Task is still active (not completed yet)
            Assert.IsTrue(_tracker.IsActive(taskId));
        }

        [Test]
        public void RaceCondition_OwnerDestroyedBeforeComplete()
        {
            int ownerObj = 10;
            int taskId = _tracker.StartTask("Load.A", ownerObj, 1, 100);
            _tracker.NotifyOwnerDestroyed(ownerObj, 3, 300);
            _tracker.Complete(taskId, 5, 500);

            var racing = _tracker.GetRacingTasks();
            Assert.AreEqual(1, racing.Count);
            Assert.IsTrue(racing[0].OwnerDestroyedBeforeComplete);
        }

        [Test]
        public void NoRaceCondition_OwnerAlive()
        {
            int ownerObj = 10;
            int taskId = _tracker.StartTask("Load.A", ownerObj, 1, 100);
            _tracker.Complete(taskId, 5, 500);

            var racing = _tracker.GetRacingTasks();
            Assert.AreEqual(0, racing.Count);
        }

        [Test]
        public void Clear_ResetsTracker()
        {
            _tracker.StartTask("Load.A", 1, 1, 100);
            _tracker.Clear();

            Assert.AreEqual(0, _tracker.ActiveTaskCount);
            Assert.AreEqual(0, _tracker.CompletedCount);
        }

        [Test]
        public void Freeze_PreventsNewRecords()
        {
            _tracker.Freeze();
            int id = _tracker.StartTask("Load.A", 1, 1, 100);
            _tracker.Complete(id, 3, 300);

            Assert.AreEqual(0, _tracker.CompletedCount);
        }

        [Test]
        public void MultipleTasks_SameOwner_AllMarkedOnDestroy()
        {
            int ownerObj = 10;
            int t1 = _tracker.StartTask("Load.A", ownerObj, 1, 100);
            int t2 = _tracker.StartTask("Load.B", ownerObj, 1, 100);

            _tracker.NotifyOwnerDestroyed(ownerObj, 3, 300);
            _tracker.Complete(t1, 5, 500);
            _tracker.Complete(t2, 6, 600);

            var racing = _tracker.GetRacingTasks();
            Assert.AreEqual(2, racing.Count);
        }
    }
}

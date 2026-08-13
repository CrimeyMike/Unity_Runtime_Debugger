using NUnit.Framework;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class LifecycleTrackerTests
    {
        private LifecycleTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _tracker = new LifecycleTracker(64);
        }

        [Test]
        public void Track_AssignsUniqueId()
        {
            int id1 = _tracker.Track("OperatorPanel", 1, 100);
            int id2 = _tracker.Track("OperatorPanel", 2, 200);

            Assert.AreEqual(1, id1);
            Assert.AreEqual(2, id2);
            Assert.AreEqual(2, _tracker.TrackedCount);
        }

        [Test]
        public void FullLifecycle_CreateEnableDisableDestroy()
        {
            int id = _tracker.Track("MyComponent", 1, 100);
            _tracker.OnEnable(id, "MyComponent", 1, 110);
            _tracker.OnDisable(id, "MyComponent", 3, 300);
            _tracker.OnDestroy(id, "MyComponent", 5, 500);

            var records = _tracker.GetRecords();
            Assert.AreEqual(4, records.Length);
            Assert.AreEqual(LifecyclePhase.Create, records[0].PhaseEnum);
            Assert.AreEqual(LifecyclePhase.Enable, records[1].PhaseEnum);
            Assert.AreEqual(LifecyclePhase.Disable, records[2].PhaseEnum);
            Assert.AreEqual(LifecyclePhase.Destroy, records[3].PhaseEnum);
        }

        [Test]
        public void IsAlive_True_AfterTrack()
        {
            int id = _tracker.Track("Obj", 1, 100);
            Assert.IsTrue(_tracker.IsAlive(id));
        }

        [Test]
        public void IsAlive_False_AfterDestroy()
        {
            int id = _tracker.Track("Obj", 1, 100);
            _tracker.OnDestroy(id, "Obj", 2, 200);
            Assert.IsFalse(_tracker.IsAlive(id));
        }

        [Test]
        public void CheckAsyncCallback_OnDestroyedObject_DetectsRaceCondition()
        {
            int id = _tracker.Track("Panel", 1, 100);
            _tracker.OnDestroy(id, "Panel", 3, 300);

            bool race = _tracker.CheckAsyncCallback(id, 42, "Panel", 5, 500);

            Assert.IsTrue(race);
        }

        [Test]
        public void CheckAsyncCallback_OnAliveObject_NoRaceCondition()
        {
            int id = _tracker.Track("Panel", 1, 100);
            _tracker.OnEnable(id, "Panel", 1, 110);

            bool race = _tracker.CheckAsyncCallback(id, 42, "Panel", 5, 500);

            Assert.IsFalse(race);
        }

        [Test]
        public void OnEnable_OnUnknownObject_NoOp()
        {
            _tracker.OnEnable(999, "Unknown", 1, 100);
            Assert.AreEqual(0, _tracker.RecordCount);
        }

        [Test]
        public void Clear_ResetsTracker()
        {
            _tracker.Track("A", 1, 100);
            _tracker.Track("B", 2, 200);
            _tracker.Clear();

            Assert.AreEqual(0, _tracker.TrackedCount);
            Assert.AreEqual(0, _tracker.RecordCount);
        }

        [Test]
        public void Freeze_PreventsNewRecords()
        {
            _tracker.Freeze();
            _tracker.Track("A", 1, 100);

            Assert.AreEqual(0, _tracker.RecordCount);
        }

        [Test]
        public void MultipleObjects_IndependentLifecycles()
        {
            int id1 = _tracker.Track("PanelA", 1, 100);
            int id2 = _tracker.Track("PanelB", 1, 100);

            _tracker.OnDestroy(id1, "PanelA", 3, 300);
            Assert.IsFalse(_tracker.IsAlive(id1));
            Assert.IsTrue(_tracker.IsAlive(id2));

            var records = _tracker.GetRecords();
            Assert.AreEqual(3, records.Length);
        }

        [Test]
        public void GetLastRecords_ReturnsMostRecentN()
        {
            int id = _tracker.Track("Obj", 1, 100);
            _tracker.OnEnable(id, "Obj", 2, 200);
            _tracker.OnDisable(id, "Obj", 3, 300);
            _tracker.OnDestroy(id, "Obj", 4, 400);

            var last2 = _tracker.GetLastRecords(2);
            Assert.AreEqual(2, last2.Length);
            Assert.AreEqual(LifecyclePhase.Disable, last2[0].PhaseEnum);
            Assert.AreEqual(LifecyclePhase.Destroy, last2[1].PhaseEnum);
        }
    }
}

using NUnit.Framework;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class TriggerSystemTests
    {

        [SetUp]
        public void SetUp()
        {
            RuntimeDebugger.Initialize(
                traceCapacity: 64, eventCapacity: 64,
                stateCapacity: 64, lifecycleCapacity: 64,
                asyncCapacity: 64, resourceCapacity: 64,
                perfCapacity: 256);
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeDebugger.Shutdown();
        }

        [Test]
        public void TriggerManually_FiresImmediately()
        {
            RuntimeDebugger.Triggers.TriggerManually(IncidentType.Custom, "Test manual trigger");

            // State should be Freezing on the same frame
            Assert.AreEqual(TriggerSystem.State.Freezing, RuntimeDebugger.Triggers.CurrentState);
        }

        [Test]
        public void ManualTrigger_AfterFrameUpdate_EntersDeepCapture()
        {
            RuntimeDebugger.Triggers.TriggerManually(IncidentType.Custom, "Test");
            RuntimeDebugger.Triggers.OnFrameUpdate(1, 100);

            Assert.AreEqual(TriggerSystem.State.DeepCapture, RuntimeDebugger.Triggers.CurrentState);
        }

        [Test]
        public void DeepCapture_AfterDuration_CompletesIncident()
        {
            RuntimeDebugger.Triggers.TriggerManually(IncidentType.Custom, "Test");
            // Enter DeepCapture
            RuntimeDebugger.Triggers.OnFrameUpdate(1, 100);

            // Simulate time passing beyond capture duration
            RuntimeDebugger.Triggers.OnFrameUpdate(200, 5000);

            Assert.AreEqual(TriggerSystem.State.IncidentReady, RuntimeDebugger.Triggers.CurrentState);
            Assert.IsTrue(RuntimeDebugger.Triggers.HasPendingIncident);
        }

        [Test]
        public void RetrieveIncident_ReturnsIncidentAndResetsState()
        {
            RuntimeDebugger.Triggers.TriggerManually(IncidentType.Custom, "Test");
            RuntimeDebugger.Triggers.OnFrameUpdate(1, 100);
            RuntimeDebugger.Triggers.OnFrameUpdate(200, 5000);

            var incident = RuntimeDebugger.Triggers.RetrieveIncident();

            Assert.IsNotNull(incident);
            Assert.AreEqual(IncidentType.Custom, incident.Type);
            Assert.AreEqual(TriggerSystem.State.Idle, RuntimeDebugger.Triggers.CurrentState);
        }

        [Test]
        public void RegisterTrigger_AddsTrigger()
        {
            var trigger = new CustomTrigger("Test", () => false);
            RuntimeDebugger.Triggers.RegisterTrigger(trigger);

            Assert.AreEqual(1, RuntimeDebugger.Triggers.TriggerCount);
        }

        [Test]
        public void CustomTrigger_FiresWhenConditionMet()
        {
            bool shouldFire = false;
            var trigger = new CustomTrigger("TestCondition", () => shouldFire);
            RuntimeDebugger.Triggers.RegisterTrigger(trigger);

            // Should not fire when condition is false
            RuntimeDebugger.Triggers.OnFrameUpdate(1, 100);
            Assert.AreEqual(TriggerSystem.State.Idle, RuntimeDebugger.Triggers.CurrentState);

            // Now set condition to true
            shouldFire = true;
            RuntimeDebugger.Triggers.OnFrameUpdate(2, 200);

            Assert.AreNotEqual(TriggerSystem.State.Idle, RuntimeDebugger.Triggers.CurrentState);
        }

        [Test]
        public void FrameSpikeTrigger_DoesNotFire_WhenFrameTimeBelowThreshold()
        {
            var trigger = new FrameSpikeTrigger(1000); // 1000ms threshold
            RuntimeDebugger.Triggers.RegisterTrigger(trigger);

            RuntimeDebugger.Triggers.OnFrameUpdate(1, 100);

            Assert.AreEqual(TriggerSystem.State.Idle, RuntimeDebugger.Triggers.CurrentState);
        }

        [Test]
        public void ManualTrigger_CapturesPreTraceData()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Turn.End");
            }

            RuntimeDebugger.Triggers.TriggerManually(IncidentType.Custom, "Test");
            RuntimeDebugger.Triggers.OnFrameUpdate(1, 100);
            RuntimeDebugger.Triggers.OnFrameUpdate(200, 5000);

            var incident = RuntimeDebugger.Triggers.RetrieveIncident();

            Assert.Greater(incident.PreTrace.Count, 0);
            Assert.Greater(incident.Events.Count, 0);
        }

        [Test]
        public void Reset_ClearsPendingIncident()
        {
            RuntimeDebugger.Triggers.TriggerManually(IncidentType.Custom, "Test");
            RuntimeDebugger.Triggers.Reset();

            Assert.AreEqual(TriggerSystem.State.Idle, RuntimeDebugger.Triggers.CurrentState);
            Assert.IsFalse(RuntimeDebugger.Triggers.HasPendingIncident);
        }

        [Test]
        public void UnregisterTrigger_RemovesTrigger()
        {
            var trigger = new CustomTrigger("Test", () => false);
            RuntimeDebugger.Triggers.RegisterTrigger(trigger);
            RuntimeDebugger.Triggers.UnregisterTrigger(trigger);

            Assert.AreEqual(0, RuntimeDebugger.Triggers.TriggerCount);
        }

        [Test]
        public void FullPipeline_ManualTrigger_ExportIncident()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Turn.End");
            }

            RuntimeDebugger.Triggers.TriggerManually(IncidentType.PerformanceSpike, "Frame spike detected");
            RuntimeDebugger.Triggers.OnFrameUpdate(1, 100);
            RuntimeDebugger.Triggers.OnFrameUpdate(200, 5000);

            var incident = RuntimeDebugger.Triggers.RetrieveIncident();

            Assert.IsNotNull(incident);
            Assert.AreEqual(IncidentType.PerformanceSpike, incident.Type);
            Assert.AreEqual("Frame spike detected", incident.TriggerDescription);
            Assert.Greater(incident.PreTrace.Count, 0);
        }
    }
}

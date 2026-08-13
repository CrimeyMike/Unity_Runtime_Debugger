using NUnit.Framework;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class EventSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            RuntimeDebugger.Initialize(traceCapacity: 64, eventCapacity: 64);
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeDebugger.Shutdown();
        }

        [Test]
        public void RecordEvent_StoresEventInBuffer()
        {
            RuntimeDebugger.RecordEvent("Turn.End");

            var events = RuntimeDebugger.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual(HashUtil.HashString("Turn.End"), events[0].EventHash);
            Assert.IsTrue(events[0].IsValid);
        }

        [Test]
        public void RecordEvent_MultipleEvents_AllStored()
        {
            RuntimeDebugger.RecordEvent("Turn.Start");
            RuntimeDebugger.RecordEvent("Turn.End");
            RuntimeDebugger.RecordEvent("UI.Panel.Open");

            var events = RuntimeDebugger.GetEvents();
            Assert.AreEqual(3, events.Length);
            Assert.AreEqual(HashUtil.HashString("Turn.Start"), events[0].EventHash);
            Assert.AreEqual(HashUtil.HashString("Turn.End"), events[1].EventHash);
            Assert.AreEqual(HashUtil.HashString("UI.Panel.Open"), events[2].EventHash);
        }

        [Test]
        public void GetEventName_ReverseLookup_Works()
        {
            RuntimeDebugger.RecordEvent("Turn.End");
            int hash = HashUtil.HashString("Turn.End");

            Assert.AreEqual("Turn.End", RuntimeDebugger.GetEventName(hash));
        }

        [Test]
        public void GetEventName_UnknownHash_ReturnsHashString()
        {
            string name = RuntimeDebugger.GetEventName(12345);
            Assert.AreEqual("#12345", name);
        }

        [Test]
        public void Trace_CreatesTraceNode()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                // Trace is active
            }

            var traces = RuntimeDebugger.GetTraceTree();
            Assert.AreEqual(1, traces.Length);
            Assert.AreEqual(HashUtil.HashString("Turn.End"), traces[0].EventHash);
            Assert.IsTrue(traces[0].IsFinished);
        }

        [Test]
        public void Trace_Nested_CorrectParentChild()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                using (RuntimeDebugger.Trace("Event.Resolve"))
                {
                    // Inner trace
                }
            }

            var traces = RuntimeDebugger.GetTraceTree();
            Assert.AreEqual(2, traces.Length);

            // Inner trace completed first → nodes[0]
            Assert.AreEqual(HashUtil.HashString("Event.Resolve"), traces[0].EventHash);
            Assert.IsFalse(traces[0].IsRoot);

            // Outer trace completed second → nodes[1]
            Assert.AreEqual(HashUtil.HashString("Turn.End"), traces[1].EventHash);
            Assert.IsTrue(traces[1].IsRoot);
            Assert.AreEqual(1, traces[1].ChildCount);
        }

        [Test]
        public void RecordEvent_InsideTrace_HasContextId()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Event.Resolve");
            }

            var events = RuntimeDebugger.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.IsTrue(events[0].ContextId >= 0, "ContextId should be >= 0 when inside a trace");
        }

        [Test]
        public void RecordEvent_OutsideTrace_HasNoContext()
        {
            RuntimeDebugger.RecordEvent("Turn.Start");

            var events = RuntimeDebugger.GetEvents();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual(-1, events[0].ContextId);
        }

        [Test]
        public void SetEnabled_DisablesEventRecording()
        {
            RuntimeDebugger.SetEnabled(false);
            RuntimeDebugger.RecordEvent("Turn.End");

            Assert.AreEqual(0, RuntimeDebugger.EventCount);
        }

        [Test]
        public void SetEnabled_DisablesTrace()
        {
            RuntimeDebugger.SetEnabled(false);
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                // Should be no-op
            }

            Assert.AreEqual(0, RuntimeDebugger.TraceCount);
        }

        [Test]
        public void SetEnabled_ReEnablesEventRecording()
        {
            RuntimeDebugger.SetEnabled(false);
            RuntimeDebugger.SetEnabled(true);
            RuntimeDebugger.RecordEvent("Turn.End");

            Assert.AreEqual(1, RuntimeDebugger.EventCount);
        }

        [Test]
        public void ClearAll_ResetsBuffers()
        {
            RuntimeDebugger.RecordEvent("A");
            RuntimeDebugger.RecordEvent("B");
            using (RuntimeDebugger.Trace("T"))
            {
            }

            RuntimeDebugger.ClearAll();

            Assert.AreEqual(0, RuntimeDebugger.EventCount);
            Assert.AreEqual(0, RuntimeDebugger.TraceCount);
        }

        [Test]
        public void FullPipeline_TurnEndToUIRefresh()
        {
            // Simulate: Turn.End → Event.Resolve → Technology.Unlock → UI.Refresh
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Turn.End");

                using (RuntimeDebugger.Trace("Event.Resolve"))
                {
                    RuntimeDebugger.RecordEvent("Event.Resolve");

                    using (RuntimeDebugger.Trace("Technology.Unlock"))
                    {
                        RuntimeDebugger.RecordEvent("Technology.Unlock");
                    }

                    using (RuntimeDebugger.Trace("UI.Refresh"))
                    {
                        RuntimeDebugger.RecordEvent("UI.Refresh");
                    }
                }
            }

            var traces = RuntimeDebugger.GetTraceTree();
            var events = RuntimeDebugger.GetEvents();

            // 4 trace nodes: Turn.End, Event.Resolve, Technology.Unlock, UI.Refresh
            Assert.AreEqual(4, traces.Length);
            // 4 events
            Assert.AreEqual(4, events.Length);

            // Verify trace tree structure
            // Completion order: Technology.Unlock, UI.Refresh, Event.Resolve, Turn.End
            Assert.AreEqual(HashUtil.HashString("Technology.Unlock"), traces[0].EventHash);
            Assert.AreEqual(HashUtil.HashString("UI.Refresh"), traces[1].EventHash);
            Assert.AreEqual(HashUtil.HashString("Event.Resolve"), traces[2].EventHash);
            Assert.AreEqual(HashUtil.HashString("Turn.End"), traces[3].EventHash);

            // Turn.End is root with 1 child
            Assert.IsTrue(traces[3].IsRoot);
            Assert.AreEqual(1, traces[3].ChildCount);

            // Event.Resolve has 2 children
            Assert.AreEqual(2, traces[2].ChildCount);

            // All events have valid context (inside trace)
            foreach (var evt in events)
                Assert.IsTrue(evt.ContextId >= 0);
        }
    }
}

using NUnit.Framework;
using System.IO;
using UnityEngine;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class IncidentBuilderTests
    {
        private string _tempExportDir;

        [SetUp]
        public void SetUp()
        {
            RuntimeDebugger.Initialize(traceCapacity: 64, eventCapacity: 64);
            _tempExportDir = Path.Combine(Application.temporaryCachePath, "RuntimeDebuggerTests");
            if (Directory.Exists(_tempExportDir))
                Directory.Delete(_tempExportDir, true);
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeDebugger.Shutdown();
            if (Directory.Exists(_tempExportDir))
                Directory.Delete(_tempExportDir, true);
        }

        [Test]
        public void BuildFromCurrentState_CapturesTracesAndEvents()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Turn.End");

                using (RuntimeDebugger.Trace("Event.Resolve"))
                {
                    RuntimeDebugger.RecordEvent("Event.Resolve");
                }

                using (RuntimeDebugger.Trace("UI.Refresh"))
                {
                    RuntimeDebugger.RecordEvent("UI.Refresh");
                }
            }

            var incident = IncidentBuilder.BuildFromCurrentState(
                IncidentType.Custom,
                "Manual capture test");

            Assert.AreEqual(IncidentType.Custom, incident.Type);
            Assert.AreEqual("Manual capture test", incident.TriggerDescription);
            Assert.AreEqual(3, incident.PreTrace.Count);
            Assert.AreEqual(3, incident.Events.Count);
        }

        [Test]
        public void BuildFromCurrentState_FreezesAndUnfreezes()
        {
            RuntimeDebugger.RecordEvent("A");

            var incident = IncidentBuilder.BuildFromCurrentState(
                IncidentType.Custom,
                "Test");

            // After build, buffers should be unfrozen
            RuntimeDebugger.RecordEvent("B");
            Assert.AreEqual(2, RuntimeDebugger.EventCount);
        }

        [Test]
        public void Export_CreatesJsonFiles()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Turn.End");
            }

            var incident = IncidentBuilder.BuildFromCurrentState(
                IncidentType.PerformanceSpike,
                "Test export");

            string dirPath = IncidentExporter.Export(incident, _tempExportDir);

            Assert.IsTrue(Directory.Exists(dirPath));
            Assert.IsTrue(File.Exists(Path.Combine(dirPath, "incident.json")));
            Assert.IsTrue(File.Exists(Path.Combine(dirPath, "timeline.json")));
            Assert.IsTrue(File.Exists(Path.Combine(dirPath, "events.json")));
            Assert.IsTrue(File.Exists(Path.Combine(dirPath, "metadata.json")));
        }

        [Test]
        public void Export_IncidentJson_ContainsCorrectFields()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Turn.End");
            }

            var incident = IncidentBuilder.BuildFromCurrentState(
                IncidentType.PerformanceSpike,
                "Spike at frame 100");

            string dirPath = IncidentExporter.Export(incident, _tempExportDir);

            string incidentJson = File.ReadAllText(Path.Combine(dirPath, "incident.json"));
            Assert.IsTrue(incidentJson.Contains("PerformanceSpike"));
            Assert.IsTrue(incidentJson.Contains("Spike at frame 100"));
        }

        [Test]
        public void Export_TimelineJson_ContainsTraceNodes()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                using (RuntimeDebugger.Trace("Event.Resolve"))
                {
                }
            }

            var incident = IncidentBuilder.BuildFromCurrentState(
                IncidentType.Custom,
                "Test");

            string dirPath = IncidentExporter.Export(incident, _tempExportDir);

            string timelineJson = File.ReadAllText(Path.Combine(dirPath, "timeline.json"));
            Assert.IsTrue(timelineJson.Contains("items"));
            Assert.IsTrue(timelineJson.Contains("NodeId"));
        }

        [Test]
        public void Export_MetadataJson_ContainsEnvironmentInfo()
        {
            var incident = IncidentBuilder.BuildFromCurrentState(
                IncidentType.Custom,
                "Test");

            string dirPath = IncidentExporter.Export(incident, _tempExportDir);

            string metadataJson = File.ReadAllText(Path.Combine(dirPath, "metadata.json"));
            Assert.IsTrue(metadataJson.Contains("UnityVersion"));
            Assert.IsTrue(metadataJson.Contains("ScenePath"));
            Assert.IsTrue(metadataJson.Contains("ExportTimestamp"));
        }

        [Test]
        public void Build_ManualConstruction_Works()
        {
            var traces = new TraceNode[]
            {
                TraceNode.Create(0, -1, HashUtil.HashString("A"), 1, 100),
                TraceNode.Create(1, 0, HashUtil.HashString("B"), 1, 110)
            };

            var events = new RuntimeEvent[]
            {
                RuntimeEvent.Create(1, 100, HashUtil.HashString("A"), -1)
            };

            var incident = IncidentBuilder.Build(
                IncidentType.Exception,
                "Manual test",
                42,
                12345,
                traces,
                null,
                events);

            Assert.AreEqual(IncidentType.Exception, incident.Type);
            Assert.AreEqual(42, incident.TriggerFrame);
            Assert.AreEqual(12345, incident.TriggerTimestampMs);
            Assert.AreEqual(2, incident.PreTrace.Count);
            Assert.AreEqual(0, incident.PostTrace.Count);
            Assert.AreEqual(1, incident.Events.Count);
        }
    }
}

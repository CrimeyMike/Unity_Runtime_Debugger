using NUnit.Framework;
using System.IO;
using UnityEngine;
using RuntimeDebugger;

namespace RuntimeDebugger.Tests
{
    [TestFixture]
    public class AIPromptBuilderTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            RuntimeDebugger.Initialize(
                traceCapacity: 64, eventCapacity: 64,
                stateCapacity: 64, lifecycleCapacity: 64,
                asyncCapacity: 64, resourceCapacity: 64,
                perfCapacity: 128);
            _tempDir = Path.Combine(Application.temporaryCachePath, "AIPromptTests");
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeDebugger.Shutdown();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void IncidentContextBuilder_ContainsIncidentSummary()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Turn.End");
            }

            var incident = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "Test context");
            string context = IncidentContextBuilder.Build(incident);

            Assert.IsTrue(context.Contains("=== INCIDENT ==="));
            Assert.IsTrue(context.Contains("Custom"));
            Assert.IsTrue(context.Contains("Test context"));
        }

        [Test]
        public void IncidentContextBuilder_ContainsTimeline()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                using (RuntimeDebugger.Trace("Event.Resolve"))
                {
                }
            }

            var incident = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "Test");
            string context = IncidentContextBuilder.Build(incident);

            Assert.IsTrue(context.Contains("=== TIMELINE ==="));
            Assert.IsTrue(context.Contains("Turn.End"));
            Assert.IsTrue(context.Contains("Event.Resolve"));
        }

        [Test]
        public void IncidentContextBuilder_ContainsLifecycleData()
        {
            int objId = RuntimeDebugger.Lifecycle.Track("MyPanel", 1, 100);
            RuntimeDebugger.Lifecycle.OnEnable(objId, "MyPanel", 1, 110);
            RuntimeDebugger.Lifecycle.OnDestroy(objId, "MyPanel", 3, 300);

            var incident = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "Test");
            string context = IncidentContextBuilder.Build(incident);

            Assert.IsTrue(context.Contains("=== LIFECYCLE ==="));
            Assert.IsTrue(context.Contains("MyPanel"));
            Assert.IsTrue(context.Contains("Create"));
            Assert.IsTrue(context.Contains("Destroy"));
        }

        [Test]
        public void IncidentContextBuilder_ContainsAsyncRaceCondition()
        {
            int ownerId = RuntimeDebugger.Lifecycle.Track("Panel", 1, 100);
            int taskId = RuntimeDebugger.Async.StartTask("Load.Async", ownerId, 1, 100);
            RuntimeDebugger.Async.NotifyOwnerDestroyed(ownerId, 3, 300);
            RuntimeDebugger.Async.Complete(taskId, 5, 500);

            var incident = IncidentBuilder.BuildFromCurrentState(IncidentType.AsyncFailure, "Race condition");
            string context = IncidentContextBuilder.Build(incident);

            Assert.IsTrue(context.Contains("=== ASYNC TRACE ==="));
            Assert.IsTrue(context.Contains("RACE CONDITION"));
            Assert.IsTrue(context.Contains("Owner destroyed"));
        }

        [Test]
        public void IncidentContextBuilder_ContainsResourceData()
        {
            int taskId = RuntimeDebugger.Resource.RecordLoadStart("Assets/icon.png", 1, 1, 100);
            RuntimeDebugger.Resource.RecordLoadComplete(taskId, "Assets/icon.png", 2, 200);

            var incident = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "Test");
            string context = IncidentContextBuilder.Build(incident);

            Assert.IsTrue(context.Contains("=== RESOURCE OPERATIONS ==="));
            Assert.IsTrue(context.Contains("LoadStart"));
            Assert.IsTrue(context.Contains("LoadComplete"));
        }

        [Test]
        public void AIPromptBuilder_BuildPrompt_ContainsInstructions()
        {
            using (RuntimeDebugger.Trace("Turn.End"))
            {
                RuntimeDebugger.RecordEvent("Turn.End");
            }

            var incident = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "Test");
            string prompt = AIPromptBuilder.BuildPrompt(incident);

            Assert.IsTrue(prompt.Contains("You are analyzing a Unity runtime incident"));
            Assert.IsTrue(prompt.Contains("Output format"));
            Assert.IsTrue(prompt.Contains("Summary:"));
            Assert.IsTrue(prompt.Contains("Evidence:"));
            Assert.IsTrue(prompt.Contains("Hypotheses:"));
            Assert.IsTrue(prompt.Contains("Verification:"));
            Assert.IsTrue(prompt.Contains("=== RUNTIME CONTEXT ==="));
        }

        [Test]
        public void AIPromptBuilder_BuildPrompt_IncludesCodeContext()
        {
            var incident = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "Test");
            string prompt = AIPromptBuilder.BuildPrompt(incident, "public void Update() { /* code */ }");

            Assert.IsTrue(prompt.Contains("=== RELEVANT CODE ==="));
            Assert.IsTrue(prompt.Contains("public void Update"));
        }

        [Test]
        public void AIPromptBuilder_BuildAndSavePrompt_CreatesFile()
        {
            var incident = IncidentBuilder.BuildFromCurrentState(IncidentType.Custom, "Test");
            string path = AIPromptBuilder.BuildAndSavePrompt(incident, _tempDir);

            Assert.IsTrue(File.Exists(path));
            string content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("RUNTIME CONTEXT"));
        }

        [Test]
        public void AIResult_ParseFromLLMResponse_ExtractsSections()
        {
            string response = @"
Summary: The Turn.End pipeline triggered excessive resource loading.
Evidence:
- 18 asset loads occurred after Turn.End
- GC allocation spiked to 4.6MB
- Canvas.Rebuild was called in the same frame
Hypotheses:
- TechnologyPanel.Refresh triggered repeated sprite loads
- Missing resource caching caused duplicate loads
Unknowns:
- Whether the loads were from cache or disk
Verification:
- Disable UI refresh and check frame time
- Add resource caching and re-measure
";

            var result = AIResult.ParseFromLLMResponse(response);

            Assert.IsTrue(result.Summary.Contains("Turn.End pipeline"));
            Assert.AreEqual(3, result.Evidence.Count);
            Assert.AreEqual(2, result.Hypotheses.Count);
            Assert.AreEqual(1, result.Unknowns.Count);
            Assert.AreEqual(2, result.VerificationSteps.Count);
        }

        [Test]
        public void AIResult_ParseFromLLMResponse_EmptyResponse_ReturnsEmptyResult()
        {
            var result = AIResult.ParseFromLLMResponse("");

            Assert.IsNotNull(result);
            Assert.IsEmpty(result.Evidence);
            Assert.IsEmpty(result.Hypotheses);
        }
    }
}

using System;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace RuntimeDebugger.Editor
{
    public static class TestRunnerUtility
    {
        private static TestResultListener s_listener;

        [MenuItem("Runtime Debugger/Run Edit Mode Tests")]
        public static void RunEditModeTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();

            if (s_listener == null)
            {
                s_listener = ScriptableObject.CreateInstance<TestResultListener>();
                api.RegisterCallbacks(s_listener);
            }

            api.Execute(new ExecutionSettings(new Filter
            {
                assemblyNames = new[] { "RuntimeDebugger.Tests.EditMode" },
                testMode = TestMode.EditMode
            }));

            Debug.Log("[TestRunner] Edit mode tests started. Check console for results.");
        }
    }

    public class TestResultListener : ScriptableObject, ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"[TestRunner] Run started: {testsToRun.Name}");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"[TestRunner] Run finished: Pass={result.PassCount}, Fail={result.FailCount}, Skip={result.SkipCount}, Inconclusive={result.InconclusiveCount}");
            if (result.FailCount > 0)
            {
                Debug.LogError($"[TestRunner] {result.FailCount} test(s) FAILED!");
            }
            else
            {
                Debug.Log("[TestRunner] All tests PASSED!");
            }
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.TestStatus == TestStatus.Failed)
            {
                Debug.LogError($"[TestRunner] FAILED: {result.FullName} - {result.Message}");
            }
            else if (result.TestStatus == TestStatus.Passed)
            {
                Debug.Log($"[TestRunner] PASSED: {result.FullName}");
            }
        }
    }
}

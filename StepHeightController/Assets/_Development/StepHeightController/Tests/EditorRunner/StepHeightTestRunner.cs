using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Loags.StepHeight.Tests
{
    internal static class StepHeightTestRunner
    {
        private const string MenuPath = "Tools/StepHeightController Dev/Tests/Run All";
        private const string EditModeMenuPath = "Tools/StepHeightController Dev/Tests/Run EditMode";
        private const string PlayModeMenuPath = "Tools/StepHeightController Dev/Tests/Run PlayMode";

        [MenuItem(MenuPath, false, 20)]
        private static void RunFocusedTests()
        {
            RunTests(TestMode.EditMode, "Loags.StepHeight.Tests.EditMode", true);
        }

        [MenuItem(EditModeMenuPath, false, 21)]
        private static void RunFocusedEditModeTests() =>
            RunTests(TestMode.EditMode, "Loags.StepHeight.Tests.EditMode", false);

        [MenuItem(PlayModeMenuPath, false, 22)]
        private static void RunFocusedPlayModeTests() =>
            RunTests(TestMode.PlayMode, "Loags.StepHeight.Tests.PlayMode", false);

        private static void RunTests(TestMode testMode, string assemblyName, bool runPlayModeAfter)
        {
            var testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunner.RegisterCallbacks(new Callbacks(runPlayModeAfter));
            testRunner.Execute(new ExecutionSettings(new Filter
            {
                testMode = testMode,
                assemblyNames = new[] { assemblyName }
            }));
        }

        private sealed class Callbacks : ICallbacks
        {
            private readonly bool _runPlayModeAfter;

            public Callbacks(bool runPlayModeAfter)
            {
                _runPlayModeAfter = runPlayModeAfter;
            }

            public void RunStarted(ITestAdaptor testsToRun) =>
                Debug.Log($"[StepHeightTests] Started {testsToRun.TestCaseCount} focused tests.");

            public void RunFinished(ITestResultAdaptor result)
            {
                string summary = $"[StepHeightTests] Finished with {result.TestStatus}. " +
                                 $"Passed: {result.PassCount}, Failed: {result.FailCount}, " +
                                 $"Skipped: {result.SkipCount}, Inconclusive: {result.InconclusiveCount}.";
                if (result.FailCount == 0) Debug.Log(summary);
                else Debug.LogError(summary);

                if (_runPlayModeAfter)
                {
                    EditorApplication.delayCall += () =>
                        RunTests(TestMode.PlayMode, "Loags.StepHeight.Tests.PlayMode", false);
                }
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus != TestStatus.Failed) return;
                Debug.LogError($"[StepHeightTests] {result.FullName}: {result.Message}\n{result.StackTrace}");
            }
        }
    }
}

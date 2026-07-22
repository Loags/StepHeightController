using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LB.StepHeight.Tests
{
    internal static class StepHeightTestRunner
    {
        private const string MenuPath = "Tools/Step Height Controller/Run Focused Tests";
        private const string EditModeMenuPath = "Tools/Step Height Controller/Run Focused EditMode Tests";
        private const string PlayModeMenuPath = "Tools/Step Height Controller/Run Focused PlayMode Tests";

        [MenuItem(MenuPath)]
        private static void RunFocusedTests()
        {
            RunTests(TestMode.EditMode, "LB.StepHeight.Tests.EditMode", true);
        }

        [MenuItem(EditModeMenuPath)]
        private static void RunFocusedEditModeTests() =>
            RunTests(TestMode.EditMode, "LB.StepHeight.Tests.EditMode", false);

        [MenuItem(PlayModeMenuPath)]
        private static void RunFocusedPlayModeTests() =>
            RunTests(TestMode.PlayMode, "LB.StepHeight.Tests.PlayMode", false);

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
                        RunTests(TestMode.PlayMode, "LB.StepHeight.Tests.PlayMode", false);
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

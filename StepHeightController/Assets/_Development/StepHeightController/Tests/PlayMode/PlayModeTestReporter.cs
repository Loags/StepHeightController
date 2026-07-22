using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(LB.StepHeight.Tests.PlayModeTestReporter))]

namespace LB.StepHeight.Tests
{
    public sealed class PlayModeTestReporter : ITestRunCallback
    {
        public void RunStarted(ITest testsToRun)
        {
            Debug.Log($"[StepHeightPlayModeTests] Started {testsToRun.TestCaseCount} focused tests.");
        }

        public void RunFinished(ITestResult testResults)
        {
            string summary = $"[StepHeightPlayModeTests] Finished with {testResults.ResultState}. " +
                             $"Passed: {testResults.PassCount}, Failed: {testResults.FailCount}, " +
                             $"Skipped: {testResults.SkipCount}, Inconclusive: {testResults.InconclusiveCount}.";
            if (testResults.FailCount == 0) Debug.Log(summary);
            else Debug.LogError(summary);
        }

        public void TestStarted(ITest test) { }

        public void TestFinished(ITestResult result)
        {
            if (result.ResultState.Status != TestStatus.Failed) return;
            Debug.LogError($"[StepHeightPlayModeTests] {result.FullName}: {result.Message}\n{result.StackTrace}");
        }
    }
}

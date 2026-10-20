using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Loags.StepHeight.Diagnostics
{
    [InitializeOnLoad]
    internal static class EditorFrameRateLimiter
    {
        private const int TargetFrameRate = 60;
        private const string PreviousVSyncKey = "Loags.StepHeight.EditorFrameRateLimiter.PreviousVSync";
        private static double _lastUpdateTime;

        static EditorFrameRateLimiter()
        {
            Application.targetFrameRate = TargetFrameRate;
            EditorApplication.update += LimitFrameRate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void LimitFrameRate()
        {
            Application.targetFrameRate = TargetFrameRate;
            if (EditorApplication.isPlaying)
            {
                QualitySettings.vSyncCount = 0;
                return;
            }

            // The Game view limit does not throttle other Editor windows.
            double remainingTime = 1.0 / TargetFrameRate - (EditorApplication.timeSinceStartup - _lastUpdateTime);
            if (remainingTime > 0) Thread.Sleep((int)Math.Ceiling(remainingTime * 1000));
            _lastUpdateTime = EditorApplication.timeSinceStartup;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            // SessionState survives the domain reload when entering Play Mode.
            if (state == PlayModeStateChange.ExitingEditMode) SessionState.SetInt(PreviousVSyncKey, QualitySettings.vSyncCount);
            if (state == PlayModeStateChange.EnteredPlayMode) QualitySettings.vSyncCount = 0;
            if (state == PlayModeStateChange.EnteredEditMode) QualitySettings.vSyncCount = SessionState.GetInt(PreviousVSyncKey, QualitySettings.vSyncCount);
        }
    }
}

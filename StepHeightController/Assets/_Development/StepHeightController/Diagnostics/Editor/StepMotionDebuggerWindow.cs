using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LB.StepHeight.Diagnostics
{
    internal sealed class StepMotionDebuggerWindow : EditorWindow
    {
        private const int MaximumSamples = 1200;
        private const int MaximumTraces = 128;
        private const float SettlingWindow = 0.1f;

        [SerializeField] private StepHeightController controller;
        [SerializeField] private bool recording = true;
        [SerializeField] private bool drawSceneGizmos = true;
        [SerializeField] private string lastControllerName = "unavailable";

        private readonly List<MotionSample> _samples = new List<MotionSample>();
        private readonly List<StepTrace> _traces = new List<StepTrace>();
        private Rigidbody _body;
        private StepTrace _activeTrace;
        private StepTrace _lastCompletedTrace;
        private Vector2 _scrollPosition;
        private float _lastFixedTime = float.NegativeInfinity;
        private float _lastCompletionTime = float.NegativeInfinity;
        private int _nextTraceId = 1;

        [MenuItem("Tools/Step Height Controller/Open Motion Debugger")]
        private static void OpenWindow() => GetWindow<StepMotionDebuggerWindow>("Step Motion Debugger");

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SceneView.duringSceneGui += DrawSceneGizmos;
            SetController(controller);
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SceneView.duringSceneGui -= DrawSceneGizmos;
            Unsubscribe();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Select the player, enter Play Mode, and record a stair run. Cyan segments are active steps; orange segments are gaps between steps. This tool lives outside the shipped package.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            StepHeightController selectedController = (StepHeightController)EditorGUILayout.ObjectField(
                "Controller", controller, typeof(StepHeightController), true);
            if (EditorGUI.EndChangeCheck()) SetController(selectedController);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Selected Player")) UseSelectedController();
                using (new EditorGUI.DisabledScope(_traces.Count == 0))
                {
                    if (GUILayout.Button("Copy Recorded Steps")) CopyRecordedSteps();
                }
                if (GUILayout.Button("Clear Trace")) ClearTrace();
            }

            recording = EditorGUILayout.Toggle("Record", recording);
            drawSceneGizmos = EditorGUILayout.Toggle("Scene Gizmos", drawSceneGizmos);

            EditorGUILayout.Space();
            DrawLiveState();
            EditorGUILayout.Space();
            DrawTraceList();
        }

        private void DrawLiveState()
        {
            EditorGUILayout.LabelField("Live State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Editor", EditorApplication.isPlaying ? "Play Mode" : "Edit Mode");
            EditorGUILayout.LabelField("Step", controller != null && controller.IsStepping ? "Active" : "Idle");

            if (_body == null) return;

            Vector3 velocity = _body.linearVelocity;
            EditorGUILayout.LabelField("Velocity", $"{velocity.magnitude:F2} m/s");
            EditorGUILayout.LabelField("Horizontal", $"{Vector3.ProjectOnPlane(velocity, _body.transform.up).magnitude:F2} m/s");
            EditorGUILayout.LabelField("Vertical", $"{Vector3.Dot(velocity, _body.transform.up):F2} m/s");
            if (_lastCompletedTrace != null)
            {
                EditorGUILayout.LabelField("Post-Step Drop",
                    $"{_lastCompletedTrace.PendingMaximumDrop * 1000f:F1} mm");
                EditorGUILayout.LabelField("Post-Step Rise",
                    $"{_lastCompletedTrace.PendingMaximumRise * 1000f:F1} mm");
                EditorGUILayout.LabelField("Post-Step Peak Vertical Speed",
                    $"{_lastCompletedTrace.PendingPeakVerticalSpeed:F2} m/s");
            }
        }

        private void DrawTraceList()
        {
            EditorGUILayout.LabelField($"Recorded Steps ({_traces.Count})", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = _traces.Count - 1; i >= 0; i--)
            {
                StepTrace trace = _traces[i];
                GUIStyle style = trace.GapBefore > Time.fixedDeltaTime * 1.5f ? EditorStyles.boldLabel :
                    EditorStyles.label;
                EditorGUILayout.LabelField(FormatTrace(trace), style);
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                Repaint();
                return;
            }

            if (controller == null) SetController(FindFirstObjectByType<StepHeightController>());
            if (!recording || controller == null || _body == null) return;
            if (Mathf.Approximately(_lastFixedTime, Time.fixedTime)) return;

            _lastFixedTime = Time.fixedTime;
            _samples.Add(new MotionSample(_body.position, _body.linearVelocity, controller.IsStepping));
            if (_samples.Count > MaximumSamples) _samples.RemoveAt(0);
            UpdatePostStepMetrics();
            Repaint();
            SceneView.RepaintAll();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SetController(FindFirstObjectByType<StepHeightController>());
                _lastFixedTime = float.NegativeInfinity;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Unsubscribe();
                _body = null;
                _activeTrace = null;
                _lastCompletedTrace = null;
            }
        }

        private void UseSelectedController()
        {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject == null) return;

            StepHeightController selectedController = selectedObject.GetComponentInParent<StepHeightController>();
            if (selectedController == null) selectedController = selectedObject.GetComponentInChildren<StepHeightController>();
            SetController(selectedController);
        }

        private void SetController(StepHeightController nextController)
        {
            if (controller == nextController && _body != null) return;

            Unsubscribe();
            controller = nextController;
            _body = controller != null ? controller.GetComponent<Rigidbody>() : null;
            if (controller == null) return;

            lastControllerName = controller.name;
            controller.StepStarted += OnStepStarted;
            controller.StepCompleted += OnStepCompleted;
            controller.StepCancelled += OnStepCancelled;
        }

        private void Unsubscribe()
        {
            if (controller == null) return;

            controller.StepStarted -= OnStepStarted;
            controller.StepCompleted -= OnStepCompleted;
            controller.StepCancelled -= OnStepCancelled;
        }

        private void OnStepStarted(StepEventData step)
        {
            float gapBefore = float.IsNegativeInfinity(_lastCompletionTime) ? -1f :
                Mathf.Max(0f, Time.fixedTime - _lastCompletionTime);
            _activeTrace = new StepTrace(_nextTraceId++, step, Time.fixedTime, gapBefore);
            if (_lastCompletedTrace != null)
            {
                _activeTrace.GapTravelBefore = _lastCompletedTrace.PendingGapTravel;
                _activeTrace.GapAverageSpeedBefore = gapBefore > 0f ?
                    _lastCompletedTrace.PendingGapTravel / gapBefore : 0f;
            }

            _traces.Add(_activeTrace);
            if (_traces.Count > MaximumTraces) _traces.RemoveAt(0);
            _lastCompletedTrace = null;
        }

        private void OnStepCompleted(StepEventData step)
        {
            if (_activeTrace == null) return;

            _activeTrace.EndTime = Time.fixedTime;
            _activeTrace.CompletionPosition = _body != null ? _body.position : step.TargetPosition;
            _lastCompletionTime = Time.fixedTime;
            _lastCompletedTrace = _activeTrace;
            _activeTrace = null;
        }

        private void OnStepCancelled(StepEventData step, StepCancellationReason reason)
        {
            if (_activeTrace == null) return;

            _activeTrace.EndTime = Time.fixedTime;
            _activeTrace.Cancelled = true;
            _activeTrace = null;
        }

        private void UpdatePostStepMetrics()
        {
            if (_lastCompletedTrace == null || _body == null || controller == null) return;

            Vector3 displacement = _body.position - _lastCompletedTrace.CompletionPosition;
            _lastCompletedTrace.PendingGapTravel = Vector3.ProjectOnPlane(displacement, controller.transform.up).magnitude;
            if (Time.fixedTime - _lastCompletedTrace.EndTime > SettlingWindow) return;

            float verticalOffset = Vector3.Dot(displacement, controller.transform.up);
            _lastCompletedTrace.PendingMaximumDrop = Mathf.Max(_lastCompletedTrace.PendingMaximumDrop,
                Mathf.Max(0f, -verticalOffset));
            _lastCompletedTrace.PendingMaximumRise = Mathf.Max(_lastCompletedTrace.PendingMaximumRise,
                Mathf.Max(0f, verticalOffset));
            float verticalSpeed = Mathf.Abs(Vector3.Dot(_body.linearVelocity, controller.transform.up));
            _lastCompletedTrace.PendingPeakVerticalSpeed = Mathf.Max(
                _lastCompletedTrace.PendingPeakVerticalSpeed, verticalSpeed);
        }

        private void ClearTrace()
        {
            _samples.Clear();
            _traces.Clear();
            _activeTrace = null;
            _lastCompletedTrace = null;
            _lastCompletionTime = float.NegativeInfinity;
            _nextTraceId = 1;
            SceneView.RepaintAll();
        }

        private void CopyRecordedSteps()
        {
            var report = new StringBuilder();
            report.AppendLine("Step Motion Debugger Trace");
            report.AppendLine($"Recorded: {DateTime.Now:O}");
            report.AppendLine($"Controller: {lastControllerName}");
            report.AppendLine($"Fixed timestep: {Time.fixedDeltaTime * 1000f:F0} ms");
            report.AppendLine($"Recorded steps: {_traces.Count}");
            report.AppendLine();

            foreach (StepTrace trace in _traces)
            {
                report.AppendLine(FormatTrace(trace));
            }

            if (_lastCompletedTrace != null)
            {
                report.AppendLine();
                report.AppendLine($"Latest post-step drop: {_lastCompletedTrace.PendingMaximumDrop * 1000f:F1} mm");
                report.AppendLine($"Latest post-step rise: {_lastCompletedTrace.PendingMaximumRise * 1000f:F1} mm");
                report.AppendLine(
                    $"Latest post-step peak vertical speed: {_lastCompletedTrace.PendingPeakVerticalSpeed:F2} m/s");
            }

            EditorGUIUtility.systemCopyBuffer = report.ToString();
            ShowNotification(new GUIContent($"Copied {_traces.Count} recorded steps"));
        }

        private static string FormatTrace(StepTrace trace)
        {
            string gap = trace.GapBefore < 0f ? "--" : $"{trace.GapBefore * 1000f:F0} ms";
            string duration = trace.EndTime < trace.StartTime ? "active" :
                $"{(trace.EndTime - trace.StartTime) * 1000f:F0} ms";
            string state = trace.Cancelled ? "cancelled" : trace.EndTime < trace.StartTime ? "active" : "complete";
            string gapMotion = trace.GapBefore < 0f ? string.Empty :
                $"  travel {trace.GapTravelBefore:F2} m  avg {trace.GapAverageSpeedBefore:F2} m/s";
            string settling = trace.EndTime < trace.StartTime ? string.Empty :
                $"  settle-drop {trace.PendingMaximumDrop * 1000f:F1} mm" +
                $"  settle-rise {trace.PendingMaximumRise * 1000f:F1} mm" +
                $"  peak-v {trace.PendingPeakVerticalSpeed:F2} m/s";
            return $"#{trace.Id}  {state}  rise {trace.Height:F2} m  motion {duration}  gap {gap}{gapMotion}{settling}";
        }

        private void DrawSceneGizmos(SceneView sceneView)
        {
            if (!drawSceneGizmos || _samples.Count == 0) return;

            for (int i = 1; i < _samples.Count; i++)
            {
                MotionSample previous = _samples[i - 1];
                MotionSample current = _samples[i];
                Handles.color = current.IsStepping ? new Color(0f, 0.9f, 1f, 1f) :
                    new Color(1f, 0.45f, 0f, 1f);
                Handles.DrawAAPolyLine(4f, previous.Position, current.Position);
            }

            foreach (StepTrace trace in _traces)
            {
                float size = HandleUtility.GetHandleSize(trace.TargetPosition) * 0.08f;
                Handles.color = trace.Cancelled ? Color.red : trace == _activeTrace ? Color.yellow : Color.green;
                Handles.SphereHandleCap(0, trace.TargetPosition, Quaternion.identity, size, EventType.Repaint);
                Handles.DrawDottedLine(trace.StartPosition, trace.TargetPosition, 4f);
                string gap = trace.GapBefore < 0f ? "first" : $"gap {trace.GapBefore * 1000f:F0} ms";
                Handles.Label(trace.TargetPosition + Vector3.up * size,
                    $"Step #{trace.Id}\n{trace.Height:F2} m, {gap}");
            }

            if (_body == null) return;

            Vector3 velocity = _body.linearVelocity;
            float arrowSize = HandleUtility.GetHandleSize(_body.position) * 0.4f;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                Handles.color = Color.magenta;
                Handles.ArrowHandleCap(0, _body.position, Quaternion.LookRotation(velocity.normalized), arrowSize,
                    EventType.Repaint);
            }
        }

        private readonly struct MotionSample
        {
            public MotionSample(Vector3 position, Vector3 velocity, bool isStepping)
            {
                Position = position;
                Velocity = velocity;
                IsStepping = isStepping;
            }

            public Vector3 Position { get; }
            public Vector3 Velocity { get; }
            public bool IsStepping { get; }
        }

        private sealed class StepTrace
        {
            public StepTrace(int id, StepEventData step, float startTime, float gapBefore)
            {
                Id = id;
                StartPosition = step.StartPosition;
                TargetPosition = step.TargetPosition;
                Height = step.Height;
                StartTime = startTime;
                EndTime = -1f;
                GapBefore = gapBefore;
            }

            public int Id { get; }
            public Vector3 StartPosition { get; }
            public Vector3 TargetPosition { get; }
            public float Height { get; }
            public float StartTime { get; }
            public float GapBefore { get; }
            public float EndTime { get; set; }
            public bool Cancelled { get; set; }
            public Vector3 CompletionPosition { get; set; }
            public float GapTravelBefore { get; set; }
            public float GapAverageSpeedBefore { get; set; }
            public float PendingGapTravel { get; set; }
            public float PendingMaximumDrop { get; set; }
            public float PendingMaximumRise { get; set; }
            public float PendingPeakVerticalSpeed { get; set; }
        }
    }
}

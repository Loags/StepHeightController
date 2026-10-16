using UnityEditor;
using UnityEngine;

namespace LB.StepHeight.Editor
{
    [CustomEditor(typeof(StepHeightController))]
    [CanEditMultipleObjects]
    internal sealed class StepHeightControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _stepHeight;
        private SerializedProperty _minimumStepHeight;
        private SerializedProperty _maximumSurfaceAngle;
        private SerializedProperty _maximumApproachAngle;
        private SerializedProperty _detectionDistance;
        private SerializedProperty _surfaceProbeInset;
        private SerializedProperty _stepSpeed;
        private SerializedProperty _minimumStepDuration;
        private SerializedProperty _landingInset;
        private SerializedProperty _clearance;
        private SerializedProperty _layersToIgnore;
        private SerializedProperty _triggerInteraction;
        private SerializedProperty _includeChildColliders;
        private SerializedProperty _debugVisualization;
        private SerializedProperty _steppingDisabled;
        private bool _showAdvanced;
        private int _heightPreset = 1;
        private int _feelPreset = 1;

        private static readonly string[] HeightPresets = { "Low steps — 0.25 m", "All-round — 0.50 m", "Tall steps — 0.75 m" };
        private static readonly string[] FeelPresets = { "Gentle", "Balanced", "Responsive" };

        private void OnEnable()
        {
            _stepHeight = serializedObject.FindProperty("stepHeight");
            _minimumStepHeight = serializedObject.FindProperty("minimumStepHeight");
            _maximumSurfaceAngle = serializedObject.FindProperty("maximumSurfaceAngle");
            _maximumApproachAngle = serializedObject.FindProperty("maximumApproachAngle");
            _detectionDistance = serializedObject.FindProperty("detectionDistance");
            _surfaceProbeInset = serializedObject.FindProperty("surfaceProbeInset");
            _stepSpeed = serializedObject.FindProperty("stepSpeed");
            _minimumStepDuration = serializedObject.FindProperty("minimumStepDuration");
            _landingInset = serializedObject.FindProperty("landingInset");
            _clearance = serializedObject.FindProperty("clearance");
            _layersToIgnore = serializedObject.FindProperty("layersToIgnore");
            _triggerInteraction = serializedObject.FindProperty("triggerInteraction");
            _includeChildColliders = serializedObject.FindProperty("includeChildColliders");
            _debugVisualization = serializedObject.FindProperty("debugVisualization");
            _steppingDisabled = serializedObject.FindProperty("steppingDisabled");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = _steppingDisabled.hasMultipleDifferentValues;
            bool steppingEnabled = EditorGUILayout.Toggle(new GUIContent("Stepping Enabled", "Accept new step requests. Turning this off also cancels an active step."), !_steppingDisabled.boolValue);
            EditorGUI.showMixedValue = false;
            bool enabledChanged = EditorGUI.EndChangeCheck();
            if (enabledChanged) _steppingDisabled.boolValue = !steppingEnabled;

            EditorGUILayout.PropertyField(_stepHeight, new GUIContent("Maximum Step Height (m)", "Largest vertical rise accepted as a step."));
            EditorGUILayout.PropertyField(_stepSpeed, new GUIContent("Step Speed (m/s)", "Average speed used to move the player onto a step."));
            EditorGUILayout.PropertyField(_layersToIgnore, new GUIContent("Ignored Layers", "Exclude these layers from all stepping checks."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _heightPreset = EditorGUILayout.Popup("Height", _heightPreset, HeightPresets);
                if (GUILayout.Button("Apply", GUILayout.Width(50f))) ApplyHeightPreset();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _feelPreset = EditorGUILayout.Popup("Feel", _feelPreset, FeelPresets);
                if (GUILayout.Button("Apply", GUILayout.Width(50f))) ApplyFeelPreset();
            }

            EditorGUILayout.LabelField("Height and feel are independent. Your ignored layers stay unchanged.", EditorStyles.wordWrappedMiniLabel);
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (_showAdvanced) DrawAdvanced();

            if (enabledChanged && Application.isPlaying)
            {
                // Use the public property as well so an Inspector toggle cancels an active step.
                foreach (Object selectedTarget in targets)
                {
                    var controller = (StepHeightController)selectedTarget;
                    controller.SteppingEnabled = steppingEnabled;
                }
            }

            serializedObject.ApplyModifiedProperties();

            DrawSetupStatus();
            DrawRuntimeControls();
            if (GUILayout.Button("Setup & Demos")) StepHeightSetupWindow.Open();
        }

        private void ApplyHeightPreset()
        {
            // Height presets touch only the accepted height range.
            _stepHeight.floatValue = _heightPreset == 0 ? 0.25f : _heightPreset == 1 ? 0.5f : 0.75f;
            _minimumStepHeight.floatValue = 0.02f;
        }

        private void ApplyFeelPreset()
        {
            _stepSpeed.floatValue = _feelPreset == 0 ? 2.5f : _feelPreset == 1 ? 4.5f : 6.5f;
            _minimumStepDuration.floatValue = _feelPreset == 0 ? 0.2f : _feelPreset == 1 ? 0.12f : 0.08f;
        }

        private void DrawAdvanced()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_minimumStepHeight, new GUIContent("Minimum Step Height (m)", "Smaller rises are ordinary ground variation."));
            EditorGUILayout.PropertyField(_maximumSurfaceAngle, new GUIContent("Maximum Surface Angle (°)", "Steeper top surfaces are not walkable."));
            EditorGUILayout.PropertyField(_maximumApproachAngle, new GUIContent("Maximum Approach Angle (°)", "Horizontal angle allowed between movement and an obstacle."));
            EditorGUILayout.PropertyField(_detectionDistance, new GUIContent("Detection Distance (m)", "Extra reach beyond the player collider."));
            EditorGUILayout.PropertyField(_surfaceProbeInset, new GUIContent("Surface Probe Inset (m)", "Distance beyond the obstacle face used to find its top."));
            EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_minimumStepDuration, new GUIContent("Minimum Step Duration (s)", "Keeps small rises smooth without slowing consecutive stairs."));
            EditorGUILayout.PropertyField(_landingInset, new GUIContent("Landing Inset (m)", "Distance beyond the obstacle face at the end of a step."));
            EditorGUILayout.PropertyField(_clearance, new GUIContent("Clearance (m)", "Small gap used by destination collision checks."));
            EditorGUILayout.LabelField("Collision & Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_triggerInteraction, new GUIContent("Trigger Interaction"));
            EditorGUILayout.PropertyField(_includeChildColliders, new GUIContent("Include Child Colliders", "Include primitive colliders belonging to this Rigidbody."));
            EditorGUILayout.PropertyField(_debugVisualization, new GUIContent("Scene Visualization"));
            EditorGUI.indentLevel--;
        }

        private void DrawSetupStatus()
        {
            if (targets.Length != 1) return;

            var controller = (StepHeightController)target;
            Rigidbody body = controller.GetComponent<Rigidbody>();
            Collider[] colliders = _includeChildColliders.boolValue ? controller.GetComponentsInChildren<Collider>(true) : controller.GetComponents<Collider>();
            bool hasSupportedCollider = false;
            foreach (Collider playerCollider in colliders)
            {
                if (playerCollider.enabled && playerCollider.gameObject.activeInHierarchy && !playerCollider.isTrigger && playerCollider.attachedRigidbody == body &&
                    (playerCollider is CapsuleCollider || playerCollider is BoxCollider ||
                     playerCollider is SphereCollider))
                {
                    hasSupportedCollider = true;
                    break;
                }
            }

            if (!hasSupportedCollider)
            {
                EditorGUILayout.HelpBox(
                    "Add an enabled, non-trigger CapsuleCollider, BoxCollider, or SphereCollider to the player hierarchy.",
                    MessageType.Error);
            }

            if (body == null)
            {
                EditorGUILayout.HelpBox("A Rigidbody is required on the same GameObject.", MessageType.Error);
            }
            else if (body.isKinematic)
            {
                EditorGUILayout.HelpBox("Use a non-kinematic Rigidbody for stepping.", MessageType.Warning);
            }
        }

        private void DrawRuntimeControls()
        {
            if (!Application.isPlaying || targets.Length != 1) return;

            var controller = (StepHeightController)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("State", controller.IsStepping ? "Stepping" : "Idle");

            if (GUILayout.Button("Refresh Collider Cache")) controller.RefreshColliderCache();
            using (new EditorGUI.DisabledScope(!controller.IsStepping))
            {
                if (GUILayout.Button("Cancel Active Step")) controller.CancelStep();
            }
        }
    }
}

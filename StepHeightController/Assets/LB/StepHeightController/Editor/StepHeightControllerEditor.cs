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

            using (new EditorGUI.DisabledScope(_steppingDisabled.hasMultipleDifferentValues))
            {
                bool steppingEnabled = EditorGUILayout.Toggle(new GUIContent("Stepping Enabled",
                    "Controls whether the component accepts new step requests."), !_steppingDisabled.boolValue);
                _steppingDisabled.boolValue = !steppingEnabled;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_stepHeight, new GUIContent("Maximum Step Height (m)",
                "Largest vertical rise accepted as a step."));
            EditorGUILayout.PropertyField(_minimumStepHeight, new GUIContent("Minimum Step Height (m)",
                "Smaller rises are treated as ordinary ground variation."));
            EditorGUILayout.PropertyField(_maximumSurfaceAngle, new GUIContent("Maximum Surface Angle (°)",
                "Steeper top surfaces are not considered walkable."));
            EditorGUILayout.PropertyField(_maximumApproachAngle, new GUIContent("Maximum Approach Angle (°)",
                "Maximum horizontal angle between movement intent and an obstacle."));
            EditorGUILayout.PropertyField(_detectionDistance, new GUIContent("Detection Distance (m)",
                "Additional horizontal reach beyond the player collider."));
            EditorGUILayout.PropertyField(_surfaceProbeInset, new GUIContent("Surface Probe Inset (m)",
                "Distance beyond an obstacle face used to find its top surface."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_stepSpeed, new GUIContent("Step Speed (m/s)",
                "Maximum average speed used to move the Rigidbody to the accepted target."));
            EditorGUILayout.PropertyField(_minimumStepDuration, new GUIContent("Minimum Step Duration (s)",
                "Prevents short steps from completing in too few physics updates without slowing consecutive stairs."));
            EditorGUILayout.PropertyField(_landingInset, new GUIContent("Landing Inset (m)",
                "Distance beyond the detected obstacle face used for the final Rigidbody position."));
            EditorGUILayout.PropertyField(_clearance, new GUIContent("Clearance (m)",
                "Small separation used by final-position collision checks."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collision Filtering", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_layersToIgnore, new GUIContent("Ignored Layers",
                "These layers are excluded consistently from detection, surface, and clearance queries."));
            EditorGUILayout.PropertyField(_triggerInteraction, new GUIContent("Trigger Interaction"));
            EditorGUILayout.PropertyField(_includeChildColliders, new GUIContent("Include Child Colliders",
                "Treat supported colliders in the player's hierarchy as one compound shape."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_debugVisualization, new GUIContent("Scene Visualization"));

            serializedObject.ApplyModifiedProperties();

            DrawSetupStatus();
            DrawRuntimeControls();
        }

        private void DrawSetupStatus()
        {
            if (targets.Length != 1) return;

            var controller = (StepHeightController)target;
            Collider[] colliders = controller.GetComponentsInChildren<Collider>(true);
            bool hasSupportedCollider = false;
            foreach (Collider playerCollider in colliders)
            {
                if (playerCollider.enabled && !playerCollider.isTrigger &&
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

            if (controller.GetComponent<Rigidbody>() == null)
            {
                EditorGUILayout.HelpBox("A Rigidbody is required on the same GameObject.", MessageType.Error);
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

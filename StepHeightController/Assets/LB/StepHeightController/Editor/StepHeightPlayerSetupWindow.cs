using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LB.StepHeight.Editor
{
    public sealed class StepHeightPlayerSetupWindow : EditorWindow
    {
        private ObjectField _playerField;
        private Toggle _smoothMovement;
        private Toggle _keepUpright;
        private Label _preview;
        private Button _applyButton;

        [MenuItem("Tools/StepHeightController/Configure Rigidbody Player", false, 1)]
        public static void Open()
        {
            var window = GetWindow<StepHeightPlayerSetupWindow>();
            window.titleContent = new GUIContent("Configure Rigidbody Player");
            window.minSize = new Vector2(520f, 600f);
            window.Show();
        }

        private void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;

        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        private void OnUndoRedo()
        {
            if (_playerField != null) RefreshPreview();
        }

        public void CreateGUI()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(StepHeightSetupWindow.AssetRoot + "Editor/StepHeightPlayerSetupWindow.uxml");
            tree.CloneTree(rootVisualElement);
            _playerField = new ObjectField("Player") { objectType = typeof(GameObject), allowSceneObjects = true, tooltip = "Choose the object in your scene that holds the player's Rigidbody." };
            _playerField.AddToClassList("player-field");
            rootVisualElement.Q<VisualElement>("playerFieldContainer").Add(_playerField);
            _smoothMovement = rootVisualElement.Q<Toggle>("smoothMovement");
            _keepUpright = rootVisualElement.Q<Toggle>("keepUpright");
            _preview = rootVisualElement.Q<Label>("previewLabel");
            _applyButton = rootVisualElement.Q<Button>("applyButton");
            _playerField.RegisterValueChangedCallback(_ => RefreshPreview());
            _smoothMovement.RegisterValueChangedCallback(_ => RefreshPreview());
            _keepUpright.RegisterValueChangedCallback(_ => RefreshPreview());
            rootVisualElement.Q<Button>("selectionButton").clicked += UseSelection;
            rootVisualElement.Q<Button>("refreshButton").clicked += RefreshPreview;
            rootVisualElement.Q<Button>("documentationButton").clicked += StepHeightSetupWindow.OpenDocumentation;
            _applyButton.clicked += Apply;
            UseSelection();
        }

        private void OnFocus()
        {
            if (_playerField != null) RefreshPreview();
        }

        private void UseSelection()
        {
            _playerField.SetValueWithoutNotify(Selection.activeGameObject);
            RefreshPreview();
        }

        private bool CanConfigure(GameObject player, out string reason)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                reason = "Leave Play mode and wait for Unity to finish importing.";
                return false;
            }

            if (player == null || EditorUtility.IsPersistent(player) || !player.scene.IsValid() || EditorSceneManager.IsPreviewScene(player.scene))
            {
                reason = "Choose a player from the open scene. Select the object that holds its Rigidbody.";
                return false;
            }

            if (player.GetComponentInChildren<CharacterController>(true) != null)
            {
                reason = "This player uses CharacterController. Choose a Rigidbody-based player.";
                return false;
            }

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body == null || body.isKinematic)
            {
                reason = "Select the object with a Rigidbody and turn off Is Kinematic.";
                return false;
            }

            if (Vector3.Dot(player.transform.up, Vector3.up) < 0.999f)
            {
                reason = "Keep the Rigidbody root upright. Rotate the visual model separately.";
                return false;
            }

            var controller = player.GetComponent<StepHeightController>();
            bool includeChildren = controller == null || new SerializedObject(controller).FindProperty("includeChildColliders").boolValue;
            Collider[] colliders = includeChildren ? player.GetComponentsInChildren<Collider>(true) : player.GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                if (collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger && collider.attachedRigidbody == body && (collider is CapsuleCollider || collider is BoxCollider || collider is SphereCollider))
                {
                    reason = string.Empty;
                    return true;
                }
            }

            reason = "Add an enabled, non-trigger Capsule, Box or Sphere collider owned by this Rigidbody. Enable Include Child Colliders for a child shape.";
            return false;
        }

        private List<string> PlannedChanges(GameObject player)
        {
            var changes = new List<string>();
            if (player.GetComponent<StepHeightController>() == null) changes.Add("Add Step Height Controller.");
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (_smoothMovement.value && body.interpolation != RigidbodyInterpolation.Interpolate) changes.Add("Smooth movement with interpolation.");
            const RigidbodyConstraints upright = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (_keepUpright.value && (body.constraints & upright) != upright) changes.Add("Keep the player upright (lock X/Z rotation).");
            return changes;
        }

        private void RefreshPreview()
        {
            var player = _playerField.value as GameObject;
            _preview.RemoveFromClassList("status-ready");
            _preview.RemoveFromClassList("status-action");
            if (!CanConfigure(player, out string reason))
            {
                _preview.text = reason;
                _preview.AddToClassList("status-action");
                _applyButton.SetEnabled(false);
                return;
            }

            List<string> changes = PlannedChanges(player);
            _preview.text = changes.Count == 0 ? "Component settings are ready. No changes to apply." : "Changes to “" + player.name + "”:\n• " + string.Join("\n• ", changes);
            _preview.EnableInClassList("status-ready", changes.Count == 0);
            _applyButton.SetEnabled(changes.Count > 0);
        }

        private void Apply()
        {
            var player = _playerField.value as GameObject;
            if (!CanConfigure(player, out _) || PlannedChanges(player).Count == 0) return;

            // One Undo restores the entire setup action, including an added component.
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure Rigidbody stepping");
            if (player.GetComponent<StepHeightController>() == null) Undo.AddComponent<StepHeightController>(player);
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (_smoothMovement.value || _keepUpright.value)
            {
                Undo.RecordObject(body, "Configure Rigidbody stepping");
                if (_smoothMovement.value) body.interpolation = RigidbodyInterpolation.Interpolate;
                if (_keepUpright.value) body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(player.scene);
            Selection.activeGameObject = player;
            RefreshPreview();
        }
    }
}

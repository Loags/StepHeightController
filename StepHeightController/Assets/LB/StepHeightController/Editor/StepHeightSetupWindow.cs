using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LB.StepHeight.Editor
{
    public sealed class StepHeightSetupWindow : EditorWindow
    {
        internal const string AssetRoot = "Assets/LB/StepHeightController/";
        private static readonly string[] PackageNames = { "com.unity.inputsystem", "com.unity.cinemachine", "com.unity.render-pipelines.universal", "com.unity.ugui" };
        private static readonly string[] TestedVersions = { "1.19.0", "3.1.7", "17.5.0", "2.5.0" };
        private bool _demosReady;

        [MenuItem("Tools/Step Height Controller/Setup & Demos", false, 0)]
        public static void Open()
        {
            var window = GetWindow<StepHeightSetupWindow>();
            window.titleContent = new GUIContent("Step Height Setup");
            window.minSize = new Vector2(500f, 600f);
            window.Show();
        }

        public void CreateGUI()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetRoot + "Editor/StepHeightSetupWindow.uxml");
            tree.CloneTree(rootVisualElement);
            rootVisualElement.Q<Button>("refreshButton").clicked += RefreshStatus;
            rootVisualElement.Q<Button>("settingsButton").clicked += () => SettingsService.OpenProjectSettings("Project/Player");
            rootVisualElement.Q<Button>("graphicsButton").clicked += () => SettingsService.OpenProjectSettings("Project/Graphics");
            rootVisualElement.Q<Button>("qualityButton").clicked += () => SettingsService.OpenProjectSettings("Project/Quality");
            rootVisualElement.Q<Button>("configureButton").clicked += StepHeightPlayerSetupWindow.Open;
            rootVisualElement.Q<Button>("documentationButton").clicked += OpenDocumentation;
            for (int i = 0; i < PackageNames.Length; i++)
            {
                string packageName = PackageNames[i];
                rootVisualElement.Q<Button>("packageButton" + i).clicked += () => UnityEditor.PackageManager.UI.Window.Open(packageName);
            }

            BindDemo("first", "FirstPersonDemo", "FirstPersonPlayer");
            BindDemo("third", "ThirdPersonDemo", "ThirdPersonPlayer");
            RefreshStatus();
            // Choose the initial view once; later refreshes preserve the user's foldout choice.
            rootVisualElement.Q<Foldout>("requirementsFoldout").SetValueWithoutNotify(!_demosReady);
        }

        private void OnFocus()
        {
            if (rootVisualElement.Q<Label>("statusLabel") != null) RefreshStatus();
        }

        private void BindDemo(string prefix, string sceneName, string prefabName)
        {
            rootVisualElement.Q<Button>(prefix + "SceneButton").clicked += () => OpenDemo(sceneName);
            rootVisualElement.Q<Button>(prefix + "SelectButton").clicked += () => SelectPrefab(prefabName);
            rootVisualElement.Q<Button>(prefix + "AddButton").clicked += () => AddPrefab(prefabName);
        }

        private void RefreshStatus()
        {
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            bool packagesReady = true;
            for (int i = 0; i < PackageNames.Length; i++)
            {
                var package = packages.FirstOrDefault(item => item.name == PackageNames[i]);
                var tested = new Version(TestedVersions[i]);
                bool ready = package != null && Version.TryParse(package.version.Split('-')[0], out Version installed) && installed.Major == tested.Major && installed >= tested;
                packagesReady &= ready;
                Label status = rootVisualElement.Q<Label>("packageStatus" + i);
                status.text = package == null ? "Not installed. Tested: " + TestedVersions[i] : "Installed: " + package.version + ". Tested: " + TestedVersions[i] + (ready ? string.Empty : " — choose a compatible version");
                status.EnableInClassList("needs-action", !ready);
            }

            bool urpActive = GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");
            bool inputReady = InputSystemEnabled();
            bool readyForDemos = packagesReady && urpActive && inputReady;
            _demosReady = readyForDemos;
            rootVisualElement.Q<Label>("settingsStatus").text = "Render pipeline: " + (urpActive ? "Universal RP active" : "select a Universal RP asset in Graphics/Quality settings") + ".\nInput: " + (inputReady ? "Input System enabled" : "choose Input System (New) or Both in Player > Other Settings; restart if prompted") + ".";
            rootVisualElement.Q<Label>("statusLabel").text = readyForDemos ? "Ready to explore the demos." : "Complete the demo requirements below, then refresh.";
            bool editing = !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
            foreach (string prefix in new[] { "first", "third" })
            {
                string sceneName = prefix == "first" ? "FirstPersonDemo" : "ThirdPersonDemo";
                string prefabName = prefix == "first" ? "FirstPersonPlayer" : "ThirdPersonPlayer";
                bool sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetRoot + "Samples/Scenes/" + sceneName + ".unity") != null;
                bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(prefabName)) != null;
                rootVisualElement.Q<Button>(prefix + "SceneButton").SetEnabled(editing && readyForDemos && sceneExists);
                rootVisualElement.Q<Button>(prefix + "SelectButton").SetEnabled(prefabExists);
                rootVisualElement.Q<Button>(prefix + "AddButton").SetEnabled(editing && readyForDemos && prefabExists);
            }

            rootVisualElement.Q<Button>("configureButton").SetEnabled(editing);
            rootVisualElement.Q<Label>("demoHint").text = "Open a demo and press Play. Adding a player creates a prefab instance in your active scene; keep one active player and camera rig.";
        }

        private static bool InputSystemEnabled()
        {
#if ENABLE_INPUT_SYSTEM
            return true;
#else
            return false;
#endif
        }

        private static string PrefabPath(string name) => AssetRoot + "Samples/Prefabs/" + name + ".prefab";

        private static void OpenDemo(string name)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(AssetRoot + "Samples/Scenes/" + name + ".unity");
        }

        private static void SelectPrefab(string name)
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private static void AddPrefab(string name)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(instance, "Add demo player");
            Selection.activeGameObject = instance;
        }

        internal static void OpenDocumentation() => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(AssetRoot + "Documentation/Setup.md"));
    }
}

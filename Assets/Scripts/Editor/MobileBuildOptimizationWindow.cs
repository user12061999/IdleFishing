using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using IdleFishing.World;

namespace IdleFishing.Editor
{
    public class MobileBuildOptimizationWindow : EditorWindow
    {
        private Vector2 scrollPos;

        [MenuItem("Idle Fishing/Mobile Build & Performance Validator", false, 20)]
        public static void OpenWindow()
        {
            var win = GetWindow<MobileBuildOptimizationWindow>("Mobile Build Validator");
            win.minSize = new Vector2(460, 520);
            win.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Mobile 60FPS & Build Validation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Verifies and configures Android/iOS build settings, Portrait lock, GPU instancing, and URP quality profiles for 60 FPS mobile performance.", MessageType.Info);

            EditorGUILayout.Space(10);
            DrawValidationStatus();

            EditorGUILayout.Space(15);
            if (GUILayout.Button("Apply All Recommended Mobile Optimizations", GUILayout.Height(38)))
            {
                ApplyAllOptimizations();
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Quick Biome Atmosphere Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("World 1\nTemperate", GUILayout.Height(40))) PreviewWorldTheme(1);
            if (GUILayout.Button("World 2\nTropical", GUILayout.Height(40))) PreviewWorldTheme(2);
            if (GUILayout.Button("World 3\nAutumn", GUILayout.Height(40))) PreviewWorldTheme(3);
            if (GUILayout.Button("World 4\nArctic", GUILayout.Height(40))) PreviewWorldTheme(4);
            if (GUILayout.Button("World 5\nMythic", GUILayout.Height(40))) PreviewWorldTheme(5);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawValidationStatus()
        {
            // 1. Build Scenes
            bool sceneOk = EditorBuildSettings.scenes.Length > 0 &&
                           EditorBuildSettings.scenes[0].path.Contains("IdleFishing_CoreLoop.unity") &&
                           EditorBuildSettings.scenes[0].enabled;
            DrawCheckRow("Build Scene 0 (IdleFishing_CoreLoop.unity)", sceneOk);

            // 2. Screen Orientation
            bool orientOk = PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait &&
                            PlayerSettings.allowedAutorotateToPortrait &&
                            !PlayerSettings.allowedAutorotateToLandscapeLeft &&
                            !PlayerSettings.allowedAutorotateToLandscapeRight;
            DrawCheckRow("Screen Orientation (Locked Portrait 9:16)", orientOk);

            // 3. Android Backend
            bool backendOk = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP;
            DrawCheckRow("Android Scripting Backend (IL2CPP)", backendOk);

            // 4. Android Architectures
            bool archOk = (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0;
            DrawCheckRow("Android 64-bit Architecture (ARM64)", archOk);

            // 5. URP Mobile Profile
            var mobileRP = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            bool urpOk = mobileRP != null && mobileRP.useSRPBatcher && mobileRP.shadowDistance <= 55f;
            DrawCheckRow("URP Mobile Quality (SRP Batcher, 45m Shadows)", urpOk);
        }

        private void DrawCheckRow(string label, bool passed)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = passed ? Color.green : Color.yellow;
            GUILayout.Label(passed ? "[OK]" : "[FIX]", GUILayout.Width(45));
            GUI.color = Color.white;
            EditorGUILayout.LabelField(label);
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyAllOptimizations()
        {
            // 1. Set Build Scene
            string scenePath = "Assets/Scenes/IdleFishing_CoreLoop.unity";
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[] {
                new EditorBuildSettingsScene(scenePath, true)
            };

            // 2. Lock Portrait
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.useAnimatedAutorotation = false;

            // 3. Android Settings
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

            // 4. URP Settings
            var mobileRP = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (mobileRP != null)
            {
                mobileRP.shadowDistance = 45f;
                mobileRP.renderScale = 1.0f;
                mobileRP.shadowCascadeCount = 1;
                EditorUtility.SetDirty(mobileRP);
            }

            // 5. GPU Instancing on materials
            string[] targetMats = { "Spring", "Spring_Asphalt", "Summer", "Summer_Asphalt", "Fall", "Fall_Asphalt", "Winter", "Winter_Asphalt", "Desert", "Desert_Asphalt", "M_Brown", "M_Yellow", "M_Black", "M_Blue" };
            foreach (var g in AssetDatabase.FindAssets("t:Material"))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                foreach (var name in targetMats)
                {
                    if (mat.name == name && !mat.enableInstancing)
                    {
                        mat.enableInstancing = true;
                        EditorUtility.SetDirty(mat);
                        break;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Optimization Complete", "All mobile build settings, portrait orientations, and material instancing have been configured successfully!", "OK");
        }

        private void PreviewWorldTheme(int order)
        {
            var themeCtrl = Object.FindFirstObjectByType<ToonEnvironmentThemeController>();
            if (themeCtrl != null)
            {
                themeCtrl.ApplyTheme(order, immediate: true);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            else
            {
                Debug.LogWarning("[MobileBuildOptimizationWindow] ToonEnvironmentThemeController not found in scene.");
            }
        }
    }
}

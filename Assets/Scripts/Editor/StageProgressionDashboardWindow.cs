using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Progression;

namespace IdleFishing.Editor
{
    public class StageProgressionDashboardWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private int selectedWorldIndex = 0;
        private readonly string[] worldOptions = new string[] { "World 1 (Temperate Bay)", "World 2 (Coral Atoll)", "World 3 (Autumn Haven)", "World 4 (Glacial Abyss)", "World 5 (Desert Oasis)" };
        private readonly string[] worldIds = new string[] { "world_1", "world_2", "world_3", "world_4", "world_5" };

        [MenuItem("Window/Idle Fishing/30-Stage Progression Dashboard")]
        public static void OpenWindow()
        {
            var win = GetWindow<StageProgressionDashboardWindow>("Stage Dashboard");
            win.minSize = new Vector2(480, 560);
            win.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("30-STAGE PROGRESSION DASHBOARD", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select and test any stage across Worlds 1 to 5. Allows rapid balance testing, stage skipping, and objective debugging.", MessageType.Info);
            EditorGUILayout.Space(5);

            var spm = UnityEngine.Object.FindFirstObjectByType<StageProgressionManager>();

            // Current Runtime Info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("RUNTIME PROGRESSION STATUS", EditorStyles.boldLabel);
            if (spm != null && spm.CurrentStage != null)
            {
                var stage = spm.CurrentStage;
                EditorGUILayout.LabelField($"Active Stage: [{stage.stageNumber}] {stage.stageTitle} ({stage.worldId})", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Target Duration: {stage.targetMinutes} min | Gold Equivalent: {stage.rewardGoldEquivalentMinutes}m");

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Active Goals Progress:", EditorStyles.miniBoldLabel);
                for (int i = 0; i < spm.ActiveGoalProgress.Count; i++)
                {
                    var g = spm.ActiveGoalProgress[i];
                    string status = g.isCompleted ? "[DONE]" : $"({g.currentCount}/{g.config.targetCount})";
                    EditorGUILayout.LabelField($"  Goal {i + 1}: {g.config.GetDefaultDescription()} - {status}");
                }
            }
            else
            {
                int savedStage = PlayerPrefs.GetInt("IdleFishing_CurrentStageNumber", 1);
                EditorGUILayout.LabelField($"Editor / Saved State: Stage {savedStage}");
                if (!EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox("Enter Play Mode to view live goal tracking and transition effects.", MessageType.None);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Controls & Cheats
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("QUICK DEBUG CHEATS", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("AutoComplete Stage", GUILayout.Height(28)))
            {
                AutoCompleteCurrentStage(spm);
            }
            if (GUILayout.Button("+5,000 Coins", GUILayout.Height(28)))
            {
                if (EconomyManager.Instance != null) EconomyManager.Instance.AddCoins(5000);
            }
            if (GUILayout.Button("Reset To Stage 1", GUILayout.Height(28)))
            {
                ResetToStage1(spm);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Register All Stages In Scene Asset", GUILayout.Height(26)))
            {
                RegisterAllStagesInScene(spm);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // World Selector
            EditorGUILayout.LabelField("WORLD STAGE SELECTOR", EditorStyles.boldLabel);
            selectedWorldIndex = EditorGUILayout.Popup("Select World:", selectedWorldIndex, worldOptions);
            string selectedWorldId = worldIds[Mathf.Clamp(selectedWorldIndex, 0, worldIds.Length - 1)];

            EditorGUILayout.Space(5);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Find all stages for this world
            var stageAssets = LoadStagesForWorld(selectedWorldId);
            if (stageAssets.Count == 0)
            {
                EditorGUILayout.HelpBox($"No StageData assets found for {selectedWorldId}.", MessageType.Warning);
            }
            else
            {
                foreach (var stage in stageAssets)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"Stage {stage.stageNumber}: {stage.stageTitle}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Goals: {stage.goals.Count} | Est. Income: {stage.estimatedBaseGoldPerMin} G/min | Duration: {stage.targetMinutes}m", EditorStyles.miniLabel);
                    if (stage.isWorldBossStage)
                    {
                        EditorGUILayout.LabelField("<color=#FFD700>★ APEX WORLD BOSS STAGE ★</color>", new GUIStyle(EditorStyles.miniBoldLabel) { richText = true });
                    }
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Jump Here", GUILayout.Width(95), GUILayout.Height(36)))
                    {
                        JumpToStage(stage.stageNumber, spm);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private List<StageData> LoadStagesForWorld(string worldId)
        {
            var list = new List<StageData>();
            string[] guids = AssetDatabase.FindAssets("t:StageData");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stage != null && stage.worldId == worldId)
                {
                    list.Add(stage);
                }
            }
            list.Sort((a, b) => a.stageNumber.CompareTo(b.stageNumber));
            return list;
        }

        private void JumpToStage(int stageNumber, StageProgressionManager spm)
        {
            PlayerPrefs.SetInt("IdleFishing_CurrentStageNumber", stageNumber);
            PlayerPrefs.Save();

            if (EditorApplication.isPlaying && spm != null)
            {
                spm.ActivateStage(stageNumber);
                Debug.Log($"[StageProgressionDashboard] Jumped to Stage {stageNumber}!");
            }
            else
            {
                Debug.Log($"[StageProgressionDashboard] Saved start stage as Stage {stageNumber}. Enter Play Mode to begin here.");
            }
        }

        private void AutoCompleteCurrentStage(StageProgressionManager spm)
        {
            if (spm == null || spm.CurrentStage == null)
            {
                Debug.LogWarning("[StageProgressionDashboard] Cannot autocomplete: SPM not active in Play Mode.");
                return;
            }

            for (int i = 0; i < spm.ActiveGoalProgress.Count; i++)
            {
                var g = spm.ActiveGoalProgress[i];
                g.currentCount = g.config.targetCount;
                GameEvents.TriggerStageGoalProgressChanged(spm.CurrentStage, i, g.currentCount, g.config.targetCount);
            }
        }

        private void ResetToStage1(StageProgressionManager spm)
        {
            PlayerPrefs.SetInt("IdleFishing_CurrentStageNumber", 1);
            PlayerPrefs.Save();

            if (EditorApplication.isPlaying && spm != null)
            {
                spm.ActivateStage(1);
            }
            Debug.Log("[StageProgressionDashboard] Reset progression to Stage 1.");
        }

        public static void RegisterAllStagesInScene(StageProgressionManager spm)
        {
            if (spm == null)
            {
                spm = UnityEngine.Object.FindFirstObjectByType<StageProgressionManager>();
            }
            if (spm == null)
            {
                Debug.LogError("[StageProgressionDashboard] StageProgressionManager not found in active scene!");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:StageData");
            var allStages = new List<StageData>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stage != null) allStages.Add(stage);
            }
            allStages.Sort((a, b) => a.stageNumber.CompareTo(b.stageNumber));

            var so = new SerializedObject(spm);
            var stagesProp = so.FindProperty("allStages");
            stagesProp.ClearArray();
            for (int i = 0; i < allStages.Count; i++)
            {
                stagesProp.InsertArrayElementAtIndex(i);
                stagesProp.GetArrayElementAtIndex(i).objectReferenceValue = allStages[i];
            }
            so.ApplyModifiedProperties();

            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            Debug.Log($"[StageProgressionDashboard] Successfully registered {allStages.Count} StageData assets in StageProgressionManager and saved scene!");
        }
    }
}

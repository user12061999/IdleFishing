using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Fishing;
using IdleFishing.Logistics;
using IdleFishing.Market;
using IdleFishing.UI;

namespace IdleFishing.Progression
{
    public class StageProgressionManager : MonoBehaviour
    {
        public static StageProgressionManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private List<StageData> allStages = new List<StageData>();
        [SerializeField] private bool autoAdvanceStage = true;
        [SerializeField] private float advanceDelaySeconds = 2.5f;

        [Header("Runtime State")]
        [SerializeField] private int currentStageNumber = 1; // 1-indexed (1 to 30)
        [SerializeField] private StageData currentStage;
        [SerializeField] private List<StageGoalProgress> activeGoalProgress = new List<StageGoalProgress>();
        [SerializeField] private bool isStageTransitioning = false;

        public int CurrentStageNumber => currentStageNumber;
        public StageData CurrentStage => currentStage;
        public IReadOnlyList<StageGoalProgress> ActiveGoalProgress => activeGoalProgress;
        public bool IsCurrentStageComplete
        {
            get
            {
                if (activeGoalProgress == null || activeGoalProgress.Count == 0) return false;
                for (int i = 0; i < activeGoalProgress.Count; i++)
                {
                    if (!activeGoalProgress[i].isCompleted) return false;
                }
                return true;
            }
        }

        private const string PREF_CURRENT_STAGE = "IdleFishing_CurrentStageNumber";
        private const string PREF_STAGE_PREFIX = "IdleFishing_Stage_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSavedStageProgress();
        }

        private void Start()
        {
            StartCoroutine(DeferredInitRoutine());
        }

        private IEnumerator DeferredInitRoutine()
        {
            // Give other managers a frame to awake and initialize
            yield return null;

            if (allStages == null || allStages.Count == 0)
            {
                LoadStagesFromResourcesOrDatabase();
            }

            SortStages();
            ActivateStage(currentStageNumber);
        }

        private void OnEnable()
        {
            GameEvents.FishCaught += OnFishCaught;
            GameEvents.FishDefeated += OnFishDefeated;
            GameEvents.BossFishDefeated += OnBossFishDefeated;
            GameEvents.ItemSold += OnItemSold;
            GameEvents.WorkerHired += OnWorkerHired;
            GameEvents.PayloadTransported += OnPayloadTransported;
            GameEvents.ContractCompleted += OnContractCompleted;
            GameEvents.FrenzyStarted += OnFrenzyStarted;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.WorldChanged += OnWorldChanged;
        }

        private void OnDisable()
        {
            GameEvents.FishCaught -= OnFishCaught;
            GameEvents.FishDefeated -= OnFishDefeated;
            GameEvents.BossFishDefeated -= OnBossFishDefeated;
            GameEvents.ItemSold -= OnItemSold;
            GameEvents.WorkerHired -= OnWorkerHired;
            GameEvents.PayloadTransported -= OnPayloadTransported;
            GameEvents.ContractCompleted -= OnContractCompleted;
            GameEvents.FrenzyStarted -= OnFrenzyStarted;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.WorldChanged -= OnWorldChanged;
        }

        private void SortStages()
        {
            allStages.Sort((a, b) => a != null && b != null ? a.stageNumber.CompareTo(b.stageNumber) : 0);
        }

        public void RegisterStage(StageData stage)
        {
            if (stage != null && !allStages.Contains(stage))
            {
                allStages.Add(stage);
                SortStages();
            }
        }

        public void ActivateStage(int stageNum)
        {
            currentStageNumber = Mathf.Max(1, stageNum);
            currentStage = allStages.Find(s => s != null && s.stageNumber == currentStageNumber);

            activeGoalProgress.Clear();
            isStageTransitioning = false;

            if (currentStage == null)
            {
                Debug.LogWarning($"[StageProgressionManager] Stage {currentStageNumber} not found in configuration list.");
                return;
            }

            Debug.Log($"[StageProgressionManager] === Starting Stage {currentStage.stageNumber}: {currentStage.stageTitle} ({currentStage.worldId}) ===");

            // Initialize progress tracking
            for (int i = 0; i < currentStage.goals.Count; i++)
            {
                var goal = currentStage.goals[i];
                var prog = new StageGoalProgress(goal);
                activeGoalProgress.Add(prog);
            }

            // Softlock Protection: Check immediate state for existing upgrades or workers
            CheckInitialGoalStates();

            // Ensure world matches active stage
            if (WorldManager.Instance != null && !string.IsNullOrEmpty(currentStage.worldId))
            {
                for (int w = 0; w < WorldManager.Instance.Worlds.Count; w++)
                {
                    if (WorldManager.Instance.Worlds[w] != null && WorldManager.Instance.Worlds[w].worldId == currentStage.worldId)
                    {
                        if (WorldManager.Instance.ActiveWorldIndex != w)
                        {
                            WorldManager.Instance.SwitchToWorld(w);
                        }
                        break;
                    }
                }
            }

            // Pity Directing: Enqueue deterministic catches for this stage
            ApplyDeterministicStageDirectives();

            // Notify UI & Systems
            GameEvents.TriggerStageStarted(currentStage);

            // Re-evaluate in case initial states fulfilled all goals immediately
            CheckStageCompletion();
        }

        private void CheckInitialGoalStates()
        {
            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.HireWorker)
                {
                    string role = prog.config.targetId.ToLower();
                    if (role.Contains("wrangler") && UpgradeManager.Instance != null)
                    {
                        int currentWranglers = UpgradeManager.Instance.GetUnlockedWranglerCount();
                        prog.currentCount = Mathf.Min(currentWranglers, prog.config.targetCount);
                    }
                    else if (role.Contains("transporter") && LogisticsManager.Instance != null)
                    {
                        int currentTransporters = LogisticsManager.Instance.WorkerCount;
                        prog.currentCount = Mathf.Min(currentTransporters, prog.config.targetCount);
                    }
                }
                else if (prog.config.goalType == StageGoalType.UpgradeReached && UpgradeManager.Instance != null)
                {
                    // Check if upgrade level already satisfies requirement
                    UpgradeType upType = UpgradeType.FishermanTeam;
                    if (Enum.TryParse<UpgradeType>(prog.config.targetId, true, out var parsedType))
                    {
                        upType = parsedType;
                    }
                    int lvl = UpgradeManager.Instance.GetLevel(upType);
                    if (lvl >= prog.config.targetLevel)
                    {
                        prog.currentCount = prog.config.targetCount;
                    }
                }
            }
        }

        private void ApplyDeterministicStageDirectives()
        {
            if (FishingSpot.Instance == null) return;

            // Clear legacy pity queue
            FishingSpot.Instance.ClearGuaranteedCatches();

            // Enqueue guaranteed stage species
            if (currentStage.guaranteedFirstCatch != null)
            {
                FishingSpot.Instance.QueueGuaranteedCatch(currentStage.guaranteedFirstCatch, false);
                Debug.Log($"[StageProgressionManager] Queued guaranteed catch: {currentStage.guaranteedFirstCatch.SpeciesName}");
            }

            // Enqueue guaranteed boss if boss stage
            if (currentStage.bossToSpawn != null)
            {
                FishingSpot.Instance.QueueGuaranteedCatch(currentStage.bossToSpawn, true);
                Debug.Log($"[StageProgressionManager] Queued guaranteed boss: {currentStage.bossToSpawn.SpeciesName}");
            }
        }

        #region Event Handlers
        private void OnFishCaught(ItemData item)
        {
            if (currentStage == null || isStageTransitioning) return;

            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.CatchFish)
                {
                    bool match = string.IsNullOrEmpty(prog.config.targetId) ||
                                 (item != null && item.ItemId.IndexOf(prog.config.targetId, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (item != null && item.ItemName.IndexOf(prog.config.targetId, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (match)
                    {
                        IncrementGoal(i, 1);
                    }
                }
            }
        }

        private void OnFishDefeated(FishChainData species, LiveFish liveFish)
        {
            if (currentStage == null || isStageTransitioning) return;

            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.DefeatFish)
                {
                    bool match = string.IsNullOrEmpty(prog.config.targetId) ||
                                 (species != null && species.SpeciesId.IndexOf(prog.config.targetId, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (species != null && species.SpeciesName.IndexOf(prog.config.targetId, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (match)
                    {
                        IncrementGoal(i, 1);
                    }
                }
            }
        }

        private void OnBossFishDefeated(FishChainData species, int bonusCoins)
        {
            if (currentStage == null || isStageTransitioning) return;

            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.DefeatBoss)
                {
                    bool match = string.IsNullOrEmpty(prog.config.targetId) ||
                                 (species != null && species.SpeciesId.IndexOf(prog.config.targetId, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 (species != null && species.SpeciesName.IndexOf(prog.config.targetId, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 prog.config.targetId.Contains("shark"); // World 1 Apex match

                    if (match)
                    {
                        IncrementGoal(i, 1);
                    }
                }
            }
        }

        private void OnItemSold(ItemData item, int price)
        {
            if (currentStage == null || isStageTransitioning) return;

            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.SellItem)
                {
                    IncrementGoal(i, 1);
                }
            }
        }

        private void OnWorkerHired(string role, int count)
        {
            if (currentStage == null || isStageTransitioning) return;

            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.HireWorker)
                {
                    if (string.IsNullOrEmpty(prog.config.targetId) ||
                        prog.config.targetId.Equals(role, StringComparison.OrdinalIgnoreCase))
                    {
                        IncrementGoal(i, 1);
                    }
                }
            }
        }

        private void OnPayloadTransported(int count)
        {
            if (currentStage == null || isStageTransitioning) return;

            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.TransportPayload)
                {
                    IncrementGoal(i, count);
                }
            }
        }

        private void OnContractCompleted(HarborContract contract)
        {
            if (currentStage == null || isStageTransitioning) return;

            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.CompleteOrder)
                {
                    IncrementGoal(i, 1);
                }
            }
        }

        private void OnFrenzyStarted()
        {
            if (currentStage == null || isStageTransitioning) return;

            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.ActivateFrenzy)
                {
                    IncrementGoal(i, 1);
                }
            }
        }

        private void OnUpgradePurchased(UpgradeType type, int newLevel)
        {
            if (currentStage == null || isStageTransitioning) return;

            for (int i = 0; i < activeGoalProgress.Count; i++)
            {
                var prog = activeGoalProgress[i];
                if (prog.config.goalType == StageGoalType.UpgradeReached)
                {
                    if (string.IsNullOrEmpty(prog.config.targetId) ||
                        prog.config.targetId.Equals(type.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        if (newLevel >= prog.config.targetLevel)
                        {
                            prog.currentCount = prog.config.targetCount;
                            GameEvents.TriggerStageGoalProgressChanged(currentStage, i, prog.currentCount, prog.config.targetCount);
                            CheckStageCompletion();
                        }
                    }
                }
            }
        }

        private void OnWorldChanged(WorldData newWorld)
        {
            if (newWorld == null) return;
            // If active stage does not belong to the newly entered world, switch to the world's starting stage
            if (currentStage == null || currentStage.worldId != newWorld.worldId)
            {
                var worldStage = allStages.Find(s => s != null && s.worldId == newWorld.worldId && s.stageInWorld == 1);
                if (worldStage != null)
                {
                    ActivateStage(worldStage.stageNumber);
                }
            }
        }
        #endregion

        private void IncrementGoal(int goalIndex, int delta)
        {
            if (goalIndex < 0 || goalIndex >= activeGoalProgress.Count) return;

            var prog = activeGoalProgress[goalIndex];
            if (prog.isCompleted) return;

            prog.currentCount = Mathf.Min(prog.currentCount + delta, prog.config.targetCount);
            GameEvents.TriggerStageGoalProgressChanged(currentStage, goalIndex, prog.currentCount, prog.config.targetCount);

            CheckStageCompletion();
        }

        private void CheckStageCompletion()
        {
            if (isStageTransitioning || !IsCurrentStageComplete) return;

            StartCoroutine(CompleteStageRoutine());
        }

        private IEnumerator CompleteStageRoutine()
        {
            isStageTransitioning = true;
            Debug.Log($"[StageProgressionManager] *** ALL GOALS SATISFIED for Stage {currentStage.stageNumber}: {currentStage.stageTitle}! ***");

            // 1. Save completion state
            string stageKey = PREF_STAGE_PREFIX + currentStage.stageId + "_Completed";
            bool isFirstClear = PlayerPrefs.GetInt(stageKey, 0) == 0;
            PlayerPrefs.SetInt(stageKey, 1);
            PlayerPrefs.Save();

            // 2. Disburse Rewards
            int goldReward = currentStage.CalculateGoldReward();
            if (EconomyManager.Instance != null && goldReward > 0)
            {
                EconomyManager.Instance.AddCoins(goldReward);
                Debug.Log($"[StageProgressionManager] Disbursed Gold Reward: +{goldReward} G");
            }

            // 3. Grant Harbor Fame (First clear)
            if (isFirstClear && currentStage.grantFameOnFirstClear > 0)
            {
                if (ContractManager.Instance != null)
                {
                    ContractManager.Instance.AddFame(currentStage.grantFameOnFirstClear);
                }
                else
                {
                    int currentFame = PlayerPrefs.GetInt("IdleFishing_HarborFame", 0) + currentStage.grantFameOnFirstClear;
                    PlayerPrefs.SetInt("IdleFishing_HarborFame", currentFame);
                    PlayerPrefs.Save();
                    GameEvents.TriggerHarborFameChanged(currentFame, 1);
                }
                Debug.Log($"[StageProgressionManager] Granted Harbor Fame: +{currentStage.grantFameOnFirstClear}");
            }

            // 4. Special stage handling (Stage 01 free angler upgrade)
            if (currentStage.stageNumber == 1 && isFirstClear && UpgradeManager.Instance != null)
            {
                // Level up fisherman team once for free
                int curLvl = UpgradeManager.Instance.GetLevel(UpgradeType.FishermanTeam);
                if (curLvl == 0)
                {
                    PlayerPrefs.SetInt("IdleFishing_Upgrade_" + UpgradeType.FishermanTeam.ToString(), 1);
                    PlayerPrefs.SetInt("IdleFishing_Upgrade_world_1_" + UpgradeType.FishermanTeam.ToString(), 1);
                    PlayerPrefs.Save();
                    GameEvents.TriggerUpgradePurchased(UpgradeType.FishermanTeam, 1);
                }
            }

            // 5. Fire Global Event
            GameEvents.TriggerStageCompleted(currentStage);

            // 6. World Voyage Check
            if (currentStage.isWorldBossStage && !string.IsNullOrEmpty(currentStage.worldVoyageUnlock))
            {
                string nextWorldId = currentStage.worldVoyageUnlock;
                PlayerPrefs.SetInt($"IdleFishing_World_{nextWorldId}_Unlocked", 1);
                PlayerPrefs.Save();
                Debug.Log($"[StageProgressionManager] Apex World Boss Cleared! World Voyage to {nextWorldId} is now UNLOCKED.");

                if (WorldManager.Instance != null && WorldManager.Instance.Worlds != null)
                {
                    WorldData unlockedWorld = null;
                    for (int w = 0; w < WorldManager.Instance.Worlds.Count; w++)
                    {
                        if (WorldManager.Instance.Worlds[w] != null && WorldManager.Instance.Worlds[w].worldId == nextWorldId)
                        {
                            unlockedWorld = WorldManager.Instance.Worlds[w];
                            break;
                        }
                    }
                    if (unlockedWorld != null)
                    {
                        GameEvents.TriggerWorldUnlocked(unlockedWorld);
                    }
                }
            }

            // 7. Show Stage Clear Celebration Modal
            if (StageClearPopupUI.Instance != null)
            {
                StageClearPopupUI.Instance.Show(currentStage, isFirstClear, () =>
                {
                    AdvanceToNextStage();
                });
            }
            else if (autoAdvanceStage)
            {
                yield return new WaitForSeconds(advanceDelaySeconds);
                AdvanceToNextStage();
            }
        }

        public void AdvanceToNextStage()
        {
            int nextStageNumber = currentStageNumber + 1;
            PlayerPrefs.SetInt(PREF_CURRENT_STAGE, nextStageNumber);
            PlayerPrefs.Save();

            ActivateStage(nextStageNumber);
        }

        private void LoadSavedStageProgress()
        {
            currentStageNumber = PlayerPrefs.GetInt(PREF_CURRENT_STAGE, 1);
        }

        public void ResetAllStageProgress()
        {
            PlayerPrefs.DeleteKey(PREF_CURRENT_STAGE);
            if (allStages != null)
            {
                for (int i = 0; i < allStages.Count; i++)
                {
                    if (allStages[i] != null)
                    {
                        PlayerPrefs.DeleteKey(PREF_STAGE_PREFIX + allStages[i].stageId + "_Completed");
                    }
                }
            }
            PlayerPrefs.Save();
            ActivateStage(1);
        }

        private void LoadStagesFromResourcesOrDatabase()
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StageData");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                var stageAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stageAsset != null && !allStages.Contains(stageAsset))
                {
                    allStages.Add(stageAsset);
                }
            }
#endif
            var resourcesStages = Resources.LoadAll<StageData>("Stages");
            if (resourcesStages != null)
            {
                for (int i = 0; i < resourcesStages.Length; i++)
                {
                    if (resourcesStages[i] != null && !allStages.Contains(resourcesStages[i]))
                    {
                        allStages.Add(resourcesStages[i]);
                    }
                }
            }
        }
    }
}

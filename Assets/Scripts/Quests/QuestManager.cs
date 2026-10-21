using System;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.UI;
using IdleFishing.Audio;
using IdleFishing.Market;

namespace IdleFishing.Quests
{
    /// <summary>
    /// Quản lý Hệ Thống Nhiệm Vụ Tân Thủ & Mốc Thưởng (FTUE & Quest Milestones):
    /// - Dẫn dắt người chơi từ Level 1 đến Level 5 theo từng mục tiêu rõ ràng (câu cá, nâng cấp, mở rộng cần, Frenzy, hợp đồng, diệt Boss, chuyển bến cảng).
    /// - Sau chuỗi nhiệm vụ cốt truyện, tự động sinh nhiệm vụ tuần hoàn vô tận (Infinite Milestones) để người chơi luôn có mục tiêu và dòng tiền thưởng.
    /// - Đồng bộ trực tiếp với GameEvents & EconomyManager.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        private static QuestManager _instance;
        public static QuestManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<QuestManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("--- QUEST MANAGER ---");
                        _instance = go.AddComponent<QuestManager>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            var inst = Instance;
        }

        private const string PREF_QUEST_INDEX = "IdleFishing_CurrentQuestIndex";
        private const string PREF_QUEST_PROGRESS = "IdleFishing_CurrentQuestProgress";
        private const string PREF_QUEST_CLAIMABLE = "IdleFishing_CurrentQuestClaimable";

        [Header("State")]
        [SerializeField] private int currentQuestIndex = 0;
        [SerializeField] private int currentProgress = 0;
        [SerializeField] private bool isClaimable = false;

        private readonly List<QuestStep> scriptedQuests = new List<QuestStep>();
        private QuestStep activeQuest = null;

        public QuestStep CurrentQuest => activeQuest;
        public int CurrentProgress => currentProgress;
        public bool IsClaimable => isClaimable;
        public int CurrentQuestIndex => currentQuestIndex;

        public event Action<QuestStep, int, int> OnQuestProgressUpdated;
        public event Action<QuestStep> OnQuestCompleted;
        public event Action<QuestStep, int> OnQuestClaimed;
        public event Action<QuestStep> OnActiveQuestChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            InitializeScriptedQuests();
            LoadState();
        }

        private void OnEnable()
        {
            GameEvents.FishCaught += HandleFishCaught;
            GameEvents.UpgradePurchased += HandleUpgradePurchased;
            GameEvents.ItemSold += HandleItemSold;
            GameEvents.FrenzyStarted += HandleFrenzyStarted;
            GameEvents.BossFishDefeated += HandleBossDefeated;
            GameEvents.ContractCompleted += HandleContractCompleted;
            GameEvents.WorldUnlocked += HandleWorldUnlocked;
            GameEvents.WorldChanged += HandleWorldChanged;
        }

        private void OnDisable()
        {
            GameEvents.FishCaught -= HandleFishCaught;
            GameEvents.UpgradePurchased -= HandleUpgradePurchased;
            GameEvents.ItemSold -= HandleItemSold;
            GameEvents.FrenzyStarted -= HandleFrenzyStarted;
            GameEvents.BossFishDefeated -= HandleBossDefeated;
            GameEvents.ContractCompleted -= HandleContractCompleted;
            GameEvents.WorldUnlocked -= HandleWorldUnlocked;
            GameEvents.WorldChanged -= HandleWorldChanged;
        }

        private void Start()
        {
            EnsureWidgetExists();
            RefreshActiveQuest();
        }

        private void EnsureWidgetExists()
        {
            if (QuestHUDWidgetUI.Instance == null)
            {
                Canvas mainCanvas = null;
                if (GameHUD.Instance != null)
                {
                    mainCanvas = GameHUD.Instance.GetComponentInParent<Canvas>();
                }

                if (mainCanvas == null)
                {
                    var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    for (int i = 0; i < canvases.Length; i++)
                    {
                        string cName = canvases[i].name;
                        if (cName.Contains("UI_Canvas") || cName.Contains("Main"))
                        {
                            mainCanvas = canvases[i];
                            break;
                        }
                    }
                }

                if (mainCanvas != null)
                {
                    GameObject widgetObj = new GameObject("QuestHUDWidgetUI");
                    widgetObj.transform.SetParent(mainCanvas.transform, false);
                    widgetObj.AddComponent<QuestHUDWidgetUI>();
                }
            }
        }

        private void InitializeScriptedQuests()
        {
            scriptedQuests.Clear();

            // ==================== WORLD 1: TEMPERATE BAY (KHỞI NGHIỆP CẢNG BIỂN) ====================
            scriptedQuests.Add(new QuestStep(
                "w1_catch_2",
                "FIRST HARBOR CATCH",
                "Catch 2 fresh fish from the pier",
                "[FISH]",
                QuestType.CatchFish,
                2,
                60,
                "Tap flopping fish on the pier to assist wranglers!"));

            scriptedQuests.Add(new QuestStep(
                "w1_upg_fishermen",
                "ANGLER CREW TRAINING",
                "Upgrade Fisherman Crew to Level 1",
                "[UPG]",
                QuestType.UpgradePillarLevel,
                1,
                80,
                "Open the bottom menu to upgrade your fishing crew speed!",
                UpgradeType.FishermanTeam,
                1));

            scriptedQuests.Add(new QuestStep(
                "w1_upg_wranglers",
                "DOCK WRANGLERS",
                "Upgrade Wrangler Crew to Level 1",
                "[UPG]",
                QuestType.UpgradePillarLevel,
                1,
                100,
                "Wranglers knock out fish and haul them to market!",
                UpgradeType.WranglerTeam,
                1));

            scriptedQuests.Add(new QuestStep(
                "w1_rod_2",
                "PIER EXPANSION",
                "Reach Fisherman Crew Level 2 to unlock 2nd Fishing Rod!",
                "[ROD]",
                QuestType.UpgradePillarLevel,
                2,
                150,
                "Level 2 grants 2 fishing rods working at the same time!",
                UpgradeType.FishermanTeam,
                2));

            scriptedQuests.Add(new QuestStep(
                "w1_earn_gold_300",
                "HARBOR COMMERCE",
                "Sell seafood to earn 300 Gold",
                "[COIN]",
                QuestType.EarnCoins,
                300,
                150,
                "Customers buy seafood continuously at the market stand."));

            scriptedQuests.Add(new QuestStep(
                "w1_ignite_frenzy",
                "FEEDING FRENZY",
                "Fill the Frenzy meter to ignite Fishing Frenzy!",
                "[FRENZY]",
                QuestType.TriggerFrenzy,
                1,
                250,
                "Catching fish and active tapping charges the orange Frenzy bar!"));

            scriptedQuests.Add(new QuestStep(
                "w1_catch_6",
                "BOUNTIFUL HARVEST",
                "Catch 6 ocean fish from the pier",
                "[FISH]",
                QuestType.CatchFish,
                6,
                350,
                "Keep reeling in catches to grow your harbor treasury!"));

            scriptedQuests.Add(new QuestStep(
                "w1_defeat_boss",
                "DEEP SEA LEVIATHAN",
                "Defeat a giant Apex Boss fish with your wranglers!",
                "[BOSS]",
                QuestType.DefeatBoss,
                1,
                450,
                "Tap rapidly on Boss fish to deal high damage!"));

            scriptedQuests.Add(new QuestStep(
                "w1_upgrades_10",
                "PROSPEROUS HARBOR",
                "Reach 10 total upgrade levels across all dock pillars",
                "[UPG]",
                QuestType.PurchaseUpgrade,
                10,
                600,
                "Balanced upgrades prepare your harbor for the maiden voyage!"));

            scriptedQuests.Add(new QuestStep(
                "w1_voyage_world2",
                "SET SAIL TO LEVEL 2",
                "Earn 1,500 Gold and Set Sail to Tropical Coral Atoll!",
                "[SAIL]",
                QuestType.UnlockWorld,
                1,
                1000,
                "Tap the ship waiting offshore or the Level badge when ready!",
                targetWorldOrder: 2));

            // ==================== WORLD 2: TROPICAL CORAL ATOLL (VƯƠNG QUỐC SAN HÔ) ====================
            scriptedQuests.Add(new QuestStep(
                "w2_catch_8",
                "CORAL REEF BOUNTY",
                "Catch 8 tropical fish in Coral Atoll",
                "[FISH]",
                QuestType.CatchFish,
                8,
                500,
                "Exotic reef species sell for much higher gold!"));

            scriptedQuests.Add(new QuestStep(
                "w2_wrangler_3",
                "EXPANDED WRANGLERS",
                "Upgrade Wrangler Crew to Level 3 (Recruit 2nd Wrangler)",
                "[UPG]",
                QuestType.UpgradePillarLevel,
                3,
                750,
                "A 2nd dock wrangler speeds up hauling during frenzy!",
                UpgradeType.WranglerTeam,
                3));

            scriptedQuests.Add(new QuestStep(
                "w2_frenzy_2",
                "DOUBLE FRENZY",
                "Trigger Fishing Frenzy 2 times",
                "[FRENZY]",
                QuestType.TriggerFrenzy,
                2,
                900,
                "Keep reeling in catches to build momentum!"));

            scriptedQuests.Add(new QuestStep(
                "w2_catch_15",
                "EXOTIC REEF HAUL",
                "Catch 15 tropical fish in Coral Atoll",
                "[FISH]",
                QuestType.CatchFish,
                15,
                1200,
                "Exotic reef species sell for massive gold!"));

            scriptedQuests.Add(new QuestStep(
                "w2_voyage_world3",
                "SET SAIL TO LEVEL 3",
                "Earn 6,000 Gold and Set Sail to Golden Autumn Cove!",
                "[SAIL]",
                QuestType.UnlockWorld,
                1,
                2500,
                "Autumn Cove features the famous Smoker BBQ grill!",
                targetWorldOrder: 3));

            // ==================== WORLD 3: GOLDEN AUTUMN COVE (VỊNH RỪNG PHONG) ====================
            scriptedQuests.Add(new QuestStep(
                "w3_catch_12",
                "SCARLET MAPLE RUN",
                "Catch 12 autumn fish and lobsters",
                "[FISH]",
                QuestType.CatchFish,
                12,
                1500,
                "Autumn lobsters and grouper fetch top tier prices!"));

            scriptedQuests.Add(new QuestStep(
                "w3_rod_4",
                "PIER ARMADA",
                "Upgrade Fisherman Crew to Level 9 (Unlock 4th Fishing Rod)",
                "[ROD]",
                QuestType.UpgradePillarLevel,
                9,
                2500,
                "4 rods pulling simultaneously supercharges your economy!",
                UpgradeType.FishermanTeam,
                9));

            scriptedQuests.Add(new QuestStep(
                "w3_voyage_world4",
                "THE FROZEN NORTH",
                "Earn 25,000 Gold and Set Sail to Arctic Frost Haven (Level 4)!",
                "[SAIL]",
                QuestType.UnlockWorld,
                1,
                6000,
                "Equip your fleet for sub-zero icy fishing grounds!",
                targetWorldOrder: 4));

            // ==================== WORLD 4: ARCTIC FROST HAVEN (VỊNH BĂNG BẮC CỰC) ====================
            scriptedQuests.Add(new QuestStep(
                "w4_catch_15",
                "SUB-ZERO BOUNTY",
                "Catch 15 Arctic cod and prize tuna",
                "[FISH]",
                QuestType.CatchFish,
                15,
                4000,
                "Arctic catches grant immense wholesale returns!"));

            scriptedQuests.Add(new QuestStep(
                "w4_voyage_world5",
                "DESERT CARAVAN",
                "Earn 80,000 Gold and Set Sail to Sunken Oasis Desert (Level 5)!",
                "[SAIL]",
                QuestType.UnlockWorld,
                1,
                15000,
                "The ultimate oasis awaits legendary anglers!",
                targetWorldOrder: 5));

            // ==================== WORLD 5: SUNKEN OASIS DESERT (ỐC ĐẢO SA MẠC) ====================
            scriptedQuests.Add(new QuestStep(
                "w5_boss_leviathan",
                "PHARAOH LEVIATHAN",
                "Defeat the ancient Sunken Oasis Leviathan Boss!",
                "[BOSS]",
                QuestType.DefeatBoss,
                1,
                25000,
                "The king of the oasis drops a treasure chest of gold!"));
        }

        private void LoadState()
        {
            currentQuestIndex = PlayerPrefs.GetInt(PREF_QUEST_INDEX, 0);
            currentProgress = PlayerPrefs.GetInt(PREF_QUEST_PROGRESS, 0);
            isClaimable = PlayerPrefs.GetInt(PREF_QUEST_CLAIMABLE, 0) == 1;
        }

        private void SaveState()
        {
            PlayerPrefs.SetInt(PREF_QUEST_INDEX, currentQuestIndex);
            PlayerPrefs.SetInt(PREF_QUEST_PROGRESS, currentProgress);
            PlayerPrefs.SetInt(PREF_QUEST_CLAIMABLE, isClaimable ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void RefreshActiveQuest()
        {
            if (currentQuestIndex < scriptedQuests.Count)
            {
                activeQuest = scriptedQuests[currentQuestIndex];
            }
            else
            {
                // Chuỗi nhiệm vụ vô tận tuần hoàn (Infinite Procedural Milestones)
                activeQuest = GenerateProceduralQuest(currentQuestIndex);
            }

            // Kiểm tra ngay các điều kiện đã hoàn thành trước đó (Retroactive check)
            EvaluateRetroactiveProgress();

            SaveState();
            OnActiveQuestChanged?.Invoke(activeQuest);
            GameEvents.TriggerActiveQuestChanged(activeQuest);
            TriggerProgressUpdate();
        }

        private QuestStep GenerateProceduralQuest(int index)
        {
            int cycle = index - scriptedQuests.Count;
            int typePick = cycle % 5;
            int rewardScale = 800 + (cycle * 250);

            switch (typePick)
            {
                case 0:
                    int fishTarget = 10 + (cycle * 2);
                    return new QuestStep($"proc_catch_{index}", "GRAND SEA HARVEST", $"Catch {fishTarget} ocean fish from the pier", "[FISH]", QuestType.CatchFish, fishTarget, rewardScale, "Keep the pier active to haul in big rewards!");
                case 1:
                    int goldTarget = 1500 + (cycle * 500);
                    return new QuestStep($"proc_gold_{index}", "HARBOR PROFITS", $"Earn {NumberFormatter.Format(goldTarget)} Gold from market sales", "[COIN]", QuestType.EarnCoins, goldTarget, rewardScale + 200, "Sell fresh seafood at the market stand!");
                case 2:
                    return new QuestStep($"proc_frenzy_{index}", "FRENZY SURGE", "Ignite Fishing Frenzy 2 times", "[FRENZY]", QuestType.TriggerFrenzy, 2, rewardScale + 150, "Fill the frenzy meter to ignite turbo fishing!");
                case 3:
                    return new QuestStep($"proc_upg_{index}", "HARBOR EXPANSION", "Purchase 5 pier upgrades", "[UPG]", QuestType.PurchaseUpgrade, 5, rewardScale + 300, "Upgrade your fishermen, wranglers, or fish value!");
                default:
                    return new QuestStep($"proc_boss_{index}", "APEX HUNT", "Defeat an Apex Boss predator", "[BOSS]", QuestType.DefeatBoss, 1, rewardScale + 400, "Team up with wranglers to conquer sea monsters!");
            }
        }

        private void EvaluateRetroactiveProgress()
        {
            if (activeQuest == null) return;

            if (activeQuest.type == QuestType.UpgradePillarLevel)
            {
                if (UpgradeManager.Instance != null)
                {
                    int lvl = UpgradeManager.Instance.GetLevel(activeQuest.targetPillar);
                    currentProgress = Mathf.Max(currentProgress, lvl);
                    if (lvl >= activeQuest.targetPillarLevel)
                    {
                        MarkClaimable();
                    }
                }
            }
            else if (activeQuest.type == QuestType.PurchaseUpgrade)
            {
                if (UpgradeManager.Instance != null)
                {
                    string worldId = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentWorldId : "world_1";
                    int total = UpgradeManager.Instance.GetTotalUpgradeLevelsForWorld(worldId);
                    currentProgress = Mathf.Max(currentProgress, total);
                    if (total >= activeQuest.targetAmount)
                    {
                        MarkClaimable();
                    }
                }
            }
            else if (activeQuest.type == QuestType.UnlockWorld)
            {
                if (WorldManager.Instance != null)
                {
                    var worlds = WorldManager.Instance.Worlds;
                    int targetIdx = activeQuest.targetWorldOrder - 1;
                    if (worlds != null && targetIdx < worlds.Count && WorldManager.Instance.IsWorldUnlocked(worlds[targetIdx]))
                    {
                        currentProgress = activeQuest.targetAmount;
                        MarkClaimable();
                    }
                }
            }
        }

        private void AddProgress(int amount)
        {
            if (activeQuest == null || isClaimable || amount <= 0) return;

            currentProgress += amount;
            if (currentProgress >= activeQuest.targetAmount)
            {
                currentProgress = activeQuest.targetAmount;
                MarkClaimable();
            }
            else
            {
                SaveState();
                TriggerProgressUpdate();
            }
        }

        private void MarkClaimable()
        {
            if (isClaimable) return;
            isClaimable = true;
            SaveState();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayChestOpen();
            }

            OnQuestCompleted?.Invoke(activeQuest);
            GameEvents.TriggerQuestCompleted(activeQuest);
            TriggerProgressUpdate();

            Debug.Log($"[QuestManager] [QUEST COMPLETED] {activeQuest.title}! Ready to Claim +{activeQuest.rewardCoins} G!");
        }

        private void TriggerProgressUpdate()
        {
            if (activeQuest == null) return;
            OnQuestProgressUpdated?.Invoke(activeQuest, currentProgress, activeQuest.targetAmount);
            GameEvents.TriggerQuestProgressChanged(activeQuest, currentProgress, activeQuest.targetAmount);
        }

        public bool ClaimCurrentQuest(RectTransform uiClaimButton = null)
        {
            if (activeQuest == null || !isClaimable) return false;

            int reward = activeQuest.rewardCoins;

            // 1. Grant coins
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(reward);
            }

            // 2. Visual Juice: Flying Coins to Top HUD
            if (FlyingCoinManager.Instance != null)
            {
                if (uiClaimButton != null)
                {
                    FlyingCoinManager.Instance.PlayFlyingCoinsFromUI(uiClaimButton, 6);
                }
                else
                {
                    FlyingCoinManager.Instance.PlayFlyingCoins(Vector3.up * 1.5f, 5);
                }
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoin();
            }

            QuestStep completed = activeQuest;

            // 3. Move to next quest
            currentQuestIndex++;
            currentProgress = 0;
            isClaimable = false;
            SaveState();

            OnQuestClaimed?.Invoke(completed, reward);
            GameEvents.TriggerQuestClaimed(completed, reward);

            // 4. Load next
            RefreshActiveQuest();

            return true;
        }

        #region Event Handlers
        private void HandleFishCaught(ItemData fish)
        {
            if (activeQuest != null && activeQuest.type == QuestType.CatchFish)
            {
                AddProgress(1);
            }
        }

        private void HandleUpgradePurchased(UpgradeType type, int newLevel)
        {
            if (activeQuest == null) return;

            if (activeQuest.type == QuestType.PurchaseUpgrade)
            {
                if (UpgradeManager.Instance != null)
                {
                    string worldId = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentWorldId : "world_1";
                    int total = UpgradeManager.Instance.GetTotalUpgradeLevelsForWorld(worldId);
                    currentProgress = total;
                    if (currentProgress >= activeQuest.targetAmount) MarkClaimable();
                    else TriggerProgressUpdate();
                }
            }
            else if (activeQuest.type == QuestType.UpgradePillarLevel && activeQuest.targetPillar == type)
            {
                currentProgress = newLevel;
                if (newLevel >= activeQuest.targetPillarLevel) MarkClaimable();
                else TriggerProgressUpdate();
            }
        }

        private void HandleItemSold(ItemData item, int price)
        {
            if (activeQuest != null && activeQuest.type == QuestType.EarnCoins)
            {
                AddProgress(price);
            }
        }

        private void HandleFrenzyStarted()
        {
            if (activeQuest != null && activeQuest.type == QuestType.TriggerFrenzy)
            {
                AddProgress(1);
            }
        }

        private void HandleBossDefeated(FishChainData species, int bonusCoins)
        {
            if (activeQuest != null && activeQuest.type == QuestType.DefeatBoss)
            {
                AddProgress(1);
            }
        }

        private void HandleContractCompleted(HarborContract contract)
        {
            if (activeQuest != null && activeQuest.type == QuestType.CompleteContract)
            {
                AddProgress(1);
            }
        }

        private void HandleWorldUnlocked(WorldData world)
        {
            if (activeQuest != null && activeQuest.type == QuestType.UnlockWorld)
            {
                if (world != null && world.worldOrder >= activeQuest.targetWorldOrder)
                {
                    currentProgress = activeQuest.targetAmount;
                    MarkClaimable();
                }
            }
        }

        private void HandleWorldChanged(WorldData world)
        {
            // Re-evaluate current quest progress when changing harbor
            EvaluateRetroactiveProgress();
            TriggerProgressUpdate();
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.UI;
using IdleFishing.Audio;

namespace IdleFishing.Economy
{
    public enum RelicType
    {
        GoldenHook,      // Midas Hook: +25% Gold per fish
        SirenConch,      // Siren Conch: +20% Fishermen reel speed
        PoseidonTrident, // Poseidon Trident: +30% Frenzy charge & +25% duration
        AbyssalGhostNet, // Ghost Net: +12% Double catch chance
        MarinersCompass, // Mariner's Compass: +5,000 Starting Gold & +25% Transporter speed
        LeviathanCrown   // Leviathan Crown: +40% Apex Boss Fish bounty & +35% Contract rewards
    }

    /// <summary>
    /// Quản lý Hệ Thống Thám Hiểm Biển Sâu & Cổ Vật Thần Thoại (Prestige & Ancient Relics Manager):
    /// - Quản lý tiền tệ cao cấp: Ancient Pearls (Ngọc Trai Cổ Đại / Harbor Medals).
    /// - Quản lý 6 Cổ Vật Thần Thoại vĩnh viễn (Ancient Relics) với các mốc buff siêu giá trị.
    /// - Xử lý quy trình Viễn Chinh Biển Sâu (Deep Sea Expedition) kết hợp màn chuyển cảnh Mây Toon (VoyageTransitionUI).
    /// </summary>
    public class PrestigeManager : MonoBehaviour
    {
        private static PrestigeManager _instance;
        public static PrestigeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<PrestigeManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private const string PREFS_PEARLS = "IdleFishing_HarborMedals";
        private const string PREFS_LIFETIME_PEARLS = "IdleFishing_LifetimePearls";
        private const string PREFS_PRESTIGE_COUNT = "IdleFishing_PrestigeCount";
        private const string PREFS_RELIC_PREFIX = "IdleFishing_Relic_";

        [Header("Prestige Currency & Progress")]
        [SerializeField] private int harborMedals = 0;         // Current spendable Ancient Pearls
        [SerializeField] private int lifetimePearls = 0;       // Total pearls ever earned
        [SerializeField] private int prestigeCount = 0;        // Total voyages completed

        // Upgrade costs
        private static readonly int[] StandardRelicCosts = new int[] { 1, 2, 3, 5, 8, 12, 18, 25, 35, 50 };
        private static readonly int[] GhostNetCosts = new int[] { 2, 5, 10, 20, 35 };

        public int AncientPearls => harborMedals;
        public int HarborMedals => harborMedals; // Backward compatibility
        public int LifetimePearls => lifetimePearls;
        public int PrestigeCount => prestigeCount;

        // ========================================================
        // GLOBAL BUFF MULTIPLIERS (Applied across the entire game)
        // ========================================================
        public float GlobalGoldMultiplier => (1.0f + (lifetimePearls * 0.04f)) * (1.0f + GetRelicLevel(RelicType.GoldenHook) * 0.25f);
        public float GlobalSpeedMultiplier => (1.0f + (lifetimePearls * 0.02f)) * (1.0f + GetRelicLevel(RelicType.SirenConch) * 0.20f);
        public float RelicFrenzyChargeMultiplier => 1.0f + (GetRelicLevel(RelicType.PoseidonTrident) * 0.30f);
        public float RelicFrenzyDurationMultiplier => 1.0f + (GetRelicLevel(RelicType.PoseidonTrident) * 0.25f);
        public float DoubleCatchChance => Mathf.Clamp01(GetRelicLevel(RelicType.AbyssalGhostNet) * 0.12f);
        public int BonusStartingGold => GetRelicLevel(RelicType.MarinersCompass) * 5000;
        public float TransporterSpeedMultiplier => 1.0f + (GetRelicLevel(RelicType.MarinersCompass) * 0.25f);
        public float BossBonusMultiplier => 1.0f + (GetRelicLevel(RelicType.LeviathanCrown) * 0.50f);
        public float BossAndContractBonusMultiplier => BossBonusMultiplier; // Backward compatibility
        public float AquariumBonusMultiplier => 1.0f + (lifetimePearls * 0.05f) + (GetRelicLevel(RelicType.LeviathanCrown) * 0.20f);

        public event Action OnPrestigeUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadState();
        }

        private void LoadState()
        {
            harborMedals = PlayerPrefs.GetInt(PREFS_PEARLS, 0);
            lifetimePearls = PlayerPrefs.GetInt(PREFS_LIFETIME_PEARLS, harborMedals);
            prestigeCount = PlayerPrefs.GetInt(PREFS_PRESTIGE_COUNT, 0);
        }

        private void SaveState()
        {
            PlayerPrefs.SetInt(PREFS_PEARLS, harborMedals);
            PlayerPrefs.SetInt(PREFS_LIFETIME_PEARLS, lifetimePearls);
            PlayerPrefs.SetInt(PREFS_PRESTIGE_COUNT, prestigeCount);
            PlayerPrefs.Save();
        }

        #region Relic Management
        public int GetRelicLevel(RelicType type)
        {
            return PlayerPrefs.GetInt(PREFS_RELIC_PREFIX + type.ToString(), 0);
        }

        public static int GetRelicMaxLevel(RelicType type)
        {
            return (type == RelicType.AbyssalGhostNet) ? 5 : 10;
        }

        public int GetRelicUpgradeCost(RelicType type)
        {
            int lvl = GetRelicLevel(type);
            int maxLvl = GetRelicMaxLevel(type);
            if (lvl >= maxLvl) return -1; // Max level reached

            if (type == RelicType.AbyssalGhostNet)
            {
                return GhostNetCosts[Mathf.Clamp(lvl, 0, GhostNetCosts.Length - 1)];
            }
            return StandardRelicCosts[Mathf.Clamp(lvl, 0, StandardRelicCosts.Length - 1)];
        }

        public bool CanUpgradeRelic(RelicType type)
        {
            int cost = GetRelicUpgradeCost(type);
            if (cost <= 0) return false;
            return harborMedals >= cost;
        }

        public bool TryUpgradeRelic(RelicType type, out string message)
        {
            int lvl = GetRelicLevel(type);
            int maxLvl = GetRelicMaxLevel(type);
            if (lvl >= maxLvl)
            {
                message = "Relic has reached MAX level!";
                return false;
            }

            int cost = GetRelicUpgradeCost(type);
            if (harborMedals < cost)
            {
                message = $"Need {cost} Ancient Pearls (Have: {harborMedals}).";
                return false;
            }

            harborMedals -= cost;
            int newLvl = lvl + 1;
            PlayerPrefs.SetInt(PREFS_RELIC_PREFIX + type.ToString(), newLvl);
            SaveState();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUpgradeFanfare();
            }

            message = $"Upgraded {GetRelicTitle(type)} to Lv. {newLvl}!";
            OnPrestigeUpdated?.Invoke();
            return true;
        }

        public static string GetRelicTitle(RelicType type)
        {
            return type switch
            {
                RelicType.GoldenHook => "Golden Hook of Midas",
                RelicType.SirenConch => "Siren's Call Conch",
                RelicType.PoseidonTrident => "Poseidon's Sea Trident",
                RelicType.AbyssalGhostNet => "Abyssal Ghost Net",
                RelicType.MarinersCompass => "Mariner's Star Compass",
                RelicType.LeviathanCrown => "Crown of the Leviathan",
                _ => "Ancient Relic"
            };
        }

        public static string GetRelicIcon(RelicType type)
        {
            return type switch
            {
                RelicType.GoldenHook => "[HOOK]",
                RelicType.SirenConch => "[CONCH]",
                RelicType.PoseidonTrident => "[TRIDENT]",
                RelicType.AbyssalGhostNet => "[NET]",
                RelicType.MarinersCompass => "[COMPASS]",
                RelicType.LeviathanCrown => "[CROWN]",
                _ => "[RELIC]"
            };
        }

        public static string GetRelicDescription(RelicType type)
        {
            return type switch
            {
                RelicType.GoldenHook => "Midas blessing imbues every fish catch with gleaming gold value.",
                RelicType.SirenConch => "Echoes of the deep sea melody hasten all fishermen's reel speed.",
                RelicType.PoseidonTrident => "Commands ocean currents to accelerate Frenzy charge and extend duration.",
                RelicType.AbyssalGhostNet => "Phantom weaves snare bonus duplicate fish upon hauling.",
                RelicType.MarinersCompass => "Grants starting gold grant on every voyage and propels transporter carts.",
                RelicType.LeviathanCrown => "Exerts majesty over the ocean, skyrocketing Apex Boss bounties by +50% per level.",
                _ => ""
            };
        }

        public string GetCurrentBonusText(RelicType type)
        {
            int lvl = GetRelicLevel(type);
            if (lvl <= 0) return "<color=#888888>Not yet unlocked</color>";

            return type switch
            {
                RelicType.GoldenHook => $"<color=#2ECC71>+{lvl * 25}% Gold Value on All Fish</color>",
                RelicType.SirenConch => $"<color=#3498DB>+{lvl * 20}% Reel & Fishing Speed</color>",
                RelicType.PoseidonTrident => $"<color=#E67E22>+{lvl * 30}% Frenzy Charge, +{lvl * 25}% Duration</color>",
                RelicType.AbyssalGhostNet => $"<color=#9B59B6>+{lvl * 12}% Double Catch Chance</color>",
                RelicType.MarinersCompass => $"<color=#F1C40F>+{NumberFormatter.Format(lvl * 5000)} G Start, +{lvl * 25}% Cart Speed</color>",
                RelicType.LeviathanCrown => $"<color=#E74C3C>+{lvl * 50}% Apex Boss Bounty Gold</color>",
                _ => ""
            };
        }

        public string GetNextBonusText(RelicType type)
        {
            int lvl = GetRelicLevel(type);
            int maxLvl = GetRelicMaxLevel(type);
            if (lvl >= maxLvl) return "<color=#FFE600>MAX LEVEL ACHIEVED</color>";

            int next = lvl + 1;
            return type switch
            {
                RelicType.GoldenHook => $"Next: +{next * 25}% Gold Value",
                RelicType.SirenConch => $"Next: +{next * 20}% Reel Speed",
                RelicType.PoseidonTrident => $"Next: +{next * 30}% Frenzy Charge",
                RelicType.AbyssalGhostNet => $"Next: +{next * 12}% Double Catch Chance",
                RelicType.MarinersCompass => $"Next: +{NumberFormatter.Format(next * 5000)} G Starting Gold",
                RelicType.LeviathanCrown => $"Next: +{next * 50}% Apex Boss Gold",
                _ => ""
            };
        }
        #endregion

        #region Expedition & Prestige
        public int CalculateMedalsEarnable(string worldId = null)
        {
            // Điểm viễn chinh (Ancient Pearls) được tính theo TẤT CẢ giá trị kiếm được của TẤT CẢ các Level:
            long totalValueAllLevels = EconomyManager.Instance != null 
                ? EconomyManager.Instance.GetTotalValueEarnedAllWorlds() 
                : 0;

            int totalUpgradesAllLevels = UpgradeManager.Instance != null 
                ? UpgradeManager.Instance.GetTotalUpgradeLevelsAllWorlds() 
                : 0;

            int unlockedWorlds = WorldManager.Instance != null 
                ? WorldManager.Instance.GetUnlockedWorldsCount() 
                : 1;

            // Ngưỡng tối thiểu: Tích lũy được ít nhất 5,000 G trên tất cả các đảo hoặc 10 cấp nâng cấp
            if (totalValueAllLevels < 5000 && totalUpgradesAllLevels < 10)
            {
                return 0;
            }

            // 1. Điểm từ tổng giá trị tài sản kiếm được của toàn bộ các level:
            // Ví dụ: 50K G -> 4 pearls; 1M G -> 20 pearls; 25M G -> 100 pearls; 100M G -> 200 pearls
            float valueContribution = Mathf.Sqrt(totalValueAllLevels / 2500f);

            // 2. Điểm từ tổng số cấp nâng cấp của tất cả các level:
            float upgradeContribution = totalUpgradesAllLevels * 0.15f;

            // 3. Điểm thưởng mốc Level đã mở khóa (+3 ngọc trai cho mỗi level vượt qua Level 1):
            float levelBonus = (unlockedWorlds - 1) * 3.0f;

            int total = Mathf.FloorToInt(valueContribution + upgradeContribution + levelBonus);
            return Mathf.Max(1, total);
        }

        public long GetTotalValueEarnedAllLevels()
        {
            return EconomyManager.Instance != null ? EconomyManager.Instance.GetTotalValueEarnedAllWorlds() : 0;
        }

        public string GetLevelValueBreakdown()
        {
            if (EconomyManager.Instance == null) return "Lv.1: 0 G";
            string[] wIds = new string[] { "world_1", "world_2", "world_3", "world_4", "world_5" };
            var list = new List<string>();
            for (int i = 0; i < wIds.Length; i++)
            {
                long earned = EconomyManager.Instance.GetWorldEarnedGold(wIds[i]);
                int coins = EconomyManager.Instance.GetCoinsForWorld(wIds[i]);
                long val = Math.Max(earned, coins);
                if (val > 0)
                {
                    list.Add($"Lv.{i + 1}: {NumberFormatter.Format(val)} G");
                }
            }
            if (list.Count == 0) return "Lv.1: 0 G";
            return string.Join(" • ", list);
        }

        public bool CanPrestige(string worldId, out string reason)
        {
            int earnable = CalculateMedalsEarnable(worldId);
            if (earnable <= 0)
            {
                reason = $"Accumulate at least {NumberFormatter.Format(5000)} G total value across levels or 10 pier upgrades to set sail.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool PerformPrestige(WorldData currentWorld, out int medalsGained)
        {
            return PerformExpedition(currentWorld, null, out medalsGained);
        }

        /// <summary>
        /// Thực hiện Chuyến Viễn Chinh Biển Sâu (Deep Sea Expedition):
        /// - Chuyển cảnh Mây Toon tụ lại và tỏa ra.
        /// - Thu hoạch Ngọc Trai Cổ Đại tính theo TẤT CẢ giá trị kiếm được của TẤT CẢ các level.
        /// - Reset toàn bộ dữ liệu (tất cả level, nâng cấp, bến tàu, stage, thủy cung).
        /// - Bảo lưu toàn bộ Cổ Vật Thần Thoại và Ngọc Trai Cổ Đại.
        /// - Khởi hành lại tại Level 1 với Tiền Vàng thưởng từ La Bàn Hàng Hải.
        /// </summary>
        public bool PerformExpedition(WorldData currentWorld, Action onMidpointComplete, out int pearlsGained)
        {
            pearlsGained = 0;
            if (currentWorld == null) return false;

            pearlsGained = CalculateMedalsEarnable(currentWorld.worldId);
            if (pearlsGained <= 0) return false;

            int gained = pearlsGained;

            // Kích hoạt hoạt ảnh chuyển cảnh Mây Toon hoành tráng nếu có
            if (VoyageTransitionUI.Instance != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayLevelCelebration();
                }

                VoyageTransitionUI.Instance.StartEngulfInward(currentWorld, () =>
                {
                    ApplyExpeditionReset(currentWorld, gained);
                    onMidpointComplete?.Invoke();

                    VoyageTransitionUI.Instance.StartDisperseOutward(1.2f, () =>
                    {
                        Debug.Log("[PrestigeManager] Expedition voyage complete! Welcome to refreshed Level 1 harbor.");
                    });
                });
            }
            else
            {
                ApplyExpeditionReset(currentWorld, gained);
                onMidpointComplete?.Invoke();
            }

            return true;
        }

        private void ApplyExpeditionReset(WorldData currentWorld, int pearlsGained)
        {
            harborMedals += pearlsGained;
            lifetimePearls += pearlsGained;
            prestigeCount++;
            SaveState();

            // 1. Reset toàn bộ nâng cấp bến tàu của TẤT CẢ các level
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.ResetAllWorldsUpgrades();
            }

            // 2. Reset ví tiền và tổng vàng tích lũy lượt chơi của TẤT CẢ các level
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.ResetAllWorldsEconomy(BonusStartingGold);
            }

            // 3. Khóa lại các Level 2, 3, 4, 5 và đưa người chơi về Level 1
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.ResetAllWorldsProgress();
            }

            // 4. Reset toàn bộ tiến độ 30 Stages về Stage 1
            if (IdleFishing.Progression.StageProgressionManager.Instance != null)
            {
                IdleFishing.Progression.StageProgressionManager.Instance.ResetAllStageProgress();
            }

            // 5. Reset hợp đồng cảng biển
            if (IdleFishing.Market.ContractManager.Instance != null)
            {
                IdleFishing.Market.ContractManager.Instance.ResetAllContracts();
            }

            // 6. Reset Thủy Cung về trạng thái ban đầu
            if (IdleFishing.Aquarium.AquariumManager.Instance != null)
            {
                IdleFishing.Aquarium.AquariumManager.Instance.ResetAquarium();
            }

            GameEvents.TriggerPrestigeCompleted(pearlsGained);
            OnPrestigeUpdated?.Invoke();

            Debug.Log($"[PrestigeManager] VOYAGE SUCCESSFUL! ALL DATA RESET TO LEVEL 1! Earned +{pearlsGained} Ancient Pearls! Spendable: {harborMedals}, Lifetime: {lifetimePearls}");
        }
        #endregion
    }
}

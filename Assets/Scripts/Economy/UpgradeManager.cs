using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;

namespace IdleFishing.Economy
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private UpgradeConfig upgradeConfig;

        private string currentWorldId = "world_1";
        private readonly Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();

        public UpgradeConfig CurrentConfig => upgradeConfig;
        public string CurrentWorldId => currentWorldId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadUpgrades();
        }

        private string GetUpgradeKey(string worldId, UpgradeType type) => $"IdleFishing_{worldId}_Upgrade_{type}";

        public void LoadUpgrades()
        {
            upgradeLevels.Clear();
            if (upgradeConfig == null || upgradeConfig.Upgrades == null) return;

            foreach (var entry in upgradeConfig.Upgrades)
            {
                string key = GetUpgradeKey(currentWorldId, entry.type);
                // Fallback to legacy key only for world_1, new worlds start clean at level 0
                int fallbackLevel = currentWorldId == "world_1" ? PlayerPrefs.GetInt("IdleFishing_Upgrade_" + entry.type.ToString(), 0) : 0;
                int level = PlayerPrefs.GetInt(key, fallbackLevel);
                upgradeLevels[entry.type] = level;
            }
        }

        public void SwitchWorld(string newWorldId, UpgradeConfig newConfig = null)
        {
            currentWorldId = newWorldId;
            if (newConfig != null)
            {
                upgradeConfig = newConfig;
            }
            LoadUpgrades();
        }

        public int GetTotalUpgradeLevelsForWorld(string worldId)
        {
            if (upgradeConfig == null || upgradeConfig.Upgrades == null) return 0;
            int total = 0;
            foreach (var entry in upgradeConfig.Upgrades)
            {
                if (worldId == currentWorldId && upgradeLevels.TryGetValue(entry.type, out int currentLvl))
                {
                    total += currentLvl;
                }
                else
                {
                    string key = GetUpgradeKey(worldId, entry.type);
                    total += PlayerPrefs.GetInt(key, 0);
                }
            }
            return total;
        }

        public int GetLevel(UpgradeType type)
        {
            if (upgradeLevels.TryGetValue(type, out int lvl))
            {
                return lvl;
            }
            return 0;
        }

        public int GetCost(UpgradeType type)
        {
            if (upgradeConfig == null) return int.MaxValue;
            var entry = upgradeConfig.GetEntry(type);
            if (entry == null) return int.MaxValue;

            return entry.GetCostForLevel(GetLevel(type));
        }

        public float GetValue(UpgradeType type)
        {
            if (upgradeConfig == null) return 1.0f;
            var entry = upgradeConfig.GetEntry(type);
            if (entry == null) return 1.0f;

            return entry.GetValueForLevel(GetLevel(type));
        }

        public bool CanUpgrade(UpgradeType type)
        {
            if (upgradeConfig == null) return false;
            var entry = upgradeConfig.GetEntry(type);
            if (entry == null) return false;

            int currentLevel = GetLevel(type);
            if (currentLevel >= entry.maxLevel) return false;

            int cost = entry.GetCostForLevel(currentLevel);
            return EconomyManager.Instance != null && EconomyManager.Instance.HasEnoughCoins(cost);
        }

        public bool TryPurchaseUpgrade(UpgradeType type)
        {
            if (!CanUpgrade(type)) return false;

            int cost = GetCost(type);
            if (!EconomyManager.Instance.TrySpendCoins(cost)) return false;

            int newLevel = GetLevel(type) + 1;
            upgradeLevels[type] = newLevel;
            PlayerPrefs.SetInt(GetUpgradeKey(currentWorldId, type), newLevel);
            if (currentWorldId == "world_1")
            {
                PlayerPrefs.SetInt("IdleFishing_Upgrade_" + type.ToString(), newLevel); // Legacy sync for world_1 only
            }
            PlayerPrefs.Save();

            GameEvents.TriggerUpgradePurchased(type, newLevel);
            if (type == UpgradeType.WranglerTeam)
            {
                GameEvents.TriggerWorkerHired("wrangler", GetUnlockedWranglerCount());
            }
            else if (type == UpgradeType.FishermanTeam)
            {
                GameEvents.TriggerWorkerHired("fisherman", GetUnlockedFishermanSlots());
            }
            return true;
        }

        #region Streamlined Pillar Milestones
        /// <summary>
        /// Mở khóa slot cần thủ theo cấp độ Đội Cần Thủ (FishermanTeam):
        /// Lv 0: 1 slot | Lv 2: 2 slots | Lv 5: 3 slots | Lv 9: 4 slots | Lv 14: 5 slots (Max)
        /// </summary>
        public int GetUnlockedFishermanSlots()
        {
            int lvl = GetLevel(UpgradeType.FishermanTeam);
            if (lvl >= 14) return 5;
            if (lvl >= 9) return 4;
            if (lvl >= 5) return 3;
            if (lvl >= 2) return 2;
            return 1;
        }

        public int GetNextFishermanSlotRequiredLevel()
        {
            int lvl = GetLevel(UpgradeType.FishermanTeam);
            if (lvl < 2) return 2;
            if (lvl < 5) return 5;
            if (lvl < 9) return 9;
            if (lvl < 14) return 14;
            return -1; // Max
        }

        /// <summary>
        /// Mở khóa số lượng thợ săn cá theo cấp độ Đội Thợ Săn (WranglerTeam):
        /// Lv 0: 1 thợ | Lv 3: 2 thợ | Lv 7: 3 thợ | Lv 12: 4 thợ (Max)
        /// </summary>
        public int GetUnlockedWranglerCount()
        {
            int lvl = GetLevel(UpgradeType.WranglerTeam);
            if (lvl >= 12) return 4;
            if (lvl >= 7) return 3;
            if (lvl >= 3) return 2;
            return 1;
        }

        public int GetNextWranglerRequiredLevel()
        {
            int lvl = GetLevel(UpgradeType.WranglerTeam);
            if (lvl < 3) return 3;
            if (lvl < 7) return 7;
            if (lvl < 12) return 12;
            return -1; // Max
        }
        #endregion

        public long GetTotalInvestedGoldAllWorlds()
        {
            long totalInvested = 0;
            string[] worldIds = new string[] { "world_1", "world_2", "world_3", "world_4", "world_5" };
            UpgradeType[] types = new UpgradeType[] { UpgradeType.FishermanTeam, UpgradeType.WranglerTeam, UpgradeType.FishValue };

            foreach (var wId in worldIds)
            {
                foreach (var type in types)
                {
                    int lvl = (wId == currentWorldId && upgradeLevels.TryGetValue(type, out int curLvl))
                        ? curLvl
                        : PlayerPrefs.GetInt(GetUpgradeKey(wId, type), 0);

                    if (lvl > 0 && upgradeConfig != null)
                    {
                        var entry = upgradeConfig.GetEntry(type);
                        if (entry != null)
                        {
                            for (int l = 0; l < lvl; l++)
                            {
                                totalInvested += entry.GetCostForLevel(l);
                            }
                        }
                    }
                }
            }
            return totalInvested;
        }

        public int GetTotalUpgradeLevelsAllWorlds()
        {
            int total = 0;
            string[] worldIds = new string[] { "world_1", "world_2", "world_3", "world_4", "world_5" };
            foreach (var wId in worldIds)
            {
                total += GetTotalUpgradeLevelsForWorld(wId);
            }
            return total;
        }

        public void ResetWorldUpgrades(string worldId)
        {
            if (upgradeConfig == null) return;
            foreach (var entry in upgradeConfig.Upgrades)
            {
                string key = GetUpgradeKey(worldId, entry.type);
                PlayerPrefs.DeleteKey(key);
                if (currentWorldId == worldId)
                {
                    upgradeLevels[entry.type] = 0;
                    GameEvents.TriggerUpgradePurchased(entry.type, 0);
                }
            }
            PlayerPrefs.Save();
        }

        public void ResetAllWorldsUpgrades()
        {
            string[] worldIds = new string[] { "world_1", "world_2", "world_3", "world_4", "world_5" };
            UpgradeType[] types = new UpgradeType[] { UpgradeType.FishermanTeam, UpgradeType.WranglerTeam, UpgradeType.FishValue };

            foreach (var wId in worldIds)
            {
                foreach (var type in types)
                {
                    PlayerPrefs.DeleteKey(GetUpgradeKey(wId, type));
                    if (wId == "world_1")
                    {
                        PlayerPrefs.DeleteKey("IdleFishing_Upgrade_" + type.ToString());
                    }
                }
            }
            upgradeLevels.Clear();
            foreach (var type in types)
            {
                upgradeLevels[type] = 0;
                GameEvents.TriggerUpgradePurchased(type, 0);
            }
            PlayerPrefs.Save();
        }
    }
}

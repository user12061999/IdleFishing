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
                // Fallback to legacy key for world_1 if needed
                int legacyLevel = PlayerPrefs.GetInt("IdleFishing_Upgrade_" + entry.type.ToString(), 0);
                int level = PlayerPrefs.GetInt(key, legacyLevel);
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
            PlayerPrefs.SetInt("IdleFishing_Upgrade_" + type.ToString(), newLevel); // Legacy sync
            PlayerPrefs.Save();

            GameEvents.TriggerUpgradePurchased(type, newLevel);
            return true;
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
    }
}

using System;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;

namespace IdleFishing.Economy
{
    public class PrestigeManager : MonoBehaviour
    {
        public static PrestigeManager Instance { get; private set; }

        private const string PREFS_MEDALS = "IdleFishing_HarborMedals";
        private const string PREFS_PRESTIGE_COUNT = "IdleFishing_PrestigeCount";

        [Header("State")]
        [SerializeField] private int harborMedals = 0;
        [SerializeField] private int prestigeCount = 0;

        public int HarborMedals => harborMedals;
        public int PrestigeCount => prestigeCount;

        // Buff Multipliers
        public float GlobalGoldMultiplier => 1.0f + (harborMedals * 0.15f);
        public float GlobalSpeedMultiplier => 1.0f + (harborMedals * 0.05f);
        public float AquariumBonusMultiplier => 1.0f + (harborMedals * 0.10f);

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
            harborMedals = PlayerPrefs.GetInt(PREFS_MEDALS, 0);
            prestigeCount = PlayerPrefs.GetInt(PREFS_PRESTIGE_COUNT, 0);
        }

        private void SaveState()
        {
            PlayerPrefs.SetInt(PREFS_MEDALS, harborMedals);
            PlayerPrefs.SetInt(PREFS_PRESTIGE_COUNT, prestigeCount);
            PlayerPrefs.Save();
        }

        public int CalculateMedalsEarnable(string worldId)
        {
            int coins = EconomyManager.Instance != null ? EconomyManager.Instance.GetCoinsForWorld(worldId) : 0;
            int totalUpgrades = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetTotalUpgradeLevelsForWorld(worldId) : 0;

            // Threshold: Requires at least 15 upgrades or 10,000 coins
            if (coins < 8000 && totalUpgrades < 12)
            {
                return 0;
            }

            // Formula: base 1 medal + sqrt(coins / 4000) + upgrades / 10
            float coinContribution = Mathf.Sqrt(coins / 4000f);
            float upgradeContribution = totalUpgrades * 0.15f;
            int total = Mathf.FloorToInt(coinContribution + upgradeContribution);
            return Mathf.Max(1, total);
        }

        public bool CanPrestige(string worldId, out string reason)
        {
            int earnable = CalculateMedalsEarnable(worldId);
            if (earnable <= 0)
            {
                reason = "Must accumulate at least 8,000 G or 12 upgrade levels to Prestige.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool PerformPrestige(WorldData currentWorld, out int medalsGained)
        {
            medalsGained = 0;
            if (currentWorld == null) return false;

            medalsGained = CalculateMedalsEarnable(currentWorld.worldId);
            if (medalsGained <= 0) return false;

            harborMedals += medalsGained;
            prestigeCount++;
            SaveState();

            // Reset current world economic progress
            if (EconomyManager.Instance != null)
            {
                PlayerPrefs.SetInt($"IdleFishing_{currentWorld.worldId}_Coins", currentWorld.startingCoins);
                if (EconomyManager.Instance.CurrentWorldId == currentWorld.worldId)
                {
                    EconomyManager.Instance.SwitchWorld(currentWorld.worldId, currentWorld.startingCoins);
                }
            }

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.ResetWorldUpgrades(currentWorld.worldId);
            }

            // Trigger events & sound/VFX
            GameEvents.TriggerPrestigeCompleted(medalsGained);
            OnPrestigeUpdated?.Invoke();

            Debug.Log($"[PrestigeManager] HARBOR PRESTIGE SUCCESSFUL! Earned +{medalsGained} Harbor Medals! Total: {harborMedals}");
            return true;
        }
    }
}

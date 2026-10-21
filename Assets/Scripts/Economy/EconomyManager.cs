using UnityEngine;
using IdleFishing.Core;

namespace IdleFishing.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Starting Economy")]
        [SerializeField] private int initialCoins = 100;
        [SerializeField] private bool resetOnStart = false;

        private string currentWorldId = "world_1";
        private int currentCoins;

        public int Coins => currentCoins;
        public string CurrentWorldId => currentWorldId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadData();
        }

        private string GetCoinsKey(string worldId) => $"IdleFishing_{worldId}_Coins";

        private void LoadData()
        {
            if (resetOnStart)
            {
                currentCoins = initialCoins;
                SaveData();
            }
            else
            {
                // Fallback to legacy key if world_1 not set yet
                string key = GetCoinsKey(currentWorldId);
                int legacyCoins = PlayerPrefs.GetInt("IdleFishing_Coins", initialCoins);
                currentCoins = PlayerPrefs.GetInt(key, legacyCoins);
            }
        }

        public void SaveData()
        {
            PlayerPrefs.SetInt(GetCoinsKey(currentWorldId), currentCoins);
            PlayerPrefs.SetInt("IdleFishing_Coins", currentCoins); // Legacy sync
            PlayerPrefs.Save();
        }

        public int GetCoinsForWorld(string worldId, int defaultStartingCoins = 100)
        {
            if (worldId == currentWorldId) return currentCoins;
            return PlayerPrefs.GetInt(GetCoinsKey(worldId), defaultStartingCoins);
        }

        public void SetCoinsForWorld(string worldId, int newCoins)
        {
            PlayerPrefs.SetInt(GetCoinsKey(worldId), newCoins);
            if (worldId == currentWorldId)
            {
                currentCoins = newCoins;
                SaveData();
                GameEvents.TriggerCoinsChanged(currentCoins);
            }
        }

        public void SwitchWorld(string newWorldId, int defaultStartingCoins = 100)
        {
            if (currentWorldId == newWorldId) return;

            // 1. Save current world coins
            SaveData();

            // 2. Switch world ID & load its isolated wallet
            currentWorldId = newWorldId;
            currentCoins = PlayerPrefs.GetInt(GetCoinsKey(currentWorldId), defaultStartingCoins);

            // 3. Notify HUD & UI
            GameEvents.TriggerCoinsChanged(currentCoins);
        }

        private const string PREF_VOYAGE_RUN_GOLD = "IdleFishing_VoyageRunLifetimeGold";
        private const string PREF_WORLD_EARNED_PREFIX = "IdleFishing_WorldEarned_";

        public long GetRunLifetimeGold()
        {
            string val = PlayerPrefs.GetString(PREF_VOYAGE_RUN_GOLD, "0");
            long.TryParse(val, out long result);
            return result;
        }

        public long GetWorldEarnedGold(string worldId)
        {
            string val = PlayerPrefs.GetString(PREF_WORLD_EARNED_PREFIX + worldId, "0");
            long.TryParse(val, out long result);
            return result;
        }

        /// <summary>
        /// Tổng giá trị tích lũy kiếm được của tất cả các Level trong lượt chơi hiện tại:
        /// Bao gồm tổng vàng kiếm được ghi nhận + vàng trong túi các đảo + vàng đã đầu tư nâng cấp.
        /// </summary>
        public long GetTotalValueEarnedAllWorlds()
        {
            long runTotal = GetRunLifetimeGold();

            // Tính tổng tiền mặt trong túi của tất cả 5 Level
            long walletsSum = 0;
            string[] worldIds = new string[] { "world_1", "world_2", "world_3", "world_4", "world_5" };
            foreach (var wId in worldIds)
            {
                walletsSum += GetCoinsForWorld(wId);
            }

            // Tính tổng giá trị nâng cấp bến tàu đã đầu tư của tất cả 5 Level
            long investedGold = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetTotalInvestedGoldAllWorlds() : 0;
            long baselineSum = walletsSum + investedGold;

            return System.Math.Max(runTotal, baselineSum);
        }

        public void ResetAllWorldsEconomy(int bonusStartingGold)
        {
            string[] worldIds = new string[] { "world_1", "world_2", "world_3", "world_4", "world_5" };
            foreach (var wId in worldIds)
            {
                int startCoins = (wId == "world_1") ? (100 + bonusStartingGold) : 100;
                PlayerPrefs.SetInt(GetCoinsKey(wId), startCoins);
                PlayerPrefs.DeleteKey(PREF_WORLD_EARNED_PREFIX + wId);
            }
            PlayerPrefs.DeleteKey(PREF_VOYAGE_RUN_GOLD);
            PlayerPrefs.DeleteKey("IdleFishing_Coins"); // Legacy key reset
            PlayerPrefs.Save();

            currentWorldId = "world_1";
            currentCoins = 100 + bonusStartingGold;
            SaveData();
            GameEvents.TriggerCoinsChanged(currentCoins);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            float multiplier = 1.0f;
            if (PrestigeManager.Instance != null)
            {
                multiplier *= PrestigeManager.Instance.GlobalGoldMultiplier;
            }
            int actualAmount = Mathf.RoundToInt(amount * multiplier);

            currentCoins += actualAmount;

            // Ghi nhận tổng giá trị kiếm được trong lượt viễn chinh này (Toàn bộ các Level)
            long newRunTotal = GetRunLifetimeGold() + actualAmount;
            PlayerPrefs.SetString(PREF_VOYAGE_RUN_GOLD, newRunTotal.ToString());

            long newWorldEarned = GetWorldEarnedGold(currentWorldId) + actualAmount;
            PlayerPrefs.SetString(PREF_WORLD_EARNED_PREFIX + currentWorldId, newWorldEarned.ToString());

            SaveData();
            GameEvents.TriggerCoinsChanged(currentCoins);

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayCoin();
            }
        }

        public bool HasEnoughCoins(int amount)
        {
            return currentCoins >= amount;
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (!HasEnoughCoins(amount)) return false;

            currentCoins -= amount;
            SaveData();
            GameEvents.TriggerCoinsChanged(currentCoins);
            return true;
        }
    }
}

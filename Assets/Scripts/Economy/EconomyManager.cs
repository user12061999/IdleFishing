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

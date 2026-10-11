using System;
using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.UI;

namespace IdleFishing.Economy
{
    public class SaveDataManager : MonoBehaviour
    {
        public static SaveDataManager Instance { get; private set; }

        private const string SAVE_TIMESTAMP_KEY = "IdleFishing_LastSaveTimestamp";
        private const float AUTO_SAVE_INTERVAL = 10f;
        private const float MAX_OFFLINE_HOURS = 8f;

        [Header("Offline Earnings Tuning")]
        [SerializeField] private float baseCoinsPerSecond = 1.2f;
        [SerializeField] private float minAwaySecondsForReward = 20f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CheckOfflineEarnings();
            StartCoroutine(AutoSaveLoop());
        }

        private void OnApplicationQuit()
        {
            SaveTimestamp();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveTimestamp();
            }
            else
            {
                CheckOfflineEarnings();
            }
        }

        public void SaveTimestamp()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            PlayerPrefs.SetString(SAVE_TIMESTAMP_KEY, nowTicks.ToString());
            PlayerPrefs.Save();
        }

        private void CheckOfflineEarnings()
        {
            string savedTicksStr = PlayerPrefs.GetString(SAVE_TIMESTAMP_KEY, string.Empty);
            if (string.IsNullOrEmpty(savedTicksStr))
            {
                SaveTimestamp();
                return;
            }

            if (long.TryParse(savedTicksStr, out long savedTicks))
            {
                DateTime lastSaveTime = new DateTime(savedTicks, DateTimeKind.Utc);
                TimeSpan awayTime = DateTime.UtcNow - lastSaveTime;

                float minAway = minAwaySecondsForReward;
                float maxHours = MAX_OFFLINE_HOURS;
                float baseRate = baseCoinsPerSecond;

                if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
                {
                    minAway = GameManager.Instance.BalanceConfig.offlineMinAwaySeconds;
                    maxHours = GameManager.Instance.BalanceConfig.offlineMaxHours;
                    baseRate = GameManager.Instance.BalanceConfig.offlineBaseCoinsPerSecond;
                }

                if (awayTime.TotalSeconds >= minAway)
                {
                    // Cap offline duration
                    double cappedSeconds = Math.Min(awayTime.TotalSeconds, maxHours * 3600);

                    // Calculate offline rate based on upgrades
                    float marketMult = 1.0f;
                    float processMult = 1.0f;

                    if (UpgradeManager.Instance != null)
                    {
                        marketMult = UpgradeManager.Instance.GetValue(UpgradeType.MarketValueMultiplier);
                        processMult = UpgradeManager.Instance.GetValue(UpgradeType.KitchenSpeed);
                    }

                    float ratePerSec = baseRate * marketMult * processMult;
                    int earnedCoins = Mathf.RoundToInt((float)cappedSeconds * ratePerSec);

                    if (earnedCoins >= 10 && OfflineEarningsPopupUI.Instance != null)
                    {
                        // Trong Unity Editor, chỉ tự động bật popup khi vắng mặt thực sự (>= 10 phút) để tránh gián đoạn các phiên test nhanh
                        if (!Application.isEditor || awayTime.TotalMinutes >= 10.0)
                        {
                            OfflineEarningsPopupUI.Instance.Show(earnedCoins, awayTime);
                            Debug.Log($"[SaveDataManager] Player was away for {awayTime.TotalMinutes:0.0} minutes, earned {earnedCoins} G offline!");
                        }
                    }

                    // Tích lũy thêm tiền vé Thủy Cung trong thời gian vắng mặt
                    if (IdleFishing.Aquarium.AquariumManager.Instance != null)
                    {
                        IdleFishing.Aquarium.AquariumManager.Instance.AddOfflineTicketRevenue((float)cappedSeconds);
                    }
                }
            }

            SaveTimestamp();
        }

        private IEnumerator AutoSaveLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(AUTO_SAVE_INTERVAL);
                SaveTimestamp();
            }
        }
    }
}

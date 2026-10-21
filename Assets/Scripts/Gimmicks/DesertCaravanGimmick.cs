using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.UI;
using IdleFishing.Fishing;
using IdleFishing.VFX;

namespace IdleFishing.Gimmicks
{
    /// <summary>
    /// Cơ chế Level 5 (Ốc Đảo Sa Mạc): Đoàn Lạc Đà Thương Gia & Cổ Vật Sa Mạc (Caravan & Relics).
    /// - Thương gia sa mạc ghé thăm bến thu mua cá quý với giá trên trời.
    /// - Người chơi giao cá cho thương gia để mở khóa 3 Cổ Vật Thần Bí vĩnh viễn:
    ///   1. Mặt Trời Vàng: Tăng +30% Tốc độ câu toàn bến.
    ///   2. Bọ Hung Ngọc: Tăng +50% Tiền bán cá.
    ///   3. Đèn Thần Sa Mạc: Triệu hồi Boss Đại Bàng Biển với mưa vàng x2!
    /// </summary>
    public class DesertCaravanGimmick : MonoBehaviour
    {
        [Header("Order Configuration")]
        [SerializeField] private int requiredFishPerOrder = 3;
        [SerializeField] private int baseOrderRewardCoins = 1200;

        [Header("Relics State")]
        private int completedOrders = 0;
        private int currentDeliveredFish = 0;

        public int CompletedOrders => completedOrders;
        public int CurrentDeliveredFish => currentDeliveredFish;
        public int RequiredFishPerOrder => requiredFishPerOrder;

        public bool HasSunRelic => completedOrders >= 1;
        public bool HasScarabRelic => completedOrders >= 2;
        public bool HasLampRelic => completedOrders >= 3;

        public float SpeedMultiplier => HasSunRelic ? 1.30f : 1.0f;
        public float PriceMultiplier => HasScarabRelic ? 1.50f : 1.0f;

        [Header("Visual References")]
        [SerializeField] private Transform caravanVisualRoot;

        private Vector3 originalScale = Vector3.one;

        private void Awake()
        {
            if (caravanVisualRoot != null)
            {
                originalScale = caravanVisualRoot.localScale;
            }
            // Load saved relics progress
            completedOrders = PlayerPrefs.GetInt("IdleFishing_Desert_CompletedOrders", 0);
        }

        private void OnEnable()
        {
            GameEvents.FishCaught += OnFishCaught;
        }

        private void OnDisable()
        {
            GameEvents.FishCaught -= OnFishCaught;
        }

        private void OnFishCaught(ItemData fish)
        {
            // Tự động đóng góp cá vào đơn hàng thương gia
            currentDeliveredFish++;
            if (currentDeliveredFish >= requiredFishPerOrder)
            {
                CompleteOrder();
            }
            else
            {
                WorldGimmickManager.Instance?.NotifyStateChanged();
            }
        }

        private void OnMouseDown()
        {
            TapCaravan();
        }

        public void TapCaravan()
        {
            // Tương tác trực tiếp: Nộp cá đang có
            StartCoroutine(BounceRoutine());

            currentDeliveredFish++;
            if (currentDeliveredFish >= requiredFishPerOrder)
            {
                CompleteOrder();
            }
            else
            {
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 1.8f,
                        0,
                        DamageStyle.GoldBonus,
                        $"[CARAVAN] DELIVER FISH TO MERCHANTS ({currentDeliveredFish}/{requiredFishPerOrder})"
                    );
                }
                WorldGimmickManager.Instance?.NotifyStateChanged();
            }
        }

        private void CompleteOrder()
        {
            currentDeliveredFish = 0;
            completedOrders++;
            PlayerPrefs.SetInt("IdleFishing_Desert_CompletedOrders", completedOrders);
            PlayerPrefs.Save();

            // Tiền thưởng
            float valueMult = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetValue(Data.UpgradeType.FishValue) : 1.0f;
            int finalReward = Mathf.RoundToInt(baseOrderRewardCoins * valueMult);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(finalReward);
            }

            // Mở khóa cổ vật tương ứng
            string relicName = "GOLDEN SUN (+30% FISHING SPEED)";
            if (completedOrders == 2) relicName = "JADE SCARAB (+50% FISH SELL VALUE)";
            else if (completedOrders >= 3)
            {
                relicName = "DESERT LAMP (+150% SEA BOSS CHANCE)";
            }

            // Hiệu ứng pháo hoa rực rỡ & nổ vàng
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnConfetti(transform.position + Vector3.up * 2f, 2.5f);
                VFXManager.Instance.SpawnGoldBurst(transform.position + Vector3.up * 1.5f, 50);
            }

            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 2.5f,
                    finalReward,
                    DamageStyle.GoldBonus,
                    $"[*] ORDER COMPLETE! +{NumberFormatter.Format(finalReward)} G & RELIC: {relicName}"
                );
            }

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayCaravanBells(transform.position);
            }

            WorldGimmickManager.Instance?.NotifyStateChanged();
            Debug.Log($"[DesertCaravanGimmick] Caravan order fulfilled! Relic: {relicName}");
        }

        private IEnumerator BounceRoutine()
        {
            if (caravanVisualRoot == null) yield break;

            float el = 0f;
            float dur = 0.20f;
            while (el < dur)
            {
                el += Time.deltaTime;
                float t = el / dur;
                float p = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                caravanVisualRoot.localScale = originalScale * p;
                yield return null;
            }
            caravanVisualRoot.localScale = originalScale;
        }

        public void SetupVisuals(Transform visualRoot)
        {
            caravanVisualRoot = visualRoot;
            if (caravanVisualRoot != null) originalScale = caravanVisualRoot.localScale;
        }
    }
}

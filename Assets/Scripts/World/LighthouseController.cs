using System;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Economy;
using IdleFishing.UI;

namespace IdleFishing.World
{
    public class LighthouseController : MonoBehaviour
    {
        public static LighthouseController Instance { get; private set; }

        [Header("Lighthouse State")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int maxLevel = 3;
        [SerializeField] private int baseUpgradeCost = 150;

        [Header("Visual Components")]
        [SerializeField] private Transform beaconHead;
        [SerializeField] private Light beaconLight;
        [SerializeField] private Renderer lampRenderer;
        [SerializeField] private float rotateSpeed = 45f;

        [Header("Interaction Pad")]
        [SerializeField] private InWorldPurchasePad upgradePad;

        public int CurrentLevel => currentLevel;
        public int MaxLevel => maxLevel;
        public bool IsMaxLevel => currentLevel >= maxLevel;

        public event Action<int> OnLevelChanged;

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
            UpdateVisuals();
            UpdatePadState();
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnNightToggled += OnNightToggled;
            }
        }

        private void OnDestroy()
        {
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnNightToggled -= OnNightToggled;
            }
        }

        private void OnNightToggled(bool night)
        {
            if (beaconLight != null)
            {
                float baseInt = 1.8f + currentLevel * 0.8f;
                beaconLight.intensity = night ? baseInt * 1.6f : baseInt * 0.75f;
                beaconLight.range = night ? 34f : 24f;
            }
        }

        private void Update()
        {
            if (beaconHead != null)
            {
                beaconHead.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
            }
        }

        public void Setup(Transform bHead, Light bLight, Renderer lRenderer, InWorldPurchasePad pad)
        {
            beaconHead = bHead;
            beaconLight = bLight;
            lampRenderer = lRenderer;
            upgradePad = pad;

            UpdateVisuals();
            UpdatePadState();
        }

        public float GetFishingSpeedMultiplier()
        {
            switch (currentLevel)
            {
                case 1: return 1.20f; // +20% tốc độ câu
                case 2: return 1.35f; // +35% tốc độ câu
                case 3: return 1.50f; // +50% tốc độ câu (Cực nhanh)
                default: return 1.0f;
            }
        }

        public float GetBossChanceBonus()
        {
            switch (currentLevel)
            {
                case 2: return 0.06f; // +6% tỷ lệ cá Boss
                case 3: return 0.12f; // +12% tỷ lệ cá Boss & Rương
                default: return 0f;
            }
        }

        public int GetUpgradeCost()
        {
            if (IsMaxLevel) return 0;
            return baseUpgradeCost * currentLevel;
        }

        public bool TryUpgrade()
        {
            if (IsMaxLevel) return false;

            int cost = GetUpgradeCost();
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendCoins(cost))
            {
                return false;
            }

            currentLevel++;
            OnLevelChanged?.Invoke(currentLevel);

            UpdateVisuals();
            UpdatePadState();

            // Visual feedback
            if (FloatingDamageManager.Instance != null)
            {
                FloatingDamageManager.Instance.SpawnDamage(
                    transform.position + Vector3.up * 4.5f,
                    0,
                    DamageStyle.GoldBonus,
                    $"[★] LIGHTHOUSE REACHED LV {currentLevel}! [★]"
                );
            }

            if (FlyingCoinManager.Instance != null)
            {
                FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 2f, 3);
            }

            Debug.Log($"[Lighthouse] Upgraded Lighthouse to Level {currentLevel}! Fishing speed x{GetFishingSpeedMultiplier():F2}");
            return true;
        }

        private void UpdateVisuals()
        {
            if (beaconLight != null)
            {
                beaconLight.intensity = 1.8f + currentLevel * 0.8f;
                beaconLight.range = 22f + currentLevel * 6f;
                beaconLight.color = currentLevel == 3
                    ? new Color(0.3f, 1f, 0.9f) // Ngọc lam huyền thoại
                    : new Color(1f, 0.92f, 0.5f); // Vàng ấm
            }

            rotateSpeed = 40f + currentLevel * 15f;
        }

        private void UpdatePadState()
        {
            if (upgradePad == null) return;

            if (IsMaxLevel)
            {
                upgradePad.SetTitle("[V] LIGHTHOUSE MAX");
                upgradePad.SetSubtitle("Lv 3 - Legendary Beacon");
            }
            else
            {
                upgradePad.SetTitle($"[+] UPGRADE LIGHTHOUSE ({GetUpgradeCost()} G)");
                upgradePad.SetSubtitle($"To Lv {currentLevel + 1}: Fishing speed +15%");
            }
            upgradePad.UpdateVisuals();
        }

        private void OnMouseDown()
        {
            // Cho phép người chơi chạm trực tiếp vào Hải Đăng để nâng cấp
            if (!IsMaxLevel)
            {
                TryUpgrade();
            }
        }
    }
}

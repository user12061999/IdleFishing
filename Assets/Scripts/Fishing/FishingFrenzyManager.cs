using System;
using System.Collections;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.Fishing
{
    public class FishingFrenzyManager : MonoBehaviour
    {
        public static FishingFrenzyManager Instance { get; private set; }

        [Header("Runtime State")]
        [SerializeField] private float currentCharge = 0f; // 0..100
        [SerializeField] private bool isFrenzyActive = false;
        [SerializeField] private float remainingFrenzyTime = 0f;

        private GameBalanceConfig balanceConfig;

        public float CurrentCharge => currentCharge;
        public bool IsFrenzyActive => isFrenzyActive;
        public float RemainingFrenzyTime => remainingFrenzyTime;

        public float FrenzyDuration => balanceConfig != null ? balanceConfig.frenzyDuration : 20.0f;
        public float SpeedMultiplier => isFrenzyActive ? (balanceConfig != null ? balanceConfig.frenzySpeedMultiplier : 2.5f) : 1.0f;
        public float DamageMultiplier => isFrenzyActive ? (balanceConfig != null ? balanceConfig.frenzyDamageMultiplier : 2.5f) : 1.0f;

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
            if (GameManager.Instance != null && GameManager.Instance.BalanceConfig != null)
            {
                balanceConfig = GameManager.Instance.BalanceConfig;
            }
        }

        private void OnEnable()
        {
            GameEvents.FishCaught += OnFishCaught;
            GameEvents.TreasureChestOpened += OnTreasureChestOpened;
        }

        private void OnDisable()
        {
            GameEvents.FishCaught -= OnFishCaught;
            GameEvents.TreasureChestOpened -= OnTreasureChestOpened;
        }

        private void OnFishCaught(ItemData item)
        {
            if (balanceConfig == null && GameManager.Instance != null)
            {
                balanceConfig = GameManager.Instance.BalanceConfig;
            }

            float charge = balanceConfig != null ? balanceConfig.frenzyChargePerFish : 4.0f;
            AddCharge(charge);
        }

        private void OnTreasureChestOpened(int bonusGold)
        {
            if (balanceConfig == null && GameManager.Instance != null)
            {
                balanceConfig = GameManager.Instance.BalanceConfig;
            }

            float charge = balanceConfig != null ? balanceConfig.frenzyChargePerChest : 40.0f;
            AddCharge(charge);
        }

        public void AddCharge(float amount)
        {
            if (isFrenzyActive) return; // Do not charge while already in Frenzy mode

            currentCharge = Mathf.Clamp(currentCharge + amount, 0f, 100f);
            GameEvents.TriggerFrenzyChargeChanged(currentCharge);

            if (currentCharge >= 100f)
            {
                ActivateFrenzy();
            }
        }

        public void ActivateFrenzy()
        {
            if (isFrenzyActive) return;

            StopAllCoroutines();
            StartCoroutine(FrenzyRoutine());
        }

        private IEnumerator FrenzyRoutine()
        {
            isFrenzyActive = true;
            remainingFrenzyTime = FrenzyDuration;
            GameEvents.TriggerFrenzyStarted();

            if (IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
            {
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.42f, 0.45f);
                IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerPunchFOV(-3.0f);
            }

            Debug.Log("[FishingFrenzyManager] 🔥 FISHING FRENZY ACTIVATED FOR 20S! 🔥");

            while (remainingFrenzyTime > 0)
            {
                remainingFrenzyTime -= Time.deltaTime;
                yield return null;
            }

            isFrenzyActive = false;
            remainingFrenzyTime = 0f;
            currentCharge = 0f;
            GameEvents.TriggerFrenzyChargeChanged(0f);
            GameEvents.TriggerFrenzyEnded();
            Debug.Log("[FishingFrenzyManager] Fishing Frenzy ended. Reverted to normal rate.");
        }
    }
}

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

        public float FrenzyDuration
        {
            get
            {
                float baseDur = balanceConfig != null ? balanceConfig.frenzyDuration : 20.0f;
                if (IdleFishing.Economy.PrestigeManager.Instance != null)
                {
                    baseDur *= IdleFishing.Economy.PrestigeManager.Instance.RelicFrenzyDurationMultiplier;
                }
                return baseDur;
            }
        }
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
            if (IdleFishing.Economy.PrestigeManager.Instance != null)
            {
                charge *= IdleFishing.Economy.PrestigeManager.Instance.RelicFrenzyChargeMultiplier;
            }
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

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    OnScreenTap();
                }
            }
        }

        private void OnScreenTap()
        {
            if (isFrenzyActive)
            {
                // Tapping liên tục khi đang Frenzy kéo dài thêm một chút thời gian bão cá
                remainingFrenzyTime = Mathf.Min(remainingFrenzyTime + 0.15f, FrenzyDuration);
                if (IdleFishing.CameraControl.MobilePortraitCameraController.Instance != null)
                {
                    IdleFishing.CameraControl.MobilePortraitCameraController.Instance.TriggerShake(0.08f, 0.06f);
                }
            }
            else
            {
                // Tapping trên màn hình tích 2% năng lượng Frenzy
                AddCharge(2.0f);
            }
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

            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayChestOpen();
            }

            if (IdleFishing.VFX.VFXManager.Instance != null)
            {
                IdleFishing.VFX.VFXManager.Instance.SpawnConfetti(new Vector3(0f, 2.5f, 7.5f), 2.5f);
                IdleFishing.VFX.VFXManager.Instance.SpawnGoldBurst(new Vector3(0f, 2.0f, 7.5f), 40);
            }

            if (IdleFishing.UI.FloatingDamageManager.Instance != null)
            {
                IdleFishing.UI.FloatingDamageManager.Instance.SpawnDamage(
                    new Vector3(0f, 2.8f, 7.5f),
                    0,
                    IdleFishing.UI.DamageStyle.GoldBonus,
                    "[FRENZY] FISHING FRENZY ACTIVATED! 2X SPEED & 2X GOLD!"
                );
            }

            Debug.Log("[FishingFrenzyManager] [FRENZY] FISHING FRENZY ACTIVATED FOR 20S!");

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

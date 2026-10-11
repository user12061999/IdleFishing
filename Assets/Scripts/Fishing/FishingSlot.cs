using System.Collections;
using UnityEngine;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.UI;

namespace IdleFishing.Fishing
{
    public class FishingSlot : MonoBehaviour
    {
        [Header("Slot Identity")]
        [SerializeField] private int slotIndex = 0; // 0 = Slot 1 (free), 1 = Slot 2, 2 = Slot 3
        [SerializeField] private string slotName = "Fisherman 1";

        [Header("References")]
        [SerializeField] private GameObject fishermanObj;
        [SerializeField] private GameObject stoolObj;
        [SerializeField] private GameObject unlockPadObj;
        [SerializeField] private TextMeshProUGUI padTitleText;
        [SerializeField] private TextMeshProUGUI padCostText;

        [Header("Runtime State")]
        [SerializeField] private bool isUnlocked = false;

        public int SlotIndex => slotIndex;
        public bool IsUnlocked => isUnlocked;
        public GameObject FishermanObj => fishermanObj;

        private void Start()
        {
            if (FishingPierManager.Instance != null)
            {
                FishingPierManager.Instance.RegisterSlot(this);
            }

            RefreshSlotState(instant: true);
        }

        private void OnEnable()
        {
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.CoinsChanged += OnCoinsChanged;
        }

        private void OnDisable()
        {
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.CoinsChanged -= OnCoinsChanged;
        }

        private void OnUpgradePurchased(UpgradeType type, int level)
        {
            if (type == UpgradeType.FishermanSlots)
            {
                RefreshSlotState(instant: false);
            }
        }

        private void OnCoinsChanged(int newCoins)
        {
            UpdatePadVisuals();
        }

        public void Setup(int index, string name, GameObject fisherman, GameObject stool, GameObject pad, TextMeshProUGUI titleText, TextMeshProUGUI costText)
        {
            slotIndex = index;
            slotName = name;
            fishermanObj = fisherman;
            stoolObj = stool;
            unlockPadObj = pad;
            padTitleText = titleText;
            padCostText = costText;
        }

        public void RefreshSlotState(bool instant = false)
        {
            int currentSlotsLvl = 0;
            if (UpgradeManager.Instance != null)
            {
                currentSlotsLvl = UpgradeManager.Instance.GetLevel(UpgradeType.FishermanSlots);
            }

            // Slot 0 is always unlocked from start.
            // Slot 1 requires FishermanSlots >= 1.
            // Slot 2 requires FishermanSlots >= 2.
            bool shouldBeUnlocked = (slotIndex == 0) || (currentSlotsLvl >= slotIndex);

            if (shouldBeUnlocked && !isUnlocked)
            {
                UnlockSlot(instant);
            }
            else if (!shouldBeUnlocked)
            {
                isUnlocked = false;
                if (fishermanObj != null) fishermanObj.SetActive(false);
                if (stoolObj != null) stoolObj.SetActive(false);

                // Show pad only if previous slot is unlocked
                bool isPreviousUnlocked = (slotIndex == 1) || (currentSlotsLvl >= slotIndex - 1);
                if (unlockPadObj != null)
                {
                    unlockPadObj.SetActive(isPreviousUnlocked);
                }
                UpdatePadVisuals();
            }
        }

        private void UnlockSlot(bool instant)
        {
            isUnlocked = true;
            if (unlockPadObj != null) unlockPadObj.SetActive(false);
            if (stoolObj != null) stoolObj.SetActive(true);

            if (fishermanObj != null)
            {
                fishermanObj.SetActive(true);
                if (!instant)
                {
                    StartCoroutine(PopInRoutine(fishermanObj.transform));
                    if (FloatingDamageManager.Instance != null)
                    {
                        FloatingDamageManager.Instance.SpawnDamage(
                            fishermanObj.transform.position + Vector3.up * 2f,
                            0,
                            DamageStyle.GoldBonus,
                            $"[!] UNLOCKED {slotName.ToUpper()}! [!]"
                        );
                    }
                }
            }
        }

        private void UpdatePadVisuals()
        {
            if (isUnlocked || unlockPadObj == null || !unlockPadObj.activeSelf) return;

            int cost = 0;
            if (UpgradeManager.Instance != null)
            {
                cost = UpgradeManager.Instance.GetCost(UpgradeType.FishermanSlots);
            }

            if (padTitleText != null)
            {
                padTitleText.text = $"[+] MUA {slotName.ToUpper()}";
            }

            if (padCostText != null)
            {
                bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.HasEnoughCoins(cost);
                string colorTag = canAfford ? "<color=#00FF88>" : "<color=#FF6666>";
                padCostText.text = $"{colorTag}{cost:N0} G</color>";
            }
        }

        public void TryPurchaseFromWorld()
        {
            if (isUnlocked) return;

            if (UpgradeManager.Instance == null) return;

            int currentSlotsLvl = UpgradeManager.Instance.GetLevel(UpgradeType.FishermanSlots);
            if (currentSlotsLvl != slotIndex - 1) return;

            int cost = UpgradeManager.Instance.GetCost(UpgradeType.FishermanSlots);
            if (EconomyManager.Instance != null && EconomyManager.Instance.HasEnoughCoins(cost))
            {
                UpgradeManager.Instance.TryPurchaseUpgrade(UpgradeType.FishermanSlots);
            }
            else
            {
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 1.5f,
                        0,
                        DamageStyle.FishAttack,
                        "NOT ENOUGH GOLD!"
                    );
                }
            }
        }

        private IEnumerator PopInRoutine(Transform target)
        {
            Vector3 originalScale = target.localScale;
            target.localScale = Vector3.zero;

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float overshoot = Mathf.Sin(t * Mathf.PI * 0.75f) * 1.25f;
                target.localScale = originalScale * overshoot;
                yield return null;
            }

            target.localScale = originalScale;
        }
    }
}

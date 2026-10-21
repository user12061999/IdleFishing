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
        [SerializeField] private TextMeshProUGUI padSubText;

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

            ConfigurePadLayout();
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

        public void Setup(int index, string name, GameObject fisherman, GameObject stool, GameObject pad, TextMeshProUGUI titleText, TextMeshProUGUI costText, TextMeshProUGUI subText = null)
        {
            slotIndex = index;
            slotName = name;
            fishermanObj = fisherman;
            stoolObj = stool;
            unlockPadObj = pad;
            padTitleText = titleText;
            padCostText = costText;
            padSubText = subText;

            ConfigurePadLayout();
        }

        private void ConfigurePadLayout()
        {
            if (padSubText == null && unlockPadObj != null)
            {
                var texts = unlockPadObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts.Length > 2) padSubText = texts[2];
            }

            ConfigureText(padTitleText, 10f, 17f, UIFontCategory.Header);
            ConfigureText(padCostText, 14f, 22f, UIFontCategory.Button);
            ConfigureText(padSubText, 9f, 13f, UIFontCategory.Default);

            if (padTitleText != null && padCostText != null)
            {
                var titleRt = padTitleText.rectTransform;
                var costRt = padCostText.rectTransform;
                titleRt.anchoredPosition = new Vector2(0, 30);
                titleRt.sizeDelta = new Vector2(190, 26);
                costRt.anchoredPosition = new Vector2(0, 0);
                costRt.sizeDelta = new Vector2(190, 28);

                if (padSubText != null)
                {
                    var subRt = padSubText.rectTransform;
                    subRt.anchoredPosition = new Vector2(0, -30);
                    subRt.sizeDelta = new Vector2(190, 22);
                }

                if (titleRt.parent is RectTransform bgRt && bgRt.sizeDelta.y < 105)
                {
                    bgRt.sizeDelta = new Vector2(Mathf.Max(bgRt.sizeDelta.x, 200), 105);
                }
            }
        }

        private void ConfigureText(TextMeshProUGUI tmp, float minSize, float maxSize, UIFontCategory category)
        {
            if (tmp == null) return;
            tmp.ApplyFont(category);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
        }

        public void RefreshSlotState(bool instant = false)
        {
            int unlockedSlots = 1;
            if (UpgradeManager.Instance != null)
            {
                unlockedSlots = UpgradeManager.Instance.GetUnlockedFishermanSlots();
            }

            // Slot 0 is always unlocked from start.
            // Slots 1..4 unlock according to FishermanTeam milestones (Lv 2, 5, 9, 14)
            bool shouldBeUnlocked = (slotIndex == 0) || (slotIndex < unlockedSlots);

            if (shouldBeUnlocked && !isUnlocked)
            {
                UnlockSlot(instant);
            }
            else if (!shouldBeUnlocked)
            {
                isUnlocked = false;
                if (fishermanObj != null) fishermanObj.SetActive(false);
                if (stoolObj != null) stoolObj.SetActive(false);

                // Show pad only for the next slot to unlock
                bool isNextToUnlock = (slotIndex == unlockedSlots);
                if (unlockPadObj != null)
                {
                    unlockPadObj.SetActive(isNextToUnlock);
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
                cost = UpgradeManager.Instance.GetCost(UpgradeType.FishermanTeam);
            }

            int reqLvl = 2;
            if (slotIndex == 1) reqLvl = 2;
            else if (slotIndex == 2) reqLvl = 5;
            else if (slotIndex == 3) reqLvl = 9;
            else if (slotIndex == 4) reqLvl = 14;

            if (padTitleText != null)
            {
                padTitleText.text = $"[+] FISHERMAN #{slotIndex + 1}";
            }

            int currentLvl = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetLevel(UpgradeType.FishermanTeam) : 0;
            bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.HasEnoughCoins(cost);
            string colorTag = canAfford ? "<color=#00FF88>" : "<color=#FF6666>";

            if (padCostText != null)
            {
                padCostText.text = $"{colorTag}{NumberFormatter.Format(cost)} G</color>";
            }

            if (padSubText != null)
            {
                padSubText.text = $"Unlocks at Pier Lv.{reqLvl}";
                padSubText.color = new Color(0.85f, 0.90f, 0.98f);
            }
        }

        public void TryPurchaseFromWorld()
        {
            if (isUnlocked) return;
            if (UpgradeManager.Instance == null) return;

            int cost = UpgradeManager.Instance.GetCost(UpgradeType.FishermanTeam);
            if (EconomyManager.Instance != null && EconomyManager.Instance.HasEnoughCoins(cost))
            {
                UpgradeManager.Instance.TryPurchaseUpgrade(UpgradeType.FishermanTeam);
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

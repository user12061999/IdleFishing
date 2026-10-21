using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.UI
{
    public class UpgradeItemSlotUI : MonoBehaviour
    {
        [Header("Upgrade Target")]
        [SerializeField] private UpgradeType upgradeType;

        [Header("UI References")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Image buttonIcon;

        private UpgradeConfig config;

        public UpgradeType Type => upgradeType;

        public void Initialize(UpgradeType type, UpgradeConfig cfg)
        {
            upgradeType = type;
            config = cfg;

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);

                if (buttonImage == null) buttonImage = buyButton.GetComponent<Image>();
                if (buttonIcon == null)
                {
                    Transform iconT = buyButton.transform.Find("Icon");
                    if (iconT != null) buttonIcon = iconT.GetComponent<Image>();
                    else
                    {
                        GameObject icGo = new GameObject("Icon");
                        icGo.transform.SetParent(buyButton.transform, false);
                        RectTransform irt = icGo.AddComponent<RectTransform>();
                        irt.anchorMin = new Vector2(0.5f, 0.5f);
                        irt.anchorMax = new Vector2(0.5f, 0.5f);
                        irt.pivot = new Vector2(0.5f, 0.5f);
                        irt.anchoredPosition = new Vector2(0f, 10f);
                        irt.sizeDelta = new Vector2(24f, 24f);
                        buttonIcon = icGo.AddComponent<Image>();
                        buttonIcon.sprite = CasualUITheme.IconGold;
                        buttonIcon.preserveAspect = true;
                        buttonIcon.raycastTarget = false;
                    }
                }

                if (costText != null)
                {
                    RectTransform crt = costText.rectTransform;
                    crt.anchorMin = new Vector2(0f, 0f);
                    crt.anchorMax = new Vector2(1f, 0f);
                    crt.pivot = new Vector2(0.5f, 0f);
                    crt.anchoredPosition = new Vector2(0f, 4f);
                    crt.sizeDelta = new Vector2(0f, 24f);
                }
            }

            if (titleText != null) titleText.ApplyFont(UIFontCategory.Header);
            if (levelText != null) levelText.ApplyFont(UIFontCategory.Numbers);
            if (costText != null) costText.ApplyFont(UIFontCategory.Button);

            Refresh();
        }

        private void OnBuyClicked()
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.TryPurchaseUpgrade(upgradeType);
                Refresh();
            }
        }

        public void Refresh()
        {
            if (UpgradeManager.Instance == null || config == null) return;

            var entry = config.GetEntry(upgradeType);
            if (entry == null) return;

            int currentLvl = UpgradeManager.Instance.GetLevel(upgradeType);
            bool isMax = currentLvl >= entry.maxLevel;
            int cost = UpgradeManager.Instance.GetCost(upgradeType);
            bool canAfford = UpgradeManager.Instance.CanUpgrade(upgradeType);

            if (titleText != null)
            {
                titleText.text = entry.title;
            }

            if (levelText != null)
            {
                levelText.text = isMax ? $"Lv. MAX" : $"Lv. {currentLvl} / {entry.maxLevel}";
            }

            if (costText != null)
            {
                costText.text = isMax ? "MAX" : $"{NumberFormatter.Format(cost)} G";
            }

            if (itemIcon != null && entry.icon != null)
            {
                itemIcon.sprite = entry.icon;
                itemIcon.preserveAspect = true;
            }

            if (buttonIcon != null)
            {
                if (isMax)
                {
                    buttonIcon.sprite = CasualUITheme.IconCrown ?? CasualUITheme.IconCheck;
                    buttonIcon.color = new Color(1f, 0.85f, 0.2f, 1f);
                }
                else
                {
                    buttonIcon.sprite = CasualUITheme.IconGold;
                    buttonIcon.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.7f);
                }
            }

            if (buttonImage != null)
            {
                if (isMax)
                {
                    buttonImage.sprite = CasualUITheme.BtnDark ?? CasualUITheme.BtnGray;
                    buttonImage.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                }
                else if (canAfford)
                {
                    buttonImage.sprite = CasualUITheme.BtnGreen;
                    buttonImage.color = Color.white;
                }
                else
                {
                    buttonImage.sprite = CasualUITheme.BtnGray;
                    buttonImage.color = new Color(0.85f, 0.85f, 0.88f, 0.95f);
                }
                buttonImage.type = Image.Type.Sliced;
            }

            if (buyButton != null)
            {
                buyButton.interactable = !isMax && canAfford;
            }
        }
    }
}

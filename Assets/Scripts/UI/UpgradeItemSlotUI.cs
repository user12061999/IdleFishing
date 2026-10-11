using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.UI
{
    public class UpgradeItemSlotUI : MonoBehaviour
    {
        [Header("Upgrade Target")]
        [SerializeField] private UpgradeType upgradeType;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button buyButton;

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
            }

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
                costText.text = isMax ? "MAX" : $"{cost:N0} G";
            }

            if (buyButton != null)
            {
                buyButton.interactable = !isMax && canAfford;
            }
        }
    }
}

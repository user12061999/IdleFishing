using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.UI
{
    public class UpgradePopupUI : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private UpgradeConfig upgradeConfig;

        [Header("Panel Elements")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private UpgradeItemSlotUI slotPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;

        private readonly List<UpgradeItemSlotUI> spawnedSlots = new List<UpgradeItemSlotUI>();

        private void Awake()
        {
            if (openButton != null)
            {
                openButton.onClick.AddListener(Open);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            BuildSlots();
        }

        private void OnEnable()
        {
            GameEvents.CoinsChanged += OnCoinsChanged;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= OnCoinsChanged;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
        }

        public void Open()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }
            RefreshAll();
        }

        public void Close()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void BuildSlots()
        {
            if (slotContainer == null || slotPrefab == null || upgradeConfig == null) return;

            foreach (var entry in upgradeConfig.Upgrades)
            {
                var slot = Instantiate(slotPrefab, slotContainer);
                slot.Initialize(entry.type, upgradeConfig);
                spawnedSlots.Add(slot);
            }
        }

        private void OnCoinsChanged(int balance)
        {
            RefreshAll();
        }

        private void OnUpgradePurchased(UpgradeType type, int level)
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (spawnedSlots[i] != null)
                {
                    spawnedSlots[i].Refresh();
                }
            }
        }
    }
}

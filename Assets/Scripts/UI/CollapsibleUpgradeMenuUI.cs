using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.UI
{
    public class CollapsibleUpgradeMenuUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UpgradeConfig upgradeConfig;

        [Header("Animation Tuning")]
        [SerializeField] private float slideSpeed = 12f;
        [SerializeField] private Vector2 collapsedPos = new Vector2(0, -730f); // Tụt xuống dưới mép màn hình
        [SerializeField] private Vector2 expandedPos = new Vector2(0, 0f);      // Trượt trồi lên từ dưới đáy

        [Header("Runtime State")]
        [SerializeField] private bool isExpanded = false;

        private RectTransform panelRect;
        private GameObject toggleButtonObj;
        private Button toggleButton;
        private TextMeshProUGUI toggleButtonText;
        private Transform contentRoot;
        private Vector2 targetPos;

        private class CardRefs
        {
            public UpgradeDataEntry entry;
            public TextMeshProUGUI levelText;
            public TextMeshProUGUI descText;
            public TextMeshProUGUI costText;
            public Button buyBtn;
            public Image buyBtnImage;
        }

        private readonly List<CardRefs> cards = new List<CardRefs>();
        private TextMeshProUGUI headerTitleText;

        public bool IsExpanded => isExpanded;

        private void Awake()
        {
            if (upgradeConfig == null)
            {
                upgradeConfig = Resources.Load<UpgradeConfig>("DefaultUpgradeConfig");
            }
        }

        private void Start()
        {
            BuildProceduralBottomMenu();
            targetPos = isExpanded ? expandedPos : collapsedPos;
            if (panelRect != null)
            {
                panelRect.anchoredPosition = targetPos;
            }
            RefreshAllCards();
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

        private void Update()
        {
            if (panelRect != null)
            {
                targetPos = isExpanded ? expandedPos : collapsedPos;
                panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
            }

            // Animate toggle button visibility
            if (toggleButtonObj != null)
            {
                toggleButtonObj.SetActive(!isExpanded);
            }
        }

        public void ToggleMenu()
        {
            if (isExpanded)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        public void OpenMenu()
        {
            isExpanded = true;
            RefreshAllCards();
        }

        public void CloseMenu()
        {
            isExpanded = false;
        }

        private void OnCoinsChanged(int newBalance)
        {
            if (isExpanded)
            {
                RefreshAllCards();
            }
        }

        private void OnUpgradePurchased(UpgradeType type, int level)
        {
            RefreshAllCards();
        }

        public void RefreshAllCards()
        {
            if (UpgradeManager.Instance == null || upgradeConfig == null) return;

            if (headerTitleText != null)
            {
                int totalUpgrades = UpgradeManager.Instance.GetTotalUpgradeLevelsForWorld(UpgradeManager.Instance.CurrentWorldId);
                headerTitleText.text = $"UPGRADES <color=#FFD700>({totalUpgrades} Levels)</color>";
            }

            for (int i = 0; i < cards.Count; i++)
            {
                CardRefs card = cards[i];
                if (card == null || card.entry == null) continue;

                int currentLvl = UpgradeManager.Instance.GetLevel(card.entry.type);
                bool isMax = currentLvl >= card.entry.maxLevel;
                int cost = UpgradeManager.Instance.GetCost(card.entry.type);
                bool canAfford = UpgradeManager.Instance.CanUpgrade(card.entry.type);
                float currentVal = UpgradeManager.Instance.GetValue(card.entry.type);

                if (card.levelText != null)
                {
                    if (card.entry.maxLevel >= 999)
                    {
                        card.levelText.text = $"Lv. {currentLvl}";
                    }
                    else
                    {
                        card.levelText.text = isMax ? "<color=#FFD700>MAX</color>" : $"Lv. {currentLvl}/{card.entry.maxLevel}";
                    }
                }

                if (card.descText != null)
                {
                    if (card.entry.type == UpgradeType.FishermanSlots)
                    {
                        card.descText.text = $"+1 Fisherman (Current: {currentVal:0}/5)";
                    }
                    else if (card.entry.type == UpgradeType.HireWrangler)
                    {
                        card.descText.text = currentVal == 0
                            ? "Hire auto fish wrangler"
                            : $"+1 Wrangler (Current: {currentVal:0}/4)";
                    }
                    else
                    {
                        card.descText.text = $"{card.entry.description} (x{currentVal:F1})";
                    }
                }

                if (card.costText != null)
                {
                    card.costText.text = isMax ? "MAX" : $"{cost:N0} G";
                }

                if (card.buyBtn != null)
                {
                    card.buyBtn.interactable = !isMax && canAfford;
                }

                if (card.buyBtnImage != null)
                {
                    if (isMax)
                    {
                        card.buyBtnImage.color = new Color(0.35f, 0.35f, 0.38f, 0.85f);
                    }
                    else if (canAfford)
                    {
                        card.buyBtnImage.color = new Color(0.2f, 0.78f, 0.38f, 1f); // Xanh lá nổi bật
                    }
                    else
                    {
                        card.buyBtnImage.color = new Color(0.55f, 0.28f, 0.28f, 0.8f); // Đỏ tối khi thiếu tiền
                    }
                }
            }
        }

        private void BuildProceduralBottomMenu()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Toggle Button (Ghim ở Đáy Màn Hình - Bottom Center)
            toggleButtonObj = new GameObject("Upgrade_BottomToggleButton");
            toggleButtonObj.transform.SetParent(transform, false);
            Image btnImg = toggleButtonObj.AddComponent<Image>();
            btnImg.sprite = whiteSp;
            btnImg.color = new Color(0.96f, 0.72f, 0.15f, 0.95f); // Vàng kim Amber
            toggleButton = toggleButtonObj.AddComponent<Button>();
            toggleButton.targetGraphic = btnImg;

            RectTransform btnRect = toggleButtonObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0, 15);
            btnRect.sizeDelta = new Vector2(300, 64);

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(toggleButtonObj.transform, false);
            toggleButtonText = btnTxtObj.AddComponent<TextMeshProUGUI>();
            toggleButtonText.text = "UPGRADES ▲";
            toggleButtonText.fontSize = 24;
            toggleButtonText.fontStyle = FontStyles.Bold;
            toggleButtonText.alignment = TextAlignmentOptions.Center;
            toggleButtonText.color = new Color(0.12f, 0.12f, 0.14f);
            toggleButtonText.raycastTarget = false;
            btnTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(290, 56);

            toggleButton.onClick.AddListener(OpenMenu);

            // 2. Bottom Drawer Panel (Trượt Từ Dưới Lên)
            GameObject panelObj = new GameObject("Upgrade_BottomDrawerPanel");
            panelObj.transform.SetParent(transform, false);
            panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(980, 710);
            panelRect.anchoredPosition = collapsedPos;

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.sprite = whiteSp;
            panelBg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f); // Dark Slate

            // 3. Header Panel
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelObj.transform, false);
            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0, 64);
            headerRect.anchoredPosition = Vector2.zero;

            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.sprite = whiteSp;
            headerBg.color = new Color(0.16f, 0.20f, 0.26f);

            GameObject titleTextObj = new GameObject("Title");
            titleTextObj.transform.SetParent(headerObj.transform, false);
            var titleTxt = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "UPGRADES";
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Left;
            titleTxt.color = new Color(1f, 0.85f, 0.2f);
            titleTxt.raycastTarget = false;
            headerTitleText = titleTxt;
            RectTransform tRect = titleTextObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.5f);
            tRect.anchorMax = new Vector2(0f, 0.5f);
            tRect.pivot = new Vector2(0f, 0.5f);
            tRect.anchoredPosition = new Vector2(20, 0);
            tRect.sizeDelta = new Vector2(500, 44);

            // Collapse Button in Header (Thu Gọn ▼)
            GameObject closeBtnObj = new GameObject("CollapseButton");
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.sprite = whiteSp;
            closeImg.color = new Color(0.85f, 0.32f, 0.32f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            RectTransform cRect = closeBtnObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1f, 0.5f);
            cRect.anchorMax = new Vector2(1f, 0.5f);
            cRect.pivot = new Vector2(1f, 0.5f);
            cRect.anchoredPosition = new Vector2(-15, 0);
            cRect.sizeDelta = new Vector2(150, 46);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var xTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
            xTxt.text = "COLLAPSE ▼";
            xTxt.fontSize = 18;
            xTxt.fontStyle = FontStyles.Bold;
            xTxt.alignment = TextAlignmentOptions.Center;
            xTxt.color = Color.white;
            xTxt.raycastTarget = false;
            closeTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 40);
            closeBtn.onClick.AddListener(CloseMenu);

            // 4. Scroll View for Upgrade Cards
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(14, 14);
            scrollRect.offsetMax = new Vector2(-14, -72);

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            RectTransform viewRect = viewportObj.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.sizeDelta = Vector2.zero;
            viewportObj.AddComponent<RectMask2D>();

            scroll.viewport = viewRect;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contRect = contentObj.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0f, 1f);
            contRect.anchorMax = new Vector2(1f, 1f);
            contRect.pivot = new Vector2(0.5f, 1f);
            contRect.sizeDelta = new Vector2(0, 100);

            VerticalLayoutGroup vLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 10;
            vLayout.padding = new RectOffset(4, 4, 8, 12);
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;

            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contRect;
            contentRoot = contentObj.transform;

            // 5. Build Cards from Config
            if (upgradeConfig != null && upgradeConfig.Upgrades != null)
            {
                foreach (var entry in upgradeConfig.Upgrades)
                {
                    CreateUpgradeCard(entry, whiteSp);
                }
            }
        }

        private void CreateUpgradeCard(UpgradeDataEntry entry, Sprite whiteSp)
        {
            GameObject cardObj = new GameObject($"Card_{entry.type}");
            cardObj.transform.SetParent(contentRoot, false);
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(0, 92);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = whiteSp;
            cardBg.color = new Color(0.18f, 0.20f, 0.25f, 0.95f);

            // Card Title & Level
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = entry.title;
            titleTxt.fontSize = 22;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(0.56f, 1f);
            tRect.pivot = new Vector2(0f, 1f);
            tRect.anchoredPosition = new Vector2(16, -10);
            tRect.sizeDelta = new Vector2(0, 30);

            // Level Text
            GameObject lvlObj = new GameObject("Level");
            lvlObj.transform.SetParent(cardObj.transform, false);
            var lvlTxt = lvlObj.AddComponent<TextMeshProUGUI>();
            lvlTxt.text = "Lv. 0 / 10";
            lvlTxt.fontSize = 20;
            lvlTxt.fontStyle = FontStyles.Bold;
            lvlTxt.color = new Color(1f, 0.85f, 0.2f);
            lvlTxt.alignment = TextAlignmentOptions.Right;
            lvlTxt.raycastTarget = false;
            RectTransform lRect = lvlObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.56f, 1f);
            lRect.anchorMax = new Vector2(0.78f, 1f);
            lRect.pivot = new Vector2(1f, 1f);
            lRect.anchoredPosition = new Vector2(0, -10);
            lRect.sizeDelta = new Vector2(0, 30);

            // Description / Bonus
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(cardObj.transform, false);
            var descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = entry.description;
            descTxt.fontSize = 17;
            descTxt.color = new Color(0.82f, 0.85f, 0.88f);
            descTxt.raycastTarget = false;
            RectTransform dRect = descObj.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0f, 0f);
            dRect.anchorMax = new Vector2(0.76f, 0f);
            dRect.pivot = new Vector2(0f, 0f);
            dRect.anchoredPosition = new Vector2(16, 12);
            dRect.sizeDelta = new Vector2(0, 34);

            // Upgrade Button
            GameObject buyBtnObj = new GameObject("BuyButton");
            buyBtnObj.transform.SetParent(cardObj.transform, false);
            Image buyImg = buyBtnObj.AddComponent<Image>();
            buyImg.sprite = whiteSp;
            buyImg.color = new Color(0.2f, 0.78f, 0.38f);
            Button buyBtn = buyBtnObj.AddComponent<Button>();
            buyBtn.targetGraphic = buyImg;

            RectTransform bRect = buyBtnObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(1f, 0.5f);
            bRect.anchorMax = new Vector2(1f, 0.5f);
            bRect.pivot = new Vector2(1f, 0.5f);
            bRect.anchoredPosition = new Vector2(-16, 0);
            bRect.sizeDelta = new Vector2(160, 68);

            GameObject costObj = new GameObject("CostText");
            costObj.transform.SetParent(buyBtnObj.transform, false);
            var costTxt = costObj.AddComponent<TextMeshProUGUI>();
            costTxt.text = "100 G";
            costTxt.fontSize = 22;
            costTxt.fontStyle = FontStyles.Bold;
            costTxt.alignment = TextAlignmentOptions.Center;
            costTxt.color = Color.white;
            costTxt.raycastTarget = false;
            costObj.GetComponent<RectTransform>().sizeDelta = new Vector2(155, 60);

            // Click listener
            UpgradeType t = entry.type;
            buyBtn.onClick.AddListener(() =>
            {
                if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryPurchaseUpgrade(t))
                {
                    RefreshAllCards();
                }
            });

            cards.Add(new CardRefs
            {
                entry = entry,
                levelText = lvlTxt,
                descText = descTxt,
                costText = costTxt,
                buyBtn = buyBtn,
                buyBtnImage = buyImg
            });
        }
    }
}

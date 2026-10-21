using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.UI
{
    /// <summary>
    /// Thanh Dock Nâng Cấp Tinh Gọn 1-Chạm (Streamlined 1-Touch Bottom Upgrade Dock).
    /// Tối giản toàn bộ hệ thống nâng cấp thành 3 Trụ Cột cốt lõi:
    /// 1. [🎣 ĐỘI CẦN THỦ] - Tăng tốc độ câu & tự động mở thêm cần thủ trên bến tàu.
    /// 2. [💪 ĐỘI THỢ SĂN] - Tăng sát thương, HP, tốc chạy & chiêu mộ thêm thợ kéo cá.
    /// 3. [💎 GIÁ TRỊ CÁ] - Tăng trực tiếp giá bán và vàng thu được từ tất cả cá.
    /// </summary>
    public class CollapsibleUpgradeMenuUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UpgradeConfig upgradeConfig;

        [Header("Animation Tuning")]
        [SerializeField] private float slideSpeed = 14f;
        [SerializeField] private Vector2 collapsedPos = new Vector2(0, -190f); // Ẩn tụt xuống đáy
        [SerializeField] private Vector2 expandedPos = new Vector2(0, 10f);    // Trồi lên 10px mép đáy

        [Header("Runtime State")]
        [SerializeField] private bool isExpanded = true; // Mặc định mở sẵn để 1-chạm nâng cấp

        private RectTransform dockRect;
        private Button togglePillButton;
        private Image togglePillIcon;
        private TextMeshProUGUI togglePillText;
        private Vector2 targetPos;

        private class DockCardRefs
        {
            public UpgradeType type;
            public RectTransform cardRect;
            public Image pillarIconImage;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI levelText;
            public TextMeshProUGUI statsText;
            public Button buyButton;
            public Image buyButtonImage;
            public Image buyButtonIcon;
            public TextMeshProUGUI costText;
        }

        private readonly List<DockCardRefs> cardList = new List<DockCardRefs>();

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
            BuildProceduralBottomDock();
            targetPos = isExpanded ? expandedPos : collapsedPos;
            if (dockRect != null)
            {
                dockRect.anchoredPosition = targetPos;
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
            if (dockRect != null)
            {
                targetPos = isExpanded ? expandedPos : collapsedPos;
                dockRect.anchoredPosition = Vector2.Lerp(dockRect.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
            }
        }

        public void ToggleMenu()
        {
            isExpanded = !isExpanded;
            UpdateTogglePillText();
            if (isExpanded)
            {
                RefreshAllCards();
            }
        }

        public void OpenMenu()
        {
            isExpanded = true;
            UpdateTogglePillText();
            RefreshAllCards();
        }

        public void CloseMenu()
        {
            isExpanded = false;
            UpdateTogglePillText();
        }

        private void UpdateTogglePillText()
        {
            if (togglePillText != null)
            {
                togglePillText.text = isExpanded ? "COLLAPSE" : "UPGRADES";
            }
            if (togglePillIcon != null)
            {
                togglePillIcon.rectTransform.localEulerAngles = isExpanded ? new Vector3(0, 0, 180f) : Vector3.zero;
            }
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

            for (int i = 0; i < cardList.Count; i++)
            {
                DockCardRefs card = cardList[i];
                if (card == null) continue;

                var entry = upgradeConfig.GetEntry(card.type);
                int currentLvl = UpgradeManager.Instance.GetLevel(card.type);
                int maxLvl = entry != null ? entry.maxLevel : 50;
                bool isMax = currentLvl >= maxLvl;
                int cost = UpgradeManager.Instance.GetCost(card.type);
                bool canAfford = UpgradeManager.Instance.CanUpgrade(card.type);
                float currentVal = UpgradeManager.Instance.GetValue(card.type);

                // 1. Level text
                if (card.levelText != null)
                {
                    card.levelText.text = isMax ? "<color=#FFD700>MAX</color>" : $"<color=#FFD700>Lv.{currentLvl}</color>";
                }

                // 2. Stats summary text
                if (card.statsText != null)
                {
                    if (card.type == UpgradeType.FishermanTeam)
                    {
                        int slots = UpgradeManager.Instance.GetUnlockedFishermanSlots();
                        int nextReq = UpgradeManager.Instance.GetNextFishermanSlotRequiredLevel();
                        int speedPct = Mathf.RoundToInt((currentVal - 1f) * 100f);
                        if (speedPct <= 0) speedPct = 0;

                        if (nextReq > 0)
                        {
                            card.statsText.text = $"Speed +{speedPct}% | {slots}/5 Rods <color=#FFB800>(Rod #{slots + 1} at Lv.{nextReq})</color>";
                        }
                        else
                        {
                            card.statsText.text = $"Speed +{speedPct}% | <color=#00FF88>5/5 MAX RODS</color>";
                        }
                    }
                    else if (card.type == UpgradeType.WranglerTeam)
                    {
                        int wranglers = UpgradeManager.Instance.GetUnlockedWranglerCount();
                        int nextReq = UpgradeManager.Instance.GetNextWranglerRequiredLevel();
                        int dmgPct = Mathf.RoundToInt((currentVal - 1f) * 100f);
                        if (dmgPct <= 0) dmgPct = 0;

                        if (nextReq > 0)
                        {
                            card.statsText.text = $"Power +{dmgPct}% | {wranglers}/4 Wranglers <color=#FFB800>(Wrangler #{wranglers + 1} at Lv.{nextReq})</color>";
                        }
                        else
                        {
                            card.statsText.text = $"Power +{dmgPct}% | <color=#00FF88>4/4 MAX WRANGLERS</color>";
                        }
                    }
                    else // FishValue
                    {
                        int valPct = Mathf.RoundToInt((currentVal - 1f) * 100f);
                        if (valPct <= 0) valPct = 0;
                        card.statsText.text = $"Sell Price x{currentVal:F2} <color=#00FF88>(+{valPct}% Gold)</color>";
                    }
                }

                // 3. Buy Button, Icon & Cost
                if (card.costText != null)
                {
                    card.costText.text = isMax ? "MAX" : $"{NumberFormatter.Format(cost)} G";
                }

                if (card.buyButtonIcon != null)
                {
                    if (isMax)
                    {
                        card.buyButtonIcon.sprite = CasualUITheme.IconCrown ?? CasualUITheme.IconCheck;
                        card.buyButtonIcon.color = new Color(1f, 0.85f, 0.2f, 1f);
                    }
                    else
                    {
                        card.buyButtonIcon.sprite = CasualUITheme.IconGold;
                        card.buyButtonIcon.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.7f);
                    }
                }

                if (card.buyButton != null)
                {
                    card.buyButton.interactable = !isMax && canAfford;
                }

                if (card.buyButtonImage != null)
                {
                    if (isMax)
                    {
                        card.buyButtonImage.sprite = CasualUITheme.BtnDark ?? CasualUITheme.BtnGray;
                        card.buyButtonImage.type = Image.Type.Sliced;
                        card.buyButtonImage.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                        if (card.costText != null) card.costText.color = new Color(0.75f, 0.75f, 0.80f);
                    }
                    else if (canAfford)
                    {
                        card.buyButtonImage.sprite = CasualUITheme.BtnGreen;
                        card.buyButtonImage.type = Image.Type.Sliced;
                        card.buyButtonImage.color = Color.white;
                        if (card.costText != null) card.costText.color = Color.white;
                    }
                    else
                    {
                        card.buyButtonImage.sprite = CasualUITheme.BtnGray;
                        card.buyButtonImage.type = Image.Type.Sliced;
                        card.buyButtonImage.color = new Color(0.85f, 0.85f, 0.88f, 0.95f);
                        if (card.costText != null) card.costText.color = new Color(0.35f, 0.38f, 0.45f);
                    }
                }
            }
        }

        private void OnBuyPillarClicked(UpgradeType type, DockCardRefs card)
        {
            if (UpgradeManager.Instance == null) return;

            bool success = UpgradeManager.Instance.TryPurchaseUpgrade(type);
            if (success)
            {
                // Juice: Button spring punch animation
                if (card != null && card.cardRect != null && gameObject.activeInHierarchy)
                {
                    StartCoroutine(PunchRoutine(card.cardRect));
                }

                // Floating damage gold text
                if (FloatingDamageManager.Instance != null && card != null && card.cardRect != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        new Vector3(0f, 1.5f, 0f),
                        0,
                        DamageStyle.GoldBonus,
                        $"LEVEL UP!"
                    );
                }

                RefreshAllCards();
            }
        }

        private IEnumerator PunchRoutine(RectTransform rect)
        {
            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.20f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                rect.localScale = baseScale * scale;
                yield return null;
            }
            rect.localScale = baseScale;
        }

        private void BuildProceduralBottomDock()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();
            Sprite dockBgSp = CasualUITheme.PopupNavyBg ?? CasualUITheme.CardRound12 ?? whiteSp;

            // 1. Root Container của Thanh Dock
            GameObject dockObj = new GameObject("Streamlined_UpgradeDock");
            dockObj.transform.SetParent(transform, false);
            dockRect = dockObj.AddComponent<RectTransform>();
            dockRect.anchorMin = new Vector2(0.5f, 0f);
            dockRect.anchorMax = new Vector2(0.5f, 0f);
            dockRect.pivot = new Vector2(0.5f, 0f);
            dockRect.sizeDelta = new Vector2(1040f, 184f);
            dockRect.anchoredPosition = expandedPos;

            // Nền Dock Glassmorphism sang trọng với Sliced Popup Frame
            Image dockBg = dockObj.AddComponent<Image>();
            dockBg.sprite = dockBgSp;
            dockBg.type = (dockBgSp.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            dockBg.color = (dockBgSp == whiteSp) ? new Color(0.07f, 0.10f, 0.15f, 0.94f) : Color.white;

            // Viền Accent vàng kim ở mép trên dock
            GameObject topStripe = new GameObject("TopAccentStripe");
            topStripe.transform.SetParent(dockObj.transform, false);
            RectTransform tsRt = topStripe.AddComponent<RectTransform>();
            tsRt.anchorMin = new Vector2(0f, 1f);
            tsRt.anchorMax = new Vector2(1f, 1f);
            tsRt.pivot = new Vector2(0.5f, 1f);
            tsRt.sizeDelta = new Vector2(-24f, 4f);
            tsRt.anchoredPosition = new Vector2(0f, -4f);
            Image tsImg = topStripe.AddComponent<Image>();
            Sprite lineSp = CasualUITheme.PopupLine ?? whiteSp;
            tsImg.sprite = lineSp;
            tsImg.type = (lineSp.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            tsImg.color = (lineSp == whiteSp) ? new Color(0.96f, 0.72f, 0.15f, 0.90f) : Color.white;
            tsImg.raycastTarget = false;

            // 2. Tab Thu Gọn Pill Button (Gắn ngay trên đỉnh dock)
            GameObject pillObj = new GameObject("Dock_TogglePill");
            pillObj.transform.SetParent(dockObj.transform, false);
            RectTransform pillRt = pillObj.AddComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0.5f, 1f);
            pillRt.anchorMax = new Vector2(0.5f, 1f);
            pillRt.pivot = new Vector2(0.5f, 0f);
            pillRt.sizeDelta = new Vector2(270f, 48f);
            pillRt.anchoredPosition = new Vector2(0f, 0f);

            Image pillBg = pillObj.AddComponent<Image>();
            togglePillButton = pillObj.AddComponent<Button>();
            CasualUITheme.StyleButton(togglePillButton, pillBg, CasualBtnStyle.SquareNavy);
            togglePillButton.onClick.AddListener(ToggleMenu);

            // Icon Chevron / Arrow inside Pill Button
            GameObject pIconObj = new GameObject("PillIcon");
            pIconObj.transform.SetParent(pillObj.transform, false);
            RectTransform piRt = pIconObj.AddComponent<RectTransform>();
            piRt.anchorMin = new Vector2(0.5f, 0.5f);
            piRt.anchorMax = new Vector2(0.5f, 0.5f);
            piRt.pivot = new Vector2(0.5f, 0.5f);
            piRt.anchoredPosition = new Vector2(0f, 10f);
            piRt.sizeDelta = new Vector2(20f, 20f);
            togglePillIcon = pIconObj.AddComponent<Image>();
            togglePillIcon.sprite = CasualUITheme.IconArrowUp;
            togglePillIcon.preserveAspect = true;
            togglePillIcon.raycastTarget = false;
            togglePillIcon.color = new Color(0.95f, 0.85f, 0.35f);

            // Text at bottom of Pill Button
            GameObject pillTxtObj = new GameObject("PillText");
            pillTxtObj.transform.SetParent(pillObj.transform, false);
            togglePillText = pillTxtObj.AddComponent<TextMeshProUGUI>();
            togglePillText.fontSize = 20;
            togglePillText.fontStyle = FontStyles.Bold;
            togglePillText.alignment = TextAlignmentOptions.Center;
            togglePillText.color = new Color(0.95f, 0.95f, 0.98f);
            togglePillText.raycastTarget = false;
            togglePillText.ApplyFont(UIFontCategory.Button);
            RectTransform ptRt = pillTxtObj.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0f, 0f);
            ptRt.anchorMax = new Vector2(1f, 0f);
            ptRt.pivot = new Vector2(0.5f, 0f);
            ptRt.anchoredPosition = new Vector2(0f, 4f);
            ptRt.sizeDelta = new Vector2(0f, 24f);
            UpdateTogglePillText();

            // 3. Tạo 3 Thẻ Nâng Cấp Nằm Ngang (3 Core Pillars)
            var pillars = new[]
            {
                new { type = UpgradeType.FishermanTeam, title = "FISHERMEN", icon = CasualUITheme.IconUpgradeFisherman ?? CasualUITheme.IconAnchor, xPos = -345f },
                new { type = UpgradeType.WranglerTeam, title = "WRANGLERS", icon = CasualUITheme.IconUpgradeWrangler ?? CasualUITheme.IconArrowUp, xPos = 0f },
                new { type = UpgradeType.FishValue, title = "FISH VALUE", icon = CasualUITheme.IconUpgradeFishValue ?? CasualUITheme.IconGold, xPos = 345f }
            };

            cardList.Clear();

            for (int i = 0; i < pillars.Length; i++)
            {
                var p = pillars[i];
                DockCardRefs card = CreatePillarCard(dockObj.transform, whiteSp, p.type, p.title, p.icon, p.xPos);
                cardList.Add(card);
            }
        }

        private DockCardRefs CreatePillarCard(Transform parent, Sprite whiteSp, UpgradeType type, string title, Sprite pillarIcon, float xPos)
        {
            DockCardRefs card = new DockCardRefs();
            card.type = type;

            // Root Card
            GameObject cardObj = new GameObject($"Card_{type}");
            cardObj.transform.SetParent(parent, false);
            card.cardRect = cardObj.AddComponent<RectTransform>();
            card.cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            card.cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            card.cardRect.pivot = new Vector2(0.5f, 0.5f);
            card.cardRect.sizeDelta = new Vector2(334f, 172f);
            card.cardRect.anchoredPosition = new Vector2(xPos, -2f);

            Image cardBg = cardObj.AddComponent<Image>();
            CasualUITheme.StyleCard(cardBg, new Color(0.08f, 0.12f, 0.20f, 0.95f));

            // Top Row: Icon + Title + Level
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.sizeDelta = new Vector2(0f, 52f);
            hRt.anchoredPosition = new Vector2(0f, -6f);

            // Dedicated Pillar Icon (Large & Vibrant at top-left)
            float textOffsetLeft = 10f;
            if (pillarIcon != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(headerObj.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = new Vector2(30f, 0f);
                iconRt.sizeDelta = new Vector2(48f, 48f);
                card.pillarIconImage = iconObj.AddComponent<Image>();
                card.pillarIconImage.sprite = pillarIcon;
                card.pillarIconImage.preserveAspect = true;
                card.pillarIconImage.raycastTarget = false;
                textOffsetLeft = 60f;
            }

            // Title Text (Left)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            card.titleText = titleObj.AddComponent<TextMeshProUGUI>();
            card.titleText.text = title;
            card.titleText.fontSize = 24f;
            card.titleText.fontStyle = FontStyles.Bold;
            card.titleText.alignment = TextAlignmentOptions.MidlineLeft;
            card.titleText.color = Color.white;
            card.titleText.raycastTarget = false;
            card.titleText.ApplyFont(UIFontCategory.Header);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(0.68f, 1f);
            tRt.anchoredPosition = new Vector2(textOffsetLeft, 0f);
            tRt.sizeDelta = Vector2.zero;

            // Level Text (Right)
            GameObject lvlObj = new GameObject("Level");
            lvlObj.transform.SetParent(headerObj.transform, false);
            card.levelText = lvlObj.AddComponent<TextMeshProUGUI>();
            card.levelText.text = "Lv. 0";
            card.levelText.fontSize = 24f;
            card.levelText.fontStyle = FontStyles.Bold;
            card.levelText.alignment = TextAlignmentOptions.MidlineRight;
            card.levelText.color = new Color(1f, 0.84f, 0f);
            card.levelText.raycastTarget = false;
            card.levelText.ApplyFont(UIFontCategory.Numbers);
            RectTransform lRt = lvlObj.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.68f, 0f);
            lRt.anchorMax = new Vector2(1f, 1f);
            lRt.anchoredPosition = new Vector2(-12f, 0f);
            lRt.sizeDelta = Vector2.zero;

            // Middle: Stats Summary Text
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(cardObj.transform, false);
            card.statsText = statsObj.AddComponent<TextMeshProUGUI>();
            card.statsText.fontSize = 20f;
            card.statsText.fontStyle = FontStyles.Bold;
            card.statsText.alignment = TextAlignmentOptions.MidlineLeft;
            card.statsText.color = new Color(0.85f, 0.90f, 0.98f);
            card.statsText.raycastTarget = false;
            card.statsText.ApplyFont(UIFontCategory.Default);
            RectTransform sRt = statsObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 1f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.sizeDelta = new Vector2(-22f, 38f);
            sRt.anchoredPosition = new Vector2(0f, -60f);

            // Bottom: 1-Touch Buy Button (Icon Inside at Top, Text at Bottom)
            GameObject btnObj = new GameObject("BuyButton");
            btnObj.transform.SetParent(cardObj.transform, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.sizeDelta = new Vector2(314f, 62f);
            bRt.anchoredPosition = new Vector2(0f, 8f);

            card.buyButtonImage = btnObj.AddComponent<Image>();
            card.buyButton = btnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(card.buyButton, card.buyButtonImage, CasualBtnStyle.Green);
            card.buyButton.onClick.AddListener(() => OnBuyPillarClicked(type, card));

            // Icon Inside Button (Center-Top)
            GameObject bIconObj = new GameObject("ButtonIcon");
            bIconObj.transform.SetParent(btnObj.transform, false);
            RectTransform biRt = bIconObj.AddComponent<RectTransform>();
            biRt.anchorMin = new Vector2(0.5f, 0.5f);
            biRt.anchorMax = new Vector2(0.5f, 0.5f);
            biRt.pivot = new Vector2(0.5f, 0.5f);
            biRt.anchoredPosition = new Vector2(0f, 13f);
            biRt.sizeDelta = new Vector2(24f, 24f);
            card.buyButtonIcon = bIconObj.AddComponent<Image>();
            card.buyButtonIcon.sprite = CasualUITheme.IconGold;
            card.buyButtonIcon.preserveAspect = true;
            card.buyButtonIcon.raycastTarget = false;

            // Button Cost Text (Anchored cleanly at bottom of button)
            GameObject costObj = new GameObject("CostText");
            costObj.transform.SetParent(btnObj.transform, false);
            card.costText = costObj.AddComponent<TextMeshProUGUI>();
            card.costText.fontSize = 24f;
            card.costText.fontStyle = FontStyles.Bold;
            card.costText.alignment = TextAlignmentOptions.Center;
            card.costText.color = Color.white;
            card.costText.raycastTarget = false;
            card.costText.ApplyFont(UIFontCategory.Button);
            RectTransform cRt = costObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0f);
            cRt.anchorMax = new Vector2(1f, 0f);
            cRt.pivot = new Vector2(0.5f, 0f);
            cRt.anchoredPosition = new Vector2(0f, 4f);
            cRt.sizeDelta = new Vector2(0f, 26f);

            return card;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Aquarium;
using IdleFishing.Audio;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.UI
{
    public class AquariumPopupUI : MonoBehaviour
    {
        public static AquariumPopupUI Instance { get; private set; }

        private GameObject popupRoot;
        private int currentTab = 0; // 0: Tank & Tourism, 1: Bestiary

        // Tab Buttons
        private Button tabTankBtn;
        private Image tabTankBg;
        private Button tabBestiaryBtn;
        private Image tabBestiaryBg;

        // Tab Containers
        private GameObject tankTabContainer;
        private GameObject bestiaryTabContainer;

        // Tab 1 Elements
        private TextMeshProUGUI ticketRateText;
        private TextMeshProUGUI ticketPendingText;
        private Button claimTicketBtn;
        private TextMeshProUGUI claimTicketBtnText;
        private Button upgradeCapacityBtn;
        private TextMeshProUGUI upgradeCapBtnText;
        private Button upgradeDecorBtn;
        private TextMeshProUGUI upgradeDecorBtnText;
        private Transform fishCardsContainer;

        // Tab 2 Elements
        private TextMeshProUGUI bestiaryProgressText;
        private TextMeshProUGUI bestiaryBuffSummaryText;
        private Transform bestiaryCardsContainer;
        private ScrollRect bestiaryScrollRect;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (AquariumFishPreviewManager.Instance == null)
            {
                gameObject.AddComponent<AquariumFishPreviewManager>();
            }

            BuildProceduralPopup();
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void Start()
        {
            WireHUDButton();
        }

        public void WireHUDButton()
        {
            var btnObj = GameObject.Find("Btn_Aquarium");
            if (btnObj != null)
            {
                var btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    var img = btnObj.GetComponent<Image>();
                    if (img != null) btn.targetGraphic = img;

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(Toggle);
                }
                var txts = btnObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in txts) t.raycastTarget = false;
            }
        }

        private void OnEnable()
        {
            GameEvents.AquariumFishAdded += OnFishChanged;
            GameEvents.AquariumFishRemoved += OnFishRemoved;
            GameEvents.AquariumTicketClaimed += OnTicketClaimed;
            GameEvents.BestiarySpeciesDiscovered += OnSpeciesDiscovered;
            GameEvents.AquariumUpgraded += OnAquariumUpgraded;

            if (AquariumManager.Instance != null)
            {
                AquariumManager.Instance.OnAquariumUpdated += HandleAquariumUpdated;
            }
        }

        private void OnDisable()
        {
            GameEvents.AquariumFishAdded -= OnFishChanged;
            GameEvents.AquariumFishRemoved -= OnFishRemoved;
            GameEvents.AquariumTicketClaimed -= OnTicketClaimed;
            GameEvents.BestiarySpeciesDiscovered -= OnSpeciesDiscovered;
            GameEvents.AquariumUpgraded -= OnAquariumUpgraded;

            if (AquariumManager.Instance != null)
            {
                AquariumManager.Instance.OnAquariumUpdated -= HandleAquariumUpdated;
            }
        }

        private void HandleAquariumUpdated() => RefreshCurrentTab();
        private void OnFishChanged(FishChainData f, bool isBoss) => RefreshCurrentTab();
        private void OnFishRemoved(FishChainData f) => RefreshCurrentTab();
        private void OnTicketClaimed(int amount) => RefreshCurrentTab();
        private void OnSpeciesDiscovered(FishChainData f) => RefreshCurrentTab();
        private void OnAquariumUpgraded(int cap, int decor) => RefreshCurrentTab();

        private void Update()
        {
            if (popupRoot != null && popupRoot.activeSelf && currentTab == 0)
            {
                UpdateLiveTicketDisplay();
            }
        }

        public void Open(int initialTab = 0)
        {
            if (popupRoot == null) BuildProceduralPopup();
            if (popupRoot != null)
            {
                popupRoot.transform.SetAsLastSibling();
                popupRoot.SetActive(true);
            }
            SwitchTab(initialTab);
        }

        public void Close()
        {
            if (AquariumFishPreviewManager.Instance != null)
            {
                AquariumFishPreviewManager.Instance.ClearAll();
            }
            if (popupRoot != null) popupRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (popupRoot == null) BuildProceduralPopup();
            if (popupRoot.activeSelf) Close();
            else Open();
        }

        private void SwitchTab(int tabIndex)
        {
            currentTab = tabIndex;

            if (AquariumFishPreviewManager.Instance != null)
            {
                AquariumFishPreviewManager.Instance.ClearAll();
            }

            if (tankTabContainer != null) tankTabContainer.SetActive(currentTab == 0);
            if (bestiaryTabContainer != null) bestiaryTabContainer.SetActive(currentTab == 1);

            // Tab Buttons appearance
            if (tabTankBg != null)
            {
                CasualUITheme.StyleButton(tabTankBtn, tabTankBg, currentTab == 0 ? CasualBtnStyle.Blue : CasualBtnStyle.Dark);
            }
            if (tabBestiaryBg != null)
            {
                CasualUITheme.StyleButton(tabBestiaryBtn, tabBestiaryBg, currentTab == 1 ? CasualBtnStyle.Blue : CasualBtnStyle.Dark);
            }

            if (currentTab == 1 && bestiaryScrollRect != null)
            {
                bestiaryScrollRect.verticalNormalizedPosition = 1f;
            }

            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            if (popupRoot == null || !popupRoot.activeSelf) return;

            if (currentTab == 0)
            {
                RefreshTankTab();
            }
            else
            {
                RefreshBestiaryTab();
            }
        }

        private void UpdateLiveTicketDisplay()
        {
            if (AquariumManager.Instance == null) return;

            float rate = AquariumManager.Instance.GetCurrentIncomeRatePerSecond();
            float pending = AquariumManager.Instance.AccumulatedRevenue;

            if (ticketRateText != null)
            {
                ticketRateText.text = $"Revenue: <color=#FFE600><b>+{rate:0.0} G/sec</b></color>";
            }

            if (ticketPendingText != null)
            {
                ticketPendingText.text = $"Pending Tickets: <color=#2ECC71><b>+{NumberFormatter.Format(Mathf.FloorToInt(pending))} G</b></color>";
            }

            if (claimTicketBtn != null)
            {
                bool canClaim = pending >= 1f;
                claimTicketBtn.interactable = canClaim;
                if (claimTicketBtnText != null)
                {
                    claimTicketBtnText.text = canClaim ? $"CLAIM (+{NumberFormatter.Format(Mathf.FloorToInt(pending))} G)" : "NO TICKETS";
                }
            }
        }

        private void RefreshTankTab()
        {
            if (AquariumManager.Instance == null) return;

            if (AquariumFishPreviewManager.Instance != null)
            {
                AquariumFishPreviewManager.Instance.ClearAll();
            }

            UpdateLiveTicketDisplay();

            // Upgrade Capacity Button
            if (upgradeCapacityBtn != null && upgradeCapBtnText != null)
            {
                int capCost = AquariumManager.Instance.GetCapacityUpgradeCost();
                bool canCap = EconomyManager.Instance != null && EconomyManager.Instance.Coins >= capCost;
                upgradeCapBtnText.text = $"EXPAND TANK (+2 SLOTS)\n<color=#FFE600>{NumberFormatter.Format(capCost)} G</color>";
                upgradeCapacityBtn.interactable = canCap;
            }

            // Upgrade Decor Button
            if (upgradeDecorBtn != null && upgradeDecorBtnText != null)
            {
                int curDecor = AquariumManager.Instance.TankDecorLevel;
                if (curDecor >= 4)
                {
                    upgradeDecorBtnText.text = "DECORATIONS\n<color=#2ECC71>MAX (x2.5)</color>";
                    upgradeDecorBtn.interactable = false;
                }
                else
                {
                    int decorCost = AquariumManager.Instance.GetDecorUpgradeCost();
                    bool canDecor = EconomyManager.Instance != null && EconomyManager.Instance.Coins >= decorCost;
                    upgradeDecorBtnText.text = $"UPGRADE LIGHTS & CORAL\n<color=#FFE600>{NumberFormatter.Format(decorCost)} G</color>";
                    upgradeDecorBtn.interactable = canDecor;
                }
            }

            // Rebuild Fish Cards
            if (fishCardsContainer != null)
            {
                for (int i = fishCardsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(fishCardsContainer.GetChild(i).gameObject);
                }

                Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();
                var fishes = AquariumManager.Instance.CaptiveFish;

                for (int i = 0; i < fishes.Count; i++)
                {
                    var f = fishes[i];
                    CreateTankFishCard(fishCardsContainer, whiteSp, f);
                }
            }
        }

        private void CreateTankFishCard(Transform parent, Sprite whiteSp, AquariumFishInstance f)
        {
            GameObject cardObj = new GameObject($"FishCard_{f.instanceId}");
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420f, 360f);

            Image bg = cardObj.AddComponent<Image>();
            CasualUITheme.StyleCard(bg, f.isBoss ? new Color(0.32f, 0.22f, 0.10f, 0.96f) : new Color(0.08f, 0.13f, 0.22f, 0.96f));

            // 1. 3D Model Animated Preview Viewport
            GameObject previewFrame = new GameObject("3D_Preview_Frame");
            previewFrame.transform.SetParent(cardObj.transform, false);
            RectTransform pfRt = previewFrame.AddComponent<RectTransform>();
            pfRt.anchorMin = new Vector2(0.5f, 1f);
            pfRt.anchorMax = new Vector2(0.5f, 1f);
            pfRt.pivot = new Vector2(0.5f, 1f);
            pfRt.anchoredPosition = new Vector2(0f, -14f);
            pfRt.sizeDelta = new Vector2(390f, 185f);

            Image pfBg = previewFrame.AddComponent<Image>();
            CasualUITheme.StyleCard(pfBg, new Color(0.04f, 0.08f, 0.15f, 0.98f));

            // Inner RawImage for RenderTexture
            GameObject rawImgObj = new GameObject("Fish_3D_RawImage");
            rawImgObj.transform.SetParent(previewFrame.transform, false);
            RectTransform rImgRt = rawImgObj.AddComponent<RectTransform>();
            rImgRt.anchorMin = Vector2.zero;
            rImgRt.anchorMax = Vector2.one;
            rImgRt.offsetMin = new Vector2(3f, 3f);
            rImgRt.offsetMax = new Vector2(-3f, -3f);

            RawImage rawImg = rawImgObj.AddComponent<RawImage>();
            rawImg.raycastTarget = false;

            if (AquariumFishPreviewManager.Instance != null)
            {
                var handle = AquariumFishPreviewManager.Instance.RequestPreview(f.speciesId, f.isBoss, f.weightKg);
                if (handle != null)
                {
                    rawImg.texture = handle.texture;
                }
            }

            // Tier Badge in upper-left corner of 3D window
            GameObject badgeObj = new GameObject("TierBadge");
            badgeObj.transform.SetParent(previewFrame.transform, false);
            RectTransform bRt = badgeObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 1f);
            bRt.anchorMax = new Vector2(0f, 1f);
            bRt.pivot = new Vector2(0f, 1f);
            bRt.anchoredPosition = new Vector2(8f, -8f);
            bRt.sizeDelta = new Vector2(105f, 28f);

            Image bImg = badgeObj.AddComponent<Image>();
            CasualUITheme.StyleCard(bImg, f.isBoss ? new Color(0.95f, 0.72f, 0.15f, 0.92f) : new Color(0.12f, 0.28f, 0.48f, 0.92f));

            GameObject bTxtObj = new GameObject("Text");
            bTxtObj.transform.SetParent(badgeObj.transform, false);
            var bTxt = bTxtObj.AddComponent<TextMeshProUGUI>();
            bTxt.text = f.isBoss ? "APEX BOSS" : $"TIER {f.tier}";
            bTxt.fontSize = 18f;
            bTxt.fontStyle = FontStyles.Bold;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxt.color = f.isBoss ? new Color(0.15f, 0.10f, 0.02f) : Color.white;
            bTxt.raycastTarget = false;
            bTxt.ApplyFont(UIFontCategory.Button);
            RectTransform btRt = bTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            // 2. Fish Title (Font size 28 bold)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = f.isBoss ? $"[BOSS] {f.speciesName}" : f.speciesName;
            title.fontSize = 28f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = f.isBoss ? new Color(1f, 0.88f, 0.25f) : Color.white;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.raycastTarget = false;
            title.ApplyFont(UIFontCategory.Header);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -212);
            tRt.sizeDelta = new Vector2(400, 32);

            // 3. Weight & Ticket Rate Stats (Font size 22 bold)
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var info = infoObj.AddComponent<TextMeshProUGUI>();
            info.text = $"Weight: <b>{f.weightKg:0.0} kg</b>   |   Ticket: <color=#FFE600><b>+{f.baseIncomeRate:0.0} G/s</b></color>";
            info.fontSize = 22f;
            info.fontStyle = FontStyles.Bold;
            info.alignment = TextAlignmentOptions.Center;
            info.color = new Color(0.85f, 0.92f, 1f);
            info.textWrappingMode = TextWrappingModes.NoWrap;
            info.raycastTarget = false;
            info.ApplyFont(UIFontCategory.Default);
            RectTransform iRt = infoObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0, 1);
            iRt.anchorMax = new Vector2(1, 1);
            iRt.pivot = new Vector2(0.5f, 1);
            iRt.anchoredPosition = new Vector2(0, -246);
            iRt.sizeDelta = new Vector2(400, 30);

            // 4. Dilemma Harvest Button (Chuyển vào xưởng mổ / Bán chợ)
            GameObject harvestBtnObj = new GameObject("HarvestBtn");
            harvestBtnObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRt = harvestBtnObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 0);
            hRt.anchorMax = new Vector2(0.5f, 0);
            hRt.pivot = new Vector2(0.5f, 0);
            hRt.anchoredPosition = new Vector2(0, 15);
            hRt.sizeDelta = new Vector2(390, 52);

            Image hImg = harvestBtnObj.AddComponent<Image>();
            Button hBtn = harvestBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(hBtn, hImg, CasualBtnStyle.Yellow);
            string instId = f.instanceId;
            hBtn.onClick.AddListener(() =>
            {
                if (AquariumManager.Instance != null)
                {
                    if (AquariumManager.Instance.TryRemoveFishToKitchen(instId))
                    {
                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayCoin();
                        }
                        RefreshTankTab();
                    }
                }
            });

            GameObject hTxtObj = new GameObject("Text");
            hTxtObj.transform.SetParent(harvestBtnObj.transform, false);
            var hTxt = hTxtObj.AddComponent<TextMeshProUGUI>();
            hTxt.text = "SEND TO MARKET";
            hTxt.fontSize = 24;
            hTxt.fontStyle = FontStyles.Bold;
            hTxt.color = new Color(0.25f, 0.18f, 0.05f);
            hTxt.alignment = TextAlignmentOptions.Center;
            hTxt.textWrappingMode = TextWrappingModes.NoWrap;
            hTxt.raycastTarget = false;
            hTxt.ApplyFont(UIFontCategory.Button);
            RectTransform htRt = hTxtObj.GetComponent<RectTransform>();
            htRt.anchorMin = Vector2.zero;
            htRt.anchorMax = Vector2.one;
            htRt.sizeDelta = Vector2.zero;
        }

        private void RefreshBestiaryTab()
        {
            if (BestiaryManager.Instance == null) return;

            if (AquariumFishPreviewManager.Instance != null)
            {
                AquariumFishPreviewManager.Instance.ClearAll();
            }

            int discovered = BestiaryManager.Instance.DiscoveredSpeciesCount;
            int total = BestiaryManager.Instance.TotalSpeciesCount;

            if (bestiaryProgressText != null)
            {
                bestiaryProgressText.text = $"UNLOCKED: <color=#FFE600><b>{discovered} / {total}</b></color> MARINE SPECIES";
            }

            if (bestiaryBuffSummaryText != null)
            {
                string b1 = discovered >= 3 ? "<color=#2ECC71>[OK] +15% Reel Speed</color>" : "<color=#888888>[3 Species] +15% Reel Speed</color>";
                string b2 = discovered >= 5 ? "<color=#3498DB>[OK] +20% Market Sell Price</color>" : "<color=#888888>[5 Species] +20% Market Sell Price</color>";
                string b3 = discovered >= 7 ? "<color=#FFE600>[OK] +30% Damage & +50% Ticket Income</color>" : "<color=#888888>[7 Species] +30% Damage & +50% Ticket Income</color>";
                bestiaryBuffSummaryText.text = $"Collection Perks:\n{b1}   |   {b2}   |   {b3}";
            }

            if (bestiaryCardsContainer != null)
            {
                for (int i = bestiaryCardsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(bestiaryCardsContainer.GetChild(i).gameObject);
                }

                Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

                // Sort: Caught fish first, neatly ordered (Apex Bosses first, then Tier 1..5, then total caught)
                var discoveredEntries = BestiaryManager.Instance.Entries
                    .Where(e => e.isDiscovered)
                    .OrderByDescending(e => e.chainData != null && e.chainData.IsApexBoss)
                    .ThenBy(e => e.tier)
                    .ThenByDescending(e => e.totalCaught)
                    .ThenBy(e => e.speciesName)
                    .ToList();

                // Uncaught fish (Apex Bosses first, then Tier)
                var undiscoveredEntries = BestiaryManager.Instance.Entries
                    .Where(e => !e.isDiscovered)
                    .OrderByDescending(e => e.chainData != null && e.chainData.IsApexBoss)
                    .ThenBy(e => e.tier)
                    .ToList();

                // 1. SECTION: CAUGHT MARINE SPECIES
                CreateSectionHeader(
                    bestiaryCardsContainer,
                    $"CAUGHT MARINE SPECIES ({discoveredEntries.Count} / {total})",
                    new Color(0.10f, 0.22f, 0.38f, 0.95f),
                    new Color(1f, 0.88f, 0.30f)
                );

                if (discoveredEntries.Count > 0)
                {
                    for (int i = 0; i < discoveredEntries.Count; i += 2)
                    {
                        Transform row = CreateCardRow(bestiaryCardsContainer, $"Row_Discovered_{i / 2}");
                        CreateBestiaryCard(row, whiteSp, discoveredEntries[i]);
                        if (i + 1 < discoveredEntries.Count)
                        {
                            CreateBestiaryCard(row, whiteSp, discoveredEntries[i + 1]);
                        }
                        else
                        {
                            CreateCardPlaceholder(row);
                        }
                    }
                }
                else
                {
                    CreateEmptyPlaceholderCard(bestiaryCardsContainer, "No species caught yet!\nCast your line in ocean waters to begin cataloging.");
                }

                // 2. SECTION: UNDISCOVERED SPECIES (???)
                if (undiscoveredEntries.Count > 0)
                {
                    CreateSectionHeader(
                        bestiaryCardsContainer,
                        $"UNDISCOVERED SPECIES ({undiscoveredEntries.Count} / {total})",
                        new Color(0.06f, 0.09f, 0.15f, 0.95f),
                        new Color(0.70f, 0.78f, 0.88f)
                    );

                    for (int i = 0; i < undiscoveredEntries.Count; i += 2)
                    {
                        Transform row = CreateCardRow(bestiaryCardsContainer, $"Row_Undiscovered_{i / 2}");
                        CreateBestiaryCard(row, whiteSp, undiscoveredEntries[i]);
                        if (i + 1 < undiscoveredEntries.Count)
                        {
                            CreateBestiaryCard(row, whiteSp, undiscoveredEntries[i + 1]);
                        }
                        else
                        {
                            CreateCardPlaceholder(row);
                        }
                    }
                }

                if (bestiaryCardsContainer is RectTransform cRt)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(cRt);
                }
            }
        }

        private void CreateSectionHeader(Transform parent, string titleText, Color bgColor, Color textColor)
        {
            GameObject headerObj = new GameObject("SectionHeader");
            headerObj.transform.SetParent(parent, false);

            RectTransform rt = headerObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 50f);

            LayoutElement le = headerObj.AddComponent<LayoutElement>();
            le.preferredHeight = 50f;
            le.flexibleHeight = 0f;

            Image bg = headerObj.AddComponent<Image>();
            CasualUITheme.StyleCard(bg, bgColor);

            GameObject textObj = new GameObject("Title");
            textObj.transform.SetParent(headerObj.transform, false);
            var txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text = titleText;
            txt.fontSize = 24f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = textColor;
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.raycastTarget = false;
            txt.ApplyFont(UIFontCategory.Header);

            RectTransform tRt = textObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;
        }

        private Transform CreateCardRow(Transform parent, string rowName)
        {
            GameObject rowObj = new GameObject(rowName);
            rowObj.transform.SetParent(parent, false);

            RectTransform rt = rowObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 330f);

            LayoutElement le = rowObj.AddComponent<LayoutElement>();
            le.preferredHeight = 330f;
            le.flexibleHeight = 0f;

            var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            return rowObj.transform;
        }

        private void CreateCardPlaceholder(Transform parent)
        {
            GameObject spacer = new GameObject("CardPlaceholder");
            spacer.transform.SetParent(parent, false);
            RectTransform rt = spacer.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420f, 330f);
            LayoutElement le = spacer.AddComponent<LayoutElement>();
            le.preferredWidth = 420f;
            le.preferredHeight = 330f;
        }

        private void CreateEmptyPlaceholderCard(Transform parent, string message)
        {
            GameObject cardObj = new GameObject("EmptyPlaceholder");
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 90f);

            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.preferredHeight = 90f;
            le.flexibleHeight = 0f;

            Image bg = cardObj.AddComponent<Image>();
            CasualUITheme.StyleCard(bg, new Color(0.06f, 0.10f, 0.16f, 0.85f));

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(cardObj.transform, false);
            var txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text = message;
            txt.fontSize = 22f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.65f, 0.75f, 0.88f);
            txt.ApplyFont(UIFontCategory.Default);

            RectTransform tRt = textObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;
        }

        private void CreateBestiaryCard(Transform parent, Sprite whiteSp, BestiaryEntry e)
        {
            GameObject cardObj = new GameObject($"BestiaryCard_{e.speciesId}");
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420f, 330f);

            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.preferredWidth = 420f;
            le.preferredHeight = 330f;

            bool isApex = e.chainData != null && e.chainData.IsApexBoss;
            Image bg = cardObj.AddComponent<Image>();
            Color cardColor = e.isDiscovered
                ? (isApex ? new Color(0.32f, 0.20f, 0.10f, 0.96f) : new Color(0.08f, 0.13f, 0.22f, 0.96f))
                : new Color(0.05f, 0.08f, 0.13f, 0.90f);
            CasualUITheme.StyleCard(bg, cardColor);

            // 1. 3D Model Animated Preview Viewport / Mystery Box
            GameObject previewFrame = new GameObject("PreviewFrame");
            previewFrame.transform.SetParent(cardObj.transform, false);
            RectTransform pfRt = previewFrame.AddComponent<RectTransform>();
            pfRt.anchorMin = new Vector2(0.5f, 1f);
            pfRt.anchorMax = new Vector2(0.5f, 1f);
            pfRt.pivot = new Vector2(0.5f, 1f);
            pfRt.anchoredPosition = new Vector2(0f, -14f);
            pfRt.sizeDelta = new Vector2(390f, 175f);

            Image pfBg = previewFrame.AddComponent<Image>();
            CasualUITheme.StyleCard(pfBg, new Color(0.04f, 0.08f, 0.15f, 0.98f));

            // Tier / Status Badge in upper-left corner of preview frame
            GameObject badgeObj = new GameObject("TierBadge");
            badgeObj.transform.SetParent(previewFrame.transform, false);
            RectTransform bRt = badgeObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 1f);
            bRt.anchorMax = new Vector2(0f, 1f);
            bRt.pivot = new Vector2(0f, 1f);
            bRt.anchoredPosition = new Vector2(8f, -8f);
            bRt.sizeDelta = new Vector2(105f, 28f);

            Image bImg = badgeObj.AddComponent<Image>();
            Color badgeBgColor;
            string badgeText;
            Color badgeTxtColor;

            if (e.isDiscovered)
            {
                badgeBgColor = isApex ? new Color(0.95f, 0.72f, 0.15f, 0.92f) : new Color(0.12f, 0.28f, 0.48f, 0.92f);
                badgeText = isApex ? "APEX BOSS" : $"TIER {e.tier}";
                badgeTxtColor = isApex ? new Color(0.15f, 0.10f, 0.02f) : Color.white;
            }
            else
            {
                badgeBgColor = isApex ? new Color(0.55f, 0.18f, 0.15f, 0.92f) : new Color(0.12f, 0.16f, 0.24f, 0.92f);
                badgeText = isApex ? "APEX BOSS" : "LOCKED";
                badgeTxtColor = isApex ? new Color(1f, 0.85f, 0.85f) : new Color(0.70f, 0.75f, 0.85f);
            }
            CasualUITheme.StyleCard(bImg, badgeBgColor);

            GameObject bTxtObj = new GameObject("Text");
            bTxtObj.transform.SetParent(badgeObj.transform, false);
            var bTxt = bTxtObj.AddComponent<TextMeshProUGUI>();
            bTxt.text = badgeText;
            bTxt.fontSize = 18f;
            bTxt.fontStyle = FontStyles.Bold;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxt.color = badgeTxtColor;
            bTxt.raycastTarget = false;
            bTxt.ApplyFont(UIFontCategory.Button);
            RectTransform btRt = bTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            // Content inside preview frame: 3D model for discovered, glowing '?' for undiscovered
            if (e.isDiscovered)
            {
                GameObject rawImgObj = new GameObject("Fish_3D_RawImage");
                rawImgObj.transform.SetParent(previewFrame.transform, false);
                RectTransform rImgRt = rawImgObj.AddComponent<RectTransform>();
                rImgRt.anchorMin = Vector2.zero;
                rImgRt.anchorMax = Vector2.one;
                rImgRt.offsetMin = new Vector2(3f, 3f);
                rImgRt.offsetMax = new Vector2(-3f, -3f);

                RawImage rawImg = rawImgObj.AddComponent<RawImage>();
                rawImg.raycastTarget = false;

                if (AquariumFishPreviewManager.Instance != null)
                {
                    var handle = AquariumFishPreviewManager.Instance.RequestPreview(e.speciesId, isApex, e.recordWeightKg);
                    if (handle != null)
                    {
                        rawImg.texture = handle.texture;
                    }
                }
            }
            else
            {
                GameObject mysteryTxtObj = new GameObject("MysteryQuestionMark");
                mysteryTxtObj.transform.SetParent(previewFrame.transform, false);
                var qTxt = mysteryTxtObj.AddComponent<TextMeshProUGUI>();
                qTxt.text = "?";
                qTxt.fontSize = 56f;
                qTxt.fontStyle = FontStyles.Bold;
                qTxt.alignment = TextAlignmentOptions.Center;
                qTxt.color = isApex ? new Color(0.85f, 0.35f, 0.25f, 0.75f) : new Color(0.35f, 0.45f, 0.60f, 0.70f);
                qTxt.raycastTarget = false;
                qTxt.ApplyFont(UIFontCategory.Header);

                RectTransform qRt = mysteryTxtObj.GetComponent<RectTransform>();
                qRt.anchorMin = Vector2.zero;
                qRt.anchorMax = Vector2.one;
                qRt.sizeDelta = Vector2.zero;
            }

            badgeObj.transform.SetAsLastSibling();

            // 2. Fish Title (Font size 20 bold)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var title = titleObj.AddComponent<TextMeshProUGUI>();
            if (e.isDiscovered)
            {
                title.text = isApex ? $"[BOSS] {e.speciesName}" : e.speciesName;
            }
            else
            {
                title.text = isApex ? "[APEX BOSS] ???" : "??? UNKNOWN SPECIES";
            }
            title.fontSize = 26f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = e.isDiscovered
                ? (isApex ? new Color(1f, 0.78f, 0.20f) : new Color(1f, 0.88f, 0.25f))
                : (isApex ? new Color(0.95f, 0.52f, 0.42f) : new Color(0.65f, 0.72f, 0.82f));
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.raycastTarget = false;
            title.ApplyFont(UIFontCategory.Header);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -202);
            tRt.sizeDelta = new Vector2(400, 30);

            // 3. Stats & Record (Font size 21 bold)
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var info = infoObj.AddComponent<TextMeshProUGUI>();
            if (e.isDiscovered)
            {
                info.text = $"Caught: <b>{e.totalCaught} fish</b>   |   Record: <b>{e.recordWeightKg:0.0} kg</b>";
            }
            else
            {
                info.text = isApex ? "<color=#FCA5A5>Apex Predator</color>" : "<color=#94A3B8>Undiscovered Marine Life</color>";
            }
            info.fontSize = 21f;
            info.fontStyle = FontStyles.Bold;
            info.alignment = TextAlignmentOptions.Center;
            info.color = new Color(0.85f, 0.92f, 1f);
            info.textWrappingMode = TextWrappingModes.NoWrap;
            info.raycastTarget = false;
            info.ApplyFont(UIFontCategory.Default);
            RectTransform iRt = infoObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0, 1);
            iRt.anchorMax = new Vector2(1, 1);
            iRt.pivot = new Vector2(0.5f, 1);
            iRt.anchoredPosition = new Vector2(0, -234);
            iRt.sizeDelta = new Vector2(400, 26);

            // 4. Status / Hint (Font size 18 bold)
            GameObject descObj = new GameObject("Status");
            descObj.transform.SetParent(cardObj.transform, false);
            var desc = descObj.AddComponent<TextMeshProUGUI>();
            if (e.isDiscovered)
            {
                string status = e.hasLivedInAquarium ? "<color=#2ECC71>[DISPLAYED IN AQUARIUM]</color>" : "<color=#F59E0B>[NOT YET IN AQUARIUM]</color>";
                desc.text = status;
            }
            else
            {
                desc.text = isApex
                    ? "<color=#FCA5A5>Activate Lighthouse or fish at night to lure this boss!</color>"
                    : "<color=#64748B>Cast your line in various ocean regions to discover!</color>";
            }
            desc.fontSize = 18f;
            desc.fontStyle = FontStyles.Bold;
            desc.alignment = TextAlignmentOptions.Center;
            desc.color = new Color(0.85f, 0.90f, 0.98f);
            desc.raycastTarget = false;
            desc.ApplyFont(UIFontCategory.Default);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0, 1);
            dRt.anchorMax = new Vector2(1, 1);
            dRt.pivot = new Vector2(0.5f, 1);
            dRt.anchoredPosition = new Vector2(0, -264);
            dRt.sizeDelta = new Vector2(390, 52);
        }

        private void BuildProceduralPopup()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            if (AquariumFishPreviewManager.Instance == null)
            {
                gameObject.AddComponent<AquariumFishPreviewManager>();
            }

            // 1. Dark Backdrop with modal overlay canvas (sortingOrder 500 covers HUD)
            popupRoot = new GameObject("Aquarium_Backdrop");
            popupRoot.transform.SetParent(transform, false);
            RectTransform rootRt = popupRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Canvas modalCanvas = popupRoot.AddComponent<Canvas>();
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = 500;
            popupRoot.AddComponent<GraphicRaycaster>();

            Image darkBg = popupRoot.AddComponent<Image>();
            darkBg.sprite = CasualUITheme.DimmedBg ?? whiteSp;
            darkBg.color = (CasualUITheme.DimmedBg != null) ? Color.white : new Color(0f, 0f, 0f, 0.88f);

            // 2. Dialog Box (Enlarged 940x1440 for 1080x1920 portrait canvas)
            GameObject dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(popupRoot.transform, false);
            RectTransform dRt = dialogBox.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0.5f);
            dRt.anchorMax = new Vector2(0.5f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.sizeDelta = new Vector2(940, 1440);

            Image dBg = dialogBox.AddComponent<Image>();
            CasualUITheme.StylePopupDialog(dBg);

            // 3. Header Ribbon Banner
            GameObject ribbonObj = new GameObject("RibbonBanner");
            ribbonObj.transform.SetParent(dialogBox.transform, false);
            RectTransform rbRt = ribbonObj.AddComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.5f, 1f);
            rbRt.anchorMax = new Vector2(0.5f, 1f);
            rbRt.pivot = new Vector2(0.5f, 0.5f);
            rbRt.anchoredPosition = new Vector2(0, -14);
            rbRt.sizeDelta = new Vector2(580, 84);
            Image rbImg = ribbonObj.AddComponent<Image>();
            Sprite ribSp = CasualUITheme.TitleRibbonBlue ?? whiteSp;
            rbImg.sprite = ribSp;
            rbImg.type = (ribSp.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            rbImg.color = Color.white;
            rbImg.raycastTarget = false;

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(ribbonObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "AQUARIUM & BESTIARY";
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.textWrappingMode = TextWrappingModes.NoWrap;
            titleTxt.raycastTarget = false;
            titleTxt.ApplyFont(UIFontCategory.Header);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(30, 6);
            tRt.offsetMax = new Vector2(-30, -4);

            // Close Button [X]
            GameObject closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 1);
            closeRt.anchorMax = new Vector2(1, 1);
            closeRt.pivot = new Vector2(1, 1);
            closeRt.anchoredPosition = new Vector2(-18, -18);
            closeRt.sizeDelta = new Vector2(56, 56);

            Image closeBg = closeBtnObj.AddComponent<Image>();
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(closeBtn, closeBg, CasualBtnStyle.SquareGray);
            closeBtn.onClick.AddListener(Close);

            GameObject closeIconObj = new GameObject("Icon");
            closeIconObj.transform.SetParent(closeBtnObj.transform, false);
            var closeIconImg = closeIconObj.AddComponent<Image>();
            closeIconImg.sprite = CasualUITheme.IconClose;
            closeIconImg.preserveAspect = true;
            closeIconImg.raycastTarget = false;
            RectTransform ciRt = closeIconObj.GetComponent<RectTransform>();
            ciRt.anchorMin = new Vector2(0.2f, 0.2f);
            ciRt.anchorMax = new Vector2(0.8f, 0.8f);
            ciRt.sizeDelta = Vector2.zero;

            // 4. Tab Header Switcher
            GameObject tabSwitcherObj = new GameObject("TabSwitcher");
            tabSwitcherObj.transform.SetParent(dialogBox.transform, false);
            RectTransform tsRt = tabSwitcherObj.AddComponent<RectTransform>();
            tsRt.anchorMin = new Vector2(0, 1);
            tsRt.anchorMax = new Vector2(1, 1);
            tsRt.pivot = new Vector2(0.5f, 1);
            tsRt.anchoredPosition = new Vector2(0, -74);
            tsRt.sizeDelta = new Vector2(-40, 60);

            // Tab 1 Button
            GameObject tab1Obj = new GameObject("Tab1_Tank");
            tab1Obj.transform.SetParent(tabSwitcherObj.transform, false);
            RectTransform t1Rt = tab1Obj.AddComponent<RectTransform>();
            t1Rt.anchorMin = new Vector2(0, 0);
            t1Rt.anchorMax = new Vector2(0.49f, 1);
            t1Rt.sizeDelta = Vector2.zero;
            tabTankBg = tab1Obj.AddComponent<Image>();
            tabTankBtn = tab1Obj.AddComponent<Button>();
            CasualUITheme.StyleButton(tabTankBtn, tabTankBg, CasualBtnStyle.Blue);
            tabTankBtn.onClick.AddListener(() => SwitchTab(0));

            GameObject t1TxtObj = new GameObject("Text");
            t1TxtObj.transform.SetParent(tab1Obj.transform, false);
            var t1Txt = t1TxtObj.AddComponent<TextMeshProUGUI>();
            t1Txt.text = "EXHIBIT TANK";
            t1Txt.fontSize = 26;
            t1Txt.fontStyle = FontStyles.Bold;
            t1Txt.alignment = TextAlignmentOptions.Center;
            t1Txt.color = Color.white;
            t1Txt.textWrappingMode = TextWrappingModes.NoWrap;
            t1Txt.raycastTarget = false;
            t1Txt.ApplyFont(UIFontCategory.Button);
            RectTransform t1TxtRt = t1TxtObj.GetComponent<RectTransform>();
            t1TxtRt.anchorMin = Vector2.zero;
            t1TxtRt.anchorMax = Vector2.one;
            t1TxtRt.sizeDelta = Vector2.zero;

            // Tab 2 Button
            GameObject tab2Obj = new GameObject("Tab2_Bestiary");
            tab2Obj.transform.SetParent(tabSwitcherObj.transform, false);
            RectTransform t2Rt = tab2Obj.AddComponent<RectTransform>();
            t2Rt.anchorMin = new Vector2(0.51f, 0);
            t2Rt.anchorMax = new Vector2(1, 1);
            t2Rt.sizeDelta = Vector2.zero;
            tabBestiaryBg = tab2Obj.AddComponent<Image>();
            tabBestiaryBtn = tab2Obj.AddComponent<Button>();
            CasualUITheme.StyleButton(tabBestiaryBtn, tabBestiaryBg, CasualBtnStyle.Dark);
            tabBestiaryBtn.onClick.AddListener(() => SwitchTab(1));

            GameObject t2TxtObj = new GameObject("Text");
            t2TxtObj.transform.SetParent(tab2Obj.transform, false);
            var t2Txt = t2TxtObj.AddComponent<TextMeshProUGUI>();
            t2Txt.text = "BESTIARY LOG";
            t2Txt.fontSize = 26;
            t2Txt.fontStyle = FontStyles.Bold;
            t2Txt.alignment = TextAlignmentOptions.Center;
            t2Txt.color = Color.white;
            t2Txt.textWrappingMode = TextWrappingModes.NoWrap;
            t2Txt.raycastTarget = false;
            t2Txt.ApplyFont(UIFontCategory.Button);
            RectTransform t2TxtRt = t2TxtObj.GetComponent<RectTransform>();
            t2TxtRt.anchorMin = Vector2.zero;
            t2TxtRt.anchorMax = Vector2.one;
            t2TxtRt.sizeDelta = Vector2.zero;

            // 5. Container Tab 1 (Tank)
            tankTabContainer = new GameObject("TankTabContainer");
            tankTabContainer.transform.SetParent(dialogBox.transform, false);
            RectTransform c1Rt = tankTabContainer.AddComponent<RectTransform>();
            c1Rt.anchorMin = Vector2.zero;
            c1Rt.anchorMax = Vector2.one;
            c1Rt.offsetMin = new Vector2(25f, 25f);
            c1Rt.offsetMax = new Vector2(-25f, -145f);

            BuildTankTabContent(tankTabContainer.transform, whiteSp);

            // 6. Container Tab 2 (Bestiary)
            bestiaryTabContainer = new GameObject("BestiaryTabContainer");
            bestiaryTabContainer.transform.SetParent(dialogBox.transform, false);
            RectTransform c2Rt = bestiaryTabContainer.AddComponent<RectTransform>();
            c2Rt.anchorMin = Vector2.zero;
            c2Rt.anchorMax = Vector2.one;
            c2Rt.offsetMin = new Vector2(25f, 25f);
            c2Rt.offsetMax = new Vector2(-25f, -145f);

            BuildBestiaryTabContent(bestiaryTabContainer.transform, whiteSp);

            UIFontConfig.Instance?.ApplyToHierarchy(popupRoot);
            popupRoot.SetActive(false);
        }

        private void BuildTankTabContent(Transform parent, Sprite whiteSp)
        {
            // Revenue Banner (Top)
            GameObject bannerObj = new GameObject("RevenueBanner");
            bannerObj.transform.SetParent(parent, false);
            RectTransform bRt = bannerObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 1f);
            bRt.anchorMax = new Vector2(1f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f);
            bRt.anchoredPosition = Vector2.zero;
            bRt.sizeDelta = new Vector2(0f, 114f);

            Image bImg = bannerObj.AddComponent<Image>();
            CasualUITheme.StyleCard(bImg, new Color(0.08f, 0.12f, 0.20f, 0.95f));

            GameObject rTxtObj = new GameObject("RateText");
            rTxtObj.transform.SetParent(bannerObj.transform, false);
            ticketRateText = rTxtObj.AddComponent<TextMeshProUGUI>();
            ticketRateText.fontSize = 26;
            ticketRateText.fontStyle = FontStyles.Bold;
            ticketRateText.alignment = TextAlignmentOptions.MidlineLeft;
            ticketRateText.color = Color.white;
            ticketRateText.textWrappingMode = TextWrappingModes.NoWrap;
            ticketRateText.ApplyFont(UIFontCategory.Default);
            RectTransform rRt = rTxtObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0f, 0.5f);
            rRt.anchorMax = new Vector2(0.60f, 1f);
            rRt.offsetMin = new Vector2(24f, 4f);
            rRt.offsetMax = new Vector2(-10f, -4f);

            GameObject pTxtObj = new GameObject("PendingText");
            pTxtObj.transform.SetParent(bannerObj.transform, false);
            ticketPendingText = pTxtObj.AddComponent<TextMeshProUGUI>();
            ticketPendingText.fontSize = 24;
            ticketPendingText.fontStyle = FontStyles.Bold;
            ticketPendingText.alignment = TextAlignmentOptions.MidlineLeft;
            ticketPendingText.color = new Color(0.2f, 0.9f, 0.5f);
            ticketPendingText.textWrappingMode = TextWrappingModes.NoWrap;
            ticketPendingText.ApplyFont(UIFontCategory.Numbers);
            RectTransform pRt = pTxtObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0f);
            pRt.anchorMax = new Vector2(0.60f, 0.5f);
            pRt.offsetMin = new Vector2(24f, 4f);
            pRt.offsetMax = new Vector2(-10f, -4f);

            // Big Claim Button
            GameObject claimBtnObj = new GameObject("ClaimTicketBtn");
            claimBtnObj.transform.SetParent(bannerObj.transform, false);
            RectTransform clRt = claimBtnObj.AddComponent<RectTransform>();
            clRt.anchorMin = new Vector2(1f, 0.5f);
            clRt.anchorMax = new Vector2(1f, 0.5f);
            clRt.pivot = new Vector2(1f, 0.5f);
            clRt.anchoredPosition = new Vector2(-18f, 0f);
            clRt.sizeDelta = new Vector2(320f, 74f);

            Image clImg = claimBtnObj.AddComponent<Image>();
            claimTicketBtn = claimBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(claimTicketBtn, clImg, CasualBtnStyle.Green);
            claimTicketBtn.onClick.AddListener(() =>
            {
                if (AquariumManager.Instance != null)
                {
                    int earned = AquariumManager.Instance.CollectTicketRevenue();
                    if (earned > 0 && FlyingCoinManager.Instance != null && Camera.main != null)
                    {
                        FlyingCoinManager.Instance.PlayFlyingCoins(Camera.main.transform.position + Camera.main.transform.forward * 2f, 6);
                    }
                }
            });

            GameObject clTxtObj = new GameObject("Text");
            clTxtObj.transform.SetParent(claimBtnObj.transform, false);
            claimTicketBtnText = clTxtObj.AddComponent<TextMeshProUGUI>();
            claimTicketBtnText.text = "CLAIM TICKETS";
            claimTicketBtnText.fontSize = 24;
            claimTicketBtnText.fontStyle = FontStyles.Bold;
            claimTicketBtnText.alignment = TextAlignmentOptions.Center;
            claimTicketBtnText.color = Color.white;
            claimTicketBtnText.textWrappingMode = TextWrappingModes.NoWrap;
            claimTicketBtnText.raycastTarget = false;
            claimTicketBtnText.ApplyFont(UIFontCategory.Button);
            RectTransform cltRt = clTxtObj.GetComponent<RectTransform>();
            cltRt.anchorMin = Vector2.zero;
            cltRt.anchorMax = Vector2.one;
            cltRt.sizeDelta = Vector2.zero;

            // Upgrade Strip
            GameObject upgStrip = new GameObject("UpgradeStrip");
            upgStrip.transform.SetParent(parent, false);
            RectTransform uRt = upgStrip.AddComponent<RectTransform>();
            uRt.anchorMin = new Vector2(0f, 1f);
            uRt.anchorMax = new Vector2(1f, 1f);
            uRt.pivot = new Vector2(0.5f, 1f);
            uRt.anchoredPosition = new Vector2(0f, -126f);
            uRt.sizeDelta = new Vector2(0f, 70f);

            // Cap Button
            GameObject capObj = new GameObject("UpgCapBtn");
            capObj.transform.SetParent(upgStrip.transform, false);
            RectTransform capRt = capObj.AddComponent<RectTransform>();
            capRt.anchorMin = new Vector2(0, 0);
            capRt.anchorMax = new Vector2(0.485f, 1);
            capRt.sizeDelta = Vector2.zero;
            Image capImg = capObj.AddComponent<Image>();
            upgradeCapacityBtn = capObj.AddComponent<Button>();
            CasualUITheme.StyleButton(upgradeCapacityBtn, capImg, CasualBtnStyle.Blue);
            upgradeCapacityBtn.onClick.AddListener(() =>
            {
                if (AquariumManager.Instance != null) AquariumManager.Instance.UpgradeTankCapacity();
            });

            GameObject capTxtObj = new GameObject("Text");
            capTxtObj.transform.SetParent(capObj.transform, false);
            upgradeCapBtnText = capTxtObj.AddComponent<TextMeshProUGUI>();
            upgradeCapBtnText.text = "EXPAND TANK";
            upgradeCapBtnText.fontSize = 22;
            upgradeCapBtnText.fontStyle = FontStyles.Bold;
            upgradeCapBtnText.alignment = TextAlignmentOptions.Center;
            upgradeCapBtnText.color = Color.white;
            upgradeCapBtnText.raycastTarget = false;
            upgradeCapBtnText.ApplyFont(UIFontCategory.Button);
            RectTransform cptRt = capTxtObj.GetComponent<RectTransform>();
            cptRt.anchorMin = Vector2.zero;
            cptRt.anchorMax = Vector2.one;
            cptRt.sizeDelta = Vector2.zero;

            // Decor Button
            GameObject decObj = new GameObject("UpgDecorBtn");
            decObj.transform.SetParent(upgStrip.transform, false);
            RectTransform decRt = decObj.AddComponent<RectTransform>();
            decRt.anchorMin = new Vector2(0.515f, 0);
            decRt.anchorMax = new Vector2(1f, 1);
            decRt.sizeDelta = Vector2.zero;
            Image decImg = decObj.AddComponent<Image>();
            upgradeDecorBtn = decObj.AddComponent<Button>();
            CasualUITheme.StyleButton(upgradeDecorBtn, decImg, CasualBtnStyle.Blue);
            upgradeDecorBtn.onClick.AddListener(() =>
            {
                if (AquariumManager.Instance != null) AquariumManager.Instance.UpgradeDecoration();
            });

            GameObject decTxtObj = new GameObject("Text");
            decTxtObj.transform.SetParent(decObj.transform, false);
            upgradeDecorBtnText = decTxtObj.AddComponent<TextMeshProUGUI>();
            upgradeDecorBtnText.text = "UPGRADE DECOR";
            upgradeDecorBtnText.fontSize = 22;
            upgradeDecorBtnText.fontStyle = FontStyles.Bold;
            upgradeDecorBtnText.alignment = TextAlignmentOptions.Center;
            upgradeDecorBtnText.color = Color.white;
            upgradeDecorBtnText.raycastTarget = false;
            upgradeDecorBtnText.ApplyFont(UIFontCategory.Button);
            RectTransform dctRt = decTxtObj.GetComponent<RectTransform>();
            dctRt.anchorMin = Vector2.zero;
            dctRt.anchorMax = Vector2.one;
            dctRt.sizeDelta = Vector2.zero;

            // Fish Cards Scroll Area with RECTMASK2D
            GameObject scrollObj = new GameObject("FishScrollView");
            scrollObj.transform.SetParent(parent, false);
            RectTransform sRt = scrollObj.AddComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = new Vector2(0f, -206f);

            scrollObj.AddComponent<RectMask2D>(); // CLIPS CONTENT TO SCROLLVIEW BOUNDS!

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.sizeDelta = new Vector2(0, 800);

            var grid = contentObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(420, 360);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(10, 10, 10, 25);
            grid.childAlignment = TextAnchor.UpperCenter;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = cRt;
            fishCardsContainer = contentObj.transform;
        }

        private void BuildBestiaryTabContent(Transform parent, Sprite whiteSp)
        {
            // Progress Header
            GameObject headerObj = new GameObject("ProgressHeader");
            headerObj.transform.SetParent(parent, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(0f, 115f);

            Image hImg = headerObj.AddComponent<Image>();
            CasualUITheme.StyleCard(hImg, new Color(0.08f, 0.12f, 0.20f, 0.95f));

            GameObject pTxtObj = new GameObject("ProgressText");
            pTxtObj.transform.SetParent(headerObj.transform, false);
            bestiaryProgressText = pTxtObj.AddComponent<TextMeshProUGUI>();
            bestiaryProgressText.fontSize = 25;
            bestiaryProgressText.fontStyle = FontStyles.Bold;
            bestiaryProgressText.alignment = TextAlignmentOptions.Center;
            bestiaryProgressText.color = Color.white;
            bestiaryProgressText.textWrappingMode = TextWrappingModes.NoWrap;
            bestiaryProgressText.ApplyFont(UIFontCategory.Default);
            RectTransform pRt = pTxtObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 0.5f);
            pRt.anchorMax = new Vector2(1, 1f);
            pRt.anchoredPosition = Vector2.zero;

            GameObject bTxtObj = new GameObject("BuffText");
            bTxtObj.transform.SetParent(headerObj.transform, false);
            bestiaryBuffSummaryText = bTxtObj.AddComponent<TextMeshProUGUI>();
            bestiaryBuffSummaryText.fontSize = 20;
            bestiaryBuffSummaryText.fontStyle = FontStyles.Bold;
            bestiaryBuffSummaryText.alignment = TextAlignmentOptions.Center;
            bestiaryBuffSummaryText.color = new Color(0.85f, 0.9f, 1f);
            bestiaryBuffSummaryText.ApplyFont(UIFontCategory.Default);
            RectTransform bRt = bTxtObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0, 0);
            bRt.anchorMax = new Vector2(1, 0.5f);
            bRt.anchoredPosition = Vector2.zero;

            // Scroll View for Bestiary Cards with RECTMASK2D
            GameObject scrollObj = new GameObject("BestiaryScrollView");
            scrollObj.transform.SetParent(parent, false);
            RectTransform sRt = scrollObj.AddComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = new Vector2(0f, -118f);

            scrollObj.AddComponent<RectMask2D>(); // CLIPS BESTIARY ITEMS PROPERLY!

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.sizeDelta = new Vector2(0, 800);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.padding = new RectOffset(15, 15, 12, 35);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = cRt;
            bestiaryScrollRect = sr;
            bestiaryCardsContainer = contentObj.transform;
        }
    }
}

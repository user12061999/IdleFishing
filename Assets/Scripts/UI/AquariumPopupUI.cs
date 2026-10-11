using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Aquarium;
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
        private Toggle autoNewToggle;

        // Tab 2 Elements
        private TextMeshProUGUI bestiaryProgressText;
        private TextMeshProUGUI bestiaryBuffSummaryText;
        private Transform bestiaryCardsContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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
        }

        private void OnDisable()
        {
            GameEvents.AquariumFishAdded -= OnFishChanged;
            GameEvents.AquariumFishRemoved -= OnFishRemoved;
            GameEvents.AquariumTicketClaimed -= OnTicketClaimed;
            GameEvents.BestiarySpeciesDiscovered -= OnSpeciesDiscovered;
            GameEvents.AquariumUpgraded -= OnAquariumUpgraded;
        }

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

        public void Open()
        {
            if (popupRoot == null) BuildProceduralPopup();
            popupRoot.SetActive(true);
            SwitchTab(0);
        }

        public void Close()
        {
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

            if (tankTabContainer != null) tankTabContainer.SetActive(currentTab == 0);
            if (bestiaryTabContainer != null) bestiaryTabContainer.SetActive(currentTab == 1);

            // Tab Buttons appearance
            Color activeCol = new Color(0.2f, 0.45f, 0.75f, 1f);
            Color inactiveCol = new Color(0.12f, 0.16f, 0.22f, 0.9f);

            if (tabTankBg != null) tabTankBg.color = currentTab == 0 ? activeCol : inactiveCol;
            if (tabBestiaryBg != null) tabBestiaryBg.color = currentTab == 1 ? activeCol : inactiveCol;

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
                ticketPendingText.text = $"Pending Tickets: <color=#2ECC71><b>+{Mathf.FloorToInt(pending):N0} G</b></color>";
            }

            if (claimTicketBtn != null)
            {
                bool canClaim = pending >= 1f;
                claimTicketBtn.interactable = canClaim;
                if (claimTicketBtnText != null)
                {
                    claimTicketBtnText.text = canClaim ? $"CLAIM TICKETS (+{Mathf.FloorToInt(pending):N0} G)" : "NO PENDING TICKETS";
                }
            }
        }

        private void RefreshTankTab()
        {
            if (AquariumManager.Instance == null) return;

            UpdateLiveTicketDisplay();

            // Upgrade Capacity Button
            if (upgradeCapacityBtn != null && upgradeCapBtnText != null)
            {
                int capCost = AquariumManager.Instance.GetCapacityUpgradeCost();
                bool canCap = EconomyManager.Instance != null && EconomyManager.Instance.Coins >= capCost;
                upgradeCapBtnText.text = $"EXPAND TANK (+2 SLOTS)\n<color=#FFE600>{capCost:N0} G</color>";
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
                    upgradeDecorBtnText.text = $"UPGRADE LIGHTS & CORAL\n<color=#FFE600>{decorCost:N0} G</color>";
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
            rt.sizeDelta = new Vector2(250, 200);

            Image bg = cardObj.AddComponent<Image>();
            bg.sprite = whiteSp;
            bg.color = f.isBoss ? new Color(0.35f, 0.28f, 0.10f, 0.95f) : new Color(0.12f, 0.17f, 0.25f, 0.95f);

            // Fish Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = f.isBoss ? $"[BOSS] {f.speciesName}" : f.speciesName;
            title.fontSize = 18;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = f.isBoss ? new Color(1f, 0.85f, 0.2f) : Color.white;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -10);
            tRt.sizeDelta = new Vector2(240, 28);

            // Weight & Tier
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var info = infoObj.AddComponent<TextMeshProUGUI>();
            info.text = $"Weight: <b>{f.weightKg:0.0} kg</b> (Tier {f.tier})\nTicket Rate: <color=#FFE600>+{f.baseIncomeRate:0.0} G/s</color>";
            info.fontSize = 16;
            info.alignment = TextAlignmentOptions.Center;
            info.color = new Color(0.8f, 0.88f, 0.95f);
            RectTransform iRt = infoObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0, 1);
            iRt.anchorMax = new Vector2(1, 1);
            iRt.pivot = new Vector2(0.5f, 1);
            iRt.anchoredPosition = new Vector2(0, -42);
            iRt.sizeDelta = new Vector2(240, 44);

            // Dilemma Harvest Button (Chuyển vào xưởng mổ)
            GameObject harvestBtnObj = new GameObject("HarvestBtn");
            harvestBtnObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRt = harvestBtnObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 0);
            hRt.anchorMax = new Vector2(0.5f, 0);
            hRt.pivot = new Vector2(0.5f, 0);
            hRt.anchoredPosition = new Vector2(0, 12);
            hRt.sizeDelta = new Vector2(220, 38);

            Image hImg = harvestBtnObj.AddComponent<Image>();
            hImg.sprite = whiteSp;
            hImg.color = new Color(0.85f, 0.32f, 0.32f);

            Button hBtn = harvestBtnObj.AddComponent<Button>();
            hBtn.targetGraphic = hImg;
            string instId = f.instanceId;
            hBtn.onClick.AddListener(() =>
            {
                if (AquariumManager.Instance != null)
                {
                    AquariumManager.Instance.TryRemoveFishToKitchen(instId);
                }
            });

            GameObject hTxtObj = new GameObject("Text");
            hTxtObj.transform.SetParent(harvestBtnObj.transform, false);
            var hTxt = hTxtObj.AddComponent<TextMeshProUGUI>();
            hTxt.text = "SEND TO MARKET";
            hTxt.fontSize = 15;
            hTxt.fontStyle = FontStyles.Bold;
            hTxt.color = Color.white;
            hTxt.alignment = TextAlignmentOptions.Center;
            hTxt.raycastTarget = false;
            hTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(210, 34);
        }

        private void RefreshBestiaryTab()
        {
            if (BestiaryManager.Instance == null) return;

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
                foreach (var entry in BestiaryManager.Instance.Entries)
                {
                    CreateBestiaryCard(bestiaryCardsContainer, whiteSp, entry);
                }
            }
        }

        private void CreateBestiaryCard(Transform parent, Sprite whiteSp, BestiaryEntry e)
        {
            GameObject cardObj = new GameObject($"BestiaryCard_{e.speciesId}");
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 160);

            bool isApex = e.chainData != null && e.chainData.IsApexBoss;
            Image bg = cardObj.AddComponent<Image>();
            bg.sprite = whiteSp;
            if (e.isDiscovered)
            {
                bg.color = isApex ? new Color(0.32f, 0.16f, 0.12f, 0.96f) : new Color(0.12f, 0.18f, 0.28f, 0.95f);
            }
            else
            {
                bg.color = isApex ? new Color(0.18f, 0.10f, 0.10f, 0.90f) : new Color(0.08f, 0.09f, 0.12f, 0.85f);
            }

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var title = titleObj.AddComponent<TextMeshProUGUI>();
            if (e.isDiscovered)
            {
                title.text = isApex ? $"[BOSS] {e.speciesName}" : $"{e.speciesName} (Tier {e.tier})";
            }
            else
            {
                title.text = isApex ? "[APEX BOSS] ???" : "??? UNKNOWN SPECIES";
            }
            title.fontSize = 18;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = e.isDiscovered
                ? (isApex ? new Color(1f, 0.72f, 0.2f) : new Color(1f, 0.85f, 0.25f))
                : (isApex ? new Color(0.85f, 0.45f, 0.35f) : new Color(0.6f, 0.6f, 0.65f));
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -10);
            tRt.sizeDelta = new Vector2(240, 28);

            // Details
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(cardObj.transform, false);
            var desc = descObj.AddComponent<TextMeshProUGUI>();
            if (e.isDiscovered)
            {
                string status = e.hasLivedInAquarium ? "<color=#2ECC71>[DISPLAYED IN AQUARIUM]</color>" : "<color=#E67E22>[NOT YET IN AQUARIUM]</color>";
                desc.text = $"Caught: <b>{e.totalCaught} fish</b>\nRecord: <b>{e.recordWeightKg:0.0} kg</b>\n{status}";
            }
            else
            {
                desc.text = isApex
                    ? "Activate the Lighthouse or fish at night to lure this Apex Ocean Boss!"
                    : "Cast your line in various ocean regions to discover this species!";
            }
            desc.fontSize = 16;
            desc.alignment = TextAlignmentOptions.Center;
            desc.color = new Color(0.8f, 0.85f, 0.9f);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0, 0);
            dRt.anchorMax = new Vector2(1, 1);
            dRt.anchoredPosition = new Vector2(0, -15);
            dRt.sizeDelta = new Vector2(-20, -45);
        }

        private void BuildProceduralPopup()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Dark Backdrop
            popupRoot = new GameObject("Aquarium_Backdrop");
            popupRoot.transform.SetParent(transform, false);
            RectTransform rootRt = popupRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Image darkBg = popupRoot.AddComponent<Image>();
            darkBg.sprite = whiteSp;
            darkBg.color = new Color(0f, 0f, 0f, 0.80f);

            // 2. Dialog Box
            GameObject dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(popupRoot.transform, false);
            RectTransform dRt = dialogBox.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0.5f);
            dRt.anchorMax = new Vector2(0.5f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.sizeDelta = new Vector2(880, 1200);

            Image dBg = dialogBox.AddComponent<Image>();
            dBg.sprite = whiteSp;
            dBg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

            // 3. Header Bar
            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(dialogBox.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(0, 64);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.sprite = whiteSp;
            hBg.color = new Color(0.05f, 0.08f, 0.13f, 1f);

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "AQUARIUM & MARINE BESTIARY";
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
            titleTxt.color = new Color(0.2f, 0.9f, 1f);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.anchoredPosition = new Vector2(25, 0);

            // Close Button [X]
            GameObject closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 0.5f);
            closeRt.anchorMax = new Vector2(1, 0.5f);
            closeRt.anchoredPosition = new Vector2(-30, 0);
            closeRt.sizeDelta = new Vector2(40, 40);

            Image closeBg = closeBtnObj.AddComponent<Image>();
            closeBg.sprite = whiteSp;
            closeBg.color = new Color(0.85f, 0.25f, 0.25f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeBtn.onClick.AddListener(Close);

            GameObject xTxtObj = new GameObject("Text");
            xTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var xTxt = xTxtObj.AddComponent<TextMeshProUGUI>();
            xTxt.text = "✕";
            xTxt.fontSize = 24;
            xTxt.fontStyle = FontStyles.Bold;
            xTxt.alignment = TextAlignmentOptions.Center;
            xTxt.color = Color.white;
            xTxt.raycastTarget = false;
            xTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);

            // 4. Tab Header Switcher
            GameObject tabSwitcherObj = new GameObject("TabSwitcher");
            tabSwitcherObj.transform.SetParent(dialogBox.transform, false);
            RectTransform tsRt = tabSwitcherObj.AddComponent<RectTransform>();
            tsRt.anchorMin = new Vector2(0, 1);
            tsRt.anchorMax = new Vector2(1, 1);
            tsRt.pivot = new Vector2(0.5f, 1);
            tsRt.anchoredPosition = new Vector2(0, -68);
            tsRt.sizeDelta = new Vector2(0, 52);

            // Tab 1 Button
            GameObject tab1Obj = new GameObject("Tab1_Tank");
            tab1Obj.transform.SetParent(tabSwitcherObj.transform, false);
            RectTransform t1Rt = tab1Obj.AddComponent<RectTransform>();
            t1Rt.anchorMin = new Vector2(0, 0);
            t1Rt.anchorMax = new Vector2(0.5f, 1);
            t1Rt.sizeDelta = Vector2.zero;
            tabTankBg = tab1Obj.AddComponent<Image>();
            tabTankBg.sprite = whiteSp;
            tabTankBtn = tab1Obj.AddComponent<Button>();
            tabTankBtn.targetGraphic = tabTankBg;
            tabTankBtn.onClick.AddListener(() => SwitchTab(0));

            GameObject t1TxtObj = new GameObject("Text");
            t1TxtObj.transform.SetParent(tab1Obj.transform, false);
            var t1Txt = t1TxtObj.AddComponent<TextMeshProUGUI>();
            t1Txt.text = "EXHIBIT TANK";
            t1Txt.fontSize = 20;
            t1Txt.fontStyle = FontStyles.Bold;
            t1Txt.alignment = TextAlignmentOptions.Center;
            t1Txt.color = Color.white;
            t1Txt.raycastTarget = false;
            t1TxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 45);

            // Tab 2 Button
            GameObject tab2Obj = new GameObject("Tab2_Bestiary");
            tab2Obj.transform.SetParent(tabSwitcherObj.transform, false);
            RectTransform t2Rt = tab2Obj.AddComponent<RectTransform>();
            t2Rt.anchorMin = new Vector2(0.5f, 0);
            t2Rt.anchorMax = new Vector2(1, 1);
            t2Rt.sizeDelta = Vector2.zero;
            tabBestiaryBg = tab2Obj.AddComponent<Image>();
            tabBestiaryBg.sprite = whiteSp;
            tabBestiaryBtn = tab2Obj.AddComponent<Button>();
            tabBestiaryBtn.targetGraphic = tabBestiaryBg;
            tabBestiaryBtn.onClick.AddListener(() => SwitchTab(1));

            GameObject t2TxtObj = new GameObject("Text");
            t2TxtObj.transform.SetParent(tab2Obj.transform, false);
            var t2Txt = t2TxtObj.AddComponent<TextMeshProUGUI>();
            t2Txt.text = "BESTIARY LOG";
            t2Txt.fontSize = 20;
            t2Txt.fontStyle = FontStyles.Bold;
            t2Txt.alignment = TextAlignmentOptions.Center;
            t2Txt.color = Color.white;
            t2Txt.raycastTarget = false;
            t2TxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 45);

            // 5. Container Tab 1 (Tank)
            tankTabContainer = new GameObject("TankTabContainer");
            tankTabContainer.transform.SetParent(dialogBox.transform, false);
            RectTransform c1Rt = tankTabContainer.AddComponent<RectTransform>();
            c1Rt.anchorMin = Vector2.zero;
            c1Rt.anchorMax = Vector2.one;
            c1Rt.anchoredPosition = new Vector2(0, -60);
            c1Rt.sizeDelta = new Vector2(-40, -140);

            BuildTankTabContent(tankTabContainer.transform, whiteSp);

            // 6. Container Tab 2 (Bestiary)
            bestiaryTabContainer = new GameObject("BestiaryTabContainer");
            bestiaryTabContainer.transform.SetParent(dialogBox.transform, false);
            RectTransform c2Rt = bestiaryTabContainer.AddComponent<RectTransform>();
            c2Rt.anchorMin = Vector2.zero;
            c2Rt.anchorMax = Vector2.one;
            c2Rt.anchoredPosition = new Vector2(0, -60);
            c2Rt.sizeDelta = new Vector2(-40, -140);

            BuildBestiaryTabContent(bestiaryTabContainer.transform, whiteSp);

            popupRoot.SetActive(false);
        }

        private void BuildTankTabContent(Transform parent, Sprite whiteSp)
        {
            // Revenue Banner (Top)
            GameObject bannerObj = new GameObject("RevenueBanner");
            bannerObj.transform.SetParent(parent, false);
            RectTransform bRt = bannerObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0, 1);
            bRt.anchorMax = new Vector2(1, 1);
            bRt.pivot = new Vector2(0.5f, 1);
            bRt.anchoredPosition = new Vector2(0, 0);
            bRt.sizeDelta = new Vector2(0, 110);

            Image bImg = bannerObj.AddComponent<Image>();
            bImg.sprite = whiteSp;
            bImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            GameObject rTxtObj = new GameObject("RateText");
            rTxtObj.transform.SetParent(bannerObj.transform, false);
            ticketRateText = rTxtObj.AddComponent<TextMeshProUGUI>();
            ticketRateText.fontSize = 20;
            ticketRateText.fontStyle = FontStyles.Bold;
            ticketRateText.alignment = TextAlignmentOptions.MidlineLeft;
            ticketRateText.color = Color.white;
            RectTransform rRt = rTxtObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0, 0.5f);
            rRt.anchorMax = new Vector2(0.55f, 1f);
            rRt.anchoredPosition = new Vector2(20, 0);

            GameObject pTxtObj = new GameObject("PendingText");
            pTxtObj.transform.SetParent(bannerObj.transform, false);
            ticketPendingText = pTxtObj.AddComponent<TextMeshProUGUI>();
            ticketPendingText.fontSize = 18;
            ticketPendingText.fontStyle = FontStyles.Bold;
            ticketPendingText.alignment = TextAlignmentOptions.MidlineLeft;
            ticketPendingText.color = new Color(0.2f, 0.9f, 0.5f);
            RectTransform pRt = pTxtObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 0);
            pRt.anchorMax = new Vector2(0.55f, 0.5f);
            pRt.anchoredPosition = new Vector2(20, 0);

            // Big Claim Button
            GameObject claimBtnObj = new GameObject("ClaimTicketBtn");
            claimBtnObj.transform.SetParent(bannerObj.transform, false);
            RectTransform clRt = claimBtnObj.AddComponent<RectTransform>();
            clRt.anchorMin = new Vector2(0.6f, 0.5f);
            clRt.anchorMax = new Vector2(0.98f, 0.5f);
            clRt.anchoredPosition = Vector2.zero;
            clRt.sizeDelta = new Vector2(0, 70);

            Image clImg = claimBtnObj.AddComponent<Image>();
            clImg.sprite = whiteSp;
            clImg.color = new Color(0.18f, 0.78f, 0.38f);

            claimTicketBtn = claimBtnObj.AddComponent<Button>();
            claimTicketBtn.targetGraphic = clImg;
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
            claimTicketBtnText.fontSize = 20;
            claimTicketBtnText.fontStyle = FontStyles.Bold;
            claimTicketBtnText.alignment = TextAlignmentOptions.Center;
            claimTicketBtnText.color = Color.white;
            claimTicketBtnText.raycastTarget = false;
            clTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 60);

            // Upgrade Strip
            GameObject upgStrip = new GameObject("UpgradeStrip");
            upgStrip.transform.SetParent(parent, false);
            RectTransform uRt = upgStrip.AddComponent<RectTransform>();
            uRt.anchorMin = new Vector2(0, 1);
            uRt.anchorMax = new Vector2(1, 1);
            uRt.pivot = new Vector2(0.5f, 1);
            uRt.anchoredPosition = new Vector2(0, -120);
            uRt.sizeDelta = new Vector2(0, 60);

            // Cap Button
            GameObject capObj = new GameObject("UpgCapBtn");
            capObj.transform.SetParent(upgStrip.transform, false);
            RectTransform capRt = capObj.AddComponent<RectTransform>();
            capRt.anchorMin = new Vector2(0, 0);
            capRt.anchorMax = new Vector2(0.48f, 1);
            capRt.sizeDelta = Vector2.zero;
            Image capImg = capObj.AddComponent<Image>();
            capImg.sprite = whiteSp;
            capImg.color = new Color(0.16f, 0.22f, 0.32f);
            upgradeCapacityBtn = capObj.AddComponent<Button>();
            upgradeCapacityBtn.targetGraphic = capImg;
            upgradeCapacityBtn.onClick.AddListener(() =>
            {
                if (AquariumManager.Instance != null) AquariumManager.Instance.UpgradeTankCapacity();
            });

            GameObject capTxtObj = new GameObject("Text");
            capTxtObj.transform.SetParent(capObj.transform, false);
            upgradeCapBtnText = capTxtObj.AddComponent<TextMeshProUGUI>();
            upgradeCapBtnText.text = "EXPAND TANK";
            upgradeCapBtnText.fontSize = 16;
            upgradeCapBtnText.fontStyle = FontStyles.Bold;
            upgradeCapBtnText.alignment = TextAlignmentOptions.Center;
            upgradeCapBtnText.color = Color.white;
            upgradeCapBtnText.raycastTarget = false;
            capTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 55);

            // Decor Button
            GameObject decObj = new GameObject("UpgDecorBtn");
            decObj.transform.SetParent(upgStrip.transform, false);
            RectTransform decRt = decObj.AddComponent<RectTransform>();
            decRt.anchorMin = new Vector2(0.52f, 0);
            decRt.anchorMax = new Vector2(1f, 1);
            decRt.sizeDelta = Vector2.zero;
            Image decImg = decObj.AddComponent<Image>();
            decImg.sprite = whiteSp;
            decImg.color = new Color(0.16f, 0.22f, 0.32f);
            upgradeDecorBtn = decObj.AddComponent<Button>();
            upgradeDecorBtn.targetGraphic = decImg;
            upgradeDecorBtn.onClick.AddListener(() =>
            {
                if (AquariumManager.Instance != null) AquariumManager.Instance.UpgradeDecoration();
            });

            GameObject decTxtObj = new GameObject("Text");
            decTxtObj.transform.SetParent(decObj.transform, false);
            upgradeDecorBtnText = decTxtObj.AddComponent<TextMeshProUGUI>();
            upgradeDecorBtnText.text = "UPGRADE DECOR";
            upgradeDecorBtnText.fontSize = 16;
            upgradeDecorBtnText.fontStyle = FontStyles.Bold;
            upgradeDecorBtnText.alignment = TextAlignmentOptions.Center;
            upgradeDecorBtnText.color = Color.white;
            upgradeDecorBtnText.raycastTarget = false;
            decTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 55);

            // Fish Cards Scroll Area
            GameObject scrollObj = new GameObject("FishScrollView");
            scrollObj.transform.SetParent(parent, false);
            RectTransform sRt = scrollObj.AddComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.anchoredPosition = new Vector2(0, -95);
            sRt.sizeDelta = new Vector2(0, -195);

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.sizeDelta = new Vector2(0, 800);

            var grid = contentObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(250, 190);
            grid.spacing = new Vector2(15, 15);
            grid.padding = new RectOffset(10, 10, 10, 10);
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
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1);
            hRt.anchoredPosition = new Vector2(0, 0);
            hRt.sizeDelta = new Vector2(0, 100);

            Image hImg = headerObj.AddComponent<Image>();
            hImg.sprite = whiteSp;
            hImg.color = new Color(0.10f, 0.14f, 0.20f, 0.95f);

            GameObject pTxtObj = new GameObject("ProgressText");
            pTxtObj.transform.SetParent(headerObj.transform, false);
            bestiaryProgressText = pTxtObj.AddComponent<TextMeshProUGUI>();
            bestiaryProgressText.fontSize = 18;
            bestiaryProgressText.fontStyle = FontStyles.Bold;
            bestiaryProgressText.alignment = TextAlignmentOptions.Center;
            bestiaryProgressText.color = Color.white;
            RectTransform pRt = pTxtObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 0.5f);
            pRt.anchorMax = new Vector2(1, 1f);
            pRt.anchoredPosition = Vector2.zero;

            GameObject bTxtObj = new GameObject("BuffText");
            bTxtObj.transform.SetParent(headerObj.transform, false);
            bestiaryBuffSummaryText = bTxtObj.AddComponent<TextMeshProUGUI>();
            bestiaryBuffSummaryText.fontSize = 13;
            bestiaryBuffSummaryText.alignment = TextAlignmentOptions.Center;
            bestiaryBuffSummaryText.color = new Color(0.85f, 0.9f, 1f);
            RectTransform bRt = bTxtObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0, 0);
            bRt.anchorMax = new Vector2(1, 0.5f);
            bRt.anchoredPosition = Vector2.zero;

            // Scroll View for Bestiary Cards
            GameObject scrollObj = new GameObject("BestiaryScrollView");
            scrollObj.transform.SetParent(parent, false);
            RectTransform sRt = scrollObj.AddComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.anchoredPosition = new Vector2(0, -60);
            sRt.sizeDelta = new Vector2(0, -120);

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.sizeDelta = new Vector2(0, 800);

            var grid = contentObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(250, 150);
            grid.spacing = new Vector2(15, 15);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = cRt;
            bestiaryCardsContainer = contentObj.transform;
        }
    }
}

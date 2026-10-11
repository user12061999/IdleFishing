using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Audio;
using IdleFishing.VFX;

namespace IdleFishing.UI
{
    public class WorldSelectionPopupUI : MonoBehaviour
    {
        public static WorldSelectionPopupUI Instance { get; private set; }

        private GameObject popupRoot;
        private Transform cardsContainer;
        private GameObject mapNavButtonObj;
        private TextMeshProUGUI mapNavButtonText;

        // Prestige UI elements
        private TextMeshProUGUI prestigeTitleText;
        private TextMeshProUGUI prestigeBuffText;
        private Button prestigeButton;
        private TextMeshProUGUI prestigeButtonText;
        private Image prestigeButtonImg;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildWorldNavButton();
            BuildProceduralModal();
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            GameEvents.WorldChanged += OnWorldChanged;
            GameEvents.WorldUnlocked += OnWorldUnlocked;
            GameEvents.PrestigeCompleted += OnPrestigeCompleted;
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= OnWorldChanged;
            GameEvents.WorldUnlocked -= OnWorldUnlocked;
            GameEvents.PrestigeCompleted -= OnPrestigeCompleted;
        }

        private void OnWorldChanged(WorldData newWorld)
        {
            UpdateNavButtonText();
            if (popupRoot != null && popupRoot.activeSelf)
            {
                RefreshUI();
            }
        }

        private void OnWorldUnlocked(WorldData unlockedWorld)
        {
            if (popupRoot != null && popupRoot.activeSelf)
            {
                RefreshUI();
            }
        }

        private void OnPrestigeCompleted(int medals)
        {
            if (popupRoot != null && popupRoot.activeSelf)
            {
                RefreshUI();
            }
        }

        public void Open()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
                RefreshUI();
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }
        }

        public void Close()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }
        }

        private void BuildWorldNavButton()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            mapNavButtonObj = new GameObject("WorldMap_NavButton");
            mapNavButtonObj.transform.SetParent(transform, false);

            RectTransform rt = mapNavButtonObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(210, 64);

            Image img = mapNavButtonObj.AddComponent<Image>();
            img.sprite = whiteSp;
            img.color = new Color(0.2f, 0.45f, 0.75f, 0.95f);

            Button btn = mapNavButtonObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(Open);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(mapNavButtonObj.transform, false);
            mapNavButtonText = txtObj.AddComponent<TextMeshProUGUI>();
            mapNavButtonText.fontSize = 22;
            mapNavButtonText.fontStyle = FontStyles.Bold;
            mapNavButtonText.alignment = TextAlignmentOptions.Center;
            mapNavButtonText.color = Color.white;
            mapNavButtonText.raycastTarget = false;
            txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 56);

            UpdateNavButtonText();
        }

        private void UpdateNavButtonText()
        {
            if (mapNavButtonText == null) return;
            string currentName = (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
                ? WorldManager.Instance.CurrentWorld.worldName
                : "Harbor";
            int order = (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
                ? WorldManager.Instance.CurrentWorld.worldOrder
                : 1;
            mapNavButtonText.text = $"[LV.{order}] {currentName.ToUpper()}";
        }

        private void BuildProceduralModal()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Dark Backdrop
            popupRoot = new GameObject("WorldSelection_Backdrop");
            popupRoot.transform.SetParent(transform, false);
            RectTransform rootRt = popupRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Image darkBg = popupRoot.AddComponent<Image>();
            darkBg.sprite = whiteSp;
            darkBg.color = new Color(0, 0, 0, 0.80f);

            // 2. Dialog Box
            GameObject panelObj = new GameObject("DialogPanel");
            panelObj.transform.SetParent(popupRoot.transform, false);
            RectTransform pRt = panelObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(880, 1260);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.sprite = whiteSp;
            panelBg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

            // 3. Header Bar
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(0, 64);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.sprite = whiteSp;
            hBg.color = new Color(0.05f, 0.08f, 0.14f, 1f);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "LEVEL EXPEDITION: 5 OCEAN LANDS";
            titleTxt.fontSize = 22;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = new Color(1f, 0.85f, 0.2f);
            titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
            titleTxt.raycastTarget = false;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(24, 0);
            tRt.offsetMax = new Vector2(-120, 0);

            // Close button
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(headerObj.transform, false);
            Button closeBtn = closeObj.AddComponent<Button>();
            Image cImg = closeObj.AddComponent<Image>();
            cImg.sprite = whiteSp;
            cImg.color = new Color(0.85f, 0.22f, 0.22f);
            RectTransform cRt = closeObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 0.5f);
            cRt.anchorMax = new Vector2(1f, 0.5f);
            cRt.pivot = new Vector2(1f, 0.5f);
            cRt.anchoredPosition = new Vector2(-16, 0);
            cRt.sizeDelta = new Vector2(50, 50);

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(closeObj.transform, false);
            var cTxt = cTxtObj.AddComponent<TextMeshProUGUI>();
            cTxt.text = "[X]";
            cTxt.fontSize = 20;
            cTxt.fontStyle = FontStyles.Bold;
            cTxt.alignment = TextAlignmentOptions.Center;
            cTxt.color = Color.white;
            cTxt.raycastTarget = false;
            cTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);

            closeBtn.onClick.AddListener(Close);

            // 4. Prestige Strip Section (Thẻ Vinh Danh Cảng Trưởng)
            GameObject prestigeStrip = new GameObject("PrestigeStrip");
            prestigeStrip.transform.SetParent(panelObj.transform, false);
            RectTransform psRt = prestigeStrip.AddComponent<RectTransform>();
            psRt.anchorMin = new Vector2(0f, 1f);
            psRt.anchorMax = new Vector2(1f, 1f);
            psRt.pivot = new Vector2(0.5f, 1f);
            psRt.anchoredPosition = new Vector2(0, -74);
            psRt.sizeDelta = new Vector2(0, 190);

            Image psBg = prestigeStrip.AddComponent<Image>();
            psBg.sprite = whiteSp;
            psBg.color = new Color(0.14f, 0.18f, 0.26f, 0.95f);

            // Prestige Title Text
            GameObject ptObj = new GameObject("PrestigeTitle");
            ptObj.transform.SetParent(prestigeStrip.transform, false);
            prestigeTitleText = ptObj.AddComponent<TextMeshProUGUI>();
            prestigeTitleText.fontSize = 20;
            prestigeTitleText.fontStyle = FontStyles.Bold;
            prestigeTitleText.alignment = TextAlignmentOptions.Left;
            prestigeTitleText.color = new Color(1f, 0.85f, 0.2f);
            prestigeTitleText.raycastTarget = false;
            RectTransform ptRt = ptObj.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0f, 1f);
            ptRt.anchorMax = new Vector2(1f, 1f);
            ptRt.pivot = new Vector2(0f, 1f);
            ptRt.anchoredPosition = new Vector2(24, -12);
            ptRt.sizeDelta = new Vector2(-48, 28);

            // Prestige Buff Text
            GameObject pbObj = new GameObject("PrestigeBuff");
            pbObj.transform.SetParent(prestigeStrip.transform, false);
            prestigeBuffText = pbObj.AddComponent<TextMeshProUGUI>();
            prestigeBuffText.fontSize = 16;
            prestigeBuffText.alignment = TextAlignmentOptions.Left;
            prestigeBuffText.color = new Color(0.85f, 0.92f, 1.0f);
            prestigeBuffText.raycastTarget = false;
            RectTransform pbRt = pbObj.GetComponent<RectTransform>();
            pbRt.anchorMin = new Vector2(0f, 1f);
            pbRt.anchorMax = new Vector2(1f, 1f);
            pbRt.pivot = new Vector2(0f, 1f);
            pbRt.anchoredPosition = new Vector2(24, -44);
            pbRt.sizeDelta = new Vector2(-48, 48);

            // Prestige Action Button
            GameObject pBtnObj = new GameObject("Btn_Prestige");
            pBtnObj.transform.SetParent(prestigeStrip.transform, false);
            prestigeButton = pBtnObj.AddComponent<Button>();
            prestigeButtonImg = pBtnObj.AddComponent<Image>();
            prestigeButtonImg.sprite = whiteSp;
            RectTransform pbtnRt = pBtnObj.GetComponent<RectTransform>();
            pbtnRt.anchorMin = new Vector2(0.5f, 0f);
            pbtnRt.anchorMax = new Vector2(0.5f, 0f);
            pbtnRt.pivot = new Vector2(0.5f, 0f);
            pbtnRt.anchoredPosition = new Vector2(0, 14);
            pbtnRt.sizeDelta = new Vector2(830, 58);

            GameObject pbtnTxtObj = new GameObject("Text");
            pbtnTxtObj.transform.SetParent(pBtnObj.transform, false);
            prestigeButtonText = pbtnTxtObj.AddComponent<TextMeshProUGUI>();
            prestigeButtonText.fontSize = 20;
            prestigeButtonText.fontStyle = FontStyles.Bold;
            prestigeButtonText.alignment = TextAlignmentOptions.Center;
            prestigeButtonText.color = Color.white;
            prestigeButtonText.raycastTarget = false;
            RectTransform pbtnTxtRt = pbtnTxtObj.GetComponent<RectTransform>();
            pbtnTxtRt.anchorMin = Vector2.zero;
            pbtnTxtRt.anchorMax = Vector2.one;
            pbtnTxtRt.sizeDelta = Vector2.zero;

            prestigeButton.onClick.AddListener(OnPrestigeButtonClicked);

            // 5. Scroll View for World Cards
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            RectTransform sRt = scrollObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = new Vector2(24, 20);
            sRt.offsetMax = new Vector2(-24, -275);

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            RectTransform vRt = viewportObj.AddComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.sizeDelta = Vector2.zero;
            viewportObj.AddComponent<RectMask2D>();
            scroll.viewport = vRt;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contRt = contentObj.AddComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0f, 1f);
            contRt.anchorMax = new Vector2(1f, 1f);
            contRt.pivot = new Vector2(0.5f, 1f);
            contRt.anchoredPosition = Vector2.zero;
            contRt.sizeDelta = new Vector2(0, 950);

            VerticalLayoutGroup vLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 16;
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childForceExpandWidth = true;

            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contRt;
            cardsContainer = contentObj.transform;
        }

        private void RefreshUI()
        {
            RefreshPrestigeBanner();
            RefreshCards();
        }

        private void RefreshPrestigeBanner()
        {
            if (PrestigeManager.Instance == null) return;

            int medals = PrestigeManager.Instance.HarborMedals;
            int count = PrestigeManager.Instance.PrestigeCount;

            prestigeTitleText.text = $"HARBOR MASTER TITLE: <b>{medals} MEDALS</b> (x{count} prestiges)";

            int goldBuff = Mathf.RoundToInt((PrestigeManager.Instance.GlobalGoldMultiplier - 1.0f) * 100f);
            int speedBuff = Mathf.RoundToInt((PrestigeManager.Instance.GlobalSpeedMultiplier - 1.0f) * 100f);
            int aqBuff = Mathf.RoundToInt((PrestigeManager.Instance.AquariumBonusMultiplier - 1.0f) * 100f);

            prestigeBuffText.text = $"Permanent perks: <color=#FFE600>+{goldBuff}% Global Gold</color> | <color=#3498DB>+{speedBuff}% Speed</color> | <color=#2ECC71>+{aqBuff}% Aquarium Tickets</color>";

            WorldData curWorld = WorldManager.Instance != null ? WorldManager.Instance.CurrentWorld : null;
            if (curWorld != null)
            {
                int earnable = PrestigeManager.Instance.CalculateMedalsEarnable(curWorld.worldId);
                bool canPrestige = PrestigeManager.Instance.CanPrestige(curWorld.worldId, out string reason);

                if (canPrestige && earnable > 0)
                {
                    prestigeButton.interactable = true;
                    prestigeButtonImg.color = new Color(0.96f, 0.72f, 0.15f); // Amber gold
                    prestigeButtonText.text = $"PRESTIGE HARBOR (Earn +{earnable} Harbor Medals)";
                    prestigeButtonText.color = new Color(0.1f, 0.1f, 0.12f);
                }
                else
                {
                    prestigeButton.interactable = false;
                    prestigeButtonImg.color = new Color(0.28f, 0.32f, 0.38f, 0.8f);
                    prestigeButtonText.text = $"Accumulate more resources to Prestige (+0 Medals)";
                    prestigeButtonText.color = new Color(0.7f, 0.7f, 0.75f);
                }
            }
        }

        private void OnPrestigeButtonClicked()
        {
            if (PrestigeManager.Instance == null || WorldManager.Instance == null) return;
            WorldData curWorld = WorldManager.Instance.CurrentWorld;
            if (curWorld == null) return;

            if (PrestigeManager.Instance.PerformPrestige(curWorld, out int medalsGained))
            {
                RefreshUI();
            }
        }

        private void RefreshCards()
        {
            if (cardsContainer == null || WorldManager.Instance == null) return;

            for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardsContainer.GetChild(i).gameObject);
            }

            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();
            var worlds = WorldManager.Instance.Worlds;

            for (int i = 0; i < worlds.Count; i++)
            {
                int worldIdx = i;
                WorldData w = worlds[i];
                bool isUnlocked = WorldManager.Instance.IsWorldUnlocked(w);
                bool isCurrent = WorldManager.Instance.ActiveWorldIndex == worldIdx;
                int coinsInWorld = EconomyManager.Instance != null ? EconomyManager.Instance.GetCoinsForWorld(w.worldId) : 0;

                // Card Root (Spacious height 270px)
                GameObject cardObj = new GameObject($"WorldCard_{w.worldId}");
                cardObj.transform.SetParent(cardsContainer, false);
                RectTransform cRt = cardObj.AddComponent<RectTransform>();
                cRt.sizeDelta = new Vector2(0, 270);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = whiteSp;
                cardBg.color = isCurrent 
                    ? new Color(0.16f, 0.28f, 0.42f, 0.95f) 
                    : (isUnlocked ? new Color(0.12f, 0.16f, 0.22f, 0.95f) : new Color(0.08f, 0.10f, 0.14f, 0.90f));

                // 1. Header: World Title & Status
                GameObject nameObj = new GameObject("Name");
                nameObj.transform.SetParent(cardObj.transform, false);
                var nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
                nameTxt.text = $"LEVEL {w.worldOrder}: {w.worldName.ToUpper()}";
                nameTxt.fontSize = 22;
                nameTxt.fontStyle = FontStyles.Bold;
                nameTxt.color = isCurrent ? new Color(0.4f, 0.9f, 1f) : Color.white;
                nameTxt.raycastTarget = false;
                RectTransform nRt = nameObj.GetComponent<RectTransform>();
                nRt.anchorMin = new Vector2(0f, 1f);
                nRt.anchorMax = new Vector2(0.65f, 1f);
                nRt.pivot = new Vector2(0f, 1f);
                nRt.anchoredPosition = new Vector2(24, -14);
                nRt.sizeDelta = new Vector2(0, 32);

                // Status Badge
                GameObject statusObj = new GameObject("StatusBadge");
                statusObj.transform.SetParent(cardObj.transform, false);
                var statusTxt = statusObj.AddComponent<TextMeshProUGUI>();
                statusTxt.fontSize = 18;
                statusTxt.fontStyle = FontStyles.Bold;
                statusTxt.alignment = TextAlignmentOptions.Right;
                statusTxt.raycastTarget = false;
                RectTransform sRt = statusObj.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.65f, 1f);
                sRt.anchorMax = new Vector2(1f, 1f);
                sRt.pivot = new Vector2(1f, 1f);
                sRt.anchoredPosition = new Vector2(-24, -14);
                sRt.sizeDelta = new Vector2(0, 32);

                if (isCurrent)
                {
                    statusTxt.text = $"[LEVEL {w.worldOrder} ACTIVE]";
                    statusTxt.color = new Color(0.2f, 0.9f, 1f);
                }
                else if (isUnlocked)
                {
                    statusTxt.text = $"[LEVEL {w.worldOrder} UNLOCKED]";
                    statusTxt.color = new Color(0.2f, 0.85f, 0.4f);
                }
                else
                {
                    statusTxt.text = $"[LEVEL {w.worldOrder} LOCKED]";
                    statusTxt.color = new Color(0.7f, 0.7f, 0.75f);
                }

                // 2. World Description
                GameObject descObj = new GameObject("Desc");
                descObj.transform.SetParent(cardObj.transform, false);
                var descTxt = descObj.AddComponent<TextMeshProUGUI>();
                descTxt.text = w.description;
                descTxt.fontSize = 16;
                descTxt.color = new Color(0.80f, 0.85f, 0.90f);
                descTxt.raycastTarget = false;
                RectTransform dRt = descObj.GetComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0f, 1f);
                dRt.anchorMax = new Vector2(1f, 1f);
                dRt.pivot = new Vector2(0f, 1f);
                dRt.anchoredPosition = new Vector2(24, -50);
                dRt.sizeDelta = new Vector2(-48, 44);

                // 3. Native Species List
                GameObject fishListObj = new GameObject("FishSpecies");
                fishListObj.transform.SetParent(cardObj.transform, false);
                var fTxt = fishListObj.AddComponent<TextMeshProUGUI>();
                string fishNames = "";
                if (w.nativeSpecies != null && w.nativeSpecies.Count > 0)
                {
                    for (int f = 0; f < w.nativeSpecies.Count; f++)
                    {
                        if (w.nativeSpecies[f] != null)
                        {
                            fishNames += (f > 0 ? ", " : "") + w.nativeSpecies[f].SpeciesName;
                        }
                    }
                }
                fTxt.text = $"Native species: <color=#3498DB><b>{fishNames}</b></color>";
                fTxt.fontSize = 16;
                fTxt.color = new Color(0.85f, 0.90f, 0.95f);
                fTxt.raycastTarget = false;
                RectTransform fRt = fishListObj.GetComponent<RectTransform>();
                fRt.anchorMin = new Vector2(0f, 1f);
                fRt.anchorMax = new Vector2(1f, 1f);
                fRt.pivot = new Vector2(0f, 1f);
                fRt.anchoredPosition = new Vector2(24, -100);
                fRt.sizeDelta = new Vector2(-48, 30);

                // 4. Wallet Info (if unlocked)
                GameObject walletObj = new GameObject("WalletText");
                walletObj.transform.SetParent(cardObj.transform, false);
                var wTxt = walletObj.AddComponent<TextMeshProUGUI>();
                wTxt.fontSize = 18;
                wTxt.fontStyle = FontStyles.Bold;
                wTxt.raycastTarget = false;
                RectTransform wRt = walletObj.GetComponent<RectTransform>();
                wRt.anchorMin = new Vector2(0f, 0f);
                wRt.anchorMax = new Vector2(0.55f, 0f);
                wRt.pivot = new Vector2(0f, 0f);
                wRt.anchoredPosition = new Vector2(24, 20);
                wRt.sizeDelta = new Vector2(0, 56);

                if (isUnlocked)
                {
                    wTxt.text = $"Harbor Vault: <color=#FFE600>{coinsInWorld:N0} G</color>";
                    wTxt.color = Color.white;
                }
                else
                {
                    bool canUnlock = WorldManager.Instance.CanUnlockWorld(w, out string reason);
                    wTxt.text = canUnlock ? "<color=#2ECC71>Ready to unlock!</color>" : $"<color=#E74C3C>{reason}</color>";
                    wTxt.fontSize = 15;
                }

                // 5. Action Button
                GameObject btnObj = new GameObject("ActionButton");
                btnObj.transform.SetParent(cardObj.transform, false);
                Button btn = btnObj.AddComponent<Button>();
                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.sprite = whiteSp;

                RectTransform bRt = btnObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 0f);
                bRt.anchorMax = new Vector2(1f, 0f);
                bRt.pivot = new Vector2(1f, 0f);
                bRt.anchoredPosition = new Vector2(-24, 16);
                bRt.sizeDelta = new Vector2(240, 60);

                GameObject btnTxtObj = new GameObject("Text");
                btnTxtObj.transform.SetParent(btnObj.transform, false);
                var bTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
                bTxt.fontSize = 19;
                bTxt.fontStyle = FontStyles.Bold;
                bTxt.alignment = TextAlignmentOptions.Center;
                bTxt.raycastTarget = false;
                RectTransform btRt = btnTxtObj.GetComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero;
                btRt.anchorMax = Vector2.one;
                btRt.sizeDelta = Vector2.zero;

                if (isCurrent)
                {
                    btnImg.color = new Color(0.25f, 0.30f, 0.38f);
                    bTxt.text = $"ACTIVE (LV {w.worldOrder})";
                    bTxt.color = new Color(0.75f, 0.75f, 0.8f);
                    btn.interactable = false;
                }
                else if (isUnlocked)
                {
                    btnImg.color = new Color(0.2f, 0.72f, 0.38f);
                    bTxt.text = $"TRAVEL (LV {w.worldOrder})";
                    bTxt.color = Color.white;
                    btn.onClick.AddListener(() =>
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                        WorldManager.Instance.SwitchToWorld(worldIdx);
                        Close();
                    });
                }
                else
                {
                    bool canUnlock = WorldManager.Instance.CanUnlockWorld(w, out _);
                    btnImg.color = canUnlock ? new Color(0.96f, 0.72f, 0.15f) : new Color(0.4f, 0.25f, 0.25f);
                    bTxt.text = canUnlock ? $"UNLOCK LV {w.worldOrder}" : $"LOCKED (LV {w.worldOrder})";
                    bTxt.color = canUnlock ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
                    btn.interactable = canUnlock;

                    btn.onClick.AddListener(() =>
                    {
                        if (WorldManager.Instance.TryUnlockWorld(w, out string _))
                        {
                            if (AudioManager.Instance != null) AudioManager.Instance.PlayChestOpen();
                            if (IdleFishing.VFX.VFXManager.Instance != null)
                            {
                                IdleFishing.VFX.VFXManager.Instance.SpawnConfetti(Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 4f : Vector3.up * 2f, 1.5f);
                            }
                            WorldManager.Instance.SwitchToWorld(worldIdx);
                            Close();
                        }
                    });
                }
            }
        }
    }
}

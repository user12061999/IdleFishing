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
                popupRoot.transform.SetAsLastSibling();
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
            var existingLevelBadge = GameObject.Find("LevelBadge_Button");
            if (existingLevelBadge != null)
            {
                // LevelBadge_Button in TopHUD already displays Level & World name and opens this popup!
                return;
            }

            mapNavButtonObj = new GameObject("WorldMap_NavButton");
            mapNavButtonObj.transform.SetParent(transform, false);

            RectTransform rt = mapNavButtonObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20, -20);
            rt.sizeDelta = new Vector2(195, 62);

            Image img = mapNavButtonObj.AddComponent<Image>();
            Button btn = mapNavButtonObj.AddComponent<Button>();
            CasualUITheme.StyleButton(btn, img, CasualBtnStyle.Blue);
            btn.onClick.AddListener(Open);

            // Icon Compass
            if (CasualUITheme.IconCompass != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(mapNavButtonObj.transform, false);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = CasualUITheme.IconCompass;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
                RectTransform iconRt = iconObj.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(10, 0);
                iconRt.sizeDelta = new Vector2(36, 36);
            }

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(mapNavButtonObj.transform, false);
            mapNavButtonText = txtObj.AddComponent<TextMeshProUGUI>();
            mapNavButtonText.fontSize = 22;
            mapNavButtonText.fontStyle = FontStyles.Bold;
            mapNavButtonText.alignment = TextAlignmentOptions.MidlineLeft;
            mapNavButtonText.color = Color.white;
            mapNavButtonText.raycastTarget = false;
            mapNavButtonText.ApplyFont(UIFontCategory.Button);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(50, 2);
            txtRt.offsetMax = new Vector2(-8, -2);

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
            // 1. Dark Backdrop
            popupRoot = new GameObject("WorldSelection_Backdrop");
            popupRoot.transform.SetParent(transform, false);
            RectTransform rootRt = popupRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Image darkBg = popupRoot.AddComponent<Image>();
            if (CasualUITheme.DimmedBg != null)
            {
                darkBg.sprite = CasualUITheme.DimmedBg;
                darkBg.color = new Color(0, 0, 0, 0.82f);
            }
            else
            {
                darkBg.sprite = FloatingHealthBarUI.GetWhiteSprite();
                darkBg.color = new Color(0, 0, 0, 0.82f);
            }

            // 2. Dialog Box
            GameObject panelObj = new GameObject("DialogPanel");
            panelObj.transform.SetParent(popupRoot.transform, false);
            RectTransform pRt = panelObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(880, 1300);

            Image panelBg = panelObj.AddComponent<Image>();
            CasualUITheme.StylePopupDialog(panelBg);

            // 3. Header Ribbon Banner
            GameObject ribbonObj = new GameObject("HeaderRibbon");
            ribbonObj.transform.SetParent(panelObj.transform, false);
            RectTransform rRt = ribbonObj.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 1f);
            rRt.anchorMax = new Vector2(0.5f, 1f);
            rRt.pivot = new Vector2(0.5f, 0.5f);
            rRt.anchoredPosition = new Vector2(0, -20);
            rRt.sizeDelta = new Vector2(620, 84);

            Image rImg = ribbonObj.AddComponent<Image>();
            rImg.sprite = CasualUITheme.TitleRibbonBlue ?? CasualUITheme.TitleRibbonYellow;
            rImg.type = (rImg.sprite != null && rImg.sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            rImg.color = Color.white;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(ribbonObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "OCEAN EXPEDITION";
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = Color.white;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.raycastTarget = false;
            titleTxt.ApplyFont(UIFontCategory.Header);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            // Close button
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(panelObj.transform, false);
            Button closeBtn = closeObj.AddComponent<Button>();
            Image cImg = closeObj.AddComponent<Image>();
            CasualUITheme.StyleButton(closeBtn, cImg, CasualBtnStyle.SquareGray);
            RectTransform cRt = closeObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(1f, 1f);
            cRt.anchoredPosition = new Vector2(-20, -20);
            cRt.sizeDelta = new Vector2(56, 56);

            if (CasualUITheme.IconClose != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(closeObj.transform, false);
                Image ic = iconObj.AddComponent<Image>();
                ic.sprite = CasualUITheme.IconClose;
                ic.color = Color.white;
                ic.raycastTarget = false;
                RectTransform icRt = iconObj.GetComponent<RectTransform>();
                icRt.anchorMin = Vector2.zero;
                icRt.anchorMax = Vector2.one;
                icRt.offsetMin = new Vector2(12, 12);
                icRt.offsetMax = new Vector2(-12, -12);
            }
            closeBtn.onClick.AddListener(Close);

            // Quick Unlock All (Test / Preview) button
            GameObject unlockAllObj = new GameObject("Btn_UnlockAllTest");
            unlockAllObj.transform.SetParent(panelObj.transform, false);
            Button uaBtn = unlockAllObj.AddComponent<Button>();
            Image uaImg = unlockAllObj.AddComponent<Image>();
            CasualUITheme.StyleButton(uaBtn, uaImg, CasualBtnStyle.Dark);
            RectTransform uaRt = unlockAllObj.GetComponent<RectTransform>();
            uaRt.anchorMin = new Vector2(0f, 1f);
            uaRt.anchorMax = new Vector2(0f, 1f);
            uaRt.pivot = new Vector2(0f, 1f);
            uaRt.anchoredPosition = new Vector2(20, -24);
            uaRt.sizeDelta = new Vector2(160, 48);

            GameObject uaTxtObj = new GameObject("Text");
            uaTxtObj.transform.SetParent(unlockAllObj.transform, false);
            var uaTxt = uaTxtObj.AddComponent<TextMeshProUGUI>();
            uaTxt.text = "UNLOCK ALL";
            uaTxt.fontSize = 19;
            uaTxt.fontStyle = FontStyles.Bold;
            uaTxt.alignment = TextAlignmentOptions.Center;
            uaTxt.color = Color.white;
            uaTxt.raycastTarget = false;
            uaTxt.ApplyFont(UIFontCategory.Button);
            RectTransform uatRt = uaTxtObj.GetComponent<RectTransform>();
            uatRt.anchorMin = Vector2.zero;
            uatRt.anchorMax = Vector2.one;
            uatRt.sizeDelta = Vector2.zero;

            uaBtn.onClick.AddListener(() =>
            {
                if (WorldManager.Instance != null)
                {
                    WorldManager.Instance.UnlockAllWorldsForTesting();
                    RefreshUI();
                }
            });

            // 4. Prestige Strip Section (Thẻ Vinh Danh Cảng Trưởng)
            GameObject prestigeStrip = new GameObject("PrestigeStrip");
            prestigeStrip.transform.SetParent(panelObj.transform, false);
            RectTransform psRt = prestigeStrip.AddComponent<RectTransform>();
            psRt.anchorMin = new Vector2(0f, 1f);
            psRt.anchorMax = new Vector2(1f, 1f);
            psRt.pivot = new Vector2(0.5f, 1f);
            psRt.anchoredPosition = new Vector2(0, -90);
            psRt.sizeDelta = new Vector2(0, 190);

            Image psBg = prestigeStrip.AddComponent<Image>();
            CasualUITheme.StyleCard(psBg, new Color(0.10f, 0.14f, 0.22f, 0.98f));

            // Crown Icon
            if (CasualUITheme.IconCrown != null)
            {
                GameObject crownObj = new GameObject("CrownIcon");
                crownObj.transform.SetParent(prestigeStrip.transform, false);
                Image crownImg = crownObj.AddComponent<Image>();
                crownImg.sprite = CasualUITheme.IconCrown;
                crownImg.color = Color.white;
                crownImg.raycastTarget = false;
                RectTransform crt = crownObj.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0f, 1f);
                crt.anchorMax = new Vector2(0f, 1f);
                crt.pivot = new Vector2(0f, 1f);
                crt.anchoredPosition = new Vector2(24, -14);
                crt.sizeDelta = new Vector2(32, 32);
            }

            // Prestige Title Text
            GameObject ptObj = new GameObject("PrestigeTitle");
            ptObj.transform.SetParent(prestigeStrip.transform, false);
            prestigeTitleText = ptObj.AddComponent<TextMeshProUGUI>();
            prestigeTitleText.fontSize = 25;
            prestigeTitleText.fontStyle = FontStyles.Bold;
            prestigeTitleText.alignment = TextAlignmentOptions.Left;
            prestigeTitleText.color = new Color(1f, 0.85f, 0.2f);
            prestigeTitleText.raycastTarget = false;
            prestigeTitleText.ApplyFont(UIFontCategory.Header);
            RectTransform ptRt = ptObj.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0f, 1f);
            ptRt.anchorMax = new Vector2(1f, 1f);
            ptRt.pivot = new Vector2(0f, 1f);
            ptRt.anchoredPosition = new Vector2(66, -14);
            ptRt.sizeDelta = new Vector2(-80, 30);

            // Prestige Buff Text
            GameObject pbObj = new GameObject("PrestigeBuff");
            pbObj.transform.SetParent(prestigeStrip.transform, false);
            prestigeBuffText = pbObj.AddComponent<TextMeshProUGUI>();
            prestigeBuffText.fontSize = 21;
            prestigeBuffText.alignment = TextAlignmentOptions.Left;
            prestigeBuffText.color = new Color(0.85f, 0.92f, 1.0f);
            prestigeBuffText.raycastTarget = false;
            prestigeBuffText.ApplyFont(UIFontCategory.Default);
            RectTransform pbRt = pbObj.GetComponent<RectTransform>();
            pbRt.anchorMin = new Vector2(0f, 1f);
            pbRt.anchorMax = new Vector2(1f, 1f);
            pbRt.pivot = new Vector2(0f, 1f);
            pbRt.anchoredPosition = new Vector2(24, -50);
            pbRt.sizeDelta = new Vector2(-48, 48);

            // Prestige Action Button
            GameObject pBtnObj = new GameObject("Btn_Prestige");
            pBtnObj.transform.SetParent(prestigeStrip.transform, false);
            prestigeButton = pBtnObj.AddComponent<Button>();
            prestigeButtonImg = pBtnObj.AddComponent<Image>();
            CasualUITheme.StyleButton(prestigeButton, prestigeButtonImg, CasualBtnStyle.Yellow);
            RectTransform pbtnRt = pBtnObj.GetComponent<RectTransform>();
            pbtnRt.anchorMin = new Vector2(0.5f, 0f);
            pbtnRt.anchorMax = new Vector2(0.5f, 0f);
            pbtnRt.pivot = new Vector2(0.5f, 0f);
            pbtnRt.anchoredPosition = new Vector2(0, 14);
            pbtnRt.sizeDelta = new Vector2(810, 58);

            GameObject pbtnTxtObj = new GameObject("Text");
            pbtnTxtObj.transform.SetParent(pBtnObj.transform, false);
            prestigeButtonText = pbtnTxtObj.AddComponent<TextMeshProUGUI>();
            prestigeButtonText.fontSize = 24;
            prestigeButtonText.fontStyle = FontStyles.Bold;
            prestigeButtonText.alignment = TextAlignmentOptions.Center;
            prestigeButtonText.color = Color.white;
            prestigeButtonText.raycastTarget = false;
            prestigeButtonText.ApplyFont(UIFontCategory.Button);
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
            sRt.offsetMax = new Vector2(-24, -295);

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

            UIFontConfig.Instance?.ApplyToHierarchy(popupRoot);
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
                    CasualUITheme.StyleButton(prestigeButton, prestigeButtonImg, CasualBtnStyle.Yellow);
                    prestigeButtonText.text = $"DEEP SEA EXPEDITION (Harvest +{earnable} Pearls & Relics)";
                    prestigeButtonText.color = new Color(0.2f, 0.12f, 0.02f);
                }
                else
                {
                    prestigeButton.interactable = true;
                    CasualUITheme.StyleButton(prestigeButton, prestigeButtonImg, CasualBtnStyle.Blue);
                    prestigeButtonText.text = "VIEW ANCIENT RELICS & EXPEDITION";
                    prestigeButtonText.color = Color.white;
                }
            }
        }

        private void OnPrestigeButtonClicked()
        {
            if (IdleFishing.UI.DeepSeaExpeditionPopupUI.Instance != null)
            {
                Close();
                IdleFishing.UI.DeepSeaExpeditionPopupUI.Instance.Open(0);
            }
        }

        private void RefreshCards()
        {
            if (cardsContainer == null || WorldManager.Instance == null) return;

            for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardsContainer.GetChild(i).gameObject);
            }

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
                CasualUITheme.StyleCard(cardBg, isCurrent 
                    ? new Color(0.14f, 0.24f, 0.38f, 0.98f) 
                    : (isUnlocked ? new Color(0.10f, 0.14f, 0.22f, 0.98f) : new Color(0.07f, 0.09f, 0.13f, 0.95f)));

                // 1. Header: World Title & Status
                GameObject nameObj = new GameObject("Name");
                nameObj.transform.SetParent(cardObj.transform, false);
                var nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
                nameTxt.text = $"LEVEL {w.worldOrder}: {w.worldName.ToUpper()}";
                nameTxt.fontSize = 26;
                nameTxt.fontStyle = FontStyles.Bold;
                nameTxt.color = isCurrent ? new Color(0.4f, 0.9f, 1f) : Color.white;
                nameTxt.raycastTarget = false;
                nameTxt.ApplyFont(UIFontCategory.Header);
                RectTransform nRt = nameObj.GetComponent<RectTransform>();
                nRt.anchorMin = new Vector2(0f, 1f);
                nRt.anchorMax = new Vector2(0.65f, 1f);
                nRt.pivot = new Vector2(0f, 1f);
                nRt.anchoredPosition = new Vector2(24, -14);
                nRt.sizeDelta = new Vector2(0, 34);

                // Status Badge
                GameObject statusObj = new GameObject("StatusBadge");
                statusObj.transform.SetParent(cardObj.transform, false);
                var statusTxt = statusObj.AddComponent<TextMeshProUGUI>();
                statusTxt.fontSize = 21;
                statusTxt.fontStyle = FontStyles.Bold;
                statusTxt.alignment = TextAlignmentOptions.Right;
                statusTxt.raycastTarget = false;
                statusTxt.ApplyFont(UIFontCategory.Button);
                RectTransform sRt = statusObj.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.65f, 1f);
                sRt.anchorMax = new Vector2(1f, 1f);
                sRt.pivot = new Vector2(1f, 1f);
                sRt.anchoredPosition = new Vector2(-24, -14);
                sRt.sizeDelta = new Vector2(0, 34);

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
                descTxt.fontSize = 20;
                descTxt.color = new Color(0.80f, 0.85f, 0.90f);
                descTxt.raycastTarget = false;
                descTxt.ApplyFont(UIFontCategory.Default);
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
                fTxt.fontSize = 20;
                fTxt.color = new Color(0.85f, 0.90f, 0.95f);
                fTxt.raycastTarget = false;
                fTxt.ApplyFont(UIFontCategory.Default);
                RectTransform fRt = fishListObj.GetComponent<RectTransform>();
                fRt.anchorMin = new Vector2(0f, 1f);
                fRt.anchorMax = new Vector2(1f, 1f);
                fRt.pivot = new Vector2(0f, 1f);
                fRt.anchoredPosition = new Vector2(24, -100);
                fRt.sizeDelta = new Vector2(-48, 30);

                // 4. Wallet Info (if unlocked)
                if (CasualUITheme.IconGold != null && isUnlocked)
                {
                    GameObject gIconObj = new GameObject("GoldIcon");
                    gIconObj.transform.SetParent(cardObj.transform, false);
                    Image gIcon = gIconObj.AddComponent<Image>();
                    gIcon.sprite = CasualUITheme.IconGold;
                    gIcon.color = Color.white;
                    gIcon.raycastTarget = false;
                    RectTransform gRt = gIconObj.GetComponent<RectTransform>();
                    gRt.anchorMin = new Vector2(0f, 0f);
                    gRt.anchorMax = new Vector2(0f, 0f);
                    gRt.pivot = new Vector2(0f, 0f);
                    gRt.anchoredPosition = new Vector2(24, 28);
                    gRt.sizeDelta = new Vector2(36, 36);
                }

                GameObject walletObj = new GameObject("WalletText");
                walletObj.transform.SetParent(cardObj.transform, false);
                var wTxt = walletObj.AddComponent<TextMeshProUGUI>();
                wTxt.fontSize = 21;
                wTxt.fontStyle = FontStyles.Bold;
                wTxt.raycastTarget = false;
                wTxt.ApplyFont(UIFontCategory.Button);
                RectTransform wRt = walletObj.GetComponent<RectTransform>();
                wRt.anchorMin = new Vector2(0f, 0f);
                wRt.anchorMax = new Vector2(0.55f, 0f);
                wRt.pivot = new Vector2(0f, 0f);
                wRt.anchoredPosition = new Vector2(isUnlocked ? 66 : 24, 20);
                wRt.sizeDelta = new Vector2(0, 56);

                if (isUnlocked)
                {
                    wTxt.text = $"Harbor Vault: <color=#FFE600>{NumberFormatter.Format(coinsInWorld)} G</color>";
                    wTxt.color = Color.white;
                }
                else
                {
                    bool canUnlock = WorldManager.Instance.CanUnlockWorld(w, out string reason);
                    wTxt.text = canUnlock ? "<color=#2ECC71>Ready to unlock!</color>" : $"<color=#E74C3C>{reason}</color>";
                    wTxt.fontSize = 19;
                }

                // 5. Action Button
                GameObject btnObj = new GameObject("ActionButton");
                btnObj.transform.SetParent(cardObj.transform, false);
                Button btn = btnObj.AddComponent<Button>();
                Image btnImg = btnObj.AddComponent<Image>();

                RectTransform bRt = btnObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 0f);
                bRt.anchorMax = new Vector2(1f, 0f);
                bRt.pivot = new Vector2(1f, 0f);
                bRt.anchoredPosition = new Vector2(-24, 16);
                bRt.sizeDelta = new Vector2(240, 60);

                GameObject btnTxtObj = new GameObject("Text");
                btnTxtObj.transform.SetParent(btnObj.transform, false);
                var bTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
                bTxt.fontSize = 22;
                bTxt.fontStyle = FontStyles.Bold;
                bTxt.alignment = TextAlignmentOptions.Center;
                bTxt.raycastTarget = false;
                bTxt.ApplyFont(UIFontCategory.Button);
                RectTransform btRt = btnTxtObj.GetComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero;
                btRt.anchorMax = Vector2.one;
                btRt.sizeDelta = Vector2.zero;

                if (isCurrent)
                {
                    CasualUITheme.StyleButton(btn, btnImg, CasualBtnStyle.Dark);
                    bTxt.text = $"ACTIVE (LV {w.worldOrder})";
                    bTxt.color = new Color(0.75f, 0.75f, 0.8f);
                    btn.interactable = false;
                }
                else if (isUnlocked)
                {
                    CasualUITheme.StyleButton(btn, btnImg, CasualBtnStyle.Green);
                    bTxt.text = $"TRAVEL (LV {w.worldOrder})";
                    bTxt.color = Color.white;
                    btn.onClick.AddListener(() =>
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                        Close();
                        WorldManager.Instance.RequestWorldTransition(worldIdx, isPreview: false);
                    });
                }
                else
                {
                    // 1. Dedicated PREVIEW Button (Cho phép vào xem trước mọi cấp độ bị khóa)
                    GameObject prevBtnObj = new GameObject("Btn_Preview");
                    prevBtnObj.transform.SetParent(cardObj.transform, false);
                    Button prevBtn = prevBtnObj.AddComponent<Button>();
                    Image prevBtnImg = prevBtnObj.AddComponent<Image>();
                    CasualUITheme.StyleButton(prevBtn, prevBtnImg, CasualBtnStyle.Blue);

                    RectTransform pbRt = prevBtnObj.GetComponent<RectTransform>();
                    pbRt.anchorMin = new Vector2(1f, 0f);
                    pbRt.anchorMax = new Vector2(1f, 0f);
                    pbRt.pivot = new Vector2(1f, 0f);
                    pbRt.anchoredPosition = new Vector2(-190, 16);
                    pbRt.sizeDelta = new Vector2(150, 60);

                    GameObject pbTxtObj = new GameObject("Text");
                    pbTxtObj.transform.SetParent(prevBtnObj.transform, false);
                    var pbTxt = pbTxtObj.AddComponent<TextMeshProUGUI>();
                    pbTxt.fontSize = 20;
                    pbTxt.fontStyle = FontStyles.Bold;
                    pbTxt.alignment = TextAlignmentOptions.Center;
                    pbTxt.text = "PREVIEW";
                    pbTxt.color = Color.white;
                    pbTxt.raycastTarget = false;
                    pbTxt.ApplyFont(UIFontCategory.Button);
                    RectTransform pbtRt = pbTxtObj.GetComponent<RectTransform>();
                    pbtRt.anchorMin = Vector2.zero;
                    pbtRt.anchorMax = Vector2.one;
                    pbtRt.sizeDelta = Vector2.zero;

                    prevBtn.onClick.AddListener(() =>
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                        Close();
                        WorldManager.Instance.RequestWorldTransition(worldIdx, isPreview: true);
                    });

                    // 2. UNLOCK Button
                    bRt.anchoredPosition = new Vector2(-24, 16);
                    bRt.sizeDelta = new Vector2(155, 60);

                    bool canUnlock = WorldManager.Instance.CanUnlockWorld(w, out _);
                    CasualUITheme.StyleButton(btn, btnImg, canUnlock ? CasualBtnStyle.Yellow : CasualBtnStyle.Gray);
                    bTxt.text = canUnlock ? $"UNLOCK" : $"LOCKED";
                    bTxt.color = canUnlock ? Color.white : new Color(0.7f, 0.7f, 0.75f);
                    btn.interactable = canUnlock;

                    btn.onClick.AddListener(() =>
                    {
                        Close();
                        WorldUnlockCelebrationModal.Instance.Show(w, worldIdx);
                    });
                }
            }

            if (cardsContainer != null)
            {
                UIFontConfig.Instance?.ApplyToHierarchy(cardsContainer.gameObject);
            }
        }
    }
}

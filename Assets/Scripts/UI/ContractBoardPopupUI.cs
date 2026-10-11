using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Market;

namespace IdleFishing.UI
{
    public class ContractBoardPopupUI : MonoBehaviour
    {
        public static ContractBoardPopupUI Instance { get; private set; }

        private GameObject popupRoot;
        private TextMeshProUGUI levelTitleText;
        private TextMeshProUGUI fameProgressText;
        private TextMeshProUGUI buffSummaryText;
        private Image fameFillImage;

        // 3 Cards
        private class ContractCardUI
        {
            public GameObject cardObj;
            public TextMeshProUGUI clientText;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI descText;
            public TextMeshProUGUI progressText;
            public TextMeshProUGUI rewardText;
            public TextMeshProUGUI timerText;
            public Button deliverButton;
            public Image deliverBtnBg;
            public TextMeshProUGUI deliverBtnText;
            public HarborContract boundContract;
        }

        private List<ContractCardUI> cardUIs = new List<ContractCardUI>();
        private float updateTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildProceduralPopup();
            Close();
        }

        private void Start()
        {
            WireHUDButton();
        }

        public void WireHUDButton()
        {
            var btnObj = GameObject.Find("Btn_ContractsBoard");
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
            GameEvents.ContractsRefreshed += RefreshCards;
            GameEvents.ContractCompleted += OnContractCompleted;
            GameEvents.HarborFameChanged += OnHarborFameChanged;
        }

        private void OnDisable()
        {
            GameEvents.ContractsRefreshed -= RefreshCards;
            GameEvents.ContractCompleted -= OnContractCompleted;
            GameEvents.HarborFameChanged -= OnHarborFameChanged;
        }

        private void OnContractCompleted(HarborContract contract)
        {
            RefreshUI();
        }

        private void OnHarborFameChanged(int fame, int level)
        {
            UpdateHarborProgressionDisplay();
        }

        private void Update()
        {
            if (popupRoot == null || !popupRoot.activeSelf) return;

            updateTimer += Time.deltaTime;
            if (updateTimer >= 0.25f)
            {
                updateTimer = 0f;
                UpdateLiveCardStates();
            }
        }

        public void Open()
        {
            if (popupRoot == null) BuildProceduralPopup();
            popupRoot.SetActive(true);
            RefreshUI();
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

        public void RefreshUI()
        {
            UpdateHarborProgressionDisplay();
            RefreshCards();
        }

        private void UpdateHarborProgressionDisplay()
        {
            if (ContractManager.Instance == null) return;

            int level = ContractManager.Instance.HarborLevel;
            int fame = ContractManager.Instance.CurrentFame;
            int baseFame = ContractManager.Instance.GetFameBaseForCurrentLevel();
            int nextFame = ContractManager.Instance.GetFameTargetForNextLevel();
            string title = ContractManager.Instance.GetLevelTitle(level);

            if (levelTitleText != null)
            {
                levelTitleText.text = $"<color=#FFE600>LEVEL {level}: {title}</color>";
            }

            if (fameProgressText != null)
            {
                if (level >= 4)
                {
                    fameProgressText.text = $"Reputation: {fame} FAME (MAX LEVEL)";
                }
                else
                {
                    fameProgressText.text = $"Reputation: {fame} / {nextFame} FAME";
                }
            }

            if (fameFillImage != null)
            {
                if (level >= 4)
                {
                    fameFillImage.fillAmount = 1.0f;
                }
                else
                {
                    float range = Mathf.Max(1, nextFame - baseFame);
                    float cur = Mathf.Clamp(fame - baseFame, 0, range);
                    fameFillImage.fillAmount = cur / range;
                }
            }

            if (buffSummaryText != null)
            {
                int priceBonus = Mathf.RoundToInt((ContractManager.Instance.GetGlobalPriceMultiplier() - 1.0f) * 100f);
                int speedBonus = Mathf.RoundToInt((ContractManager.Instance.GetGlobalStationSpeedMultiplier() - 1.0f) * 100f);
                buffSummaryText.text = $"Harbor Perks: <color=#2ECC71>+{priceBonus}% Order Sell Price</color>  |  <color=#3498DB>+{speedBonus}% Logistics Speed</color>";
            }
        }

        private void RefreshCards()
        {
            if (ContractManager.Instance == null) return;

            var contracts = ContractManager.Instance.ActiveContracts;
            for (int i = 0; i < cardUIs.Count; i++)
            {
                var card = cardUIs[i];
                if (i < contracts.Count && contracts[i] != null)
                {
                    card.boundContract = contracts[i];
                    card.cardObj.SetActive(true);
                    BindCardData(card, contracts[i]);
                }
                else
                {
                    card.boundContract = null;
                    card.cardObj.SetActive(false);
                }
            }
        }

        private void BindCardData(ContractCardUI card, HarborContract c)
        {
            if (card == null || c == null) return;

            card.clientText.text = $"{c.clientIcon} {c.clientName}";
            card.titleText.text = c.contractTitle;
            card.descText.text = c.contractDesc;

            card.rewardText.text = $"<color=#FFE600>+{c.rewardGold:N0} G</color>  |  <color=#58D68D>+{c.rewardFame} FAME</color>\n<color=#E74C3C>+{c.frenzyChargeBonus:0}% FRENZY</color>";

            UpdateSingleCardLiveState(card);
        }

        private void UpdateLiveCardStates()
        {
            for (int i = 0; i < cardUIs.Count; i++)
            {
                var card = cardUIs[i];
                if (card.cardObj.activeSelf && card.boundContract != null)
                {
                    UpdateSingleCardLiveState(card);
                }
            }
        }

        private void UpdateSingleCardLiveState(ContractCardUI card)
        {
            var c = card.boundContract;
            if (c == null) return;

            if (c.isCompleted)
            {
                card.progressText.text = "<color=#2ECC71><b>COMPLETED</b></color>";
                card.timerText.text = "Preparing new contract...";
                card.deliverButton.interactable = false;
                card.deliverBtnBg.color = new Color(0.3f, 0.35f, 0.4f, 0.8f);
                card.deliverBtnText.text = "COMPLETE";
                return;
            }

            int available = ContractManager.Instance != null ? ContractManager.Instance.GetAvailableItemCount(c.targetItem) : 0;
            string itemName = c.targetItem != null ? c.targetItem.ItemName : "Seafood";

            if (available >= c.requiredCount)
            {
                card.progressText.text = $"Required: <b>{itemName}</b>\nProgress: <color=#2ECC71><b>{available} / {c.requiredCount} (READY)</b></color>";
                card.deliverButton.interactable = true;
                card.deliverBtnBg.color = new Color(0.18f, 0.80f, 0.44f, 1f); // Vibrant Green
                card.deliverBtnText.text = "DELIVER NOW";
            }
            else
            {
                card.progressText.text = $"Required: <b>{itemName}</b>\nProgress: <color=#F39C12><b>{available} / {c.requiredCount}</b></color>";
                card.deliverButton.interactable = false;
                card.deliverBtnBg.color = new Color(0.28f, 0.32f, 0.40f, 0.9f); // Dim navy
                card.deliverBtnText.text = $"NEED {c.requiredCount - available} MORE";
            }

            // Live Countdown Timer
            int minutes = Mathf.FloorToInt(Mathf.Max(0, c.timeRemaining) / 60f);
            int seconds = Mathf.FloorToInt(Mathf.Max(0, c.timeRemaining) % 60f);
            card.timerText.text = $"Deadline: {minutes:D2}:{seconds:D2}";
        }

        private void BuildProceduralPopup()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Dark Backdrop
            popupRoot = new GameObject("ContractBoard_Backdrop");
            popupRoot.transform.SetParent(transform, false);

            RectTransform rootRect = popupRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            Image darkBg = popupRoot.AddComponent<Image>();
            darkBg.sprite = whiteSp;
            darkBg.color = new Color(0f, 0f, 0f, 0.80f);

            // 2. Main Dialog Box
            GameObject dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(popupRoot.transform, false);
            RectTransform dRect = dialogBox.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.sizeDelta = new Vector2(860, 560);

            // Dialog Border & BG
            Image dBg = dialogBox.AddComponent<Image>();
            dBg.sprite = whiteSp;
            dBg.color = new Color(0.10f, 0.13f, 0.19f, 0.98f); // Deep oceanic charcoal

            // 3. Header Title Bar
            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(dialogBox.transform, false);
            RectTransform headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = new Vector2(0, 0);
            headerRt.sizeDelta = new Vector2(0, 60);

            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.sprite = whiteSp;
            headerBg.color = new Color(0.06f, 0.08f, 0.13f, 1f);

            GameObject titleTxtObj = new GameObject("TitleText");
            titleTxtObj.transform.SetParent(headerObj.transform, false);
            var titleTxt = titleTxtObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "HARBOR COMMERCE CONTRACTS";
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
            titleTxt.color = new Color(1f, 0.88f, 0.4f);
            titleTxt.raycastTarget = false;
            RectTransform titleRt = titleTxtObj.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.anchoredPosition = new Vector2(25, 0);

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
            closeBg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeBtn.onClick.AddListener(Close);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTxt.text = "✕";
            closeTxt.fontSize = 24;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;
            closeTxt.raycastTarget = false;
            RectTransform cTxtRt = closeTxtObj.GetComponent<RectTransform>();
            cTxtRt.anchorMin = Vector2.zero;
            cTxtRt.anchorMax = Vector2.one;

            // 4. Harbor Progression Bar Section
            GameObject progSection = new GameObject("HarborProgressionSection");
            progSection.transform.SetParent(dialogBox.transform, false);
            RectTransform progRt = progSection.AddComponent<RectTransform>();
            progRt.anchorMin = new Vector2(0, 1);
            progRt.anchorMax = new Vector2(1, 1);
            progRt.pivot = new Vector2(0.5f, 1);
            progRt.anchoredPosition = new Vector2(0, -65);
            progRt.sizeDelta = new Vector2(-40, 85);

            Image progBg = progSection.AddComponent<Image>();
            progBg.sprite = whiteSp;
            progBg.color = new Color(0.14f, 0.18f, 0.26f, 0.95f);

            // Level Title
            GameObject lvlTitleObj = new GameObject("LevelTitle");
            lvlTitleObj.transform.SetParent(progSection.transform, false);
            levelTitleText = lvlTitleObj.AddComponent<TextMeshProUGUI>();
            levelTitleText.fontSize = 20;
            levelTitleText.fontStyle = FontStyles.Bold;
            levelTitleText.alignment = TextAlignmentOptions.MidlineLeft;
            levelTitleText.color = Color.white;
            RectTransform lvlRt = lvlTitleObj.GetComponent<RectTransform>();
            lvlRt.anchorMin = new Vector2(0, 0.6f);
            lvlRt.anchorMax = new Vector2(0.6f, 1f);
            lvlRt.anchoredPosition = new Vector2(15, 0);

            // Fame Progress Text
            GameObject fameTxtObj = new GameObject("FameText");
            fameTxtObj.transform.SetParent(progSection.transform, false);
            fameProgressText = fameTxtObj.AddComponent<TextMeshProUGUI>();
            fameProgressText.fontSize = 18;
            fameProgressText.fontStyle = FontStyles.Bold;
            fameProgressText.alignment = TextAlignmentOptions.MidlineRight;
            fameProgressText.color = new Color(0.8f, 0.9f, 1f);
            RectTransform fTxtRt = fameTxtObj.GetComponent<RectTransform>();
            fTxtRt.anchorMin = new Vector2(0.6f, 0.6f);
            fTxtRt.anchorMax = new Vector2(1f, 1f);
            fTxtRt.anchoredPosition = new Vector2(-15, 0);

            // Fame Progress Bar Background
            GameObject barBgObj = new GameObject("FameBarBG");
            barBgObj.transform.SetParent(progSection.transform, false);
            RectTransform barBgRt = barBgObj.AddComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0, 0.32f);
            barBgRt.anchorMax = new Vector2(1, 0.52f);
            barBgRt.anchoredPosition = new Vector2(0, 0);
            barBgRt.sizeDelta = new Vector2(-30, 0);
            Image barBgImg = barBgObj.AddComponent<Image>();
            barBgImg.sprite = whiteSp;
            barBgImg.color = new Color(0.08f, 0.10f, 0.15f, 1f);

            // Fame Progress Bar Fill
            GameObject barFillObj = new GameObject("FameBarFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            RectTransform barFillRt = barFillObj.AddComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.sizeDelta = Vector2.zero;
            fameFillImage = barFillObj.AddComponent<Image>();
            fameFillImage.sprite = whiteSp;
            fameFillImage.type = Image.Type.Filled;
            fameFillImage.fillMethod = Image.FillMethod.Horizontal;
            fameFillImage.color = new Color(0.18f, 0.8f, 0.44f, 1f); // Green Fame

            // Buff Summary Text
            GameObject buffTxtObj = new GameObject("BuffSummary");
            buffTxtObj.transform.SetParent(progSection.transform, false);
            buffSummaryText = buffTxtObj.AddComponent<TextMeshProUGUI>();
            buffSummaryText.fontSize = 16;
            buffSummaryText.alignment = TextAlignmentOptions.MidlineLeft;
            buffSummaryText.color = new Color(0.85f, 0.85f, 0.85f);
            RectTransform bTxtRt = buffTxtObj.GetComponent<RectTransform>();
            bTxtRt.anchorMin = new Vector2(0, 0);
            bTxtRt.anchorMax = new Vector2(1, 0.3f);
            bTxtRt.anchoredPosition = new Vector2(15, 0);

            // 5. Contracts Container (3 Cards side by side)
            GameObject cardsContainer = new GameObject("CardsContainer");
            cardsContainer.transform.SetParent(dialogBox.transform, false);
            RectTransform ccRt = cardsContainer.AddComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0, 0);
            ccRt.anchorMax = new Vector2(1, 1);
            ccRt.anchoredPosition = new Vector2(0, -65);
            ccRt.sizeDelta = new Vector2(-40, -185);

            cardUIs.Clear();
            float cardWidth = 260;
            float spacing = 15;
            float startX = -(cardWidth + spacing);

            for (int i = 0; i < 3; i++)
            {
                var card = CreateCardSlot(cardsContainer.transform, whiteSp, i);
                RectTransform cRt = card.cardObj.GetComponent<RectTransform>();
                cRt.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0);
                cardUIs.Add(card);
            }

            popupRoot.SetActive(false);
        }

        private ContractCardUI CreateCardSlot(Transform parent, Sprite whiteSp, int index)
        {
            var card = new ContractCardUI();

            GameObject cObj = new GameObject($"ContractCard_{index}");
            cObj.transform.SetParent(parent, false);
            card.cardObj = cObj;

            RectTransform cRt = cObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(260, 360);

            Image cBg = cObj.AddComponent<Image>();
            cBg.sprite = whiteSp;
            cBg.color = new Color(0.14f, 0.18f, 0.25f, 0.98f);

            // Client Header
            GameObject clientObj = new GameObject("ClientHeader");
            clientObj.transform.SetParent(cObj.transform, false);
            card.clientText = clientObj.AddComponent<TextMeshProUGUI>();
            card.clientText.fontSize = 18;
            card.clientText.fontStyle = FontStyles.Bold;
            card.clientText.alignment = TextAlignmentOptions.Center;
            card.clientText.color = new Color(1f, 0.85f, 0.35f);
            RectTransform clRt = clientObj.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(0, 1);
            clRt.anchorMax = new Vector2(1, 1);
            clRt.pivot = new Vector2(0.5f, 1);
            clRt.anchoredPosition = new Vector2(0, -12);
            clRt.sizeDelta = new Vector2(-20, 30);

            // Contract Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cObj.transform, false);
            card.titleText = titleObj.AddComponent<TextMeshProUGUI>();
            card.titleText.fontSize = 18;
            card.titleText.fontStyle = FontStyles.Bold;
            card.titleText.alignment = TextAlignmentOptions.Center;
            card.titleText.color = Color.white;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -44);
            tRt.sizeDelta = new Vector2(-20, 35);

            // Contract Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(cObj.transform, false);
            card.descText = descObj.AddComponent<TextMeshProUGUI>();
            card.descText.fontSize = 15;
            card.descText.alignment = TextAlignmentOptions.Center;
            card.descText.color = new Color(0.75f, 0.8f, 0.9f);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0, 1);
            dRt.anchorMax = new Vector2(1, 1);
            dRt.pivot = new Vector2(0.5f, 1);
            dRt.anchoredPosition = new Vector2(0, -80);
            dRt.sizeDelta = new Vector2(-20, 45);

            // Progress Text (Required & Available)
            GameObject progObj = new GameObject("ProgressText");
            progObj.transform.SetParent(cObj.transform, false);
            card.progressText = progObj.AddComponent<TextMeshProUGUI>();
            card.progressText.fontSize = 16;
            card.progressText.alignment = TextAlignmentOptions.Center;
            card.progressText.color = Color.white;
            RectTransform pRt = progObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 1);
            pRt.anchorMax = new Vector2(1, 1);
            pRt.pivot = new Vector2(0.5f, 1);
            pRt.anchoredPosition = new Vector2(0, -135);
            pRt.sizeDelta = new Vector2(-20, 50);

            // Rewards Text
            GameObject rewObj = new GameObject("RewardsText");
            rewObj.transform.SetParent(cObj.transform, false);
            card.rewardText = rewObj.AddComponent<TextMeshProUGUI>();
            card.rewardText.fontSize = 16;
            card.rewardText.alignment = TextAlignmentOptions.Center;
            card.rewardText.color = Color.white;
            RectTransform rRt = rewObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0, 1);
            rRt.anchorMax = new Vector2(1, 1);
            rRt.pivot = new Vector2(0.5f, 1);
            rRt.anchoredPosition = new Vector2(0, -195);
            rRt.sizeDelta = new Vector2(-20, 50);

            // Timer Text
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(cObj.transform, false);
            card.timerText = timerObj.AddComponent<TextMeshProUGUI>();
            card.timerText.fontSize = 15;
            card.timerText.fontStyle = FontStyles.Bold;
            card.timerText.alignment = TextAlignmentOptions.Center;
            card.timerText.color = new Color(0.9f, 0.75f, 0.4f);
            RectTransform tmRt = timerObj.GetComponent<RectTransform>();
            tmRt.anchorMin = new Vector2(0, 1);
            tmRt.anchorMax = new Vector2(1, 1);
            tmRt.pivot = new Vector2(0.5f, 1);
            tmRt.anchoredPosition = new Vector2(0, -252);
            tmRt.sizeDelta = new Vector2(-20, 25);

            // Deliver Button
            GameObject btnObj = new GameObject("DeliverBtn");
            btnObj.transform.SetParent(cObj.transform, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0);
            bRt.anchorMax = new Vector2(0.5f, 0);
            bRt.pivot = new Vector2(0.5f, 0);
            bRt.anchoredPosition = new Vector2(0, 16);
            bRt.sizeDelta = new Vector2(230, 52);

            card.deliverBtnBg = btnObj.AddComponent<Image>();
            card.deliverBtnBg.sprite = whiteSp;
            card.deliverBtnBg.color = new Color(0.18f, 0.80f, 0.44f);

            card.deliverButton = btnObj.AddComponent<Button>();
            card.deliverButton.targetGraphic = card.deliverBtnBg;
            card.deliverButton.onClick.AddListener(() =>
            {
                if (card.boundContract != null && ContractManager.Instance != null)
                {
                    ContractManager.Instance.TryFulfillContract(card.boundContract);
                }
            });

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            card.deliverBtnText = btnTxtObj.AddComponent<TextMeshProUGUI>();
            card.deliverBtnText.text = "DELIVER";
            card.deliverBtnText.fontSize = 18;
            card.deliverBtnText.fontStyle = FontStyles.Bold;
            card.deliverBtnText.alignment = TextAlignmentOptions.Center;
            card.deliverBtnText.color = Color.white;
            card.deliverBtnText.raycastTarget = false;
            RectTransform btRt = btnTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;

            return card;
        }
    }
}

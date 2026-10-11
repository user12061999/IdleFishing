using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Economy;

namespace IdleFishing.UI
{
    public class OfflineEarningsPopupUI : MonoBehaviour
    {
        public static OfflineEarningsPopupUI Instance { get; private set; }

        private GameObject popupRoot;
        private TextMeshProUGUI timeText;
        private TextMeshProUGUI rewardText;
        private int currentRewardAmount;

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

        public void Show(int earnedCoins, TimeSpan awayTime)
        {
            if (earnedCoins <= 0) return;
            currentRewardAmount = earnedCoins;

            if (popupRoot == null)
            {
                BuildProceduralPopup();
            }

            if (timeText != null)
            {
                if (awayTime.TotalHours >= 1)
                {
                    timeText.text = $"You were away for: <b>{Math.Floor(awayTime.TotalHours)}h {awayTime.Minutes}m</b>";
                }
                else
                {
                    timeText.text = $"You were away for: <b>{awayTime.Minutes}m {awayTime.Seconds}s</b>";
                }
            }

            if (rewardText != null)
            {
                rewardText.text = $"+{earnedCoins:N0} G";
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }
        }

        public void ClaimReward(int multiplier)
        {
            int total = currentRewardAmount * multiplier;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(total);
            }
            GameEvents.TriggerOfflineEarningsClaimed(total);

            if (FlyingCoinManager.Instance != null && Camera.main != null)
            {
                FlyingCoinManager.Instance.PlayFlyingCoins(Camera.main.transform.position + Camera.main.transform.forward * 2f, 6);
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void BuildProceduralPopup()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Dark Backdrop
            popupRoot = new GameObject("OfflineEarnings_Backdrop");
            popupRoot.transform.SetParent(transform, false);

            RectTransform rootRect = popupRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            Image darkBg = popupRoot.AddComponent<Image>();
            darkBg.sprite = whiteSp;
            darkBg.color = new Color(0f, 0f, 0f, 0.75f); // 75% dark tint

            // 2. Center Dialog Box
            GameObject dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(popupRoot.transform, false);
            RectTransform dRect = dialogBox.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.sizeDelta = new Vector2(620, 420);

            Image dBg = dialogBox.AddComponent<Image>();
            dBg.sprite = whiteSp;
            dBg.color = new Color(0.14f, 0.16f, 0.22f, 0.98f);

            // 3. Header Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(dialogBox.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "WELCOME BACK!";
            titleTxt.fontSize = 26;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(1f, 0.85f, 0.2f); // Gold
            titleTxt.raycastTarget = false;
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0, -22);
            tRect.sizeDelta = new Vector2(560, 44);

            // Close Button [X] at top-right
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-15, -15);
            closeRt.sizeDelta = new Vector2(40, 40);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.sprite = whiteSp;
            closeImg.color = new Color(0.7f, 0.2f, 0.2f, 0.85f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(() =>
            {
                if (popupRoot != null) popupRoot.SetActive(false);
            });

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTxt.text = "✕";
            closeTxt.fontSize = 22;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;
            closeTxt.raycastTarget = false;
            RectTransform ctRt = closeTxtObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.sizeDelta = Vector2.zero;

            // 4. Subtitle / Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(dialogBox.transform, false);
            var descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = "While you were away, your seafood workshop continued auto processing and sales:";
            descTxt.fontSize = 18;
            descTxt.alignment = TextAlignmentOptions.Center;
            descTxt.color = new Color(0.8f, 0.84f, 0.9f);
            descTxt.raycastTarget = false;
            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 1f);
            descRect.anchorMax = new Vector2(1f, 1f);
            descRect.pivot = new Vector2(0.5f, 1f);
            descRect.anchoredPosition = new Vector2(0, -75);
            descRect.sizeDelta = new Vector2(560, 44);

            // 5. Away Time Text
            GameObject timeObj = new GameObject("TimeText");
            timeObj.transform.SetParent(dialogBox.transform, false);
            timeText = timeObj.AddComponent<TextMeshProUGUI>();
            timeText.fontSize = 18;
            timeText.alignment = TextAlignmentOptions.Center;
            timeText.color = new Color(0.65f, 0.72f, 0.85f);
            timeText.raycastTarget = false;
            RectTransform trRect = timeObj.GetComponent<RectTransform>();
            trRect.anchorMin = new Vector2(0f, 1f);
            trRect.anchorMax = new Vector2(1f, 1f);
            trRect.pivot = new Vector2(0.5f, 1f);
            trRect.anchoredPosition = new Vector2(0, -130);
            trRect.sizeDelta = new Vector2(560, 32);

            // 6. Reward Coins Amount
            GameObject rewObj = new GameObject("RewardAmount");
            rewObj.transform.SetParent(dialogBox.transform, false);
            rewardText = rewObj.AddComponent<TextMeshProUGUI>();
            rewardText.text = "+500 G";
            rewardText.fontSize = 44;
            rewardText.fontStyle = FontStyles.Bold;
            rewardText.alignment = TextAlignmentOptions.Center;
            rewardText.color = new Color(1f, 0.88f, 0.2f);
            rewardText.raycastTarget = false;
            RectTransform rewRect = rewObj.GetComponent<RectTransform>();
            rewRect.anchorMin = new Vector2(0f, 1f);
            rewRect.anchorMax = new Vector2(1f, 1f);
            rewRect.pivot = new Vector2(0.5f, 1f);
            rewRect.anchoredPosition = new Vector2(0, -175);
            rewRect.sizeDelta = new Vector2(560, 56);

            // 7. Claim Regular Button (1x)
            GameObject claimBtnObj = new GameObject("ClaimButton");
            claimBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform clRect = claimBtnObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0.5f, 0f);
            clRect.anchorMax = new Vector2(0.5f, 0f);
            clRect.pivot = new Vector2(0.5f, 0f);
            clRect.anchoredPosition = new Vector2(-135, 25);
            clRect.sizeDelta = new Vector2(240, 56);

            Image claimImg = claimBtnObj.AddComponent<Image>();
            claimImg.sprite = whiteSp;
            claimImg.color = new Color(0.3f, 0.35f, 0.42f);

            Button claimBtn = claimBtnObj.AddComponent<Button>();
            claimBtn.targetGraphic = claimImg;
            claimBtn.onClick.AddListener(() => ClaimReward(1));

            GameObject clTxtObj = new GameObject("Text");
            clTxtObj.transform.SetParent(claimBtnObj.transform, false);
            var clTxt = clTxtObj.AddComponent<TextMeshProUGUI>();
            clTxt.text = "CLAIM";
            clTxt.fontSize = 20;
            clTxt.fontStyle = FontStyles.Bold;
            clTxt.alignment = TextAlignmentOptions.Center;
            clTxt.color = Color.white;
            clTxt.raycastTarget = false;
            RectTransform clTxtRt = clTxtObj.GetComponent<RectTransform>();
            clTxtRt.anchorMin = Vector2.zero;
            clTxtRt.anchorMax = Vector2.one;
            clTxtRt.sizeDelta = Vector2.zero;

            // 8. Claim Double Button (2x Ads)
            GameObject doubleBtnObj = new GameObject("DoubleButton");
            doubleBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform dblRect = doubleBtnObj.AddComponent<RectTransform>();
            dblRect.anchorMin = new Vector2(0.5f, 0f);
            dblRect.anchorMax = new Vector2(0.5f, 0f);
            dblRect.pivot = new Vector2(0.5f, 0f);
            dblRect.anchoredPosition = new Vector2(135, 25);
            dblRect.sizeDelta = new Vector2(240, 56);

            Image doubleImg = doubleBtnObj.AddComponent<Image>();
            doubleImg.sprite = whiteSp;
            doubleImg.color = new Color(0.2f, 0.78f, 0.38f); // Bright Green

            Button doubleBtn = doubleBtnObj.AddComponent<Button>();
            doubleBtn.targetGraphic = doubleImg;
            doubleBtn.onClick.AddListener(() => ClaimReward(2));

            GameObject dblTxtObj = new GameObject("Text");
            dblTxtObj.transform.SetParent(doubleBtnObj.transform, false);
            var dblTxt = dblTxtObj.AddComponent<TextMeshProUGUI>();
            dblTxt.text = "2X CLAIM (ADS)";
            dblTxt.fontSize = 20;
            dblTxt.fontStyle = FontStyles.Bold;
            dblTxt.alignment = TextAlignmentOptions.Center;
            dblTxt.color = Color.white;
            dblTxt.raycastTarget = false;
            RectTransform dblTxtRt = dblTxtObj.GetComponent<RectTransform>();
            dblTxtRt.anchorMin = Vector2.zero;
            dblTxtRt.anchorMax = Vector2.one;
            dblTxtRt.sizeDelta = Vector2.zero;

            popupRoot.SetActive(false);
        }
    }
}

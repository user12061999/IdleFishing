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
                rewardText.text = $"+{NumberFormatter.Format(earnedCoins)} G";
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
            darkBg.sprite = CasualUITheme.DimmedBg ?? whiteSp;
            darkBg.color = (CasualUITheme.DimmedBg != null) ? Color.white : new Color(0f, 0f, 0f, 0.78f);

            // 2. Center Dialog Box
            GameObject dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(popupRoot.transform, false);
            RectTransform dRect = dialogBox.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.sizeDelta = new Vector2(640, 440);

            Image dBg = dialogBox.AddComponent<Image>();
            CasualUITheme.StylePopupDialog(dBg);

            // 3. Header Title Ribbon
            GameObject ribbonObj = new GameObject("RibbonBanner");
            ribbonObj.transform.SetParent(dialogBox.transform, false);
            RectTransform rbRt = ribbonObj.AddComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.5f, 1f);
            rbRt.anchorMax = new Vector2(0.5f, 1f);
            rbRt.pivot = new Vector2(0.5f, 0.5f);
            rbRt.anchoredPosition = new Vector2(0, -12);
            rbRt.sizeDelta = new Vector2(360, 80);
            Image rbImg = ribbonObj.AddComponent<Image>();
            Sprite ribSp = CasualUITheme.TitleRibbonYellow ?? whiteSp;
            rbImg.sprite = ribSp;
            rbImg.type = (ribSp.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            rbImg.color = Color.white;
            rbImg.raycastTarget = false;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(ribbonObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "WELCOME BACK!";
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;
            titleTxt.ApplyFont(UIFontCategory.Header);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(20, 8);
            tRect.offsetMax = new Vector2(-20, -4);

            // Close Button [X] at top-right
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-15, -15);
            closeRt.sizeDelta = new Vector2(48, 48);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(closeBtn, closeImg, CasualBtnStyle.SquareGray);
            closeBtn.onClick.AddListener(() =>
            {
                if (popupRoot != null) popupRoot.SetActive(false);
            });

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

            // 4. Subtitle / Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(dialogBox.transform, false);
            var descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = "While you were away, your seafood workshop continued auto processing and sales:";
            descTxt.fontSize = 21;
            descTxt.alignment = TextAlignmentOptions.Center;
            descTxt.color = new Color(0.80f, 0.88f, 0.98f);
            descTxt.raycastTarget = false;
            descTxt.ApplyFont(UIFontCategory.Default);
            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 1f);
            descRect.anchorMax = new Vector2(1f, 1f);
            descRect.pivot = new Vector2(0.5f, 1f);
            descRect.anchoredPosition = new Vector2(0, -78);
            descRect.sizeDelta = new Vector2(560, 48);

            // 5. Away Time Text
            GameObject timeObj = new GameObject("TimeText");
            timeObj.transform.SetParent(dialogBox.transform, false);
            timeText = timeObj.AddComponent<TextMeshProUGUI>();
            timeText.fontSize = 20;
            timeText.alignment = TextAlignmentOptions.Center;
            timeText.color = new Color(0.65f, 0.76f, 0.92f);
            timeText.raycastTarget = false;
            timeText.ApplyFont(UIFontCategory.Default);
            RectTransform trRect = timeObj.GetComponent<RectTransform>();
            trRect.anchorMin = new Vector2(0f, 1f);
            trRect.anchorMax = new Vector2(1f, 1f);
            trRect.pivot = new Vector2(0.5f, 1f);
            trRect.anchoredPosition = new Vector2(0, -134);
            trRect.sizeDelta = new Vector2(560, 32);

            // 6. Reward Coins Card
            GameObject rewCardObj = new GameObject("RewardCard");
            rewCardObj.transform.SetParent(dialogBox.transform, false);
            RectTransform rcRt = rewCardObj.AddComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(0.5f, 1f);
            rcRt.anchorMax = new Vector2(0.5f, 1f);
            rcRt.pivot = new Vector2(0.5f, 1f);
            rcRt.anchoredPosition = new Vector2(0, -172);
            rcRt.sizeDelta = new Vector2(400, 80);
            Image rcImg = rewCardObj.AddComponent<Image>();
            CasualUITheme.StyleCard(rcImg, new Color(0.08f, 0.12f, 0.20f, 0.95f));

            // Gold Icon in Reward Card
            GameObject rewIconObj = new GameObject("Icon_Gold");
            rewIconObj.transform.SetParent(rewCardObj.transform, false);
            RectTransform riRt = rewIconObj.AddComponent<RectTransform>();
            riRt.anchorMin = new Vector2(0f, 0.5f);
            riRt.anchorMax = new Vector2(0f, 0.5f);
            riRt.pivot = new Vector2(0.5f, 0.5f);
            riRt.anchoredPosition = new Vector2(44, 0);
            riRt.sizeDelta = new Vector2(60, 60);
            Image riImg = rewIconObj.AddComponent<Image>();
            riImg.sprite = CasualUITheme.IconGolds ?? CasualUITheme.IconGold;
            riImg.preserveAspect = true;
            riImg.raycastTarget = false;

            GameObject rewObj = new GameObject("RewardAmount");
            rewObj.transform.SetParent(rewCardObj.transform, false);
            rewardText = rewObj.AddComponent<TextMeshProUGUI>();
            rewardText.text = "+500 G";
            rewardText.fontSize = 42;
            rewardText.fontStyle = FontStyles.Bold;
            rewardText.alignment = TextAlignmentOptions.MidlineLeft;
            rewardText.color = new Color(1f, 0.90f, 0.25f);
            rewardText.raycastTarget = false;
            rewardText.ApplyFont(UIFontCategory.Numbers);
            RectTransform rewRect = rewObj.GetComponent<RectTransform>();
            rewRect.anchorMin = Vector2.zero;
            rewRect.anchorMax = Vector2.one;
            rewRect.offsetMin = new Vector2(90, 0);
            rewRect.offsetMax = new Vector2(-15, 0);

            // 7. Claim Regular Button (1x)
            GameObject claimBtnObj = new GameObject("ClaimButton");
            claimBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform clRect = claimBtnObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0.5f, 0f);
            clRect.anchorMax = new Vector2(0.5f, 0f);
            clRect.pivot = new Vector2(0.5f, 0f);
            clRect.anchoredPosition = new Vector2(-135, 25);
            clRect.sizeDelta = new Vector2(240, 60);

            Image claimImg = claimBtnObj.AddComponent<Image>();
            Button claimBtn = claimBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(claimBtn, claimImg, CasualBtnStyle.Blue);
            claimBtn.onClick.AddListener(() => ClaimReward(1));

            GameObject clTxtObj = new GameObject("Text");
            clTxtObj.transform.SetParent(claimBtnObj.transform, false);
            var clTxt = clTxtObj.AddComponent<TextMeshProUGUI>();
            clTxt.text = "CLAIM";
            clTxt.fontSize = 24;
            clTxt.fontStyle = FontStyles.Bold;
            clTxt.alignment = TextAlignmentOptions.Center;
            clTxt.color = Color.white;
            clTxt.raycastTarget = false;
            clTxt.ApplyFont(UIFontCategory.Button);
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
            dblRect.sizeDelta = new Vector2(240, 60);

            Image doubleImg = doubleBtnObj.AddComponent<Image>();
            Button doubleBtn = doubleBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(doubleBtn, doubleImg, CasualBtnStyle.Yellow);
            doubleBtn.onClick.AddListener(() => ClaimReward(2));

            // Ad Icon in Double Button
            GameObject adIconObj = new GameObject("Icon_Ad");
            adIconObj.transform.SetParent(doubleBtnObj.transform, false);
            RectTransform adRt = adIconObj.AddComponent<RectTransform>();
            adRt.anchorMin = new Vector2(0f, 0.5f);
            adRt.anchorMax = new Vector2(0f, 0.5f);
            adRt.pivot = new Vector2(0.5f, 0.5f);
            adRt.anchoredPosition = new Vector2(30, 0);
            adRt.sizeDelta = new Vector2(32, 32);
            Image adImg = adIconObj.AddComponent<Image>();
            adImg.sprite = CasualUITheme.IconAd;
            adImg.preserveAspect = true;
            adImg.raycastTarget = false;

            GameObject dblTxtObj = new GameObject("Text");
            dblTxtObj.transform.SetParent(doubleBtnObj.transform, false);
            var dblTxt = dblTxtObj.AddComponent<TextMeshProUGUI>();
            dblTxt.text = "2X CLAIM";
            dblTxt.fontSize = 24;
            dblTxt.fontStyle = FontStyles.Bold;
            dblTxt.alignment = TextAlignmentOptions.Center;
            dblTxt.color = Color.white;
            dblTxt.raycastTarget = false;
            dblTxt.ApplyFont(UIFontCategory.Button);
            RectTransform dblTxtRt = dblTxtObj.GetComponent<RectTransform>();
            dblTxtRt.anchorMin = Vector2.zero;
            dblTxtRt.anchorMax = Vector2.one;
            dblTxtRt.offsetMin = new Vector2(36, 0);
            dblTxtRt.offsetMax = Vector2.zero;

            UIFontConfig.Instance?.ApplyToHierarchy(popupRoot);
            popupRoot.SetActive(false);
        }
    }
}

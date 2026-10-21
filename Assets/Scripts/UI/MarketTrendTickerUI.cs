using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Market;

namespace IdleFishing.UI
{
    public class MarketTrendTickerUI : MonoBehaviour
    {
        public static MarketTrendTickerUI Instance { get; private set; }

        private RectTransform bannerRect;
        private Image bgImage;
        private Image borderImage;
        private TextMeshProUGUI trendTitleText;
        private TextMeshProUGUI timerText;

        private float punchScaleTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildProceduralUI();
            UpdateTrendDisplay(MarketTrendManager.Instance != null ? MarketTrendManager.Instance.CurrentTrend : null);
        }

        private void OnEnable()
        {
            GameEvents.MarketTrendChanged += UpdateTrendDisplay;
        }

        private void OnDisable()
        {
            GameEvents.MarketTrendChanged -= UpdateTrendDisplay;
        }

        private void Update()
        {
            if (MarketTrendManager.Instance != null)
            {
                float rem = MarketTrendManager.Instance.TimeRemaining;
                if (timerText != null)
                {
                    int minutes = Mathf.FloorToInt(rem / 60f);
                    int seconds = Mathf.FloorToInt(rem % 60f);
                    timerText.text = $"ENDS: {minutes:D2}:{seconds:D2}";
                }
            }

            // Punch scale bounce on new trend
            if (punchScaleTimer > 0f)
            {
                punchScaleTimer -= Time.deltaTime;
                float s = 1.0f + Mathf.Sin(punchScaleTimer * 12f) * 0.08f;
                if (bannerRect != null) bannerRect.localScale = new Vector3(s, s, 1f);
            }
            else if (bannerRect != null && bannerRect.localScale != Vector3.one)
            {
                bannerRect.localScale = Vector3.one;
            }
        }

        private void BuildProceduralUI()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Root Container (Docked to LeftQuestEventDockUI)
            Transform parentContainer = (LeftQuestEventDockUI.Instance != null)
                ? LeftQuestEventDockUI.Instance.ContentContainer
                : transform;

            GameObject bannerObj = new GameObject("MarketTrend_Ticker");
            bannerObj.transform.SetParent(parentContainer, false);
            bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0f, 1f);
            bannerRect.anchorMax = new Vector2(0f, 1f);
            bannerRect.pivot = new Vector2(0f, 1f);
            bannerRect.anchoredPosition = Vector2.zero;
            bannerRect.sizeDelta = new Vector2(540f, 58f);

            var le = bannerObj.AddComponent<LayoutElement>();
            le.preferredWidth = 540f;
            le.preferredHeight = 58f;
            le.minHeight = 58f;

            // 2. Background Border
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(bannerObj.transform, false);
            RectTransform borderRt = borderObj.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = Vector2.zero;
            borderImage = borderObj.AddComponent<Image>();
            if (CasualUITheme.CardRound12 != null)
            {
                borderImage.sprite = CasualUITheme.CardRound12;
                borderImage.type = Image.Type.Sliced;
                borderImage.color = new Color(0.96f, 0.72f, 0.15f, 0.95f); // Amber gold border
            }
            else
            {
                borderImage.sprite = whiteSp;
                borderImage.color = new Color(1.0f, 0.72f, 0.20f, 0.85f);
            }
            borderImage.raycastTarget = false;

            // 3. Inner Background
            GameObject innerBg = new GameObject("InnerBG");
            innerBg.transform.SetParent(bannerObj.transform, false);
            RectTransform innerRt = innerBg.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.sizeDelta = new Vector2(-4, -4); // 2px border
            bgImage = innerBg.AddComponent<Image>();
            if (CasualUITheme.CardRound12 != null)
            {
                bgImage.sprite = CasualUITheme.CardRound12;
                bgImage.type = Image.Type.Sliced;
                bgImage.color = new Color(0.08f, 0.12f, 0.18f, 0.96f); // Deep navy
            }
            else
            {
                bgImage.sprite = whiteSp;
                bgImage.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);
            }
            bgImage.raycastTarget = false;

            // 4. Trend Info Text (Left to Center)
            GameObject titleObj = new GameObject("TrendTitle");
            titleObj.transform.SetParent(innerBg.transform, false);
            trendTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0);
            titleRt.anchorMax = new Vector2(0.68f, 1);
            titleRt.pivot = new Vector2(0, 0.5f);
            titleRt.anchoredPosition = new Vector2(10, 0);
            titleRt.sizeDelta = new Vector2(-12, 0);

            trendTitleText.fontSize = 16.5f;
            trendTitleText.fontStyle = FontStyles.Bold;
            trendTitleText.alignment = TextAlignmentOptions.MidlineLeft;
            trendTitleText.color = new Color(1f, 0.92f, 0.5f);
            trendTitleText.text = "MARKET: Stable";
            trendTitleText.textWrappingMode = TextWrappingModes.NoWrap;
            trendTitleText.raycastTarget = false;
            trendTitleText.ApplyFont(UIFontCategory.Button);

            // 5. Timer Text (Right)
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(innerBg.transform, false);
            RectTransform timerRt = timerObj.AddComponent<RectTransform>();
            timerRt.anchorMin = new Vector2(0.68f, 0);
            timerRt.anchorMax = new Vector2(1f, 1);
            timerRt.pivot = new Vector2(1, 0.5f);
            timerRt.anchoredPosition = new Vector2(-10, 0);
            timerRt.sizeDelta = new Vector2(-12, 0);

            timerText = timerObj.AddComponent<TextMeshProUGUI>();
            timerText.fontSize = 15f;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.MidlineRight;
            timerText.color = new Color(0.85f, 0.9f, 1f);
            timerText.text = "ENDS: 02:00";
            timerText.textWrappingMode = TextWrappingModes.NoWrap;
            timerText.raycastTarget = false;
            timerText.ApplyFont(UIFontCategory.Numbers);
        }

        public void UpdateTrendDisplay(MarketTrend trend)
        {
            if (trendTitleText == null) return;

            if (trend == null)
            {
                trendTitleText.text = "MARKET: Stable";
                return;
            }

            int percent = Mathf.RoundToInt((trend.priceMultiplier - 1.0f) * 100f);
            trendTitleText.text = $"{trend.iconBadge} <color=#FFE600>{trend.trendTitle}</color> <color=#2ECC71>+{percent}%</color>";
            punchScaleTimer = 0.5f;
        }
    }
}

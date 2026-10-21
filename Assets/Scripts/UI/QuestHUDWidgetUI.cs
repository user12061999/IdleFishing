using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Quests;
using IdleFishing.Audio;

namespace IdleFishing.UI
{
    /// <summary>
    /// Widget Nhiệm Vụ Tân Thủ & Mốc Thưởng (Quest HUD Widget):
    /// - Nằm gọn gàng dưới thanh Market Trend (Top-Center, y = -180).
    /// - Hiển thị Icon, Tiêu đề, Mô tả, Thanh tiến độ trơn mượt và Phần thưởng vàng.
    /// - Khi hoàn thành: Nút "CLAIM! +XX G" phát sáng nhịp tim (Pulse animation).
    /// - Bấm Claim: Tung mưa tiền vàng (Flying Coins) bay thẳng lên bộ đếm Coins trên HUD và chuyển sang nhiệm vụ tiếp theo với hiệu ứng Spring Bounce.
    /// - Có thể thu nhỏ/mở rộng gọn gàng bằng nút toggle.
    /// </summary>
    public class QuestHUDWidgetUI : MonoBehaviour
    {
        private static QuestHUDWidgetUI _instance;
        public static QuestHUDWidgetUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<QuestHUDWidgetUI>();
                }
                return _instance;
            }
        }

        private RectTransform widgetRect;
        private Image bgCardImage;
        private Image borderImage;

        // Content
        private Image iconBadgeImage;
        private TextMeshProUGUI iconTagText;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI descText;

        // Progress Bar
        private RectTransform sliderContainer;
        private Image sliderFillImage;
        private TextMeshProUGUI progressText;

        // Claim / Action Button
        private RectTransform claimButtonRect;
        private Button claimButton;
        private Image claimButtonImage;
        private TextMeshProUGUI claimButtonText;

        // Hint Toast
        private TextMeshProUGUI hintToastText;
        private Coroutine hintToastRoutine;

        // Animation States
        private float targetFill = 0f;
        private float currentFill = 0f;
        private bool isClaimable = false;
        private bool isCollapsed = false;
        private Coroutine punchRoutine;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            // If the 30-Stage Progression system is active, StageObjectiveHUDUI is the primary progression HUD.
            // Skip legacy QuestHUDWidgetUI so there is no duplicate quest UI.
            if (IdleFishing.Progression.StageProgressionManager.Instance != null)
            {
                return;
            }

            BuildProceduralUI();
            // Đảm bảo QuestManager đã khởi tạo
            if (QuestManager.Instance != null && QuestManager.Instance.CurrentQuest != null)
            {
                UpdateDisplay(QuestManager.Instance.CurrentQuest, QuestManager.Instance.CurrentProgress, QuestManager.Instance.CurrentQuest.targetAmount);
            }
        }

        private void OnEnable()
        {
            GameEvents.ActiveQuestChanged += OnActiveQuestChanged;
            GameEvents.QuestProgressChanged += OnQuestProgressChanged;
            GameEvents.QuestCompleted += OnQuestCompleted;
            GameEvents.QuestClaimed += OnQuestClaimed;
        }

        private void OnDisable()
        {
            GameEvents.ActiveQuestChanged -= OnActiveQuestChanged;
            GameEvents.QuestProgressChanged -= OnQuestProgressChanged;
            GameEvents.QuestCompleted -= OnQuestCompleted;
            GameEvents.QuestClaimed -= OnQuestClaimed;
        }

        private void Update()
        {
            // Smooth fill lerp
            if (sliderFillImage != null && Mathf.Abs(currentFill - targetFill) > 0.002f)
            {
                currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 10f);
                sliderFillImage.fillAmount = currentFill;
            }

            // Pulse animation on Claim button when ready
            if (isClaimable && claimButtonRect != null)
            {
                float s = 1.0f + Mathf.Sin(Time.unscaledTime * 6.5f) * 0.06f;
                claimButtonRect.localScale = new Vector3(s, s, 1f);
            }
        }

        private void BuildProceduralUI()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Root Parent & Container (Docked to LeftQuestEventDockUI)
            Transform parentContainer = (LeftQuestEventDockUI.Instance != null) 
                ? LeftQuestEventDockUI.Instance.ContentContainer 
                : transform;

            // 2. Outer Border Frame (396 x 94)
            GameObject borderObj = new GameObject("Quest_Widget_Card");
            borderObj.transform.SetParent(parentContainer, false);
            RectTransform borderRt = borderObj.AddComponent<RectTransform>();
            borderRt.anchorMin = new Vector2(0f, 1f);
            borderRt.anchorMax = new Vector2(0f, 1f);
            borderRt.pivot = new Vector2(0f, 1f);
            borderRt.sizeDelta = new Vector2(540f, 112f);

            var le = borderObj.AddComponent<LayoutElement>();
            le.preferredWidth = 540f;
            le.preferredHeight = 112f;
            le.minHeight = 112f;

            borderImage = borderObj.AddComponent<Image>();
            borderImage.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            borderImage.type = Image.Type.Sliced;
            borderImage.color = new Color(0.95f, 0.72f, 0.20f, 0.75f);
            borderImage.raycastTarget = false;

            // 3. Inner Card Frame (Deep Slate Navy)
            GameObject cardObj = new GameObject("Quest_Card_Frame");
            cardObj.transform.SetParent(borderObj.transform, false);
            RectTransform cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.anchorMin = Vector2.zero;
            cardRt.anchorMax = Vector2.one;
            cardRt.offsetMin = new Vector2(3f, 3f);
            cardRt.offsetMax = new Vector2(-3f, -3f);

            bgCardImage = cardObj.AddComponent<Image>();
            bgCardImage.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            bgCardImage.type = Image.Type.Sliced;
            bgCardImage.color = new Color(0.06f, 0.10f, 0.18f, 0.96f);

            // Tap on card to show hint
            Button cardTapBtn = cardObj.AddComponent<Button>();
            cardTapBtn.transition = Selectable.Transition.None;
            cardTapBtn.onClick.AddListener(OnCardClicked);

            // 3. Left Icon Badge Box (52x52)
            GameObject iconBox = new GameObject("Icon_Badge");
            iconBox.transform.SetParent(cardObj.transform, false);
            RectTransform iconBoxRt = iconBox.AddComponent<RectTransform>();
            iconBoxRt.anchorMin = new Vector2(0f, 0.5f);
            iconBoxRt.anchorMax = new Vector2(0f, 0.5f);
            iconBoxRt.pivot = new Vector2(0.5f, 0.5f);
            iconBoxRt.anchoredPosition = new Vector2(34f, 0f);
            iconBoxRt.sizeDelta = new Vector2(52f, 52f);

            Image iconBoxImg = iconBox.AddComponent<Image>();
            iconBoxImg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            iconBoxImg.type = Image.Type.Sliced;
            iconBoxImg.color = new Color(0.12f, 0.22f, 0.36f, 0.95f);
            iconBoxImg.raycastTarget = false;

            // Icon Image inside badge
            GameObject icImgObj = new GameObject("Badge_Icon");
            icImgObj.transform.SetParent(iconBox.transform, false);
            RectTransform icImgRt = icImgObj.AddComponent<RectTransform>();
            icImgRt.anchorMin = new Vector2(0.5f, 0.5f);
            icImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            icImgRt.pivot = new Vector2(0.5f, 0.5f);
            icImgRt.anchoredPosition = new Vector2(0f, 6f);
            icImgRt.sizeDelta = new Vector2(34f, 34f);
            iconBadgeImage = icImgObj.AddComponent<Image>();
            iconBadgeImage.sprite = CasualUITheme.IconFish ?? whiteSp;
            iconBadgeImage.preserveAspect = true;
            iconBadgeImage.raycastTarget = false;

            GameObject iconTxtObj = new GameObject("Text_Tag");
            iconTxtObj.transform.SetParent(iconBox.transform, false);
            iconTagText = iconTxtObj.AddComponent<TextMeshProUGUI>();
            iconTagText.text = "QUEST";
            iconTagText.fontSize = 13f;
            iconTagText.fontStyle = FontStyles.Bold;
            iconTagText.alignment = TextAlignmentOptions.Center;
            iconTagText.color = new Color(1f, 0.85f, 0.25f);
            iconTagText.textWrappingMode = TextWrappingModes.NoWrap;
            iconTagText.raycastTarget = false;
            iconTagText.ApplyFont(UIFontCategory.Button);
            RectTransform itRt = iconTxtObj.GetComponent<RectTransform>();
            itRt.anchorMin = new Vector2(0f, 0f);
            itRt.anchorMax = new Vector2(1f, 0f);
            itRt.pivot = new Vector2(0.5f, 0f);
            itRt.anchoredPosition = new Vector2(0f, 2f);
            itRt.sizeDelta = new Vector2(0f, 16f);

            // 4. Center Content (Title, Description, Progress Slider)
            // Title Text
            GameObject titleObj = new GameObject("Text_Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "1. HARBOR QUEST";
            titleText.fontSize = 21f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = new Color(0.96f, 0.98f, 1f);
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.raycastTarget = false;
            titleText.ApplyFont(UIFontCategory.Header);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(74f, 25f);
            titleRt.sizeDelta = new Vector2(235f, 26f);

            // Description Text
            GameObject descObj = new GameObject("Text_Desc");
            descObj.transform.SetParent(cardObj.transform, false);
            descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = "Catch fish to fulfill requirements";
            descText.fontSize = 17f;
            descText.fontStyle = FontStyles.Normal;
            descText.alignment = TextAlignmentOptions.Left;
            descText.color = new Color(0.85f, 0.92f, 1f);
            descText.textWrappingMode = TextWrappingModes.NoWrap;
            descText.raycastTarget = false;
            descText.ApplyFont(UIFontCategory.Default);
            RectTransform descRt = descObj.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0.5f);
            descRt.anchorMax = new Vector2(0f, 0.5f);
            descRt.pivot = new Vector2(0f, 0.5f);
            descRt.anchoredPosition = new Vector2(74f, 0f);
            descRt.sizeDelta = new Vector2(235f, 22f);

            // Progress Bar Slider Container
            GameObject sliderObj = new GameObject("Progress_Slider");
            sliderObj.transform.SetParent(cardObj.transform, false);
            sliderContainer = sliderObj.AddComponent<RectTransform>();
            sliderContainer.anchorMin = new Vector2(0f, 0.5f);
            sliderContainer.anchorMax = new Vector2(0f, 0.5f);
            sliderContainer.pivot = new Vector2(0f, 0.5f);
            sliderContainer.anchoredPosition = new Vector2(74f, -26f);
            sliderContainer.sizeDelta = new Vector2(235f, 18f);

            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.sprite = CasualUITheme.SliderBg ?? whiteSp;
            sliderBg.type = Image.Type.Sliced;
            sliderBg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);
            sliderBg.raycastTarget = false;

            // Slider Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(sliderObj.transform, false);
            sliderFillImage = fillObj.AddComponent<Image>();
            sliderFillImage.sprite = CasualUITheme.SliderFillYellow ?? whiteSp;
            sliderFillImage.color = new Color(0.20f, 0.85f, 0.45f); // Crisp Emerald Green
            sliderFillImage.type = Image.Type.Filled;
            sliderFillImage.fillMethod = Image.FillMethod.Horizontal;
            sliderFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            sliderFillImage.fillAmount = 0f;
            sliderFillImage.raycastTarget = false;
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            // Progress Text
            GameObject progTxtObj = new GameObject("Text_Progress");
            progTxtObj.transform.SetParent(sliderObj.transform, false);
            progressText = progTxtObj.AddComponent<TextMeshProUGUI>();
            progressText.text = "0 / 1";
            progressText.fontSize = 14f;
            progressText.fontStyle = FontStyles.Bold;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.color = Color.white;
            progressText.textWrappingMode = TextWrappingModes.NoWrap;
            progressText.raycastTarget = false;
            progressText.ApplyFont(UIFontCategory.Numbers);
            RectTransform ptRt = progTxtObj.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero;
            ptRt.anchorMax = Vector2.one;
            ptRt.sizeDelta = Vector2.zero;

            // 5. Right Action / Claim Button (98x62)
            GameObject btnObj = new GameObject("Button_Claim");
            btnObj.transform.SetParent(cardObj.transform, false);
            claimButtonRect = btnObj.AddComponent<RectTransform>();
            claimButtonRect.anchorMin = new Vector2(1f, 0.5f);
            claimButtonRect.anchorMax = new Vector2(1f, 0.5f);
            claimButtonRect.pivot = new Vector2(1f, 0.5f);
            claimButtonRect.anchoredPosition = new Vector2(-10f, 0f);
            claimButtonRect.sizeDelta = new Vector2(100f, 64f);

            claimButtonImage = btnObj.AddComponent<Image>();
            claimButtonImage.sprite = CasualUITheme.BtnGreen ?? whiteSp;
            claimButtonImage.type = Image.Type.Sliced;
            claimButtonImage.color = Color.white;

            claimButton = btnObj.AddComponent<Button>();
            claimButton.targetGraphic = claimButtonImage;
            claimButton.onClick.AddListener(OnClaimButtonClicked);

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            claimButtonText = btnTxtObj.AddComponent<TextMeshProUGUI>();
            claimButtonText.text = "CLAIM!";
            claimButtonText.fontSize = 20f;
            claimButtonText.fontStyle = FontStyles.Bold;
            claimButtonText.alignment = TextAlignmentOptions.Center;
            claimButtonText.color = Color.white;
            claimButtonText.textWrappingMode = TextWrappingModes.NoWrap;
            claimButtonText.raycastTarget = false;
            claimButtonText.ApplyFont(UIFontCategory.Button);
            RectTransform btRt = btnTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            // 6. Floating Hint Toast Banner (Popup above widget on click)
            GameObject toastObj = new GameObject("Hint_Toast");
            toastObj.transform.SetParent(borderObj.transform, false);
            RectTransform toastRt = toastObj.AddComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0.5f, 0f);
            toastRt.anchorMax = new Vector2(0.5f, 0f);
            toastRt.pivot = new Vector2(0.5f, 1f);
            toastRt.anchoredPosition = new Vector2(0f, -6f);
            toastRt.sizeDelta = new Vector2(410f, 38f);

            Image toastImg = toastObj.AddComponent<Image>();
            toastImg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            toastImg.type = Image.Type.Sliced;
            toastImg.color = new Color(0.04f, 0.08f, 0.16f, 0.96f);
            toastImg.raycastTarget = false;

            GameObject toastTxtObj = new GameObject("Text");
            toastTxtObj.transform.SetParent(toastObj.transform, false);
            hintToastText = toastTxtObj.AddComponent<TextMeshProUGUI>();
            hintToastText.text = "";
            hintToastText.fontSize = 16f;
            hintToastText.fontStyle = FontStyles.Bold;
            hintToastText.alignment = TextAlignmentOptions.Center;
            hintToastText.color = new Color(1f, 0.90f, 0.35f);
            hintToastText.raycastTarget = false;
            hintToastText.ApplyFont(UIFontCategory.Header);
            RectTransform ttRt = toastTxtObj.GetComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.sizeDelta = Vector2.zero;
            toastObj.SetActive(false);
        }

        public void UpdateDisplay(QuestStep quest, int current, int target)
        {
            if (quest == null)
            {
                if (borderImage != null) borderImage.gameObject.SetActive(false);
                return;
            }
            if (borderImage != null) borderImage.gameObject.SetActive(true);

            if (iconTagText != null)
            {
                string raw = quest.iconTag != null ? quest.iconTag.Trim('[', ']', ' ') : "QUEST";
                iconTagText.text = raw.ToUpper();
            }

            if (iconBadgeImage != null)
            {
                string tagUpper = quest.iconTag != null ? quest.iconTag.ToUpper() : "";
                if (tagUpper.Contains("CONTRACT") || tagUpper.Contains("SUPPLY"))
                    iconBadgeImage.sprite = CasualUITheme.IconContract ?? CasualUITheme.IconFish;
                else if (tagUpper.Contains("UPGRADE") || tagUpper.Contains("ROD") || tagUpper.Contains("BOAT"))
                    iconBadgeImage.sprite = CasualUITheme.IconArrowUp ?? CasualUITheme.IconFish;
                else if (tagUpper.Contains("GOLD") || tagUpper.Contains("COIN") || tagUpper.Contains("EARN"))
                    iconBadgeImage.sprite = CasualUITheme.IconGold ?? CasualUITheme.IconFish;
                else
                    iconBadgeImage.sprite = CasualUITheme.IconFish;
            }

            if (titleText != null) titleText.text = quest.title;
            if (descText != null) descText.text = quest.description;

            targetFill = quest.GetProgressNormalized(current);
            if (progressText != null) progressText.text = quest.GetFormattedProgress(current);

            bool claimable = QuestManager.Instance != null && QuestManager.Instance.IsClaimable;
            SetClaimableVisual(claimable, quest.rewardCoins);
        }

        private void SetClaimableVisual(bool claimable, int rewardCoins)
        {
            isClaimable = claimable;

            if (claimable)
            {
                if (claimButtonImage != null)
                {
                    claimButtonImage.sprite = CasualUITheme.BtnGreen ?? claimButtonImage.sprite;
                    claimButtonImage.color = Color.white;
                }
                if (claimButtonText != null)
                {
                    claimButtonText.text = $"CLAIM! +{NumberFormatter.Format(rewardCoins)} G";
                    claimButtonText.color = Color.white;
                }
                if (sliderFillImage != null)
                {
                    sliderFillImage.color = new Color(0.20f, 0.90f, 0.45f); // Shiny green
                }
                if (borderImage != null)
                {
                    borderImage.color = new Color(0.20f, 0.90f, 0.45f, 0.90f);
                }
            }
            else
            {
                if (claimButtonRect != null) claimButtonRect.localScale = Vector3.one;
                if (claimButtonImage != null)
                {
                    claimButtonImage.sprite = CasualUITheme.BtnYellow ?? claimButtonImage.sprite;
                    claimButtonImage.color = new Color(0.9f, 0.9f, 0.9f, 0.85f);
                }
                if (claimButtonText != null)
                {
                    claimButtonText.text = $"+{NumberFormatter.Format(rewardCoins)} G";
                    claimButtonText.color = new Color(0.2f, 0.15f, 0.05f);
                }
                if (sliderFillImage != null)
                {
                    sliderFillImage.color = new Color(0.25f, 0.70f, 1f); // Clean light blue
                }
                if (borderImage != null)
                {
                    borderImage.color = new Color(0.95f, 0.72f, 0.20f, 0.40f);
                }
            }
        }

        private void OnClaimButtonClicked()
        {
            if (QuestManager.Instance == null) return;

            if (isClaimable)
            {
                // Claim quest with flying coins originating from this button!
                QuestManager.Instance.ClaimCurrentQuest(claimButtonRect);
                PunchWidgetScale();
            }
            else
            {
                // If not yet claimable, tapping shows helpful hint
                ShowHintToast();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayUIClick();
                }
            }
        }

        private void OnCardClicked()
        {
            ShowHintToast();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }
        }

        public void ShowHintToast()
        {
            var q = QuestManager.Instance != null ? QuestManager.Instance.CurrentQuest : null;
            if (q == null || string.IsNullOrEmpty(q.hintActionText)) return;

            if (hintToastText != null && hintToastText.transform.parent != null)
            {
                hintToastText.text = $"[HINT] {q.hintActionText}";
                hintToastText.transform.parent.gameObject.SetActive(true);

                if (hintToastRoutine != null) StopCoroutine(hintToastRoutine);
                hintToastRoutine = StartCoroutine(HideHintToastRoutine());
            }
        }

        private IEnumerator HideHintToastRoutine()
        {
            yield return new WaitForSeconds(3.5f);
            if (hintToastText != null && hintToastText.transform.parent != null)
            {
                hintToastText.transform.parent.gameObject.SetActive(false);
            }
        }

        private void PunchWidgetScale()
        {
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(PunchScaleRoutine());
        }

        private IEnumerator PunchScaleRoutine()
        {
            Transform t = borderImage != null ? borderImage.transform : transform;
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                if (t == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float s = 1f + Mathf.Sin(progress * Mathf.PI) * 0.12f;
                t.localScale = originalScale * s;
                yield return null;
            }

            if (t != null) t.localScale = originalScale;
        }

        #region Event Callbacks
        private void OnActiveQuestChanged(QuestStep newQuest)
        {
            int cur = QuestManager.Instance != null ? QuestManager.Instance.CurrentProgress : 0;
            int target = newQuest != null ? newQuest.targetAmount : 1;
            UpdateDisplay(newQuest, cur, target);
            PunchWidgetScale();
        }

        private void OnQuestProgressChanged(QuestStep quest, int cur, int target)
        {
            UpdateDisplay(quest, cur, target);
        }

        private void OnQuestCompleted(QuestStep quest)
        {
            SetClaimableVisual(true, quest != null ? quest.rewardCoins : 50);
            PunchWidgetScale();
        }

        private void OnQuestClaimed(QuestStep quest, int reward)
        {
            // Transitioning to next
        }
        #endregion
    }
}

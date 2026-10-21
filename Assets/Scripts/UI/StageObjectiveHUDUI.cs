using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Progression;
using IdleFishing.Audio;

namespace IdleFishing.UI
{
    /// <summary>
    /// HUD Hiển Thị Mục Tiêu Vượt Màn 30 Stages (Stage Objectives HUD):
    /// - Tích hợp gọn gàng vào Dock bên trái (LeftQuestEventDockUI) hoặc góc trên bên trái màn hình.
    /// - Không bao giờ che giữa màn hình hay cản trở khu vực câu cá và bến tàu.
    /// - Hỗ trợ thu gọn/mở rộng 2 tầng:
    ///   1. Nút [-] / [+] ngay trên thẻ để chuyển giữa Full View (chi tiết 3 mục tiêu) và Compact View (thanh 1 dòng thanh thoát).
    ///   2. Nút Tab TASKS / HIDE của Left Dock để trượt ẩn hoàn toàn 100% khỏi màn hình.
    /// - Thiết kế Casual UI Theme cao cấp: Sliced CardRound12, thanh đo xanh ngọc lục bảo lướt mượt mà, icon checkmark rực rỡ, badge thưởng Gold & Fame.
    /// </summary>
    public class StageObjectiveHUDUI : MonoBehaviour
    {
        private static StageObjectiveHUDUI _instance;
        public static StageObjectiveHUDUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<StageObjectiveHUDUI>();
                }
                return _instance;
            }
        }

        [Header("State")]
        [SerializeField] private bool isCollapsed = false;

        private RectTransform cardRootRect;
        private LayoutElement layoutElement;
        private Image borderImage;
        private Image bgCardImage;

        // Header Elements
        private Image iconBadgeImage;
        private TextMeshProUGUI headerTitleText;
        private Button toggleButton;
        private TextMeshProUGUI toggleButtonText;

        // Reward Bar
        private GameObject rewardRowObj;
        private TextMeshProUGUI rewardBadgeText;

        // Goals Container & Rows
        private GameObject goalsContainerObj;
        private RectTransform goalsContainerRect;

        private class GoalRowUI
        {
            public GameObject rowObj;
            public Image checkmarkIcon;
            public TextMeshProUGUI labelText;
            public Image barBg;
            public Image barFill;
            public TextMeshProUGUI countText;
            public float currentFill = 0f;
            public float targetFill = 0f;
        }

        private readonly List<GoalRowUI> goalRows = new List<GoalRowUI>();
        private Coroutine pulseRoutine;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            BuildProceduralUI();

            // Sync with active stage if StageProgressionManager is initialized
            if (StageProgressionManager.Instance != null && StageProgressionManager.Instance.CurrentStage != null)
            {
                UpdateStageDisplay(StageProgressionManager.Instance.CurrentStage);
            }
        }

        private void OnEnable()
        {
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.StageGoalProgressChanged += OnStageGoalProgressChanged;
            GameEvents.StageCompleted += OnStageCompleted;
        }

        private void OnDisable()
        {
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.StageGoalProgressChanged -= OnStageGoalProgressChanged;
            GameEvents.StageCompleted -= OnStageCompleted;
        }

        private void Update()
        {
            // Smoothly animate progress bars
            for (int i = 0; i < goalRows.Count; i++)
            {
                var row = goalRows[i];
                if (row.barFill != null && Mathf.Abs(row.currentFill - row.targetFill) > 0.002f)
                {
                    row.currentFill = Mathf.Lerp(row.currentFill, row.targetFill, Time.unscaledDeltaTime * 12f);
                    row.barFill.fillAmount = row.currentFill;
                }
            }
        }

        public void ToggleCollapse()
        {
            isCollapsed = !isCollapsed;
            ApplyCollapseState();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }
        }

        private void ApplyCollapseState()
        {
            if (goalsContainerObj != null)
            {
                goalsContainerObj.SetActive(!isCollapsed);
            }
            if (rewardRowObj != null)
            {
                rewardRowObj.SetActive(!isCollapsed);
            }

            if (toggleButtonText != null)
            {
                toggleButtonText.text = isCollapsed ? "[+]" : "[-]";
            }

            float targetHeight = isCollapsed ? 56f : 230f;
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = 540f;
                layoutElement.preferredHeight = targetHeight;
                layoutElement.minHeight = targetHeight;
            }
            if (cardRootRect != null)
            {
                cardRootRect.sizeDelta = new Vector2(540f, targetHeight);
            }

            // In compact view, show quick summary in header title
            var stage = StageProgressionManager.Instance != null ? StageProgressionManager.Instance.CurrentStage : null;
            if (stage != null && headerTitleText != null)
            {
                if (isCollapsed)
                {
                    int doneCount = 0;
                    if (StageProgressionManager.Instance.ActiveGoalProgress != null)
                    {
                        for (int i = 0; i < StageProgressionManager.Instance.ActiveGoalProgress.Count; i++)
                        {
                            if (StageProgressionManager.Instance.ActiveGoalProgress[i].isCompleted) doneCount++;
                        }
                    }
                    headerTitleText.text = $"STAGE {stage.stageNumber}: {stage.stageTitle.ToUpper()} ({doneCount}/{stage.goals.Count})";
                }
                else
                {
                    headerTitleText.text = $"STAGE {stage.stageNumber}: {stage.stageTitle.ToUpper()}";
                }
            }
        }

        private void OnStageStarted(StageData stage)
        {
            UpdateStageDisplay(stage);
        }

        private void OnStageGoalProgressChanged(StageData stage, int goalIndex, int current, int target)
        {
            if (goalIndex >= 0 && goalIndex < goalRows.Count)
            {
                var row = goalRows[goalIndex];
                float fill = Mathf.Clamp01((float)current / Mathf.Max(1, target));
                row.targetFill = fill;
                if (row.countText != null) row.countText.text = $"{current} / {target}";

                bool isDone = current >= target;
                if (row.checkmarkIcon != null)
                {
                    row.checkmarkIcon.color = isDone ? new Color(0.20f, 0.95f, 0.45f, 1f) : new Color(0.40f, 0.50f, 0.65f, 0.5f);
                }
            }
        }

        private void OnStageCompleted(StageData stage)
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(CelebrateStageClearRoutine(stage));
        }

        private IEnumerator CelebrateStageClearRoutine(StageData stage)
        {
            if (headerTitleText != null)
            {
                headerTitleText.text = $"<color=#FFD700>STAGE {stage.stageNumber} COMPLETE!</color>";
            }

            // Punch scale celebration
            Transform t = cardRootRect != null ? cardRootRect.transform : transform;
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                if (t == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float s = 1.0f + Mathf.Sin(progress * Mathf.PI) * 0.10f;
                t.localScale = originalScale * s;
                yield return null;
            }

            if (t != null) t.localScale = originalScale;
        }

        public void UpdateStageDisplay(StageData stage)
        {
            if (stage == null) return;

            if (headerTitleText != null)
            {
                headerTitleText.text = $"STAGE {stage.stageNumber}: {stage.stageTitle.ToUpper()}";
            }

            if (rewardBadgeText != null)
            {
                int gold = stage.CalculateGoldReward();
                rewardBadgeText.text = $"Reward: <color=#FFD700>+{NumberFormatter.Format(gold)} G</color>  |  <color=#00E5FF>+{stage.grantFameOnFirstClear} Fame</color>";
            }

            int activeCount = stage.goals != null ? stage.goals.Count : 0;
            for (int i = 0; i < goalRows.Count; i++)
            {
                if (i < activeCount)
                {
                    goalRows[i].rowObj.SetActive(true);
                    var cfg = stage.goals[i];
                    if (goalRows[i].labelText != null)
                    {
                        goalRows[i].labelText.text = cfg.GetDefaultDescription();
                    }

                    int cur = 0;
                    if (StageProgressionManager.Instance != null && 
                        StageProgressionManager.Instance.ActiveGoalProgress != null && 
                        i < StageProgressionManager.Instance.ActiveGoalProgress.Count)
                    {
                        cur = StageProgressionManager.Instance.ActiveGoalProgress[i].currentCount;
                    }

                    float fill = Mathf.Clamp01((float)cur / Mathf.Max(1, cfg.targetCount));
                    goalRows[i].targetFill = fill;
                    goalRows[i].currentFill = fill;
                    if (goalRows[i].barFill != null) goalRows[i].barFill.fillAmount = fill;
                    if (goalRows[i].countText != null) goalRows[i].countText.text = $"{cur} / {cfg.targetCount}";

                    bool isDone = cur >= cfg.targetCount;
                    if (goalRows[i].checkmarkIcon != null)
                    {
                        goalRows[i].checkmarkIcon.color = isDone ? new Color(0.20f, 0.95f, 0.45f, 1f) : new Color(0.40f, 0.50f, 0.65f, 0.5f);
                    }
                }
                else
                {
                    goalRows[i].rowObj.SetActive(false);
                }
            }

            ApplyCollapseState();
        }

        private void BuildProceduralUI()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Determine Parent Container: Prefer docking neatly inside LeftQuestEventDockUI
            Transform parentContainer = null;
            if (LeftQuestEventDockUI.Instance != null && LeftQuestEventDockUI.Instance.ContentContainer != null)
            {
                parentContainer = LeftQuestEventDockUI.Instance.ContentContainer;
            }
            else
            {
                // Fallback to Canvas: docked to Top-Left, NEVER dead center
                Canvas parentCanvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
                parentContainer = parentCanvas != null ? parentCanvas.transform : transform;
            }

            // Clean up any existing children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // 2. Outer Border Frame (540 x 230)
            GameObject borderObj = new GameObject("Stage_Objective_Card");
            borderObj.transform.SetParent(parentContainer, false);
            // Place at the very top of the dock so player immediately sees stage goals
            borderObj.transform.SetSiblingIndex(0);

            cardRootRect = borderObj.AddComponent<RectTransform>();
            cardRootRect.anchorMin = new Vector2(0f, 1f);
            cardRootRect.anchorMax = new Vector2(0f, 1f);
            cardRootRect.pivot = new Vector2(0f, 1f);
            cardRootRect.sizeDelta = new Vector2(540f, 230f);
            if (parentContainer != (LeftQuestEventDockUI.Instance != null ? LeftQuestEventDockUI.Instance.ContentContainer : null))
            {
                cardRootRect.anchoredPosition = new Vector2(16f, -145f);
            }

            layoutElement = borderObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 540f;
            layoutElement.preferredHeight = 230f;
            layoutElement.minHeight = 56f;

            borderImage = borderObj.AddComponent<Image>();
            borderImage.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            borderImage.type = Image.Type.Sliced;
            borderImage.color = new Color(0.95f, 0.75f, 0.22f, 0.85f); // Elegant Gold Border
            borderImage.raycastTarget = false;

            // 3. Inner Slate Frame (Deep Slate Navy)
            GameObject innerObj = new GameObject("Card_Inner");
            innerObj.transform.SetParent(borderObj.transform, false);
            RectTransform innerRt = innerObj.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(3f, 3f);
            innerRt.offsetMax = new Vector2(-3f, -3f);

            bgCardImage = innerObj.AddComponent<Image>();
            bgCardImage.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            bgCardImage.type = Image.Type.Sliced;
            bgCardImage.color = new Color(0.06f, 0.10f, 0.18f, 0.96f);

            // 4. Header Bar (Top 44px)
            GameObject headerObj = new GameObject("Header_Bar");
            headerObj.transform.SetParent(innerObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -4f);
            hRt.sizeDelta = new Vector2(-12f, 44f);

            // Icon Badge
            GameObject iconObj = new GameObject("Icon_Stage");
            iconObj.transform.SetParent(headerObj.transform, false);
            RectTransform icRt = iconObj.AddComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0f, 0.5f);
            icRt.anchorMax = new Vector2(0f, 0.5f);
            icRt.pivot = new Vector2(0f, 0.5f);
            icRt.anchoredPosition = new Vector2(6f, 0f);
            icRt.sizeDelta = new Vector2(34f, 34f);

            iconBadgeImage = iconObj.AddComponent<Image>();
            iconBadgeImage.sprite = CasualUITheme.IconCrown ?? CasualUITheme.IconFish ?? whiteSp;
            iconBadgeImage.preserveAspect = true;
            iconBadgeImage.raycastTarget = false;
            iconBadgeImage.color = new Color(1f, 0.88f, 0.25f);

            // Header Title Text
            GameObject titleObj = new GameObject("Text_Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            headerTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            headerTitleText.text = "STAGE 1: FIRST CATCH";
            headerTitleText.fontSize = 20f;
            headerTitleText.fontStyle = FontStyles.Bold;
            headerTitleText.color = new Color(1f, 0.88f, 0.25f);
            headerTitleText.alignment = TextAlignmentOptions.MidlineLeft;
            headerTitleText.textWrappingMode = TextWrappingModes.NoWrap;
            headerTitleText.raycastTarget = false;
            headerTitleText.ApplyFont(UIFontCategory.Header);

            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.anchoredPosition = new Vector2(44f, 0f);
            tRt.sizeDelta = new Vector2(-96f, 0f);

            // Collapse / Expand Toggle Button
            GameObject btnObj = new GameObject("Btn_ToggleCollapse");
            btnObj.transform.SetParent(headerObj.transform, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(1f, 0.5f);
            bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = new Vector2(-4f, 0f);
            bRt.sizeDelta = new Vector2(44f, 32f);

            Image btnImg = btnObj.AddComponent<Image>();
            toggleButton = btnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(toggleButton, btnImg, CasualBtnStyle.SquareNavy);
            toggleButton.onClick.AddListener(ToggleCollapse);

            GameObject bTxtObj = new GameObject("Text");
            bTxtObj.transform.SetParent(btnObj.transform, false);
            toggleButtonText = bTxtObj.AddComponent<TextMeshProUGUI>();
            toggleButtonText.text = "[-]";
            toggleButtonText.fontSize = 18f;
            toggleButtonText.fontStyle = FontStyles.Bold;
            toggleButtonText.color = new Color(0.9f, 0.95f, 1f);
            toggleButtonText.alignment = TextAlignmentOptions.Center;
            toggleButtonText.raycastTarget = false;
            toggleButtonText.ApplyFont(UIFontCategory.Button);
            RectTransform btRt = bTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            // 5. Reward Preview Line (Below header)
            rewardRowObj = new GameObject("Reward_Row");
            rewardRowObj.transform.SetParent(innerObj.transform, false);
            RectTransform rRt = rewardRowObj.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0f, 1f);
            rRt.anchorMax = new Vector2(1f, 1f);
            rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = new Vector2(0f, -50f);
            rRt.sizeDelta = new Vector2(-16f, 28f);

            Image rBg = rewardRowObj.AddComponent<Image>();
            rBg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            rBg.type = Image.Type.Sliced;
            rBg.color = new Color(0.10f, 0.16f, 0.26f, 0.90f);
            rBg.raycastTarget = false;

            GameObject rewardTxtObj = new GameObject("Reward_Text");
            rewardTxtObj.transform.SetParent(rewardRowObj.transform, false);
            RectTransform rtText = rewardTxtObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.sizeDelta = Vector2.zero;

            rewardBadgeText = rewardTxtObj.AddComponent<TextMeshProUGUI>();
            rewardBadgeText.text = "Reward: <color=#FFD700>+42 G</color>  |  <color=#00E5FF>+1 Fame</color>";
            rewardBadgeText.fontSize = 16f;
            rewardBadgeText.fontStyle = FontStyles.Bold;
            rewardBadgeText.alignment = TextAlignmentOptions.Center;
            rewardBadgeText.color = Color.white;
            rewardBadgeText.raycastTarget = false;
            rewardBadgeText.ApplyFont(UIFontCategory.Numbers);

            // 6. Goals Container (Houses up to 3 goal rows)
            goalsContainerObj = new GameObject("Goals_Container");
            goalsContainerObj.transform.SetParent(innerObj.transform, false);
            goalsContainerRect = goalsContainerObj.AddComponent<RectTransform>();
            goalsContainerRect.anchorMin = new Vector2(0f, 1f);
            goalsContainerRect.anchorMax = new Vector2(1f, 1f);
            goalsContainerRect.pivot = new Vector2(0.5f, 1f);
            goalsContainerRect.anchoredPosition = new Vector2(0f, -84f);
            goalsContainerRect.sizeDelta = new Vector2(-16f, 138f);

            var vlg = goalsContainerObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 2, 2);
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            goalRows.Clear();
            for (int i = 0; i < 3; i++)
            {
                var row = CreateGoalRow(goalsContainerObj.transform, i);
                goalRows.Add(row);
            }

            ApplyCollapseState();
        }

        private GoalRowUI CreateGoalRow(Transform parent, int index)
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            var rowObj = new GameObject($"GoalRow_{index}");
            rowObj.transform.SetParent(parent, false);

            var le = rowObj.AddComponent<LayoutElement>();
            le.preferredHeight = 36f;
            le.minHeight = 36f;

            var rect = rowObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 36f);

            // Checkmark Icon (Left edge)
            var checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(rowObj.transform, false);
            var checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0f, 0.5f);
            checkRect.anchoredPosition = new Vector2(6f, 0f);
            checkRect.sizeDelta = new Vector2(22f, 22f);

            var checkImg = checkObj.AddComponent<Image>();
            checkImg.sprite = CasualUITheme.IconCheck ?? whiteSp;
            checkImg.preserveAspect = true;
            checkImg.color = new Color(0.20f, 0.95f, 0.45f, 1f);
            checkImg.raycastTarget = false;

            // Goal Description Text
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rowObj.transform, false);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "Catch 1 Herring";
            labelText.fontSize = 16.5f;
            labelText.fontStyle = FontStyles.Normal;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = new Color(0.92f, 0.96f, 1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.raycastTarget = false;
            labelText.ApplyFont(UIFontCategory.Default);

            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.58f, 1f);
            labelRt.pivot = new Vector2(0f, 0.5f);
            labelRt.anchoredPosition = new Vector2(32f, 0f);
            labelRt.sizeDelta = new Vector2(-36f, 0f);

            // Progress Bar Background (Right side)
            var barBgObj = new GameObject("BarBg");
            barBgObj.transform.SetParent(rowObj.transform, false);
            var barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.60f, 0.08f);
            barBgRect.anchorMax = new Vector2(1f, 0.92f);
            barBgRect.pivot = new Vector2(0.5f, 0.5f);
            barBgRect.anchoredPosition = Vector2.zero;
            barBgRect.sizeDelta = Vector2.zero;

            var barBgImg = barBgObj.AddComponent<Image>();
            barBgImg.sprite = CasualUITheme.SliderBg ?? whiteSp;
            barBgImg.type = Image.Type.Sliced;
            barBgImg.color = new Color(0.04f, 0.08f, 0.14f, 0.95f);
            barBgImg.raycastTarget = false;

            // Progress Bar Fill
            var barFillObj = new GameObject("BarFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            var barFillRect = barFillObj.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.sizeDelta = Vector2.zero;

            var barFillImg = barFillObj.AddComponent<Image>();
            barFillImg.sprite = CasualUITheme.SliderFillGreen ?? whiteSp;
            barFillImg.type = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFillImg.fillAmount = 0f;
            barFillImg.color = new Color(0.20f, 0.90f, 0.45f, 1f); // Vibrant Emerald Green
            barFillImg.raycastTarget = false;

            // Progress Fraction Text (Centered on bar)
            var countObj = new GameObject("CountText");
            countObj.transform.SetParent(barBgObj.transform, false);
            var countText = countObj.AddComponent<TextMeshProUGUI>();
            countText.text = "0 / 1";
            countText.fontSize = 15f;
            countText.fontStyle = FontStyles.Bold;
            countText.alignment = TextAlignmentOptions.Center;
            countText.color = Color.white;
            countText.raycastTarget = false;
            countText.ApplyFont(UIFontCategory.Numbers);

            var countRt = countObj.GetComponent<RectTransform>();
            countRt.anchorMin = Vector2.zero;
            countRt.anchorMax = Vector2.one;
            countRt.sizeDelta = Vector2.zero;

            return new GoalRowUI
            {
                rowObj = rowObj,
                checkmarkIcon = checkImg,
                labelText = labelText,
                barBg = barBgImg,
                barFill = barFillImg,
                countText = countText,
                currentFill = 0f,
                targetFill = 0f
            };
        }
    }
}

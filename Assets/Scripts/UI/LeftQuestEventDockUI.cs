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
    /// Thanh Dock Nhiệm Vụ & Sự Kiện Bên Trái (Left-Side Collapsible Quest & Event Dock):
    /// - Gom toàn bộ: Nhiệm Vụ Tân Thủ (Quest), Event Giá Cá (Market Trend), Cơ Chế Vùng (World Gimmick), Frenzy 2X vào góc trái.
    /// - Để trống 65% màn hình ở giữa và bên phải cho bến tàu, cần thủ và các popup modal.
    /// - Cho phép bấm vào Tab để thu gọn (Collapse) hoặc mở rộng (Expand) 1 chạm mượt mà.
    /// - Có badge thông báo nhấp nháy trên tab khi có nhiệm vụ hoàn thành chờ nhận thưởng.
    /// </summary>
    public class LeftQuestEventDockUI : MonoBehaviour
    {
        private static LeftQuestEventDockUI _instance;
        public static LeftQuestEventDockUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<LeftQuestEventDockUI>();
                    if (_instance == null)
                    {
                        var canvas = FindAnyObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            _instance = canvas.gameObject.AddComponent<LeftQuestEventDockUI>();
                        }
                    }
                }
                return _instance;
            }
        }

        [Header("Tuning")]
        [SerializeField] private float slideSpeed = 16f;
        [SerializeField] private Vector2 expandedPos = new Vector2(16f, -145f);
        [SerializeField] private Vector2 collapsedPos = new Vector2(-540f, -145f);

        [Header("Runtime State")]
        [SerializeField] private bool isCollapsed = true;

        private RectTransform dockRect;
        private RectTransform contentContainer;
        private Button toggleTabButton;
        private TextMeshProUGUI toggleArrowText;
        private TextMeshProUGUI tabLabelText;
        private GameObject claimBadgeObj;
        private Button collapseHeaderBtn;
        private TextMeshProUGUI collapseHeaderBtnText;

        public RectTransform ContentContainer => contentContainer;
        public bool IsCollapsed => isCollapsed;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            if (contentContainer == null)
            {
                BuildProceduralDock();
            }
        }

        private void Start()
        {
            isCollapsed = true;
            if (dockRect != null)
            {
                dockRect.anchoredPosition = collapsedPos;
            }
            UpdateTabVisuals();
            CheckClaimBadge();
        }

        private void OnEnable()
        {
            GameEvents.QuestCompleted += OnQuestCompleted;
            GameEvents.QuestClaimed += OnQuestClaimed;
            GameEvents.ActiveQuestChanged += OnActiveQuestChanged;
        }

        private void OnDisable()
        {
            GameEvents.QuestCompleted -= OnQuestCompleted;
            GameEvents.QuestClaimed -= OnQuestClaimed;
            GameEvents.ActiveQuestChanged -= OnActiveQuestChanged;
        }

        private void Update()
        {
            if (dockRect != null)
            {
                Vector2 target = isCollapsed ? collapsedPos : expandedPos;
                dockRect.anchoredPosition = Vector2.Lerp(dockRect.anchoredPosition, target, Time.unscaledDeltaTime * slideSpeed);
            }

            // Pulse badge if claimable
            if (claimBadgeObj != null && claimBadgeObj.activeSelf)
            {
                float s = 1.0f + Mathf.Sin(Time.unscaledTime * 7f) * 0.12f;
                claimBadgeObj.transform.localScale = new Vector3(s, s, 1f);
            }
        }

        public void ToggleCollapse()
        {
            isCollapsed = !isCollapsed;
            UpdateTabVisuals();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }
        }

        public void Expand()
        {
            if (isCollapsed)
            {
                isCollapsed = false;
                UpdateTabVisuals();
            }
        }

        public void Collapse()
        {
            if (!isCollapsed)
            {
                isCollapsed = true;
                UpdateTabVisuals();
            }
        }

        private void UpdateTabVisuals()
        {
            if (toggleArrowText != null)
            {
                toggleArrowText.text = isCollapsed ? ">" : "<";
            }
            if (tabLabelText != null)
            {
                tabLabelText.text = isCollapsed ? "TASKS" : "HIDE";
            }
            if (collapseHeaderBtnText != null)
            {
                collapseHeaderBtnText.text = isCollapsed ? "[+] EXPAND" : "[-] COLLAPSE";
            }
        }

        private void OnQuestCompleted(QuestStep quest)
        {
            CheckClaimBadge();
        }

        private void OnQuestClaimed(QuestStep quest, int reward)
        {
            CheckClaimBadge();
        }

        private void OnActiveQuestChanged(QuestStep quest)
        {
            CheckClaimBadge();
        }

        private void CheckClaimBadge()
        {
            if (claimBadgeObj == null) return;
            bool canClaim = (QuestManager.Instance != null && QuestManager.Instance.IsClaimable);
            claimBadgeObj.SetActive(canClaim);
        }

        private void BuildProceduralDock()
        {
            Canvas parentCanvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (parentCanvas == null && GameHUD.Instance != null)
            {
                parentCanvas = GameHUD.Instance.GetComponentInParent<Canvas>();
            }

            Transform rootParent = (parentCanvas != null) ? parentCanvas.transform : transform;
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Root Dock GameObject (Anchored Top-Left)
            GameObject dockObj = new GameObject("Left_QuestEvent_Dock");
            dockObj.transform.SetParent(rootParent, false);

            dockRect = dockObj.AddComponent<RectTransform>();
            dockRect.anchorMin = new Vector2(0f, 1f);
            dockRect.anchorMax = new Vector2(0f, 1f);
            dockRect.pivot = new Vector2(0f, 1f);
            dockRect.sizeDelta = new Vector2(540f, 0f); // Scaled width for clear legibility on mobile
            dockRect.anchoredPosition = expandedPos;

            // 2. Header Bar with Collapse Button
            GameObject headerObj = new GameObject("Dock_Header");
            headerObj.transform.SetParent(dockObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0f, 1f);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(0f, 52f);

            Image hBg = headerObj.AddComponent<Image>();
            CasualUITheme.StyleCard(hBg, new Color(0.06f, 0.10f, 0.18f, 0.95f));

            // Header Title
            GameObject titleObj = new GameObject("Text_Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "[*] QUESTS & EVENTS";
            titleTmp.fontSize = 22f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.85f, 0.35f);
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.raycastTarget = false;
            titleTmp.ApplyFont(UIFontCategory.Header);

            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(0.64f, 1f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.anchoredPosition = new Vector2(14f, 0f);
            tRt.sizeDelta = new Vector2(-15f, 0f);

            // Header Collapse Button
            GameObject hBtnObj = new GameObject("Btn_CollapseHeader");
            hBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform hbRt = hBtnObj.AddComponent<RectTransform>();
            hbRt.anchorMin = new Vector2(0.64f, 0f);
            hbRt.anchorMax = new Vector2(1f, 1f);
            hbRt.pivot = new Vector2(1f, 0.5f);
            hbRt.offsetMin = new Vector2(4f, 4f);
            hbRt.offsetMax = new Vector2(-4f, -4f);

            Image hbImg = hBtnObj.AddComponent<Image>();
            collapseHeaderBtn = hBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(collapseHeaderBtn, hbImg, CasualBtnStyle.SquareNavy);
            collapseHeaderBtn.onClick.AddListener(ToggleCollapse);

            GameObject hbTxtObj = new GameObject("Text");
            hbTxtObj.transform.SetParent(hBtnObj.transform, false);
            collapseHeaderBtnText = hbTxtObj.AddComponent<TextMeshProUGUI>();
            collapseHeaderBtnText.text = "[-] COLLAPSE";
            collapseHeaderBtnText.fontSize = 18f;
            collapseHeaderBtnText.fontStyle = FontStyles.Bold;
            collapseHeaderBtnText.color = new Color(0.85f, 0.90f, 1f);
            collapseHeaderBtnText.alignment = TextAlignmentOptions.Center;
            collapseHeaderBtnText.raycastTarget = false;
            collapseHeaderBtnText.ApplyFont(UIFontCategory.Button);
            RectTransform hbtRt = hbTxtObj.GetComponent<RectTransform>();
            hbtRt.anchorMin = Vector2.zero;
            hbtRt.anchorMax = Vector2.one;
            hbtRt.sizeDelta = Vector2.zero;

            // 3. Content Container (Houses Quest, Market Trend, World Gimmick, Frenzy)
            GameObject contentObj = new GameObject("Dock_Content");
            contentObj.transform.SetParent(dockObj.transform, false);
            contentContainer = contentObj.AddComponent<RectTransform>();
            contentContainer.anchorMin = new Vector2(0f, 1f);
            contentContainer.anchorMax = new Vector2(1f, 1f);
            contentContainer.pivot = new Vector2(0f, 1f);
            contentContainer.anchoredPosition = new Vector2(0f, -56f);
            contentContainer.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 4);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // 4. Protruding Toggle Tab Button (Attached to right edge of Dock)
            GameObject tabObj = new GameObject("Dock_ToggleTab");
            tabObj.transform.SetParent(dockObj.transform, false);
            RectTransform tabRt = tabObj.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(1f, 1f);
            tabRt.anchorMax = new Vector2(1f, 1f);
            tabRt.pivot = new Vector2(0f, 1f);
            tabRt.anchoredPosition = new Vector2(2f, 0f);
            tabRt.sizeDelta = new Vector2(62f, 130f);

            Image tabImg = tabObj.AddComponent<Image>();
            toggleTabButton = tabObj.AddComponent<Button>();
            CasualUITheme.StyleButton(toggleTabButton, tabImg, CasualBtnStyle.SquareNavy);
            toggleTabButton.onClick.AddListener(ToggleCollapse);

            // Tab Arrow
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(tabObj.transform, false);
            toggleArrowText = arrowObj.AddComponent<TextMeshProUGUI>();
            toggleArrowText.text = "<";
            toggleArrowText.fontSize = 28f;
            toggleArrowText.fontStyle = FontStyles.Bold;
            toggleArrowText.color = new Color(1f, 0.85f, 0.35f);
            toggleArrowText.alignment = TextAlignmentOptions.Center;
            toggleArrowText.raycastTarget = false;
            toggleArrowText.ApplyFont(UIFontCategory.Button);
            RectTransform aRt = arrowObj.GetComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0f, 0.5f);
            aRt.anchorMax = new Vector2(1f, 1f);
            aRt.anchoredPosition = new Vector2(0f, -6f);
            aRt.sizeDelta = Vector2.zero;

            // Tab Label Text
            GameObject tabTxtObj = new GameObject("Label");
            tabTxtObj.transform.SetParent(tabObj.transform, false);
            tabLabelText = tabTxtObj.AddComponent<TextMeshProUGUI>();
            tabLabelText.text = "TASKS";
            tabLabelText.fontSize = 16f;
            tabLabelText.fontStyle = FontStyles.Bold;
            tabLabelText.color = Color.white;
            tabLabelText.alignment = TextAlignmentOptions.Center;
            tabLabelText.raycastTarget = false;
            tabLabelText.ApplyFont(UIFontCategory.Button);
            RectTransform tlRt = tabTxtObj.GetComponent<RectTransform>();
            tlRt.anchorMin = new Vector2(0f, 0f);
            tlRt.anchorMax = new Vector2(1f, 0.5f);
            tlRt.anchoredPosition = new Vector2(0f, 4f);
            tlRt.sizeDelta = Vector2.zero;

            // Claim Alert Notification Badge (Green/Gold Dot with "!")
            claimBadgeObj = new GameObject("ClaimAlertBadge");
            claimBadgeObj.transform.SetParent(tabObj.transform, false);
            RectTransform cbRt = claimBadgeObj.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(0.5f, 0.5f);
            cbRt.anchoredPosition = new Vector2(-4f, -4f);
            cbRt.sizeDelta = new Vector2(24f, 24f);

            Image cbImg = claimBadgeObj.AddComponent<Image>();
            cbImg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            cbImg.color = new Color(0.18f, 0.85f, 0.40f, 1f); // Vibrant Green
            cbImg.raycastTarget = false;

            GameObject exObj = new GameObject("Exclamation");
            exObj.transform.SetParent(claimBadgeObj.transform, false);
            var exTxt = exObj.AddComponent<TextMeshProUGUI>();
            exTxt.text = "!";
            exTxt.fontSize = 17f;
            exTxt.fontStyle = FontStyles.Bold;
            exTxt.color = Color.white;
            exTxt.alignment = TextAlignmentOptions.Center;
            exTxt.raycastTarget = false;
            exTxt.ApplyFont(UIFontCategory.Button);
            RectTransform exRt = exObj.GetComponent<RectTransform>();
            exRt.anchorMin = Vector2.zero;
            exRt.anchorMax = Vector2.one;
            exRt.sizeDelta = Vector2.zero;

            claimBadgeObj.SetActive(false);
        }
    }
}

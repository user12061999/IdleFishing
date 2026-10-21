using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Audio;
using IdleFishing.VFX;

namespace IdleFishing.UI
{
    /// <summary>
    /// Thanh điều khiển nhanh cho phép người chơi trực tiếp chọn và xem thử (Preview)
    /// bất kỳ Level / Vùng Đất nào trong 5 Level ngay trong game mà không cần cày vàng.
    /// </summary>
    public class LevelPreviewBarUI : MonoBehaviour
    {
        public static LevelPreviewBarUI Instance { get; private set; }

        private GameObject barRoot;
        private GameObject fullBarContainer;
        private GameObject collapsedButtonObj;

        private List<Button> levelButtons = new List<Button>();
        private List<Image> levelButtonImages = new List<Image>();
        private List<TextMeshProUGUI> levelButtonTexts = new List<TextMeshProUGUI>();

        private bool isCollapsed = true;

        private readonly Color activeLevelColor = new Color(0.20f, 0.75f, 0.45f, 0.98f);    // Green Emerald
        private readonly Color inactiveLevelColor = new Color(0.14f, 0.22f, 0.32f, 0.95f);  // Navy Slate
        private readonly Color hoverLevelColor = new Color(0.25f, 0.40f, 0.58f, 0.95f);

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
            BuildProceduralBar();
            RefreshButtonStates();
        }

        private void OnEnable()
        {
            GameEvents.WorldChanged += HandleWorldChanged;
            GameEvents.WorldUnlocked += HandleWorldUnlocked;
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= HandleWorldChanged;
            GameEvents.WorldUnlocked -= HandleWorldUnlocked;
        }

        private void HandleWorldChanged(WorldData newWorld)
        {
            RefreshButtonStates();
        }

        private void HandleWorldUnlocked(WorldData newWorld)
        {
            RefreshButtonStates();
        }

        [SerializeField] private bool enableInGameBar = false;

        private void BuildProceduralBar()
        {
            if (!enableInGameBar) return;

            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            barRoot = new GameObject("LevelPreviewBar_Root");
            barRoot.transform.SetParent(transform, false);
            RectTransform brRt = barRoot.AddComponent<RectTransform>();
            brRt.anchorMin = Vector2.zero;
            brRt.anchorMax = Vector2.one;
            brRt.sizeDelta = Vector2.zero;

            // ========================================================
            // 1. FULL EXPANDED BAR (Ngang, nằm dưới Top HUD, gọn gàng)
            // ========================================================
            fullBarContainer = new GameObject("FullBar_Container");
            fullBarContainer.transform.SetParent(barRoot.transform, false);

            RectTransform fbRt = fullBarContainer.AddComponent<RectTransform>();
            fbRt.anchorMin = new Vector2(0.5f, 1f);
            fbRt.anchorMax = new Vector2(0.5f, 1f);
            fbRt.pivot = new Vector2(0.5f, 1f);
            fbRt.anchoredPosition = new Vector2(0, -112); // Right below Top HUD
            fbRt.sizeDelta = new Vector2(840, 56);

            Image fbBg = fullBarContainer.AddComponent<Image>();
            CasualUITheme.StyleCard(fbBg, new Color(0.08f, 0.12f, 0.18f, 0.96f));

            // Title Label: "PREVIEW:"
            GameObject labelObj = new GameObject("Label_Preview");
            labelObj.transform.SetParent(fullBarContainer.transform, false);
            var labelTxt = labelObj.AddComponent<TextMeshProUGUI>();
            labelTxt.text = "PREVIEW:";
            labelTxt.fontSize = 18;
            labelTxt.fontStyle = FontStyles.Bold;
            labelTxt.alignment = TextAlignmentOptions.MidlineLeft;
            labelTxt.color = new Color(1f, 0.85f, 0.25f, 0.95f); // Golden Yellow
            labelTxt.textWrappingMode = TextWrappingModes.NoWrap;
            labelTxt.raycastTarget = false;
            labelTxt.ApplyFont(UIFontCategory.Button);
            RectTransform lRt = labelObj.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0f, 1f);
            lRt.pivot = new Vector2(0f, 0.5f);
            lRt.anchoredPosition = new Vector2(18, 0);
            lRt.sizeDelta = new Vector2(110, 0);

            // 5 Level Buttons: [LV 1] [LV 2] [LV 3] [LV 4] [LV 5]
            float startX = 135f;
            float btnWidth = 115f;
            float spacing = 12f;

            for (int i = 0; i < 5; i++)
            {
                int levelIndex = i;
                int levelNum = i + 1;

                GameObject btnObj = new GameObject($"Btn_Level_{levelNum}");
                btnObj.transform.SetParent(fullBarContainer.transform, false);

                RectTransform bRt = btnObj.AddComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0f, 0.5f);
                bRt.anchorMax = new Vector2(0f, 0.5f);
                bRt.pivot = new Vector2(0f, 0.5f);
                bRt.anchoredPosition = new Vector2(startX + i * (btnWidth + spacing), 0);
                bRt.sizeDelta = new Vector2(btnWidth, 44);

                Image btnImg = btnObj.AddComponent<Image>();
                Button btn = btnObj.AddComponent<Button>();
                CasualUITheme.StyleButton(btn, btnImg, CasualBtnStyle.Dark);

                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(btnObj.transform, false);
                var txt = txtObj.AddComponent<TextMeshProUGUI>();
                txt.text = $"LV {levelNum}";
                txt.fontSize = 17;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
                txt.textWrappingMode = TextWrappingModes.NoWrap;
                txt.raycastTarget = false;
                txt.ApplyFont(UIFontCategory.Button);

                RectTransform tRt = txtObj.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.sizeDelta = Vector2.zero;

                btn.onClick.AddListener(() => OnLevelButtonClicked(levelIndex));

                levelButtons.Add(btn);
                levelButtonImages.Add(btnImg);
                levelButtonTexts.Add(txt);
            }

            // Collapse Button: "[-]" on the far right
            GameObject minBtnObj = new GameObject("Btn_Minimize");
            minBtnObj.transform.SetParent(fullBarContainer.transform, false);
            RectTransform mbRt = minBtnObj.AddComponent<RectTransform>();
            mbRt.anchorMin = new Vector2(1f, 0.5f);
            mbRt.anchorMax = new Vector2(1f, 0.5f);
            mbRt.pivot = new Vector2(1f, 0.5f);
            mbRt.anchoredPosition = new Vector2(-16, 0);
            mbRt.sizeDelta = new Vector2(44, 44);

            Image mbImg = minBtnObj.AddComponent<Image>();
            Button minBtn = minBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(minBtn, mbImg, CasualBtnStyle.SquareGray);

            GameObject mbTxtObj = new GameObject("Text");
            mbTxtObj.transform.SetParent(minBtnObj.transform, false);
            var mbTxt = mbTxtObj.AddComponent<TextMeshProUGUI>();
            mbTxt.text = "-";
            mbTxt.fontSize = 22;
            mbTxt.fontStyle = FontStyles.Bold;
            mbTxt.alignment = TextAlignmentOptions.Center;
            mbTxt.color = new Color(0.85f, 0.90f, 0.95f);
            mbTxt.raycastTarget = false;
            mbTxt.ApplyFont(UIFontCategory.Button);
            RectTransform mbtRt = mbTxtObj.GetComponent<RectTransform>();
            mbtRt.anchorMin = Vector2.zero;
            mbtRt.anchorMax = Vector2.one;
            mbtRt.sizeDelta = Vector2.zero;

            minBtn.onClick.AddListener(ToggleCollapse);

            // ========================================================
            // 2. COLLAPSED FLOATING PILL BUTTON "[+ PREVIEW]"
            // ========================================================
            collapsedButtonObj = new GameObject("CollapsedPill_Button");
            collapsedButtonObj.transform.SetParent(barRoot.transform, false);

            RectTransform cRt = collapsedButtonObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 1f);
            cRt.anchorMax = new Vector2(0.5f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = new Vector2(0, -112);
            cRt.sizeDelta = new Vector2(240, 48);

            Image cImg = collapsedButtonObj.AddComponent<Image>();
            Button cBtn = collapsedButtonObj.AddComponent<Button>();
            CasualUITheme.StyleButton(cBtn, cImg, CasualBtnStyle.Blue);

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(collapsedButtonObj.transform, false);
            var cTxt = cTxtObj.AddComponent<TextMeshProUGUI>();
            cTxt.text = "+ PREVIEW";
            cTxt.fontSize = 20;
            cTxt.fontStyle = FontStyles.Bold;
            cTxt.alignment = TextAlignmentOptions.Center;
            cTxt.color = Color.white;
            cTxt.textWrappingMode = TextWrappingModes.NoWrap;
            cTxt.raycastTarget = false;
            cTxt.ApplyFont(UIFontCategory.Button);
            RectTransform ctRt = cTxtObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.sizeDelta = Vector2.zero;

            cBtn.onClick.AddListener(ToggleCollapse);

            // Mặc định thu gọn thanh preview để nhường tầm nhìn câu cá & dock nâng cấp
            collapsedButtonObj.SetActive(true);
            fullBarContainer.SetActive(false);
        }

        private void ToggleCollapse()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            isCollapsed = !isCollapsed;
            fullBarContainer.SetActive(!isCollapsed);
            collapsedButtonObj.SetActive(isCollapsed);
        }

        private void OnLevelButtonClicked(int levelIndex)
        {
            if (WorldManager.Instance == null) return;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

            // Kích hoạt chuỗi chuyển cảnh Thuyền Ngoài Khơi & Load In/Out tới cấp độ đã chọn
            WorldManager.Instance.RequestWorldTransition(levelIndex, isPreview: true);

            RefreshButtonStates();
        }

        public void RefreshButtonStates()
        {
            if (WorldManager.Instance == null) return;

            int activeIndex = WorldManager.Instance.ActiveWorldIndex;

            for (int i = 0; i < levelButtons.Count; i++)
            {
                if (i >= levelButtonImages.Count || i >= levelButtonTexts.Count) continue;

                int order = i + 1;
                bool isActive = (i == activeIndex);

                if (isActive)
                {
                    CasualUITheme.StyleButton(levelButtons[i], levelButtonImages[i], CasualBtnStyle.Green);
                    levelButtonTexts[i].text = $"<b>LV {order}</b>";
                    levelButtonTexts[i].color = Color.white;
                }
                else
                {
                    CasualUITheme.StyleButton(levelButtons[i], levelButtonImages[i], CasualBtnStyle.Dark);
                    levelButtonTexts[i].text = $"LV {order}";
                    levelButtonTexts[i].color = new Color(0.80f, 0.88f, 0.95f);
                }
            }
        }
    }
}

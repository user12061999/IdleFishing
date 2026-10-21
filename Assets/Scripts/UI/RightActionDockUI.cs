using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Audio;

namespace IdleFishing.UI
{
    /// <summary>
    /// Right-side Collapsible Action Dock (Vertical layout for AQUARIUM, VOYAGE, AUDIO):
    /// - Arranged vertically along the right screen edge with large, comfortable buttons (92x94).
    /// - Features a protruding toggle tab [MENU / HIDE] mirroring the left TASKS dock.
    /// - Smooth lerp slide animation between expanded and collapsed states.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class RightActionDockUI : MonoBehaviour
    {
        private static RightActionDockUI _instance;
        public static RightActionDockUI Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<RightActionDockUI>();
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Dock Framing")]
        [SerializeField] private Vector2 expandedPos = new Vector2(-12f, -115f);
        [SerializeField] private Vector2 collapsedPos = new Vector2(98f, -115f);
        [SerializeField] private float slideSpeed = 14f;

        [SerializeField] private RectTransform dockRect;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Button toggleTabButton;
        [SerializeField] private TextMeshProUGUI toggleArrowText;
        [SerializeField] private TextMeshProUGUI toggleLabelText;
        [SerializeField] private GameObject tabAlertBadgeObj;
        private bool isCollapsed = false;

        public bool IsCollapsed => isCollapsed;
        public Transform ButtonContainer
        {
            get
            {
                if (buttonContainer != null) return buttonContainer;
                buttonContainer = transform.Find("VerticalButtonContainer");
                if (buttonContainer != null) return buttonContainer;
                return transform;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildProceduralUI();
        }

        private void Start()
        {
            UpdateTabVisuals();
            SortDockButtons();
        }

        public void SortDockButtons()
        {
            var container = ButtonContainer;
            if (container == null) return;
            var aq = container.Find("Btn_Aquarium");
            var exp = container.Find("Btn_Expedition");
            var audio = container.Find("Btn_AudioSettings");

            if (aq != null) aq.SetSiblingIndex(0);
            if (exp != null) exp.SetSiblingIndex(1);
            if (audio != null) audio.SetSiblingIndex(2);
        }

        private void Update()
        {
            if (dockRect == null) dockRect = GetComponent<RectTransform>();

            if (dockRect != null)
            {
                Vector2 target = isCollapsed ? collapsedPos : expandedPos;
                dockRect.anchoredPosition = Vector2.Lerp(dockRect.anchoredPosition, target, Time.unscaledDeltaTime * slideSpeed);
            }

            // Pulse tab alert badge if active
            if (tabAlertBadgeObj != null && tabAlertBadgeObj.activeSelf)
            {
                float s = 1.0f + Mathf.Sin(Time.unscaledTime * 6f) * 0.12f;
                tabAlertBadgeObj.transform.localScale = new Vector3(s, s, 1f);
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
                toggleArrowText.text = isCollapsed ? "<" : ">";
            }
            if (toggleLabelText != null)
            {
                toggleLabelText.text = isCollapsed ? "MENU" : "HIDE";
            }
        }

        public void SetTabAlert(bool hasAlert)
        {
            if (tabAlertBadgeObj != null)
            {
                tabAlertBadgeObj.SetActive(hasAlert);
            }
        }

        private void BuildProceduralUI()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();
            Sprite cardSp = CasualUITheme.CardRound12 ?? whiteSp;

            // 1. Root Dock
            if (dockRect == null) dockRect = GetComponent<RectTransform>();
            if (dockRect == null) dockRect = gameObject.AddComponent<RectTransform>();
            dockRect.anchorMin = new Vector2(1f, 1f);
            dockRect.anchorMax = new Vector2(1f, 1f);
            dockRect.pivot = new Vector2(1f, 1f);
            dockRect.anchoredPosition = expandedPos;
            dockRect.sizeDelta = new Vector2(100f, 320f);

            // 2. Vertical Button Container
            if (buttonContainer == null)
            {
                Transform existing = transform.Find("VerticalButtonContainer");
                if (existing != null)
                {
                    buttonContainer = existing;
                }
                else
                {
                    GameObject containerObj = new GameObject("VerticalButtonContainer");
                    containerObj.transform.SetParent(transform, false);
                    buttonContainer = containerObj.transform;

                    RectTransform cRt = containerObj.AddComponent<RectTransform>();
                    cRt.anchorMin = new Vector2(0f, 0f);
                    cRt.anchorMax = new Vector2(1f, 1f);
                    cRt.pivot = new Vector2(0.5f, 1f);
                    cRt.anchoredPosition = Vector2.zero;
                    cRt.sizeDelta = Vector2.zero;

                    var vlg = containerObj.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = 10f;
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    vlg.childControlWidth = false;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = false;
                    vlg.childForceExpandHeight = false;
                }
            }

            // 3. Protruding Toggle Tab (Attached to left edge of dock)
            Transform existingTab = transform.Find("Dock_ToggleTab");
            if (existingTab != null)
            {
                toggleTabButton = existingTab.GetComponent<Button>();
                var arrowT = existingTab.Find("Arrow");
                if (arrowT != null) toggleArrowText = arrowT.GetComponent<TextMeshProUGUI>();
                var lblT = existingTab.Find("Label");
                if (lblT != null) toggleLabelText = lblT.GetComponent<TextMeshProUGUI>();
                var badgeT = existingTab.Find("TabAlertBadge");
                if (badgeT != null) tabAlertBadgeObj = badgeT.gameObject;
            }
            else
            {
                GameObject tabObj = new GameObject("Dock_ToggleTab");
                tabObj.transform.SetParent(transform, false);
                RectTransform tabRt = tabObj.AddComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0f, 0.5f);
                tabRt.anchorMax = new Vector2(0f, 0.5f);
                tabRt.pivot = new Vector2(1f, 0.5f);
                tabRt.anchoredPosition = new Vector2(-2f, 0f);
                tabRt.sizeDelta = new Vector2(46f, 105f);

                Image tabImg = tabObj.AddComponent<Image>();
                tabImg.sprite = cardSp;
                tabImg.type = Image.Type.Sliced;
                tabImg.color = new Color(0.08f, 0.12f, 0.20f, 0.94f);

                toggleTabButton = tabObj.AddComponent<Button>();
                CasualUITheme.StyleButton(toggleTabButton, tabImg, CasualBtnStyle.SquareNavy);
                toggleTabButton.onClick.AddListener(ToggleCollapse);

                // Tab Arrow
                GameObject arrowObj = new GameObject("Arrow");
                arrowObj.transform.SetParent(tabObj.transform, false);
                toggleArrowText = arrowObj.AddComponent<TextMeshProUGUI>();
                toggleArrowText.text = ">";
                toggleArrowText.fontSize = 26f;
                toggleArrowText.fontStyle = FontStyles.Bold;
                toggleArrowText.color = new Color(1f, 0.88f, 0.35f);
                toggleArrowText.alignment = TextAlignmentOptions.Center;
                toggleArrowText.raycastTarget = false;
                toggleArrowText.ApplyFont(UIFontCategory.Button);
                RectTransform aRt = arrowObj.GetComponent<RectTransform>();
                aRt.anchorMin = new Vector2(0f, 0.5f);
                aRt.anchorMax = new Vector2(1f, 1f);
                aRt.anchoredPosition = new Vector2(0f, -4f);
                aRt.sizeDelta = Vector2.zero;

                // Tab Label Text
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(tabObj.transform, false);
                toggleLabelText = labelObj.AddComponent<TextMeshProUGUI>();
                toggleLabelText.text = "HIDE";
                toggleLabelText.fontSize = 13f;
                toggleLabelText.fontStyle = FontStyles.Bold;
                toggleLabelText.color = Color.white;
                toggleLabelText.alignment = TextAlignmentOptions.Center;
                toggleLabelText.raycastTarget = false;
                toggleLabelText.ApplyFont(UIFontCategory.Button);
                RectTransform lRt = labelObj.GetComponent<RectTransform>();
                lRt.anchorMin = new Vector2(0f, 0f);
                lRt.anchorMax = new Vector2(1f, 0.5f);
                lRt.anchoredPosition = new Vector2(0f, 4f);
                lRt.sizeDelta = Vector2.zero;

                // Tab Alert Badge (Green dot)
                tabAlertBadgeObj = new GameObject("TabAlertBadge");
                tabAlertBadgeObj.transform.SetParent(tabObj.transform, false);
                RectTransform tbRt = tabAlertBadgeObj.AddComponent<RectTransform>();
                tbRt.anchorMin = new Vector2(0f, 1f);
                tbRt.anchorMax = new Vector2(0f, 1f);
                tbRt.pivot = new Vector2(0.5f, 0.5f);
                tbRt.anchoredPosition = new Vector2(4f, -4f);
                tbRt.sizeDelta = new Vector2(18f, 18f);

                Image tbImg = tabAlertBadgeObj.AddComponent<Image>();
                tbImg.sprite = cardSp;
                tbImg.color = new Color(0.18f, 0.85f, 0.40f, 1f);
                tbImg.raycastTarget = false;
            }
        }

        public static Transform GetOrCreateContainer(Transform canvasTransform)
        {
            if (Instance != null && Instance.ButtonContainer != null)
            {
                return Instance.ButtonContainer;
            }

            // Look in scene
            var existing = FindFirstObjectByType<RightActionDockUI>();
            if (existing != null)
            {
                Instance = existing;
                return existing.ButtonContainer;
            }

            // Create under canvas
            Transform parentCanvas = canvasTransform;
            if (parentCanvas == null)
            {
                var cv = GameObject.Find("UI_Canvas");
                if (cv != null) parentCanvas = cv.transform;
            }

            if (parentCanvas == null) return null;

            GameObject dockObj = new GameObject("RightActions_Dock");
            dockObj.transform.SetParent(parentCanvas, false);
            var dockScript = dockObj.AddComponent<RightActionDockUI>();
            Instance = dockScript;
            return dockScript.ButtonContainer;
        }
    }
}

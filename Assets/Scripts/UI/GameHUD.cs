using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;

namespace IdleFishing.UI
{
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Level & Land Display")]
        [SerializeField] private Button levelBadgeButton;
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI landNameText;
        [SerializeField] private GameObject nextLevelAlertBadge;
        [SerializeField] private TextMeshProUGUI nextLevelAlertText;

        private Coroutine punchRoutine;

        public RectTransform CoinsRect => coinsText != null ? coinsText.rectTransform : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (levelBadgeButton != null)
            {
                levelBadgeButton.onClick.AddListener(OnLevelBadgeClicked);
            }

            if (nextLevelAlertBadge != null)
            {
                Button alertBtn = nextLevelAlertBadge.GetComponent<Button>();
                if (alertBtn == null) alertBtn = nextLevelAlertBadge.AddComponent<Button>();
                alertBtn.onClick.AddListener(OnLevelBadgeClicked);
            }
        }

        private void OnEnable()
        {
            GameEvents.CoinsChanged += UpdateCoins;
            GameEvents.WorldChanged += OnWorldChanged;
            GameEvents.WorldUnlocked += OnWorldUnlocked;
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= UpdateCoins;
            GameEvents.WorldChanged -= OnWorldChanged;
            GameEvents.WorldUnlocked -= OnWorldUnlocked;
        }

        private void Start()
        {
            StyleCoinsBar();
            StyleHUDButtons();
            if (levelTitleText != null) levelTitleText.ApplyFont(UIFontCategory.Header);
            if (landNameText != null) landNameText.ApplyFont(UIFontCategory.Header);
            if (nextLevelAlertText != null)
            {
                nextLevelAlertText.fontSize = 18f;
                nextLevelAlertText.ApplyFont(UIFontCategory.Button);
            }

            var tapHintObj = GameObject.Find("PlayerTapHint");
            if (tapHintObj != null)
            {
                var tapTmp = tapHintObj.GetComponent<TextMeshProUGUI>();
                if (tapTmp != null)
                {
                    tapTmp.fontSize = 30f;
                    tapTmp.ApplyFont(UIFontCategory.Header);
                }
            }

            if (EconomyManager.Instance != null)
            {
                UpdateCoins(EconomyManager.Instance.Coins);
            }
            UpdateLevelDisplay();
        }

        private void StyleHUDButtons()
        {
            // 1. Contracts Board Button - Deactivated per user request (Streamlining Core Loop)
            var cBtnObj = GameObject.Find("Btn_ContractsBoard");
            if (cBtnObj != null)
            {
                cBtnObj.SetActive(false);
            }

            // 2. Right Actions Dock (Vertical Layout for Aquarium, Voyage, Audio with Toggle Tab)
            Transform dockContainer = RightActionDockUI.GetOrCreateContainer(transform.parent != null ? transform.parent : transform);

            var aqBtnObj = GameObject.Find("Btn_Aquarium");
            if (aqBtnObj == null)
            {
                aqBtnObj = new GameObject("Btn_Aquarium");
                var rt = aqBtnObj.AddComponent<RectTransform>();
                var btn = aqBtnObj.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    if (AquariumPopupUI.Instance != null)
                    {
                        AquariumPopupUI.Instance.Toggle();
                    }
                    if (IdleFishing.Audio.AudioManager.Instance != null)
                    {
                        IdleFishing.Audio.AudioManager.Instance.PlayUIClick();
                    }
                });
            }

            if (dockContainer != null && aqBtnObj.transform.parent != dockContainer)
            {
                aqBtnObj.transform.SetParent(dockContainer, false);
            }
            aqBtnObj.transform.SetSiblingIndex(0); // Top action in vertical dock

            RectTransform aqRt = aqBtnObj.GetComponent<RectTransform>();
            if (aqRt != null)
            {
                aqRt.anchorMin = new Vector2(0.5f, 1f);
                aqRt.anchorMax = new Vector2(0.5f, 1f);
                aqRt.pivot = new Vector2(0.5f, 1f);
                aqRt.sizeDelta = new Vector2(92f, 94f);
            }

            var aqLe = aqBtnObj.GetComponent<LayoutElement>();
            if (aqLe == null) aqLe = aqBtnObj.AddComponent<LayoutElement>();
            aqLe.preferredWidth = 92f;
            aqLe.preferredHeight = 94f;
            aqLe.minWidth = 92f;
            aqLe.minHeight = 94f;

            CasualUITheme.StyleTileButton(
                aqBtnObj,
                CasualUITheme.IconFish,
                "AQUARIUM",
                CasualBtnStyle.TileBlue,
                new Color(0.45f, 0.90f, 1f),
                new Vector2(92f, 94f),
                new Vector2(48f, 48f)
            );

            // 3. Level Badge Button (Center)
            if (levelBadgeButton != null)
            {
                RectTransform lbRt = levelBadgeButton.GetComponent<RectTransform>();
                if (lbRt != null)
                {
                    lbRt.sizeDelta = new Vector2(260f, 80f);
                }
                if (levelTitleText != null)
                {
                    levelTitleText.fontSize = 28f;
                    levelTitleText.fontStyle = FontStyles.Bold;
                    levelTitleText.color = new Color(0.35f, 0.88f, 1f);
                    levelTitleText.ApplyFont(UIFontCategory.Header);
                }
                if (landNameText != null)
                {
                    landNameText.fontSize = 22f;
                    landNameText.fontStyle = FontStyles.Bold;
                    landNameText.color = Color.white;
                    landNameText.ApplyFont(UIFontCategory.Header);
                }
            }
        }

        public static RectTransform GetOrCreateTopRightGroup(Transform mainCanvasOrTopHUD)
        {
            if (mainCanvasOrTopHUD == null) return null;
            if (mainCanvasOrTopHUD.name == "TopRight_ActionsGroup") return mainCanvasOrTopHUD as RectTransform;

            // Check upwards in hierarchy if already under TopRight_ActionsGroup or TopHUD
            Transform cur = mainCanvasOrTopHUD;
            Transform topHUD = null;
            while (cur != null)
            {
                if (cur.name == "TopRight_ActionsGroup") return cur as RectTransform;
                if (cur.name == "TopHUD") { topHUD = cur; break; }
                cur = cur.parent;
            }

            if (topHUD == null)
            {
                topHUD = mainCanvasOrTopHUD.Find("TopHUD");
            }
            Transform targetParent = topHUD != null ? topHUD : mainCanvasOrTopHUD;

            Transform existing = targetParent.Find("TopRight_ActionsGroup");
            if (existing != null) return existing as RectTransform;

            GameObject groupObj = new GameObject("TopRight_ActionsGroup");
            groupObj.transform.SetParent(targetParent, false);
            RectTransform rt = groupObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -10f);
            rt.sizeDelta = new Vector2(250f, 82f);

            var hlg = groupObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            return rt;
        }

        private void StyleCoinsBar()
        {
            if (coinsText == null) return;

            // 1. Text typography: Bold, large, radiant gold with dark outline
            coinsText.ApplyFont(UIFontCategory.Numbers);
            coinsText.fontSize = 38f;
            coinsText.fontStyle = FontStyles.Bold;
            coinsText.alignment = TextAlignmentOptions.MidlineLeft;
            coinsText.color = new Color(1f, 0.90f, 0.20f); // Bright Gold
            coinsText.raycastTarget = false;

            RectTransform textRt = coinsText.rectTransform;
            if (textRt != null)
            {
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(58f, 0f);
                textRt.offsetMax = new Vector2(-12f, 0f);
            }

            // 2. Bar Container
            Transform parent = coinsText.transform.parent;
            if (parent != null)
            {
                Image barImg = parent.GetComponent<Image>();
                if (barImg != null)
                {
                    if (CasualUITheme.ResourceBarBg != null)
                    {
                        barImg.sprite = CasualUITheme.ResourceBarBg;
                        barImg.type = Image.Type.Sliced;
                    }
                    // Deep Dark Slate Navy capsule: crisp high contrast against bright water & numbers
                    barImg.color = new Color(0.07f, 0.10f, 0.16f, 0.96f);
                }

                RectTransform barRt = parent.GetComponent<RectTransform>();
                if (barRt != null)
                {
                    barRt.sizeDelta = new Vector2(250f, 64f);
                }

                // 3. Icon
                Transform iconTrans = parent.Find("Icon_Gold");
                if (iconTrans != null)
                {
                    Image iconImg = iconTrans.GetComponent<Image>();
                    if (iconImg != null && CasualUITheme.IconGold != null)
                    {
                        iconImg.sprite = CasualUITheme.IconGold;
                        iconImg.color = Color.white;
                    }
                    RectTransform iconRt = iconTrans.GetComponent<RectTransform>();
                    if (iconRt != null)
                    {
                        iconRt.sizeDelta = new Vector2(64f, 64f);
                        iconRt.anchorMin = new Vector2(0f, 0.5f);
                        iconRt.anchorMax = new Vector2(0f, 0.5f);
                        iconRt.pivot = new Vector2(0.5f, 0.5f);
                        iconRt.anchoredPosition = new Vector2(10f, 0f);
                    }
                }
            }
        }

        public void SetupLevelBadge(
            Button badgeBtn,
            TextMeshProUGUI lvlText,
            TextMeshProUGUI landText,
            GameObject nextLvlBadge = null,
            TextMeshProUGUI nextLvlText = null)
        {
            levelBadgeButton = badgeBtn;
            levelTitleText = lvlText;
            landNameText = landText;
            nextLevelAlertBadge = nextLvlBadge;
            nextLevelAlertText = nextLvlText;

            if (levelTitleText != null) levelTitleText.ApplyFont(UIFontCategory.Header);
            if (landNameText != null) landNameText.ApplyFont(UIFontCategory.Header);
            if (nextLevelAlertText != null) nextLevelAlertText.ApplyFont(UIFontCategory.Button);

            if (levelBadgeButton != null)
            {
                levelBadgeButton.onClick.RemoveAllListeners();
                levelBadgeButton.onClick.AddListener(OnLevelBadgeClicked);
            }

            UpdateLevelDisplay();
        }

        private void OnLevelBadgeClicked()
        {
            if (IdleFishing.Audio.AudioManager.Instance != null)
            {
                IdleFishing.Audio.AudioManager.Instance.PlayUIClick();
            }

            // Nếu cấp độ tiếp theo đã đủ điều kiện mở khóa, mở ngay màn hình chúc mừng hoành tráng
            if (WorldManager.Instance != null)
            {
                var worlds = WorldManager.Instance.Worlds;
                int curIndex = WorldManager.Instance.ActiveWorldIndex;
                int nextIndex = curIndex + 1;

                if (worlds != null && nextIndex < worlds.Count)
                {
                    WorldData nextWorld = worlds[nextIndex];
                    if (!WorldManager.Instance.IsWorldUnlocked(nextWorld) && WorldManager.Instance.CanUnlockWorld(nextWorld, out _))
                    {
                        WorldUnlockCelebrationModal.Instance.Show(nextWorld, nextIndex);
                        return;
                    }
                }
            }

            if (WorldSelectionPopupUI.Instance != null)
            {
                WorldSelectionPopupUI.Instance.Open();
            }
        }

        private void OnWorldChanged(WorldData newWorld)
        {
            UpdateLevelDisplay();
        }

        private void OnWorldUnlocked(WorldData unlockedWorld)
        {
            UpdateLevelDisplay();
        }

        public void UpdateLevelDisplay()
        {
            WorldData curWorld = WorldManager.Instance != null ? WorldManager.Instance.CurrentWorld : null;
            if (curWorld != null)
            {
                if (levelTitleText != null)
                {
                    levelTitleText.text = $"LEVEL {curWorld.worldOrder}";
                }
                if (landNameText != null)
                {
                    landNameText.text = curWorld.worldName;
                }
            }

            CheckNextLevelReady();
        }

        public void CheckNextLevelReady()
        {
            if (nextLevelAlertBadge == null || WorldManager.Instance == null) return;

            var worlds = WorldManager.Instance.Worlds;
            int curIndex = WorldManager.Instance.ActiveWorldIndex;
            int nextIndex = curIndex + 1;

            bool canUnlockNext = false;
            if (nextIndex < worlds.Count)
            {
                WorldData nextWorld = worlds[nextIndex];
                if (!WorldManager.Instance.IsWorldUnlocked(nextWorld))
                {
                    canUnlockNext = WorldManager.Instance.CanUnlockWorld(nextWorld, out _);
                }
            }

            nextLevelAlertBadge.SetActive(canUnlockNext);
            if (nextLevelAlertText != null && canUnlockNext)
            {
                nextLevelAlertText.text = "LEVEL UP READY!";
            }
        }

        public void PunchCoinsCounter()
        {
            if (coinsText == null) return;
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(PunchCoinsRoutine());
        }

        private System.Collections.IEnumerator PunchCoinsRoutine()
        {
            Transform t = coinsText.transform;
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.28f;
                t.localScale = originalScale * scale;
                yield return null;
            }

            t.localScale = originalScale;
        }

        public void UpdateCoins(int coins)
        {
            if (coinsText != null)
            {
                coinsText.text = $"{NumberFormatter.Format(coins)} G";
                coinsText.color = new Color(1f, 0.90f, 0.20f);
            }
            CheckNextLevelReady();
        }
    }
}

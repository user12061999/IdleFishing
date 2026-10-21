using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Progression;
using IdleFishing.Audio;
using IdleFishing.VFX;

namespace IdleFishing.UI
{
    /// <summary>
    /// Juicy Stage Clear Celebration Modal (Hybrid Casual Tycoon):
    /// - Displays when all goals for a stage are fulfilled.
    /// - Features spring bounce animation, confetti VFX, gold coin rewards, and unlocked features.
    /// - Includes an idle auto-advance countdown (8 seconds) so AFK/idle players are never blocked.
    /// </summary>
    public class StageClearPopupUI : MonoBehaviour
    {
        private static StageClearPopupUI _instance;
        public static StageClearPopupUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<StageClearPopupUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("StageClearPopupUI");
                        _instance = go.AddComponent<StageClearPopupUI>();
                    }
                }
                return _instance;
            }
        }

        private GameObject modalRoot;
        private RectTransform dialogBoxRect;
        private CanvasGroup canvasGroup;

        // UI Text & Component References
        private TextMeshProUGUI stageSubtitleText;
        private TextMeshProUGUI goldRewardText;
        private TextMeshProUGUI fameRewardText;
        private GameObject unlocksContainerObj;
        private TextMeshProUGUI unlocksDescriptionText;
        private Button continueButton;
        private TextMeshProUGUI continueButtonText;

        private Action onContinueCallback;
        private Coroutine activePopRoutine;
        private Coroutine autoAdvanceRoutine;
        private bool isClosing = false;
        private float inputLockTimer = 0f;
        private const float INPUT_LOCK_DURATION = 1.25f; // Protect against rapid taps during combat

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            BuildProceduralModal();
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (inputLockTimer > 0f)
            {
                inputLockTimer -= Time.unscaledDeltaTime;
                if (continueButton != null)
                {
                    continueButton.interactable = (inputLockTimer <= 0f);
                }
            }
        }

        public void Show(StageData stage, bool isFirstClear, Action onContinue)
        {
            if (stage == null)
            {
                onContinue?.Invoke();
                return;
            }

            onContinueCallback = onContinue;
            isClosing = false;
            inputLockTimer = INPUT_LOCK_DURATION;

            if (modalRoot == null)
            {
                BuildProceduralModal();
            }

            modalRoot.SetActive(true);
            if (continueButton != null)
            {
                continueButton.interactable = false;
            }

            PopulateData(stage, isFirstClear);

            // Celebration VFX & Sound
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnConfetti(new Vector3(0f, 2.5f, 7.5f), 2.0f);
                VFXManager.Instance.SpawnGoldBurst(new Vector3(0f, 1.8f, 7.5f), 25);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayChestOpen();
                AudioManager.Instance.PlayCoin();
            }

            // Spring Bounce In Animation
            if (activePopRoutine != null) StopCoroutine(activePopRoutine);
            activePopRoutine = StartCoroutine(BounceInRoutine());

            // Idle Auto-Advance Routine (10 seconds)
            if (autoAdvanceRoutine != null) StopCoroutine(autoAdvanceRoutine);
            autoAdvanceRoutine = StartCoroutine(AutoAdvanceCountdownRoutine(10f));
        }

        private void PopulateData(StageData stage, bool isFirstClear)
        {
            if (stageSubtitleText != null)
            {
                stageSubtitleText.text = $"STAGE {stage.stageNumber}: {stage.stageTitle.ToUpper()}";
            }

            int gold = stage.CalculateGoldReward();
            if (goldRewardText != null)
            {
                goldRewardText.text = $"+{NumberFormatter.Format(gold)} GOLD";
            }

            int fame = stage.grantFameOnFirstClear;
            if (fameRewardText != null)
            {
                fameRewardText.text = (isFirstClear && fame > 0) ? $"+{fame} HARBOR FAME" : "STAGE MASTERED";
            }

            // Check unlocks
            bool hasUnlocks = stage.featureUnlocks != null && stage.featureUnlocks.Count > 0;
            if (unlocksContainerObj != null)
            {
                unlocksContainerObj.SetActive(hasUnlocks && isFirstClear);
            }

            if (unlocksDescriptionText != null && hasUnlocks && isFirstClear)
            {
                var formatted = new List<string>();
                for (int i = 0; i < stage.featureUnlocks.Count; i++)
                {
                    string feat = stage.featureUnlocks[i].Replace("_", " ").ToUpper();
                    formatted.Add($"• UNLOCKED: {feat}");
                }
                unlocksDescriptionText.text = string.Join("\n", formatted);
            }
        }

        private IEnumerator BounceInRoutine()
        {
            if (dialogBoxRect == null || canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            dialogBoxRect.localScale = Vector3.one * 0.65f;

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Overshoot spring curve
                float scale = 1.0f + Mathf.Sin(t * Mathf.PI) * 0.12f * (1.0f - t);
                dialogBoxRect.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.0f, t) * scale;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 2f);

                yield return null;
            }

            dialogBoxRect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
        }

        private IEnumerator AutoAdvanceCountdownRoutine(float totalSeconds)
        {
            float remaining = totalSeconds;
            while (remaining > 0f)
            {
                if (continueButtonText != null)
                {
                    if (inputLockTimer > 0f)
                    {
                        continueButtonText.text = "NEXT STAGE";
                    }
                    else
                    {
                        continueButtonText.text = $"NEXT STAGE ({Mathf.CeilToInt(remaining)}s)";
                    }
                }
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            OnContinueClicked();
        }

        public void OnContinueClicked()
        {
            // Ignore accidental taps from frenzy attacks while locked
            if (inputLockTimer > 0f) return;
            if (isClosing) return;
            isClosing = true;

            if (autoAdvanceRoutine != null) StopCoroutine(autoAdvanceRoutine);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }

            StartCoroutine(CloseAndAdvanceRoutine());
        }

        private IEnumerator CloseAndAdvanceRoutine()
        {
            if (canvasGroup != null && dialogBoxRect != null)
            {
                float elapsed = 0f;
                float duration = 0.2f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    canvasGroup.alpha = 1f - t;
                    dialogBoxRect.localScale = Vector3.one * Mathf.Lerp(1.0f, 0.85f, t);
                    yield return null;
                }
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            var callback = onContinueCallback;
            onContinueCallback = null;
            callback?.Invoke();
        }

        #region Procedural Modal Construction
        private void BuildProceduralModal()
        {
            if (modalRoot != null) return;

            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // Find or build dedicated high-sort canvas
            GameObject canvasObj = GameObject.Find("UI_Canvas_CelebrationOverlay");
            Canvas canvas;
            if (canvasObj == null)
            {
                canvasObj = new GameObject("UI_Canvas_CelebrationOverlay");
                DontDestroyOnLoad(canvasObj);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1800; // Above all gameplay and HUD

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }

            // 1. Modal Root (Fullscreen Overlay)
            modalRoot = new GameObject("StageClear_Modal_Root");
            modalRoot.transform.SetParent(canvas.transform, false);

            var rootRt = modalRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            var overlayBg = modalRoot.AddComponent<Image>();
            overlayBg.sprite = whiteSp;
            overlayBg.color = new Color(0.04f, 0.08f, 0.14f, 0.88f);
            overlayBg.raycastTarget = true; // Blocks clicks from reaching game, but does NOT advance modal!

            canvasGroup = modalRoot.AddComponent<CanvasGroup>();

            // 2. Main Dialog Box (Scaled up for clear mobile visibility)
            GameObject dialogObj = new GameObject("Dialog_Box");
            dialogObj.transform.SetParent(modalRoot.transform, false);
            dialogBoxRect = dialogObj.AddComponent<RectTransform>();
            dialogBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogBoxRect.pivot = new Vector2(0.5f, 0.5f);
            dialogBoxRect.anchoredPosition = new Vector2(0f, 10f);
            dialogBoxRect.sizeDelta = new Vector2(800f, 920f);

            var dialogBg = dialogObj.AddComponent<Image>();
            dialogBg.sprite = CasualUITheme.PopupNavyBg ?? CasualUITheme.CardRound12 ?? whiteSp;
            dialogBg.type = Image.Type.Sliced;
            dialogBg.color = new Color(0.08f, 0.14f, 0.24f, 0.98f);
            dialogBg.raycastTarget = true;

            // 3. Header Ribbon Banner
            GameObject ribbonObj = new GameObject("Header_Ribbon");
            ribbonObj.transform.SetParent(dialogObj.transform, false);
            var ribbonRt = ribbonObj.AddComponent<RectTransform>();
            ribbonRt.anchorMin = new Vector2(0.5f, 1f);
            ribbonRt.anchorMax = new Vector2(0.5f, 1f);
            ribbonRt.pivot = new Vector2(0.5f, 0.5f);
            ribbonRt.anchoredPosition = new Vector2(0f, 16f);
            ribbonRt.sizeDelta = new Vector2(640f, 96f);

            var ribbonImg = ribbonObj.AddComponent<Image>();
            ribbonImg.sprite = CasualUITheme.TitleRibbonYellow ?? CasualUITheme.CardRound12 ?? whiteSp;
            ribbonImg.type = Image.Type.Sliced;
            ribbonImg.color = Color.white;
            ribbonImg.raycastTarget = false;

            GameObject rTxtObj = new GameObject("Ribbon_Text");
            rTxtObj.transform.SetParent(ribbonObj.transform, false);
            var rTxtRt = rTxtObj.AddComponent<RectTransform>();
            rTxtRt.anchorMin = Vector2.zero;
            rTxtRt.anchorMax = Vector2.one;
            rTxtRt.sizeDelta = Vector2.zero;

            var ribbonText = rTxtObj.AddComponent<TextMeshProUGUI>();
            ribbonText.text = "STAGE COMPLETED!";
            ribbonText.fontSize = 38f;
            ribbonText.fontStyle = FontStyles.Bold;
            ribbonText.alignment = TextAlignmentOptions.Center;
            ribbonText.color = new Color(0.18f, 0.10f, 0.02f);
            ribbonText.raycastTarget = false;
            ribbonText.ApplyFont(UIFontCategory.Header);

            // 4. Stage Subtitle
            GameObject subObj = new GameObject("Stage_Subtitle");
            subObj.transform.SetParent(dialogObj.transform, false);
            var subRt = subObj.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 1f);
            subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -80f);
            subRt.sizeDelta = new Vector2(720f, 44f);

            stageSubtitleText = subObj.AddComponent<TextMeshProUGUI>();
            stageSubtitleText.text = "STAGE 1: FIRST CATCH";
            stageSubtitleText.fontSize = 26f;
            stageSubtitleText.fontStyle = FontStyles.Bold;
            stageSubtitleText.alignment = TextAlignmentOptions.Center;
            stageSubtitleText.color = new Color(1f, 0.88f, 0.25f);
            stageSubtitleText.raycastTarget = false;
            stageSubtitleText.ApplyFont(UIFontCategory.Header);

            // 5. Crown / Success Badge Icon
            GameObject iconObj = new GameObject("Success_Icon");
            iconObj.transform.SetParent(dialogObj.transform, false);
            var iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0f, -170f);
            iconRt.sizeDelta = new Vector2(110f, 110f);

            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = CasualUITheme.IconCrown ?? CasualUITheme.IconCheck ?? whiteSp;
            iconImg.preserveAspect = true;
            iconImg.color = new Color(1f, 0.85f, 0.20f, 1f);
            iconImg.raycastTarget = false;

            // 6. Rewards Slate Card
            GameObject slateObj = new GameObject("Rewards_Slate");
            slateObj.transform.SetParent(dialogObj.transform, false);
            var slateRt = slateObj.AddComponent<RectTransform>();
            slateRt.anchorMin = new Vector2(0.5f, 0.5f);
            slateRt.anchorMax = new Vector2(0.5f, 0.5f);
            slateRt.pivot = new Vector2(0.5f, 0.5f);
            slateRt.anchoredPosition = new Vector2(0f, -10f);
            slateRt.sizeDelta = new Vector2(720f, 210f);

            var slateBg = slateObj.AddComponent<Image>();
            slateBg.sprite = CasualUITheme.ItemFrameNavy ?? CasualUITheme.CardRound12 ?? whiteSp;
            slateBg.type = Image.Type.Sliced;
            slateBg.color = new Color(0.04f, 0.08f, 0.16f, 0.95f);
            slateBg.raycastTarget = false;

            // Slate Title
            GameObject slateTitleObj = new GameObject("Slate_Title");
            slateTitleObj.transform.SetParent(slateObj.transform, false);
            var stRt = slateTitleObj.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 1f);
            stRt.anchorMax = new Vector2(1f, 1f);
            stRt.pivot = new Vector2(0.5f, 1f);
            stRt.anchoredPosition = new Vector2(0f, -14f);
            stRt.sizeDelta = new Vector2(0f, 32f);

            var stText = slateTitleObj.AddComponent<TextMeshProUGUI>();
            stText.text = "REWARDS RECEIVED";
            stText.fontSize = 20f;
            stText.fontStyle = FontStyles.Bold;
            stText.alignment = TextAlignmentOptions.Center;
            stText.color = new Color(0.60f, 0.75f, 0.95f);
            stText.raycastTarget = false;
            stText.ApplyFont(UIFontCategory.Button);

            // Gold Reward Row
            GameObject goldRowObj = new GameObject("Gold_Row");
            goldRowObj.transform.SetParent(slateObj.transform, false);
            var grRt = goldRowObj.AddComponent<RectTransform>();
            grRt.anchorMin = new Vector2(0.05f, 0.5f);
            grRt.anchorMax = new Vector2(0.48f, 0.5f);
            grRt.pivot = new Vector2(0.5f, 0.5f);
            grRt.anchoredPosition = new Vector2(0f, -16f);
            grRt.sizeDelta = new Vector2(0f, 68f);

            var goldBg = goldRowObj.AddComponent<Image>();
            goldBg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            goldBg.type = Image.Type.Sliced;
            goldBg.color = new Color(0.12f, 0.20f, 0.32f, 0.9f);
            goldBg.raycastTarget = false;

            GameObject gTxtObj = new GameObject("Gold_Text");
            gTxtObj.transform.SetParent(goldRowObj.transform, false);
            var gtRt = gTxtObj.AddComponent<RectTransform>();
            gtRt.anchorMin = Vector2.zero;
            gtRt.anchorMax = Vector2.one;
            gtRt.sizeDelta = Vector2.zero;

            goldRewardText = gTxtObj.AddComponent<TextMeshProUGUI>();
            goldRewardText.text = "<color=#FFD700>+42 GOLD</color>";
            goldRewardText.fontSize = 28f;
            goldRewardText.fontStyle = FontStyles.Bold;
            goldRewardText.alignment = TextAlignmentOptions.Center;
            goldRewardText.color = Color.white;
            goldRewardText.raycastTarget = false;
            goldRewardText.ApplyFont(UIFontCategory.Numbers);

            // Fame Reward Row
            GameObject fameRowObj = new GameObject("Fame_Row");
            fameRowObj.transform.SetParent(slateObj.transform, false);
            var frRt = fameRowObj.AddComponent<RectTransform>();
            frRt.anchorMin = new Vector2(0.52f, 0.5f);
            frRt.anchorMax = new Vector2(0.95f, 0.5f);
            frRt.pivot = new Vector2(0.5f, 0.5f);
            frRt.anchoredPosition = new Vector2(0f, -16f);
            frRt.sizeDelta = new Vector2(0f, 68f);

            var fameBg = fameRowObj.AddComponent<Image>();
            fameBg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            fameBg.type = Image.Type.Sliced;
            fameBg.color = new Color(0.12f, 0.20f, 0.32f, 0.9f);
            fameBg.raycastTarget = false;

            GameObject fTxtObj = new GameObject("Fame_Text");
            fTxtObj.transform.SetParent(fameRowObj.transform, false);
            var ftRt = fTxtObj.AddComponent<RectTransform>();
            ftRt.anchorMin = Vector2.zero;
            ftRt.anchorMax = Vector2.one;
            ftRt.sizeDelta = Vector2.zero;

            fameRewardText = fTxtObj.AddComponent<TextMeshProUGUI>();
            fameRewardText.text = "<color=#00E5FF>+1 FAME</color>";
            fameRewardText.fontSize = 26f;
            fameRewardText.fontStyle = FontStyles.Bold;
            fameRewardText.alignment = TextAlignmentOptions.Center;
            fameRewardText.color = Color.white;
            fameRewardText.raycastTarget = false;
            fameRewardText.ApplyFont(UIFontCategory.Numbers);

            // 7. Unlocks Container
            unlocksContainerObj = new GameObject("Unlocks_Container");
            unlocksContainerObj.transform.SetParent(dialogObj.transform, false);
            var uRt = unlocksContainerObj.AddComponent<RectTransform>();
            uRt.anchorMin = new Vector2(0.5f, 0f);
            uRt.anchorMax = new Vector2(0.5f, 0f);
            uRt.pivot = new Vector2(0.5f, 0f);
            uRt.anchoredPosition = new Vector2(0f, 145f);
            uRt.sizeDelta = new Vector2(720f, 84f);

            var uBg = unlocksContainerObj.AddComponent<Image>();
            uBg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            uBg.type = Image.Type.Sliced;
            uBg.color = new Color(0.10f, 0.25f, 0.20f, 0.92f); // Gentle emerald tint
            uBg.raycastTarget = false;

            GameObject uTxtObj = new GameObject("Unlocks_Text");
            uTxtObj.transform.SetParent(unlocksContainerObj.transform, false);
            var utRt = uTxtObj.AddComponent<RectTransform>();
            utRt.anchorMin = Vector2.zero;
            utRt.anchorMax = Vector2.one;
            utRt.sizeDelta = new Vector2(-24f, -8f);

            unlocksDescriptionText = uTxtObj.AddComponent<TextMeshProUGUI>();
            unlocksDescriptionText.text = "• UNLOCKED: NEW WORKER!";
            unlocksDescriptionText.fontSize = 20f;
            unlocksDescriptionText.fontStyle = FontStyles.Bold;
            unlocksDescriptionText.alignment = TextAlignmentOptions.Center;
            unlocksDescriptionText.color = new Color(0.40f, 1.0f, 0.65f);
            unlocksDescriptionText.raycastTarget = false;
            unlocksDescriptionText.ApplyFont(UIFontCategory.Button);

            // 8. Next Stage Action Button
            GameObject btnObj = new GameObject("Btn_Continue");
            btnObj.transform.SetParent(dialogObj.transform, false);
            var btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 32f);
            btnRt.sizeDelta = new Vector2(560f, 96f);

            var btnImg = btnObj.AddComponent<Image>();
            continueButton = btnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(continueButton, btnImg, CasualBtnStyle.Green);
            continueButton.onClick.AddListener(OnContinueClicked);

            GameObject bTxtObj = new GameObject("Button_Text");
            bTxtObj.transform.SetParent(btnObj.transform, false);
            var btRt = bTxtObj.AddComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            continueButtonText = bTxtObj.AddComponent<TextMeshProUGUI>();
            continueButtonText.text = "NEXT STAGE";
            continueButtonText.fontSize = 32f;
            continueButtonText.fontStyle = FontStyles.Bold;
            continueButtonText.alignment = TextAlignmentOptions.Center;
            continueButtonText.color = Color.white;
            continueButtonText.raycastTarget = false;
            continueButtonText.ApplyFont(UIFontCategory.Button);
        }
        #endregion
    }
}

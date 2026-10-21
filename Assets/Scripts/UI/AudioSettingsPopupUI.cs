using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Audio;
using IdleFishing.Data;

namespace IdleFishing.UI
{
    /// <summary>
    /// Popup Cài Đặt Âm Lượng & Âm Thanh (Audio Settings Popup UI):
    /// - Cho phép người chơi tinh chỉnh âm lượng 3 kênh riêng biệt:
    ///   • BGM (Nhạc Nền Đảo)
    ///   • SFX (Hiệu Ứng Thao Tác, Câu Cá, Chặt Cá, Thu Vàng)
    ///   • AMBIENCE (Môi Trường Biển, Sóng Vỗ, Gió Bão, Đêm Khuya)
    /// - Nút MUTE ALL chuyển đổi nhanh trạng thái im lặng.
    /// - Hiển thị bản nhạc & chủ đề âm thanh đang phát theo Thế giới hiện tại.
    /// - Tích hợp nút icon bánh răng/âm thanh [♫] trên HUD để người chơi mở bảng bất kỳ lúc nào.
    /// </summary>
    public class AudioSettingsPopupUI : MonoBehaviour
    {
        private static AudioSettingsPopupUI _instance;
        public static AudioSettingsPopupUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<AudioSettingsPopupUI>();
                    if (_instance == null)
                    {
                        var canvas = FindAnyObjectByType<Canvas>();
                        GameObject go = new GameObject("--- AUDIO SETTINGS POPUP ---");
                        if (canvas != null) go.transform.SetParent(canvas.transform, false);
                        _instance = go.AddComponent<AudioSettingsPopupUI>();
                    }
                }
                return _instance;
            }
        }

        private GameObject popupRoot;
        private RectTransform dialogBoxRect;
        private CanvasGroup canvasGroup;
        private GameObject hudNavButtonObj;

        // Sliders & Text
        private Slider bgmSlider;
        private TextMeshProUGUI bgmValueText;

        private Slider sfxSlider;
        private TextMeshProUGUI sfxValueText;

        private Slider ambientSlider;
        private TextMeshProUGUI ambientValueText;

        // Mute Toggle
        private Button muteButton;
        private Image muteButtonImage;
        private TextMeshProUGUI muteButtonText;

        // Info
        private TextMeshProUGUI islandTrackInfoText;

        private Coroutine bounceRoutine;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            BuildHUDNavButton();
            BuildProceduralModal();

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void Start()
        {
            if (hudNavButtonObj == null) BuildHUDNavButton();
            SyncUIWithAudioManager();
        }

        public void Open()
        {
            if (popupRoot == null)
            {
                BuildProceduralModal();
            }

            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();
            SyncUIWithAudioManager();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }

            if (bounceRoutine != null) StopCoroutine(bounceRoutine);
            bounceRoutine = StartCoroutine(BounceInRoutine());
        }

        public void Close()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }
        }

        private void SyncUIWithAudioManager()
        {
            if (AudioManager.Instance == null) return;

            float bgm = AudioManager.Instance.BGMVolume;
            float sfx = AudioManager.Instance.SFXVolume;
            float amb = AudioManager.Instance.AmbientVolume;
            bool muted = AudioManager.Instance.IsMuted;

            if (bgmSlider != null)
            {
                bgmSlider.SetValueWithoutNotify(bgm);
                if (bgmValueText != null) bgmValueText.text = $"{Mathf.RoundToInt(bgm * 100f)}%";
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(sfx);
                if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(sfx * 100f)}%";
            }

            if (ambientSlider != null)
            {
                ambientSlider.SetValueWithoutNotify(amb);
                if (ambientValueText != null) ambientValueText.text = $"{Mathf.RoundToInt(amb * 100f)}%";
            }

            UpdateMuteVisuals(muted);
            UpdateIslandTrackInfo();
        }

        private void UpdateMuteVisuals(bool isMuted)
        {
            if (muteButtonImage != null)
            {
                muteButtonImage.sprite = isMuted ? (CasualUITheme.BtnRed ?? muteButtonImage.sprite) : (CasualUITheme.BtnGreen ?? muteButtonImage.sprite);
                muteButtonImage.color = Color.white;
            }

            if (muteButtonText != null)
            {
                muteButtonText.text = isMuted ? "[MUTED] SOUND OFF" : "[SOUND ON] ACTIVE";
            }
        }

        private void UpdateIslandTrackInfo()
        {
            if (islandTrackInfoText == null) return;

            int worldOrder = 1;
            string worldName = "Temperate Bay";
            if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
            {
                worldOrder = WorldManager.Instance.CurrentWorld.worldOrder;
                worldName = WorldManager.Instance.CurrentWorld.worldName;
            }

            string bgmName = "BGM_TemperateBay_Loop";
            if (AudioManager.Instance != null)
            {
                var clip = AudioManager.Instance.GetBgmForWorld(worldOrder);
                if (clip != null) bgmName = clip.name;
            }

            islandTrackInfoText.text = $"NOW PLAYING • LEVEL {worldOrder}: {worldName.ToUpper()}\n<color=#FCD34D>{bgmName}</color>";
        }

        private IEnumerator BounceInRoutine()
        {
            if (dialogBoxRect == null || canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            dialogBoxRect.localScale = Vector3.one * 0.75f;

            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float scaleT = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

                dialogBoxRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.75f, 1f, scaleT);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 1.5f);
                yield return null;
            }

            dialogBoxRect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
        }

        #region HUD Nav Button
        private void BuildHUDNavButton()
        {
            if (hudNavButtonObj != null) return;

            // Find UI_Canvas
            Canvas mainCanvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (mainCanvas == null && GameHUD.Instance != null) mainCanvas = GameHUD.Instance.GetComponentInParent<Canvas>();
            if (mainCanvas == null)
            {
                var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i].name.Contains("UI_Canvas"))
                    {
                        mainCanvas = canvases[i];
                        break;
                    }
                }
            }

            if (mainCanvas == null) return;

            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            Transform dockContainer = RightActionDockUI.GetOrCreateContainer(mainCanvas.transform);
            Transform finalParent = dockContainer != null ? dockContainer : mainCanvas.transform;

            hudNavButtonObj = new GameObject("Btn_AudioSettings");
            hudNavButtonObj.transform.SetParent(finalParent, false);
            hudNavButtonObj.transform.SetSiblingIndex(2); // Bottom action in vertical dock

            RectTransform rt = hudNavButtonObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(92f, 94f);

            var le = hudNavButtonObj.AddComponent<LayoutElement>();
            le.preferredWidth = 92f;
            le.preferredHeight = 94f;
            le.minWidth = 92f;
            le.minHeight = 94f;

            Button btn = hudNavButtonObj.AddComponent<Button>();
            btn.onClick.AddListener(Open);

            CasualUITheme.StyleTileButton(
                hudNavButtonObj,
                CasualUITheme.IconSound,
                "AUDIO",
                CasualBtnStyle.TileBlue,
                new Color(0.95f, 0.95f, 1f),
                new Vector2(92f, 94f),
                new Vector2(48f, 48f)
            );
        }
        #endregion

        #region Modal Construction
        private void BuildProceduralModal()
        {
            if (popupRoot != null) return;

            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Root Canvas - Overlay above all other UI
            popupRoot = new GameObject("AudioSettings_PopupRoot");
            popupRoot.transform.SetParent(transform, false);

            RectTransform rootRt = popupRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Canvas canvas = popupRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500; // Above standard HUD and menus

            popupRoot.AddComponent<GraphicRaycaster>();
            canvasGroup = popupRoot.AddComponent<CanvasGroup>();

            // 2. Dimmed Dark Backdrop
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(popupRoot.transform, false);
            RectTransform bdRt = backdrop.AddComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.sprite = CasualUITheme.DimmedBg ?? whiteSp;
            bdImg.color = CasualUITheme.DimmedBg != null ? Color.white : new Color(0.02f, 0.05f, 0.10f, 0.88f);

            Button bdBtn = backdrop.AddComponent<Button>();
            bdBtn.transition = Selectable.Transition.None;
            bdBtn.onClick.AddListener(Close);

            // 3. Center Dialog Box
            GameObject dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(popupRoot.transform, false);
            dialogBoxRect = dialogBox.AddComponent<RectTransform>();
            dialogBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogBoxRect.pivot = new Vector2(0.5f, 0.5f);
            dialogBoxRect.sizeDelta = new Vector2(780f, 960f);

            // Outer Card Border
            Image dlgBorderImg = dialogBox.AddComponent<Image>();
            dlgBorderImg.sprite = CasualUITheme.CardRound20 ?? whiteSp;
            dlgBorderImg.type = Image.Type.Sliced;
            dlgBorderImg.color = new Color(0.95f, 0.75f, 0.20f, 0.85f); // Gold Amber Border

            // Inner Card Body
            GameObject cardBody = new GameObject("CardBody");
            cardBody.transform.SetParent(dialogBox.transform, false);
            RectTransform cbRt = cardBody.AddComponent<RectTransform>();
            cbRt.anchorMin = Vector2.zero;
            cbRt.anchorMax = Vector2.one;
            cbRt.offsetMin = new Vector2(4f, 4f);
            cbRt.offsetMax = new Vector2(-4f, -4f);

            Image cbImg = cardBody.AddComponent<Image>();
            cbImg.sprite = CasualUITheme.PopupNavyBg ?? CasualUITheme.CardRound20 ?? whiteSp;
            cbImg.type = Image.Type.Sliced;
            cbImg.color = new Color(0.06f, 0.10f, 0.18f, 0.98f); // Deep Slate Navy

            // 4. Header Ribbon Banner
            GameObject ribbonObj = new GameObject("HeaderRibbon");
            ribbonObj.transform.SetParent(dialogBox.transform, false);
            RectTransform rbRt = ribbonObj.AddComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.5f, 1f);
            rbRt.anchorMax = new Vector2(0.5f, 1f);
            rbRt.pivot = new Vector2(0.5f, 0.5f);
            rbRt.anchoredPosition = new Vector2(0f, 16f);
            rbRt.sizeDelta = new Vector2(560f, 90f);

            Image rbImg = ribbonObj.AddComponent<Image>();
            rbImg.sprite = CasualUITheme.TitleRibbonYellow ?? CasualUITheme.CardRound12 ?? whiteSp;
            rbImg.type = Image.Type.Sliced;
            rbImg.color = Color.white;

            GameObject titleTextObj = new GameObject("Text_Title");
            titleTextObj.transform.SetParent(ribbonObj.transform, false);
            var titleTxt = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "AUDIO & SETTINGS";
            titleTxt.fontSize = 28f;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(0.20f, 0.12f, 0.02f);
            titleTxt.ApplyFont(UIFontCategory.Header);
            RectTransform ttRt = titleTextObj.GetComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.sizeDelta = Vector2.zero;

            // 5. Close Button (X)
            GameObject closeBtnObj = new GameObject("Button_Close");
            closeBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-20f, -20f);
            closeRt.sizeDelta = new Vector2(56f, 56f);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.sprite = CasualUITheme.IconClose ?? CasualUITheme.BtnRed ?? whiteSp;
            closeImg.color = Color.white;

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(Close);

            // 6. Sliders Group Container (Centered)
            float startY = -135f;
            float spacingY = 135f;

            // Slider 1: BGM
            bgmSlider = CreateAudioSliderRow(dialogBox.transform, "Row_BGM", startY, "[MUSIC] BACKGROUND MUSIC", out bgmValueText);
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);

            // Slider 2: SFX
            sfxSlider = CreateAudioSliderRow(dialogBox.transform, "Row_SFX", startY - spacingY, "[SFX] SOUND EFFECTS", out sfxValueText);
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);

            // Slider 3: Ambience
            ambientSlider = CreateAudioSliderRow(dialogBox.transform, "Row_Ambient", startY - (spacingY * 2), "[AMBIENT] OCEAN & ISLAND AMBIENCE", out ambientValueText);
            ambientSlider.onValueChanged.AddListener(OnAmbientSliderChanged);

            // 7. Mute All Toggle Button
            GameObject muteBtnObj = new GameObject("Button_MuteToggle");
            muteBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform mtRt = muteBtnObj.AddComponent<RectTransform>();
            mtRt.anchorMin = new Vector2(0.5f, 0f);
            mtRt.anchorMax = new Vector2(0.5f, 0f);
            mtRt.pivot = new Vector2(0.5f, 0f);
            mtRt.anchoredPosition = new Vector2(0f, 220f);
            mtRt.sizeDelta = new Vector2(480f, 74f);

            muteButtonImage = muteBtnObj.AddComponent<Image>();
            muteButtonImage.sprite = CasualUITheme.BtnGreen ?? whiteSp;
            muteButtonImage.type = Image.Type.Sliced;
            muteButtonImage.color = Color.white;

            muteButton = muteBtnObj.AddComponent<Button>();
            muteButton.targetGraphic = muteButtonImage;
            muteButton.onClick.AddListener(OnMuteToggleClicked);

            GameObject mtTxtObj = new GameObject("Text");
            mtTxtObj.transform.SetParent(muteBtnObj.transform, false);
            muteButtonText = mtTxtObj.AddComponent<TextMeshProUGUI>();
            muteButtonText.text = "[SOUND ON] ACTIVE";
            muteButtonText.fontSize = 22f;
            muteButtonText.fontStyle = FontStyles.Bold;
            muteButtonText.alignment = TextAlignmentOptions.Center;
            muteButtonText.color = Color.white;
            muteButtonText.ApplyFont(UIFontCategory.Button);
            RectTransform mttRt = mtTxtObj.GetComponent<RectTransform>();
            mttRt.anchorMin = Vector2.zero;
            mttRt.anchorMax = Vector2.one;
            mttRt.sizeDelta = Vector2.zero;

            // 8. Now Playing Island Info Box
            GameObject infoBox = new GameObject("InfoBox_NowPlaying");
            infoBox.transform.SetParent(dialogBox.transform, false);
            RectTransform ifRt = infoBox.AddComponent<RectTransform>();
            ifRt.anchorMin = new Vector2(0.5f, 0f);
            ifRt.anchorMax = new Vector2(0.5f, 0f);
            ifRt.pivot = new Vector2(0.5f, 0f);
            ifRt.anchoredPosition = new Vector2(0f, 85f);
            ifRt.sizeDelta = new Vector2(680f, 105f);

            Image ifImg = infoBox.AddComponent<Image>();
            ifImg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            ifImg.type = Image.Type.Sliced;
            ifImg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);

            GameObject infoTxtObj = new GameObject("Text");
            infoTxtObj.transform.SetParent(infoBox.transform, false);
            islandTrackInfoText = infoTxtObj.AddComponent<TextMeshProUGUI>();
            islandTrackInfoText.text = "NOW PLAYING • LEVEL 1: TEMPERATE BAY\n<color=#FCD34D>BGM_TemperateBay_Loop</color>";
            islandTrackInfoText.fontSize = 22f;
            islandTrackInfoText.fontStyle = FontStyles.Normal;
            islandTrackInfoText.alignment = TextAlignmentOptions.Center;
            islandTrackInfoText.color = new Color(0.75f, 0.85f, 0.95f);
            islandTrackInfoText.ApplyFont(UIFontCategory.Header);
            RectTransform iftRt = infoTxtObj.GetComponent<RectTransform>();
            iftRt.anchorMin = Vector2.zero;
            iftRt.anchorMax = Vector2.one;
            iftRt.sizeDelta = Vector2.zero;

            // 9. Footer Version
            GameObject verObj = new GameObject("Text_Version");
            verObj.transform.SetParent(dialogBox.transform, false);
            var verTxt = verObj.AddComponent<TextMeshProUGUI>();
            verTxt.text = "Idle Fishing Arcade • v1.2.0 • Casual Audio Engine";
            verTxt.fontSize = 18f;
            verTxt.fontStyle = FontStyles.Normal;
            verTxt.alignment = TextAlignmentOptions.Center;
            verTxt.color = new Color(0.45f, 0.55f, 0.70f);
            verTxt.ApplyFont(UIFontCategory.Default);
            RectTransform vtRt = verObj.GetComponent<RectTransform>();
            vtRt.anchorMin = new Vector2(0.5f, 0f);
            vtRt.anchorMax = new Vector2(0.5f, 0f);
            vtRt.pivot = new Vector2(0.5f, 0f);
            vtRt.anchoredPosition = new Vector2(0f, 26f);
            vtRt.sizeDelta = new Vector2(500f, 30f);
        }

        private Slider CreateAudioSliderRow(
            Transform parent,
            string rowName,
            float anchoredY,
            string label,
            out TextMeshProUGUI valueTextOut)
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            GameObject row = new GameObject(rowName);
            row.transform.SetParent(parent, false);
            RectTransform rowRt = row.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 1f);
            rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, anchoredY);
            rowRt.sizeDelta = new Vector2(660f, 105f);

            // Label Text
            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(row.transform, false);
            var lblTxt = lblObj.AddComponent<TextMeshProUGUI>();
            lblTxt.text = label;
            lblTxt.fontSize = 20f;
            lblTxt.fontStyle = FontStyles.Bold;
            lblTxt.color = new Color(0.92f, 0.95f, 1f);
            lblTxt.alignment = TextAlignmentOptions.Left;
            lblTxt.ApplyFont(UIFontCategory.Header);
            RectTransform lblRt = lblObj.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 1f);
            lblRt.anchorMax = new Vector2(0f, 1f);
            lblRt.pivot = new Vector2(0f, 1f);
            lblRt.anchoredPosition = new Vector2(10f, 0f);
            lblRt.sizeDelta = new Vector2(460f, 32f);

            // Percentage Value Text
            GameObject valObj = new GameObject("ValueText");
            valObj.transform.SetParent(row.transform, false);
            valueTextOut = valObj.AddComponent<TextMeshProUGUI>();
            valueTextOut.text = "100%";
            valueTextOut.fontSize = 22f;
            valueTextOut.fontStyle = FontStyles.Bold;
            valueTextOut.color = new Color(1f, 0.85f, 0.25f); // Amber Gold
            valueTextOut.alignment = TextAlignmentOptions.Right;
            valueTextOut.ApplyFont(UIFontCategory.Numbers);
            RectTransform valRt = valObj.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(1f, 1f);
            valRt.anchorMax = new Vector2(1f, 1f);
            valRt.pivot = new Vector2(1f, 1f);
            valRt.anchoredPosition = new Vector2(-10f, 0f);
            valRt.sizeDelta = new Vector2(140f, 32f);

            // Slider Component Root
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);
            RectTransform sRt = sliderObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0f);
            sRt.anchorMax = new Vector2(0.5f, 0f);
            sRt.pivot = new Vector2(0.5f, 0f);
            sRt.anchoredPosition = new Vector2(0f, 8f);
            sRt.sizeDelta = new Vector2(640f, 44f);

            // Slider Background Image
            Image sBgImg = sliderObj.AddComponent<Image>();
            sBgImg.sprite = CasualUITheme.SliderBg ?? whiteSp;
            sBgImg.type = Image.Type.Sliced;
            sBgImg.color = new Color(0.04f, 0.08f, 0.14f, 0.95f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = new Vector2(6f, 6f);
            faRt.offsetMax = new Vector2(-6f, -6f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRt = fill.AddComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;

            Image fImg = fill.AddComponent<Image>();
            fImg.sprite = CasualUITheme.SliderFillYellow ?? CasualUITheme.SliderFillGreen ?? whiteSp;
            fImg.type = Image.Type.Sliced;
            fImg.color = new Color(0.20f, 0.85f, 0.45f); // Vibrant Emerald Green

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fRt;
            slider.targetGraphic = fImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            return slider;
        }
        #endregion

        #region Callbacks
        private void OnBgmSliderChanged(float val)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(val);
            }
            if (bgmValueText != null)
            {
                bgmValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void OnSfxSliderChanged(float val)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(val);
                AudioManager.Instance.PlayUIClick();
            }
            if (sfxValueText != null)
            {
                sfxValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void OnAmbientSliderChanged(float val)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetAmbientVolume(val);
            }
            if (ambientValueText != null)
            {
                ambientValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void OnMuteToggleClicked()
        {
            if (AudioManager.Instance == null) return;

            bool newMuted = !AudioManager.Instance.IsMuted;
            AudioManager.Instance.SetMuted(newMuted);

            UpdateMuteVisuals(newMuted);

            if (!newMuted)
            {
                AudioManager.Instance.PlayUIClick();
            }
        }
        #endregion
    }
}

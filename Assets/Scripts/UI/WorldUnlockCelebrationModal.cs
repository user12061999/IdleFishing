using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Audio;
using IdleFishing.VFX;

namespace IdleFishing.UI
{
    /// <summary>
    /// Màn hình Chúc Mừng & Chuyển Đảo Hoành Tráng (World Unlock Celebration Modal):
    /// - Tự động hiển thị khi người chơi tích đủ tiền mở khóa Bến Cảng mới hoặc khi bấm Mở Khóa trên tàu/menu.
    /// - Pháo hoa giấy Confetti bùng nổ, âm thanh còi tàu & kèn trumpet ăn mừng.
    /// - Spotlight giới thiệu Cơ Chế Độc Quyền (Signature Gimmick) & Các Loài Cá Quý của Thế Giới mới.
    /// - Nút "NHỔ NEO RA KHƠI NGAY! ⛵" khởi động chuỗi chuyển cảnh Thuyền Ngoài Khơi rẽ sóng & mây tụ ngoạn mục.
    /// </summary>
    public class WorldUnlockCelebrationModal : MonoBehaviour
    {
        private static WorldUnlockCelebrationModal _instance;
        public static WorldUnlockCelebrationModal Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<WorldUnlockCelebrationModal>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("--- WORLD UNLOCK CELEBRATION MODAL ---");
                        _instance = go.AddComponent<WorldUnlockCelebrationModal>();
                    }
                }
                return _instance;
            }
        }

        private GameObject modalRoot;
        private RectTransform dialogBoxRect;
        private CanvasGroup canvasGroup;

        // UI Text & Component References
        private TextMeshProUGUI ribbonTitleText;
        private TextMeshProUGUI worldOrderBadgeText;
        private TextMeshProUGUI worldNameText;
        private TextMeshProUGUI worldDescText;

        // Signature Gimmick Spotlight Box
        private TextMeshProUGUI gimmickBadgeText;
        private TextMeshProUGUI gimmickTitleText;
        private TextMeshProUGUI gimmickDescText;

        // Native Fish Showcase Box
        private TextMeshProUGUI fishListText;

        // Unlock Status / Cost
        private TextMeshProUGUI statusCostText;

        // Action Buttons
        private Button setSailButton;
        private TextMeshProUGUI setSailButtonText;
        private Image setSailButtonImg;
        private Button stayButton;

        private WorldData pendingWorld;
        private int pendingWorldIndex = -1;
        private Coroutine activePopRoutine;

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

        private void OnEnable()
        {
            GameEvents.WorldUnlocked += HandleWorldUnlocked;
        }

        private void OnDisable()
        {
            GameEvents.WorldUnlocked -= HandleWorldUnlocked;
        }

        private void HandleWorldUnlocked(WorldData unlockedWorld)
        {
            // Có thể dùng để cập nhật trạng thái nếu modal đang mở
        }

        public void Show(WorldData targetWorld, int targetIndex)
        {
            if (targetWorld == null) return;

            pendingWorld = targetWorld;
            pendingWorldIndex = targetIndex;

            if (modalRoot == null)
            {
                BuildProceduralModal();
            }

            modalRoot.SetActive(true);
            PopulateData(targetWorld, targetIndex);

            // 1. Pháo hoa ăn mừng & âm thanh còi tàu + rương vàng
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnConfetti(new Vector3(0f, 2.5f, 7.5f), 2.2f);
                VFXManager.Instance.SpawnGoldBurst(new Vector3(0f, 2.0f, 7.5f), 35);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayChestOpen();
                AudioManager.Instance.PlayCoin();
            }

            // 2. Hoạt ảnh Popup nảy phóng to mượt mà (Spring Bounce)
            if (activePopRoutine != null) StopCoroutine(activePopRoutine);
            activePopRoutine = StartCoroutine(BounceInRoutine());
        }

        public void Close()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }
        }

        private void PopulateData(WorldData world, int worldIndex)
        {
            bool isUnlocked = WorldManager.Instance != null && WorldManager.Instance.IsWorldUnlocked(world);
            bool isCurrent = WorldManager.Instance != null && WorldManager.Instance.ActiveWorldIndex == worldIndex;

            if (worldOrderBadgeText != null)
            {
                worldOrderBadgeText.text = $"[ NEW REGION DISCOVERED • LEVEL {world.worldOrder} ]";
            }

            if (worldNameText != null)
            {
                worldNameText.text = world.worldName.ToUpper();
            }

            if (worldDescText != null)
            {
                worldDescText.text = world.description;
            }

            // Setup Signature Gimmick Spotlight per Level
            SetupGimmickSpotlight(world.worldOrder);

            // Setup Native Fish List
            if (fishListText != null)
            {
                string speciesStr = "";
                if (world.nativeSpecies != null && world.nativeSpecies.Count > 0)
                {
                    for (int i = 0; i < world.nativeSpecies.Count; i++)
                    {
                        if (world.nativeSpecies[i] == null) continue;
                        string prefix = world.nativeSpecies[i].IsApexBoss ? "[BOSS] <color=#F87171>" : "<color=#38BDF8>";
                        string suffix = "</color>";
                        speciesStr += (i > 0 ? "   |   " : "") + $"{prefix}{world.nativeSpecies[i].SpeciesName}{suffix}";
                    }
                }
                else
                {
                    speciesStr = "<color=#94A3B8>Tropical reef fish, rays and ocean bosses</color>";
                }
                fishListText.text = speciesStr;
            }

            // Status & Cost Info
            if (statusCostText != null)
            {
                if (isCurrent)
                {
                    statusCostText.text = "<color=#38BDF8>Currently docked at this harbor</color>";
                }
                else if (isUnlocked)
                {
                    statusCostText.text = "<color=#4ADE80>Harbor is ready! The crew is waiting!</color>";
                }
                else
                {
                    statusCostText.text = $"Unlock Cost: <color=#FFE600><b>{NumberFormatter.Format(world.unlockRequiredCoins)} G</b></color> (Conditions Met!)";
                }
            }

            // Set Sail Button Text & Style
            if (setSailButtonText != null)
            {
                setSailButtonText.text = isCurrent ? "CURRENT HARBOR" : "SET SAIL NOW!";
            }

            if (setSailButton != null && setSailButtonImg != null)
            {
                CasualUITheme.StyleButton(setSailButton, setSailButtonImg, isCurrent ? CasualBtnStyle.Dark : CasualBtnStyle.Green);
                setSailButton.interactable = !isCurrent;
            }
        }

        private void SetupGimmickSpotlight(int worldOrder)
        {
            if (gimmickTitleText == null || gimmickDescText == null) return;

            switch (worldOrder)
            {
                case 1:
                    if (gimmickBadgeText != null) gimmickBadgeText.text = "[ CORE AUTOMATION & APEX BOSS ]";
                    gimmickTitleText.text = "APEX BLACKTIP SHARK HUNT";
                    gimmickDescText.text = "The birthplace of your seafood empire! Automate anglers, fish wranglers, and cart transporters. Hunt down the Apex Blacktip Shark for massive pirate bounties!";
                    break;

                case 2:
                    if (gimmickBadgeText != null) gimmickBadgeText.text = "[ SIGNATURE GIMMICK: PEARL FRENZY x3 ]";
                    gimmickTitleText.text = "[SHELL] GIANT CLAM & PEARL HARVEST";
                    gimmickDescText.text = "The Giant Clam periodically opens to reveal Golden Pearls in crystal clear waters. Tap to gather 3 pearls and trigger <color=#FDE047><b>PEARL FRENZY: 3X FISH VALUE</b></color> for 20s! Aquarium tourists tip extra gold per second.";
                    break;

                case 3:
                    if (gimmickBadgeText != null) gimmickBadgeText.text = "[ SIGNATURE GIMMICK: SMOKED BBQ x4 ]";
                    gimmickTitleText.text = "[BBQ] AUTUMN SMOKER BBQ GRILL";
                    gimmickDescText.text = "A fragrant charcoal smoker grill warms the harbor. Tap repeatedly to fan the flames and speed up grilling. Each golden grilled fish sells to maple leaf tourists for <color=#FDE047><b>4X GOLD</b></color>!";
                    break;

                case 4:
                    if (gimmickBadgeText != null) gimmickBadgeText.text = "[ SIGNATURE GIMMICK: BLIZZARD BREAK ]";
                    gimmickTitleText.text = "[ICE] BLIZZARD SURVIVAL & ICE BREAKER";
                    gimmickDescText.text = "Freezing blizzards periodically slow anglers by 50% and spawn a massive Treasure Iceberg. Rapidly tap to shatter the ice, rescue the dock, and trigger a <color=#FDE047><b>GOLD SHOWER</b></color> plus <color=#4ADE80><b>2X FISHING SPEED</b></color> buff!";
                    break;

                case 5:
                    if (gimmickBadgeText != null) gimmickBadgeText.text = "[ SIGNATURE GIMMICK: ANCIENT RELICS ]";
                    gimmickTitleText.text = "[CARAVAN] DESERT CARAVAN & 3 SACRED RELICS";
                    gimmickDescText.text = "Merchant camel caravans visit the oasis dock seeking exotic fish. Deliver enough fish to permanently unlock <color=#FDE047><b>3 SACRED RELICS</b></color>: <i>Golden Sun (+30% Speed)</i>, <i>Jade Scarab (+50% Value)</i>, and the <i>Desert Genie Lamp</i>!";
                    break;

                default:
                    if (gimmickBadgeText != null) gimmickBadgeText.text = "[ NEW FRONTIER ]";
                    gimmickTitleText.text = "UNCHARTED EXPEDITION";
                    gimmickDescText.text = "Explore uncharted ocean waters with brand new marine species and exclusive harbor mechanics waiting for you!";
                    break;
            }
        }

        private void OnSetSailClicked()
        {
            if (pendingWorld == null || pendingWorldIndex < 0) return;

            // 1. Mở khóa thế giới nếu chưa mở
            if (WorldManager.Instance != null && !WorldManager.Instance.IsWorldUnlocked(pendingWorld))
            {
                WorldManager.Instance.TryUnlockWorld(pendingWorld, out _);
            }

            // 2. Đóng popup
            modalRoot.SetActive(false);

            // 3. Còi tàu hú vang vọng
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayShipHorn(Vector3.zero);
            }

            // 4. Kích hoạt thuyền nhổ neo rẽ sóng ra khơi
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.RequestWorldTransition(pendingWorldIndex, isPreview: false);
            }
        }

        private IEnumerator BounceInRoutine()
        {
            if (dialogBoxRect == null || canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            dialogBoxRect.localScale = Vector3.one * 0.75f;

            float elapsed = 0f;
            float duration = 0.28f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease out back formula
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

        private void BuildProceduralModal()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Root Canvas
            modalRoot = new GameObject("WorldUnlockCelebration_Root");
            modalRoot.transform.SetParent(transform, false);

            Canvas canvas = modalRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 350; // On top of all normal menus, under system notices

            CanvasScaler scaler = modalRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = 0.5f;

            modalRoot.AddComponent<GraphicRaycaster>();
            canvasGroup = modalRoot.AddComponent<CanvasGroup>();

            // 2. Dark Translucent Dimmed Backdrop
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(modalRoot.transform, false);
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
            dialogBox.transform.SetParent(modalRoot.transform, false);
            dialogBoxRect = dialogBox.AddComponent<RectTransform>();
            dialogBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogBoxRect.pivot = new Vector2(0.5f, 0.5f);
            dialogBoxRect.sizeDelta = new Vector2(660, 780);

            Image dBg = dialogBox.AddComponent<Image>();
            CasualUITheme.StylePopupDialog(dBg);

            // 4. Header Yellow Ribbon Banner: "🎉 NEW HARBOR DISCOVERED! 🎉"
            GameObject ribbon = new GameObject("HeaderRibbon");
            ribbon.transform.SetParent(dialogBox.transform, false);
            RectTransform rbRt = ribbon.AddComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.5f, 1f);
            rbRt.anchorMax = new Vector2(0.5f, 1f);
            rbRt.pivot = new Vector2(0.5f, 0.5f);
            rbRt.anchoredPosition = new Vector2(0, -10);
            rbRt.sizeDelta = new Vector2(460, 84);

            Image rbImg = ribbon.AddComponent<Image>();
            Sprite ribSp = CasualUITheme.TitleRibbonYellow ?? whiteSp;
            rbImg.sprite = ribSp;
            rbImg.type = (ribSp.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            rbImg.color = Color.white;
            rbImg.raycastTarget = false;

            GameObject ribTxtObj = new GameObject("RibbonText");
            ribTxtObj.transform.SetParent(ribbon.transform, false);
            ribbonTitleText = ribTxtObj.AddComponent<TextMeshProUGUI>();
            ribbonTitleText.text = "NEW HARBOR DISCOVERED!";
            ribbonTitleText.fontSize = 22;
            ribbonTitleText.fontStyle = FontStyles.Bold;
            ribbonTitleText.alignment = TextAlignmentOptions.Center;
            ribbonTitleText.color = Color.white;
            ribbonTitleText.raycastTarget = false;
            ribbonTitleText.ApplyFont(UIFontCategory.Header);
            RectTransform rbtRt = ribTxtObj.GetComponent<RectTransform>();
            rbtRt.anchorMin = Vector2.zero;
            rbtRt.anchorMax = Vector2.one;
            rbtRt.offsetMin = new Vector2(25, 8);
            rbtRt.offsetMax = new Vector2(-25, -4);

            // Close Button [X] at top-right
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform cRt = closeBtnObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(1f, 1f);
            cRt.anchoredPosition = new Vector2(-16, -16);
            cRt.sizeDelta = new Vector2(48, 48);

            Image cImg = closeBtnObj.AddComponent<Image>();
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(closeBtn, cImg, CasualBtnStyle.SquareGray);
            closeBtn.onClick.AddListener(Close);

            GameObject cIcon = new GameObject("Icon");
            cIcon.transform.SetParent(closeBtnObj.transform, false);
            var cIconImg = cIcon.AddComponent<Image>();
            cIconImg.sprite = CasualUITheme.IconClose;
            cIconImg.preserveAspect = true;
            cIconImg.raycastTarget = false;
            RectTransform ciRt = cIcon.GetComponent<RectTransform>();
            ciRt.anchorMin = new Vector2(0.2f, 0.2f);
            ciRt.anchorMax = new Vector2(0.8f, 0.8f);
            ciRt.sizeDelta = Vector2.zero;

            // 5. World Order Badge (Sub-Header)
            GameObject badgeObj = new GameObject("WorldOrderBadge");
            badgeObj.transform.SetParent(dialogBox.transform, false);
            worldOrderBadgeText = badgeObj.AddComponent<TextMeshProUGUI>();
            worldOrderBadgeText.text = "[ NEW REGION DISCOVERED • LEVEL 2 ]";
            worldOrderBadgeText.fontSize = 15;
            worldOrderBadgeText.fontStyle = FontStyles.Bold;
            worldOrderBadgeText.alignment = TextAlignmentOptions.Center;
            worldOrderBadgeText.color = new Color(0.20f, 0.85f, 0.95f);
            worldOrderBadgeText.raycastTarget = false;
            worldOrderBadgeText.ApplyFont(UIFontCategory.Button);
            RectTransform bRt = badgeObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 1f);
            bRt.anchorMax = new Vector2(0.5f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f);
            bRt.anchoredPosition = new Vector2(0, -68);
            bRt.sizeDelta = new Vector2(580, 24);

            // 6. World Large Title: "TROPICAL CORAL ATOLL"
            GameObject titleObj = new GameObject("WorldTitle");
            titleObj.transform.SetParent(dialogBox.transform, false);
            worldNameText = titleObj.AddComponent<TextMeshProUGUI>();
            worldNameText.text = "TROPICAL CORAL ATOLL";
            worldNameText.fontSize = 28;
            worldNameText.fontStyle = FontStyles.Bold;
            worldNameText.alignment = TextAlignmentOptions.Center;
            worldNameText.color = Color.white;
            worldNameText.raycastTarget = false;
            worldNameText.ApplyFont(UIFontCategory.Header);
            RectTransform wtRt = titleObj.GetComponent<RectTransform>();
            wtRt.anchorMin = new Vector2(0.5f, 1f);
            wtRt.anchorMax = new Vector2(0.5f, 1f);
            wtRt.pivot = new Vector2(0.5f, 1f);
            wtRt.anchoredPosition = new Vector2(0, -94);
            wtRt.sizeDelta = new Vector2(600, 36);

            // 7. World Short Description
            GameObject descObj = new GameObject("WorldDesc");
            descObj.transform.SetParent(dialogBox.transform, false);
            worldDescText = descObj.AddComponent<TextMeshProUGUI>();
            worldDescText.text = "A paradise coral reef lagoon with clear shallow water, colorful marine life, and gentle ocean breezes.";
            worldDescText.fontSize = 15;
            worldDescText.alignment = TextAlignmentOptions.Center;
            worldDescText.color = new Color(0.78f, 0.85f, 0.92f);
            worldDescText.raycastTarget = false;
            worldDescText.ApplyFont(UIFontCategory.Default);
            RectTransform wdRt = descObj.GetComponent<RectTransform>();
            wdRt.anchorMin = new Vector2(0.5f, 1f);
            wdRt.anchorMax = new Vector2(0.5f, 1f);
            wdRt.pivot = new Vector2(0.5f, 1f);
            wdRt.anchoredPosition = new Vector2(0, -132);
            wdRt.sizeDelta = new Vector2(580, 40);

            // =========================================================================
            // 8. SIGNATURE GIMMICK SPOTLIGHT BOX (Nổi bật nhất modal)
            // =========================================================================
            GameObject gimmickBox = new GameObject("GimmickSpotlight_Box");
            gimmickBox.transform.SetParent(dialogBox.transform, false);
            RectTransform gbRt = gimmickBox.AddComponent<RectTransform>();
            gbRt.anchorMin = new Vector2(0.5f, 1f);
            gbRt.anchorMax = new Vector2(0.5f, 1f);
            gbRt.pivot = new Vector2(0.5f, 1f);
            gbRt.anchoredPosition = new Vector2(0, -182);
            gbRt.sizeDelta = new Vector2(600, 220);

            Image gbBg = gimmickBox.AddComponent<Image>();
            CasualUITheme.StyleCard(gbBg, new Color(0.08f, 0.16f, 0.28f, 0.98f)); // Rich Midnight Navy

            // Gimmick Top Tag
            GameObject gBadgeObj = new GameObject("GimmickBadge");
            gBadgeObj.transform.SetParent(gimmickBox.transform, false);
            gimmickBadgeText = gBadgeObj.AddComponent<TextMeshProUGUI>();
            gimmickBadgeText.text = "[ EXCLUSIVE SIGNATURE GIMMICK ]";
            gimmickBadgeText.fontSize = 14;
            gimmickBadgeText.fontStyle = FontStyles.Bold;
            gimmickBadgeText.alignment = TextAlignmentOptions.Center;
            gimmickBadgeText.color = new Color(1.0f, 0.85f, 0.30f); // Warm Gold
            gimmickBadgeText.raycastTarget = false;
            gimmickBadgeText.ApplyFont(UIFontCategory.Button);
            RectTransform gbtRt = gBadgeObj.GetComponent<RectTransform>();
            gbtRt.anchorMin = new Vector2(0.5f, 1f);
            gbtRt.anchorMax = new Vector2(0.5f, 1f);
            gbtRt.pivot = new Vector2(0.5f, 1f);
            gbtRt.anchoredPosition = new Vector2(0, -12);
            gbtRt.sizeDelta = new Vector2(560, 22);

            // Gimmick Title: "[SHELL] GIANT CLAM & PEARL HARVEST"
            GameObject gTitleObj = new GameObject("GimmickTitle");
            gTitleObj.transform.SetParent(gimmickBox.transform, false);
            gimmickTitleText = gTitleObj.AddComponent<TextMeshProUGUI>();
            gimmickTitleText.text = "[SHELL] GIANT CLAM & PEARL HARVEST";
            gimmickTitleText.fontSize = 22;
            gimmickTitleText.fontStyle = FontStyles.Bold;
            gimmickTitleText.alignment = TextAlignmentOptions.Center;
            gimmickTitleText.color = Color.white;
            gimmickTitleText.raycastTarget = false;
            gimmickTitleText.ApplyFont(UIFontCategory.Header);
            RectTransform gtRt = gTitleObj.GetComponent<RectTransform>();
            gtRt.anchorMin = new Vector2(0.5f, 1f);
            gtRt.anchorMax = new Vector2(0.5f, 1f);
            gtRt.pivot = new Vector2(0.5f, 1f);
            gtRt.anchoredPosition = new Vector2(0, -36);
            gtRt.sizeDelta = new Vector2(560, 32);

            // Gimmick Description
            GameObject gDescObj = new GameObject("GimmickDesc");
            gDescObj.transform.SetParent(gimmickBox.transform, false);
            gimmickDescText = gDescObj.AddComponent<TextMeshProUGUI>();
            gimmickDescText.text = "The Giant Clam periodically opens to reveal Golden Pearls in the water. Tap to gather 3 pearls and trigger <b>PEARL FRENZY: 3X FISH VALUE</b> for 20s! Unlocks Aquarium tourists who tip extra gold.";
            gimmickDescText.fontSize = 16;
            gimmickDescText.alignment = TextAlignmentOptions.Center;
            gimmickDescText.color = new Color(0.85f, 0.92f, 0.98f);
            gimmickDescText.raycastTarget = false;
            gimmickDescText.ApplyFont(UIFontCategory.Default);
            RectTransform gdRt = gDescObj.GetComponent<RectTransform>();
            gdRt.anchorMin = new Vector2(0.5f, 1f);
            gdRt.anchorMax = new Vector2(0.5f, 1f);
            gdRt.pivot = new Vector2(0.5f, 1f);
            gdRt.anchoredPosition = new Vector2(0, -74);
            gdRt.sizeDelta = new Vector2(550, 130);

            // =========================================================================
            // 9. NATIVE FISH SPECIES SHOWCASE
            // =========================================================================
            GameObject fishBox = new GameObject("NativeFish_Box");
            fishBox.transform.SetParent(dialogBox.transform, false);
            RectTransform fbRt = fishBox.AddComponent<RectTransform>();
            fbRt.anchorMin = new Vector2(0.5f, 1f);
            fbRt.anchorMax = new Vector2(0.5f, 1f);
            fbRt.pivot = new Vector2(0.5f, 1f);
            fbRt.anchoredPosition = new Vector2(0, -416);
            fbRt.sizeDelta = new Vector2(600, 120);

            Image fbBg = fishBox.AddComponent<Image>();
            CasualUITheme.StyleCard(fbBg, new Color(0.06f, 0.10f, 0.18f, 0.95f));

            GameObject fbTitleObj = new GameObject("Title");
            fbTitleObj.transform.SetParent(fishBox.transform, false);
            var fbTitle = fbTitleObj.AddComponent<TextMeshProUGUI>();
            fbTitle.text = "NATIVE FISH & APEX BOSSES:";
            fbTitle.fontSize = 14;
            fbTitle.fontStyle = FontStyles.Bold;
            fbTitle.alignment = TextAlignmentOptions.Center;
            fbTitle.color = new Color(0.6f, 0.8f, 1f);
            fbTitle.raycastTarget = false;
            fbTitle.ApplyFont(UIFontCategory.Button);
            RectTransform fbtRt = fbTitleObj.GetComponent<RectTransform>();
            fbtRt.anchorMin = new Vector2(0.5f, 1f);
            fbtRt.anchorMax = new Vector2(0.5f, 1f);
            fbtRt.pivot = new Vector2(0.5f, 1f);
            fbtRt.anchoredPosition = new Vector2(0, -12);
            fbtRt.sizeDelta = new Vector2(560, 20);

            GameObject fbListObj = new GameObject("List");
            fbListObj.transform.SetParent(fishBox.transform, false);
            fishListText = fbListObj.AddComponent<TextMeshProUGUI>();
            fishListText.text = "Darwin Clownfish  •  Yellow Tang  •  Spotted Eagle Ray  •  Emperor Angelfish";
            fishListText.fontSize = 16;
            fishListText.fontStyle = FontStyles.Bold;
            fishListText.alignment = TextAlignmentOptions.Center;
            fishListText.color = Color.white;
            fishListText.raycastTarget = false;
            fishListText.ApplyFont(UIFontCategory.Default);
            RectTransform fblRt = fbListObj.GetComponent<RectTransform>();
            fblRt.anchorMin = new Vector2(0.5f, 1f);
            fblRt.anchorMax = new Vector2(0.5f, 1f);
            fblRt.pivot = new Vector2(0.5f, 1f);
            fblRt.anchoredPosition = new Vector2(0, -38);
            fblRt.sizeDelta = new Vector2(560, 68);

            // =========================================================================
            // 10. UNLOCK STATUS / COST
            // =========================================================================
            GameObject statusObj = new GameObject("StatusCostText");
            statusObj.transform.SetParent(dialogBox.transform, false);
            statusCostText = statusObj.AddComponent<TextMeshProUGUI>();
            statusCostText.text = "<color=#4ADE80>Ready to set sail!</color>";
            statusCostText.fontSize = 17;
            statusCostText.fontStyle = FontStyles.Bold;
            statusCostText.alignment = TextAlignmentOptions.Center;
            statusCostText.color = Color.white;
            statusCostText.raycastTarget = false;
            statusCostText.ApplyFont(UIFontCategory.Button);
            RectTransform scRt = statusObj.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.5f, 0f);
            scRt.anchorMax = new Vector2(0.5f, 0f);
            scRt.pivot = new Vector2(0.5f, 0f);
            scRt.anchoredPosition = new Vector2(0, 160);
            scRt.sizeDelta = new Vector2(580, 28);

            // =========================================================================
            // 11. PRIMARY BUTTON: "SET SAIL NOW!"
            // =========================================================================
            GameObject sailBtnObj = new GameObject("Btn_SetSailNow");
            sailBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform sbRt = sailBtnObj.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.5f, 0f);
            sbRt.anchorMax = new Vector2(0.5f, 0f);
            sbRt.pivot = new Vector2(0.5f, 0f);
            sbRt.anchoredPosition = new Vector2(0, 80);
            sbRt.sizeDelta = new Vector2(480, 68);

            setSailButton = sailBtnObj.AddComponent<Button>();
            setSailButtonImg = sailBtnObj.AddComponent<Image>();
            CasualUITheme.StyleButton(setSailButton, setSailButtonImg, CasualBtnStyle.Green);
            setSailButton.onClick.AddListener(OnSetSailClicked);

            GameObject sbtObj = new GameObject("Text");
            sbtObj.transform.SetParent(sailBtnObj.transform, false);
            setSailButtonText = sbtObj.AddComponent<TextMeshProUGUI>();
            setSailButtonText.text = "SET SAIL NOW!";
            setSailButtonText.fontSize = 21;
            setSailButtonText.fontStyle = FontStyles.Bold;
            setSailButtonText.alignment = TextAlignmentOptions.Center;
            setSailButtonText.color = Color.white;
            setSailButtonText.raycastTarget = false;
            setSailButtonText.ApplyFont(UIFontCategory.Button);
            RectTransform sbtRt = sbtObj.GetComponent<RectTransform>();
            sbtRt.anchorMin = Vector2.zero;
            sbtRt.anchorMax = Vector2.one;
            sbtRt.sizeDelta = Vector2.zero;

            // =========================================================================
            // 12. SECONDARY BUTTON: "STAY AT CURRENT HARBOR"
            // =========================================================================
            GameObject stayBtnObj = new GameObject("Btn_StayHere");
            stayBtnObj.transform.SetParent(dialogBox.transform, false);
            RectTransform stRt = stayBtnObj.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 0f);
            stRt.anchorMax = new Vector2(0.5f, 0f);
            stRt.pivot = new Vector2(0.5f, 0f);
            stRt.anchoredPosition = new Vector2(0, 22);
            stRt.sizeDelta = new Vector2(340, 48);

            stayButton = stayBtnObj.AddComponent<Button>();
            Image stayImg = stayBtnObj.AddComponent<Image>();
            CasualUITheme.StyleButton(stayButton, stayImg, CasualBtnStyle.Dark);
            stayButton.onClick.AddListener(Close);

            GameObject sttObj = new GameObject("Text");
            sttObj.transform.SetParent(stayBtnObj.transform, false);
            var stt = sttObj.AddComponent<TextMeshProUGUI>();
            stt.text = "STAY AT CURRENT DOCK";
            stt.fontSize = 16;
            stt.fontStyle = FontStyles.Bold;
            stt.alignment = TextAlignmentOptions.Center;
            stt.color = new Color(0.75f, 0.82f, 0.90f);
            stt.raycastTarget = false;
            stt.ApplyFont(UIFontCategory.Button);
            RectTransform sttRt = sttObj.GetComponent<RectTransform>();
            sttRt.anchorMin = Vector2.zero;
            sttRt.anchorMax = Vector2.one;
            sttRt.sizeDelta = Vector2.zero;
        }
    }
}

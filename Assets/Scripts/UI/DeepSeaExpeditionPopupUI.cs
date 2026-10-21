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

namespace IdleFishing.UI
{
    /// <summary>
    /// Popup Thám Hiểm Biển Sâu & Kho Cổ Vật Thần Thoại (Deep Sea Expedition & Ancient Relics UI):
    /// - Quản lý 2 Tab chính:
    ///   • Tab 1: KHO CỔ VẬT (Ancient Relics Vault) - Nâng cấp 6 Cổ vật Thần Thoại bằng Ngọc Trai Cổ Đại.
    ///   • Tab 2: HẢI TRÌNH VIỄN CHINH (Expedition Voyage) - Đổi tiến độ đảo lấy Ngọc Trai và khởi hành hải trình mới.
    /// - Tích hợp chuyển cảnh Mây Toon (VoyageTransitionUI), thông báo nổi và hiệu ứng Spring Bounce.
    /// - Tích hợp nút HUD [⚓] trên thanh Top HUD kế bên các nút chức năng.
    /// </summary>
    public class DeepSeaExpeditionPopupUI : MonoBehaviour
    {
        private static DeepSeaExpeditionPopupUI _instance;
        public static DeepSeaExpeditionPopupUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DeepSeaExpeditionPopupUI>();
                    if (_instance == null)
                    {
                        var canvas = FindAnyObjectByType<Canvas>();
                        GameObject go = new GameObject("--- EXPEDITION POPUP UI ---");
                        if (canvas != null) go.transform.SetParent(canvas.transform, false);
                        _instance = go.AddComponent<DeepSeaExpeditionPopupUI>();
                    }
                }
                return _instance;
            }
        }

        private GameObject popupRoot;
        private RectTransform dialogBoxRect;
        private CanvasGroup canvasGroup;
        private GameObject hudNavButtonObj;
        private GameObject hudAlertBadgeObj;

        // Header Currency
        private TextMeshProUGUI pearlBalanceText;
        private TextMeshProUGUI lifetimePearlsText;
        private TextMeshProUGUI voyageCountText;

        // Tabs
        private int currentTab = 0; // 0: Relics, 1: Voyage
        private GameObject relicsTabContainer;
        private GameObject voyageTabContainer;
        private Button tabRelicsBtn;
        private Image tabRelicsBg;
        private Button tabVoyageBtn;
        private Image tabVoyageBg;

        // Tab 1: Relics Elements
        private readonly List<RelicCardUI> relicCards = new List<RelicCardUI>();

        private class RelicCardUI
        {
            public RelicType relicType;
            public GameObject cardObj;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI levelText;
            public TextMeshProUGUI currentBuffText;
            public TextMeshProUGUI nextBuffText;
            public Button upgradeBtn;
            public Image upgradeBtnBg;
            public TextMeshProUGUI upgradeBtnText;
        }

        // Tab 2: Voyage Elements
        private TextMeshProUGUI harborStatusText;
        private TextMeshProUGUI earnablePearlsText;
        private TextMeshProUGUI expeditionSummaryText;
        private Button setSailBtn;
        private Image setSailBtnBg;
        private TextMeshProUGUI setSailBtnText;

        // Confirmation Prompt
        private GameObject confirmDialogObj;
        private TextMeshProUGUI confirmDescText;
        private Button confirmYesBtn;
        private Button confirmNoBtn;

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

            if (popupRoot != null) popupRoot.SetActive(false);
            if (confirmDialogObj != null) confirmDialogObj.SetActive(false);
        }

        private void Start()
        {
            if (hudNavButtonObj == null) BuildHUDNavButton();
            RefreshHUDAlertState();
        }

        private void OnEnable()
        {
            if (PrestigeManager.Instance != null)
            {
                PrestigeManager.Instance.OnPrestigeUpdated += RefreshUI;
            }
            GameEvents.CoinsChanged += HandleCoinsChanged;
            GameEvents.WorldChanged += HandleWorldChanged;
        }

        private void OnDisable()
        {
            if (PrestigeManager.Instance != null)
            {
                PrestigeManager.Instance.OnPrestigeUpdated -= RefreshUI;
            }
            GameEvents.CoinsChanged -= HandleCoinsChanged;
            GameEvents.WorldChanged -= HandleWorldChanged;
        }

        private void HandleCoinsChanged(int newCoins)
        {
            RefreshHUDAlertState();
        }

        private void HandleWorldChanged(WorldData world)
        {
            RefreshHUDAlertState();
            if (popupRoot != null && popupRoot.activeSelf)
            {
                RefreshUI();
            }
        }

        public void Open(int defaultTab = 0)
        {
            if (popupRoot == null) BuildProceduralModal();

            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();
            if (confirmDialogObj != null) confirmDialogObj.SetActive(false);

            SwitchTab(defaultTab);
            RefreshUI();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

            if (bounceRoutine != null) StopCoroutine(bounceRoutine);
            bounceRoutine = StartCoroutine(BounceInRoutine());
        }

        public void Close()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
            if (confirmDialogObj != null) confirmDialogObj.SetActive(false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        }

        private void SwitchTab(int tabIndex)
        {
            currentTab = Mathf.Clamp(tabIndex, 0, 1);

            if (relicsTabContainer != null) relicsTabContainer.SetActive(currentTab == 0);
            if (voyageTabContainer != null) voyageTabContainer.SetActive(currentTab == 1);

            if (tabRelicsBg != null)
            {
                CasualUITheme.StyleButton(tabRelicsBtn, tabRelicsBg, currentTab == 0 ? CasualBtnStyle.Blue : CasualBtnStyle.Dark);
            }
            if (tabVoyageBg != null)
            {
                CasualUITheme.StyleButton(tabVoyageBtn, tabVoyageBg, currentTab == 1 ? CasualBtnStyle.Yellow : CasualBtnStyle.Dark);
            }

            RefreshUI();
        }

        public void RefreshUI()
        {
            if (PrestigeManager.Instance == null) return;

            int pearls = PrestigeManager.Instance.AncientPearls;
            int lifetime = PrestigeManager.Instance.LifetimePearls;
            int count = PrestigeManager.Instance.PrestigeCount;

            if (pearlBalanceText != null) pearlBalanceText.text = $"PEARLS: <color=#FCD34D><b>{pearlPearlsFormat(pearls)}</b></color>";
            if (lifetimePearlsText != null) lifetimePearlsText.text = $"LIFETIME: <color=#38BDF8><b>{lifetime}</b></color>";
            if (voyageCountText != null) voyageCountText.text = $"VOYAGES: <b>x{count}</b>";

            if (currentTab == 0)
            {
                RefreshRelicsTab();
            }
            else
            {
                RefreshVoyageTab();
            }

            RefreshHUDAlertState();
        }

        private string pearlPearlsFormat(int amount)
        {
            return $"{amount}";
        }

        private void RefreshRelicsTab()
        {
            if (PrestigeManager.Instance == null) return;

            for (int i = 0; i < relicCards.Count; i++)
            {
                var card = relicCards[i];
                var type = card.relicType;
                int lvl = PrestigeManager.Instance.GetRelicLevel(type);
                int maxLvl = PrestigeManager.GetRelicMaxLevel(type);
                int cost = PrestigeManager.Instance.GetRelicUpgradeCost(type);

                card.titleText.text = PrestigeManager.GetRelicTitle(type);
                card.levelText.text = (lvl >= maxLvl) ? "<color=#FCD34D><b>[MAX]</b></color>" : $"[LV {lvl} / {maxLvl}]";
                card.currentBuffText.text = PrestigeManager.Instance.GetCurrentBonusText(type);
                card.nextBuffText.text = PrestigeManager.Instance.GetNextBonusText(type);

                if (lvl >= maxLvl)
                {
                    card.upgradeBtn.interactable = false;
                    CasualUITheme.StyleButton(card.upgradeBtn, card.upgradeBtnBg, CasualBtnStyle.Dark);
                    card.upgradeBtnText.text = "MAX LEVEL";
                    card.upgradeBtnText.color = new Color(1f, 0.85f, 0.25f);
                }
                else
                {
                    bool canAfford = PrestigeManager.Instance.AncientPearls >= cost;
                    card.upgradeBtn.interactable = canAfford;
                    CasualUITheme.StyleButton(card.upgradeBtn, card.upgradeBtnBg, canAfford ? CasualBtnStyle.Green : CasualBtnStyle.Dark);
                    card.upgradeBtnText.text = canAfford ? $"UPGRADE\n{cost} Pearls" : $"NEED {cost} PEARLS";
                    card.upgradeBtnText.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.75f);
                }
            }
        }

        private void RefreshVoyageTab()
        {
            if (PrestigeManager.Instance == null || WorldManager.Instance == null) return;

            WorldData curWorld = WorldManager.Instance.CurrentWorld;
            string wName = curWorld != null ? curWorld.worldName : "Temperate Bay";
            int wOrder = curWorld != null ? curWorld.worldOrder : 1;
            int coins = EconomyManager.Instance != null && curWorld != null ? EconomyManager.Instance.GetCoinsForWorld(curWorld.worldId) : 0;
            int upgrades = UpgradeManager.Instance != null && curWorld != null ? UpgradeManager.Instance.GetTotalUpgradeLevelsForWorld(curWorld.worldId) : 0;

            long totalVal = PrestigeManager.Instance != null ? PrestigeManager.Instance.GetTotalValueEarnedAllLevels() : 0;
            string breakdown = PrestigeManager.Instance != null ? PrestigeManager.Instance.GetLevelValueBreakdown() : "Lv.1: 0 G";
            int totalUpgrades = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetTotalUpgradeLevelsAllWorlds() : 0;

            if (harborStatusText != null)
            {
                harborStatusText.text = $"TOTAL VALUE (ALL LEVELS): <color=#FCD34D><b>{NumberFormatter.Format(totalVal)} G</b></color>\n<size=21><color=#CBD5E1>{breakdown}</color>  •  <color=#38BDF8>{totalUpgrades}</color> Total Upgrades</size>";
            }

            int earnable = curWorld != null ? PrestigeManager.Instance.CalculateMedalsEarnable(curWorld.worldId) : 0;
            bool canPrestige = curWorld != null && PrestigeManager.Instance.CanPrestige(curWorld.worldId, out string reason);

            if (earnablePearlsText != null)
            {
                earnablePearlsText.text = earnable > 0
                    ? $"HARVEST ON VOYAGE:\n<size=38><color=#FCD34D><b>+{earnable} ANCIENT PEARLS</b></color></size>"
                    : $"HARVEST ON VOYAGE:\n<size=28><color=#94A3B8>Accumulate more resources</color></size>";
            }

            int startGoldBonus = PrestigeManager.Instance.BonusStartingGold;
            if (expeditionSummaryText != null)
            {
                expeditionSummaryText.text =
                    $"<b>VOYAGE PERMANENCE GUARANTEE:</b>\n" +
                    $"• <color=#4ADE80>[OK] Pearls calculated from ALL value earned across ALL levels.</color>\n" +
                    $"• <color=#4ADE80>[OK] All 6 Ancient Relics & Pearls remain permanent forever.</color>\n" +
                    $"• <color=#F87171>[RESET] All levels, stages & pier upgrades reset back to Level 1.</color>\n" +
                    $"• <color=#FCD34D>[BONUS] Mariner's Compass grants +{NumberFormatter.Format(startGoldBonus)} G starting boost!</color>";
            }

            if (setSailBtn != null)
            {
                setSailBtn.interactable = canPrestige && earnable > 0;
                CasualUITheme.StyleButton(setSailBtn, setSailBtnBg, (canPrestige && earnable > 0) ? CasualBtnStyle.Yellow : CasualBtnStyle.Dark);
                if (setSailBtnText != null)
                {
                    setSailBtnText.text = (canPrestige && earnable > 0)
                        ? $"SET SAIL ON EXPEDITION (+{earnable} Pearls)"
                        : "ACCUMULATE MORE RESOURCES (+0 Pearls)";
                    setSailBtnText.color = (canPrestige && earnable > 0) ? new Color(0.2f, 0.12f, 0.02f) : new Color(0.65f, 0.65f, 0.70f);
                }
            }
        }

        private void RefreshHUDAlertState()
        {
            if (hudAlertBadgeObj == null || PrestigeManager.Instance == null || WorldManager.Instance == null) return;

            WorldData cur = WorldManager.Instance.CurrentWorld;
            if (cur != null)
            {
                int earnable = PrestigeManager.Instance.CalculateMedalsEarnable(cur.worldId);
                hudAlertBadgeObj.SetActive(earnable > 0);
            }
            else
            {
                hudAlertBadgeObj.SetActive(false);
            }
        }

        private void OnSetSailClicked()
        {
            if (PrestigeManager.Instance == null || WorldManager.Instance == null) return;
            WorldData cur = WorldManager.Instance.CurrentWorld;
            if (cur == null) return;

            int earnable = PrestigeManager.Instance.CalculateMedalsEarnable(cur.worldId);
            if (earnable <= 0) return;

            // Show Confirmation Modal
            if (confirmDialogObj != null)
            {
                confirmDialogObj.SetActive(true);
                confirmDialogObj.transform.SetAsLastSibling();
                if (confirmDescText != null)
                {
                    confirmDescText.text =
                        $"Are you ready to set sail on a Grand Voyage?\n\n" +
                        $"You will harvest <color=#FCD34D><b>+{earnable} Ancient Pearls</b></color> calculated from all value earned across all levels!\n\n" +
                        $"<color=#F87171>All levels, pier upgrades and stages will reset to Level 1.</color>\n\n" +
                        $"<b>All 6 Ancient Relics and Pearls are kept permanently!</b>";
                }
            }
        }

        private void OnConfirmExpeditionYes()
        {
            if (confirmDialogObj != null) confirmDialogObj.SetActive(false);
            if (PrestigeManager.Instance == null || WorldManager.Instance == null) return;

            WorldData cur = WorldManager.Instance.CurrentWorld;
            if (cur == null) return;

            Close();

            PrestigeManager.Instance.PerformExpedition(cur, () =>
            {
                Debug.Log("[DeepSeaExpeditionPopupUI] Expedition midpoint complete.");
            }, out int gained);
        }

        private void OnConfirmExpeditionNo()
        {
            if (confirmDialogObj != null) confirmDialogObj.SetActive(false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
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

            hudNavButtonObj = new GameObject("Btn_Expedition");
            hudNavButtonObj.transform.SetParent(finalParent, false);
            hudNavButtonObj.transform.SetSiblingIndex(1); // Middle action in vertical dock

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
            btn.onClick.AddListener(() => Open(0));

            CasualUITheme.StyleTileButton(
                hudNavButtonObj,
                CasualUITheme.IconAnchor,
                "VOYAGE",
                CasualBtnStyle.TileBlue,
                new Color(0.40f, 0.90f, 1f),
                new Vector2(92f, 94f),
                new Vector2(48f, 48f)
            );

            // Alert Notification Badge (Pulsing green dot when ready to set sail)
            hudAlertBadgeObj = new GameObject("AlertBadge");
            hudAlertBadgeObj.transform.SetParent(hudNavButtonObj.transform, false);
            RectTransform abRt = hudAlertBadgeObj.AddComponent<RectTransform>();
            abRt.anchorMin = new Vector2(1f, 1f);
            abRt.anchorMax = new Vector2(1f, 1f);
            abRt.pivot = new Vector2(0.5f, 0.5f);
            abRt.anchoredPosition = new Vector2(-10f, -10f);
            abRt.sizeDelta = new Vector2(20f, 20f);

            Image abImg = hudAlertBadgeObj.AddComponent<Image>();
            abImg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            abImg.color = new Color(0.2f, 0.85f, 0.4f, 1f);
            abImg.raycastTarget = false;
            hudAlertBadgeObj.SetActive(false);
        }
        #endregion

        #region Modal Construction
        private void BuildProceduralModal()
        {
            if (popupRoot != null) return;

            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Root Canvas - Overlay
            popupRoot = new GameObject("DeepSeaExpedition_PopupRoot");
            popupRoot.transform.SetParent(transform, false);

            RectTransform rootRt = popupRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Canvas canvas = popupRoot.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;

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

            // 3. Center Dialog Box (Scaled for 1080x1920)
            GameObject dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(popupRoot.transform, false);
            dialogBoxRect = dialogBox.AddComponent<RectTransform>();
            dialogBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogBoxRect.pivot = new Vector2(0.5f, 0.5f);
            dialogBoxRect.sizeDelta = new Vector2(860f, 1200f);

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
            rbRt.sizeDelta = new Vector2(620f, 92f);

            Image rbImg = ribbonObj.AddComponent<Image>();
            rbImg.sprite = CasualUITheme.TitleRibbonYellow ?? CasualUITheme.CardRound12 ?? whiteSp;
            rbImg.type = Image.Type.Sliced;
            rbImg.color = Color.white;

            GameObject titleTextObj = new GameObject("Text_Title");
            titleTextObj.transform.SetParent(ribbonObj.transform, false);
            var titleTxt = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "ANCIENT RELICS & EXPEDITION";
            titleTxt.fontSize = 32f;
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

            // 6. Currency Header Status Bar (Top of Dialog)
            GameObject curBar = new GameObject("Bar_CurrencyHeader");
            curBar.transform.SetParent(dialogBox.transform, false);
            RectTransform curRt = curBar.AddComponent<RectTransform>();
            curRt.anchorMin = new Vector2(0.5f, 1f);
            curRt.anchorMax = new Vector2(0.5f, 1f);
            curRt.pivot = new Vector2(0.5f, 1f);
            curRt.anchoredPosition = new Vector2(0f, -68f);
            curRt.sizeDelta = new Vector2(780f, 64f);

            Image curBg = curBar.AddComponent<Image>();
            curBg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            curBg.type = Image.Type.Sliced;
            curBg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);

            // Pearl Balance Text
            GameObject pObj = new GameObject("Text_Pearls");
            pObj.transform.SetParent(curBar.transform, false);
            pearlBalanceText = pObj.AddComponent<TextMeshProUGUI>();
            pearlBalanceText.text = "PEARLS: <color=#FCD34D><b>0</b></color>";
            pearlBalanceText.fontSize = 24f;
            pearlBalanceText.fontStyle = FontStyles.Bold;
            pearlBalanceText.alignment = TextAlignmentOptions.MidlineLeft;
            pearlBalanceText.color = Color.white;
            pearlBalanceText.ApplyFont(UIFontCategory.Header);
            RectTransform pRt = pObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0f);
            pRt.anchorMax = new Vector2(0.40f, 1f);
            pRt.offsetMin = new Vector2(18f, 0f);
            pRt.offsetMax = Vector2.zero;

            // Lifetime Pearls Text
            GameObject ltObj = new GameObject("Text_Lifetime");
            ltObj.transform.SetParent(curBar.transform, false);
            lifetimePearlsText = ltObj.AddComponent<TextMeshProUGUI>();
            lifetimePearlsText.text = "LIFETIME: <color=#38BDF8><b>0</b></color>";
            lifetimePearlsText.fontSize = 22f;
            lifetimePearlsText.fontStyle = FontStyles.Normal;
            lifetimePearlsText.alignment = TextAlignmentOptions.MidlineLeft;
            lifetimePearlsText.color = new Color(0.8f, 0.88f, 0.95f);
            lifetimePearlsText.ApplyFont(UIFontCategory.Default);
            RectTransform ltRt = ltObj.GetComponent<RectTransform>();
            ltRt.anchorMin = new Vector2(0.40f, 0f);
            ltRt.anchorMax = new Vector2(0.72f, 1f);
            ltRt.sizeDelta = Vector2.zero;

            // Voyage Count Text
            GameObject vObj = new GameObject("Text_Voyages");
            vObj.transform.SetParent(curBar.transform, false);
            voyageCountText = vObj.AddComponent<TextMeshProUGUI>();
            voyageCountText.text = "VOYAGES: <b>x0</b>";
            voyageCountText.fontSize = 22f;
            voyageCountText.fontStyle = FontStyles.Normal;
            voyageCountText.alignment = TextAlignmentOptions.MidlineRight;
            voyageCountText.color = new Color(0.8f, 0.88f, 0.95f);
            voyageCountText.ApplyFont(UIFontCategory.Default);
            RectTransform vRt = vObj.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0.72f, 0f);
            vRt.anchorMax = new Vector2(1f, 1f);
            vRt.offsetMin = Vector2.zero;
            vRt.offsetMax = new Vector2(-18f, 0f);

            // 7. Navigation Tabs: [ANCIENT RELICS] | [EXPEDITION VOYAGE]
            GameObject tabNav = new GameObject("Nav_Tabs");
            tabNav.transform.SetParent(dialogBox.transform, false);
            RectTransform tnRt = tabNav.AddComponent<RectTransform>();
            tnRt.anchorMin = new Vector2(0.5f, 1f);
            tnRt.anchorMax = new Vector2(0.5f, 1f);
            tnRt.pivot = new Vector2(0.5f, 1f);
            tnRt.anchoredPosition = new Vector2(0f, -142f);
            tnRt.sizeDelta = new Vector2(780f, 60f);

            // Tab 0: Relics Button
            GameObject tab0Obj = new GameObject("Btn_TabRelics");
            tab0Obj.transform.SetParent(tabNav.transform, false);
            RectTransform t0Rt = tab0Obj.AddComponent<RectTransform>();
            t0Rt.anchorMin = new Vector2(0f, 0f);
            t0Rt.anchorMax = new Vector2(0.50f, 1f);
            t0Rt.offsetMin = new Vector2(0f, 0f);
            t0Rt.offsetMax = new Vector2(-6f, 0f);

            tabRelicsBg = tab0Obj.AddComponent<Image>();
            tabRelicsBtn = tab0Obj.AddComponent<Button>();
            CasualUITheme.StyleButton(tabRelicsBtn, tabRelicsBg, CasualBtnStyle.Blue);
            tabRelicsBtn.onClick.AddListener(() => SwitchTab(0));

            GameObject t0TxtObj = new GameObject("Text");
            t0TxtObj.transform.SetParent(tab0Obj.transform, false);
            var t0Txt = t0TxtObj.AddComponent<TextMeshProUGUI>();
            t0Txt.text = "[VAULT] ANCIENT RELICS";
            t0Txt.fontSize = 24f;
            t0Txt.fontStyle = FontStyles.Bold;
            t0Txt.alignment = TextAlignmentOptions.Center;
            t0Txt.color = Color.white;
            t0Txt.ApplyFont(UIFontCategory.Button);
            RectTransform t0tRt = t0TxtObj.GetComponent<RectTransform>();
            t0tRt.anchorMin = Vector2.zero;
            t0tRt.anchorMax = Vector2.one;
            t0tRt.sizeDelta = Vector2.zero;

            // Tab 1: Voyage Button
            GameObject tab1Obj = new GameObject("Btn_TabVoyage");
            tab1Obj.transform.SetParent(tabNav.transform, false);
            RectTransform t1Rt = tab1Obj.AddComponent<RectTransform>();
            t1Rt.anchorMin = new Vector2(0.50f, 0f);
            t1Rt.anchorMax = new Vector2(1f, 1f);
            t1Rt.offsetMin = new Vector2(6f, 0f);
            t1Rt.offsetMax = new Vector2(0f, 0f);

            tabVoyageBg = tab1Obj.AddComponent<Image>();
            tabVoyageBtn = tab1Obj.AddComponent<Button>();
            CasualUITheme.StyleButton(tabVoyageBtn, tabVoyageBg, CasualBtnStyle.Dark);
            tabVoyageBtn.onClick.AddListener(() => SwitchTab(1));

            GameObject t1TxtObj = new GameObject("Text");
            t1TxtObj.transform.SetParent(tab1Obj.transform, false);
            var t1Txt = t1TxtObj.AddComponent<TextMeshProUGUI>();
            t1Txt.text = "[VOYAGE] EXPEDITION";
            t1Txt.fontSize = 24f;
            t1Txt.fontStyle = FontStyles.Bold;
            t1Txt.alignment = TextAlignmentOptions.Center;
            t1Txt.color = Color.white;
            t1Txt.ApplyFont(UIFontCategory.Button);
            RectTransform t1tRt = t1TxtObj.GetComponent<RectTransform>();
            t1tRt.anchorMin = Vector2.zero;
            t1tRt.anchorMax = Vector2.one;
            t1tRt.sizeDelta = Vector2.zero;

            // 8. Content Containers
            BuildRelicsTabContent(dialogBox.transform);
            BuildVoyageTabContent(dialogBox.transform);
            BuildConfirmationDialog(popupRoot.transform);
        }

        private void BuildRelicsTabContent(Transform parent)
        {
            relicsTabContainer = new GameObject("Tab_RelicsContent");
            relicsTabContainer.transform.SetParent(parent, false);
            RectTransform rt = relicsTabContainer.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(32f, 24f);
            rt.offsetMax = new Vector2(-32f, -212f);

            // ScrollView Setup
            ScrollRect scrollRect = relicsTabContainer.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 25f;

            // Viewport Mask
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(relicsTabContainer.transform, false);
            RectTransform vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            scrollRect.viewport = vpRt;

            // Content Container
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(0f, 0f);

            scrollRect.content = cRt;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 6, 16);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            relicCards.Clear();
            var relicTypes = (RelicType[])Enum.GetValues(typeof(RelicType));
            for (int i = 0; i < relicTypes.Length; i++)
            {
                var card = CreateRelicCard(content.transform, relicTypes[i]);
                relicCards.Add(card);
            }
        }

        private RelicCardUI CreateRelicCard(Transform parent, RelicType type)
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            GameObject cardObj = new GameObject($"Card_{type}");
            cardObj.transform.SetParent(parent, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(0f, 140f);

            var le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 140f;
            le.preferredHeight = 140f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.08f, 0.14f, 0.22f, 0.95f);

            // Icon Plinth
            GameObject icPlinth = new GameObject("IconPlinth");
            icPlinth.transform.SetParent(cardObj.transform, false);
            RectTransform ipRt = icPlinth.AddComponent<RectTransform>();
            ipRt.anchorMin = new Vector2(0f, 0.5f);
            ipRt.anchorMax = new Vector2(0f, 0.5f);
            ipRt.pivot = new Vector2(0f, 0.5f);
            ipRt.anchoredPosition = new Vector2(16f, 0f);
            ipRt.sizeDelta = new Vector2(88f, 88f);

            Image ipImg = icPlinth.AddComponent<Image>();
            ipImg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            ipImg.type = Image.Type.Sliced;
            ipImg.color = new Color(0.04f, 0.08f, 0.14f, 0.95f);

            // Icon graphic: use sprite if available, otherwise stylish text
            Sprite iconSp = null;
            Color iconTint = Color.white;
            switch (type)
            {
                case RelicType.GoldenHook:
                    iconSp = CasualUITheme.IconFish;
                    iconTint = new Color(1f, 0.82f, 0.2f);
                    break;
                case RelicType.MarinersCompass:
                    iconSp = CasualUITheme.IconCompass;
                    iconTint = new Color(0.35f, 0.85f, 1f);
                    break;
                case RelicType.LeviathanCrown:
                    iconSp = CasualUITheme.IconCrown;
                    iconTint = new Color(1f, 0.82f, 0.2f);
                    break;
            }

            if (iconSp != null)
            {
                GameObject icImgObj = new GameObject("IconImg");
                icImgObj.transform.SetParent(icPlinth.transform, false);
                RectTransform icImgRt = icImgObj.AddComponent<RectTransform>();
                icImgRt.anchorMin = new Vector2(0.5f, 0.5f);
                icImgRt.anchorMax = new Vector2(0.5f, 0.5f);
                icImgRt.sizeDelta = new Vector2(56f, 56f);
                Image icImg = icImgObj.AddComponent<Image>();
                icImg.sprite = iconSp;
                icImg.color = iconTint;
                icImg.raycastTarget = false;
            }
            else
            {
                GameObject icTxtObj = new GameObject("Text");
                icTxtObj.transform.SetParent(icPlinth.transform, false);
                var icTxt = icTxtObj.AddComponent<TextMeshProUGUI>();
                icTxt.text = PrestigeManager.GetRelicIcon(type);
                icTxt.fontSize = 22f;
                icTxt.fontStyle = FontStyles.Bold;
                icTxt.alignment = TextAlignmentOptions.Center;
                icTxt.color = new Color(1f, 0.85f, 0.25f);
                icTxt.raycastTarget = false;
                icTxt.ApplyFont(UIFontCategory.Button);
                RectTransform ictRt = icTxtObj.GetComponent<RectTransform>();
                ictRt.anchorMin = Vector2.zero;
                ictRt.anchorMax = Vector2.one;
                ictRt.sizeDelta = Vector2.zero;
            }

            // Level Text Badge (Placed at top right above Upgrade Button)
            GameObject lvlObj = new GameObject("Text_Level");
            lvlObj.transform.SetParent(cardObj.transform, false);
            var lvlTxt = lvlObj.AddComponent<TextMeshProUGUI>();
            lvlTxt.text = $"[LV 0 / {PrestigeManager.GetRelicMaxLevel(type)}]";
            lvlTxt.fontSize = 22f;
            lvlTxt.fontStyle = FontStyles.Bold;
            lvlTxt.alignment = TextAlignmentOptions.MidlineRight;
            lvlTxt.color = new Color(0.35f, 0.85f, 1f);
            lvlTxt.raycastTarget = false;
            lvlTxt.ApplyFont(UIFontCategory.Numbers);
            RectTransform ltRt = lvlObj.GetComponent<RectTransform>();
            ltRt.anchorMin = new Vector2(1f, 1f);
            ltRt.anchorMax = new Vector2(1f, 1f);
            ltRt.pivot = new Vector2(1f, 1f);
            ltRt.anchoredPosition = new Vector2(-18f, -10f);
            ltRt.sizeDelta = new Vector2(210f, 28f);

            // Upgrade Button (Placed below Level Badge on the right)
            GameObject upBtnObj = new GameObject("Button_Upgrade");
            upBtnObj.transform.SetParent(cardObj.transform, false);
            RectTransform ubRt = upBtnObj.AddComponent<RectTransform>();
            ubRt.anchorMin = new Vector2(1f, 0f);
            ubRt.anchorMax = new Vector2(1f, 0f);
            ubRt.pivot = new Vector2(1f, 0f);
            ubRt.anchoredPosition = new Vector2(-16f, 12f);
            ubRt.sizeDelta = new Vector2(196f, 76f);

            Image ubImg = upBtnObj.AddComponent<Image>();
            Button ubBtn = upBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(ubBtn, ubImg, CasualBtnStyle.Green);
            ubBtn.onClick.AddListener(() =>
            {
                if (PrestigeManager.Instance != null)
                {
                    PrestigeManager.Instance.TryUpgradeRelic(type, out _);
                }
            });

            GameObject uTxtObj = new GameObject("Text");
            uTxtObj.transform.SetParent(upBtnObj.transform, false);
            var uTxt = uTxtObj.AddComponent<TextMeshProUGUI>();
            uTxt.text = "UPGRADE\n1 Pearl";
            uTxt.fontSize = 20f;
            uTxt.fontStyle = FontStyles.Bold;
            uTxt.alignment = TextAlignmentOptions.Center;
            uTxt.color = Color.white;
            uTxt.raycastTarget = false;
            uTxt.ApplyFont(UIFontCategory.Button);
            RectTransform utRt = uTxtObj.GetComponent<RectTransform>();
            utRt.anchorMin = Vector2.zero;
            utRt.anchorMax = Vector2.one;
            utRt.sizeDelta = Vector2.zero;

            // Details Container (Center Column)
            GameObject dtObj = new GameObject("Details");
            dtObj.transform.SetParent(cardObj.transform, false);
            RectTransform dtRt = dtObj.AddComponent<RectTransform>();
            dtRt.anchorMin = new Vector2(0f, 0f);
            dtRt.anchorMax = new Vector2(1f, 1f);
            dtRt.offsetMin = new Vector2(118f, 10f);
            dtRt.offsetMax = new Vector2(-225f, -10f);

            // Title Text
            GameObject titleObj = new GameObject("Text_Title");
            titleObj.transform.SetParent(dtObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = PrestigeManager.GetRelicTitle(type);
            titleTxt.fontSize = 24f;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.TopLeft;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;
            titleTxt.ApplyFont(UIFontCategory.Header);
            RectTransform ttRt = titleObj.GetComponent<RectTransform>();
            ttRt.anchorMin = new Vector2(0f, 0.65f);
            ttRt.anchorMax = new Vector2(1f, 1f);
            ttRt.offsetMin = Vector2.zero;
            ttRt.offsetMax = Vector2.zero;

            // Current Buff Text
            GameObject cbObj = new GameObject("Text_CurrentBuff");
            cbObj.transform.SetParent(dtObj.transform, false);
            var cbTxt = cbObj.AddComponent<TextMeshProUGUI>();
            cbTxt.text = PrestigeManager.Instance != null ? PrestigeManager.Instance.GetCurrentBonusText(type) : "";
            cbTxt.fontSize = 21f;
            cbTxt.fontStyle = FontStyles.Bold;
            cbTxt.alignment = TextAlignmentOptions.MidlineLeft;
            cbTxt.raycastTarget = false;
            cbTxt.ApplyFont(UIFontCategory.Default);
            RectTransform cbtRt = cbObj.GetComponent<RectTransform>();
            cbtRt.anchorMin = new Vector2(0f, 0.32f);
            cbtRt.anchorMax = new Vector2(1f, 0.65f);
            cbtRt.offsetMin = Vector2.zero;
            cbtRt.offsetMax = Vector2.zero;

            // Next Buff Text
            GameObject nbObj = new GameObject("Text_NextBuff");
            nbObj.transform.SetParent(dtObj.transform, false);
            var nbTxt = nbObj.AddComponent<TextMeshProUGUI>();
            nbTxt.text = PrestigeManager.Instance != null ? PrestigeManager.Instance.GetNextBonusText(type) : "";
            nbTxt.fontSize = 19f;
            nbTxt.fontStyle = FontStyles.Normal;
            nbTxt.alignment = TextAlignmentOptions.BottomLeft;
            nbTxt.color = new Color(0.70f, 0.80f, 0.90f);
            nbTxt.raycastTarget = false;
            nbTxt.ApplyFont(UIFontCategory.Default);
            RectTransform nbtRt = nbObj.GetComponent<RectTransform>();
            nbtRt.anchorMin = new Vector2(0f, 0f);
            nbtRt.anchorMax = new Vector2(1f, 0.32f);
            nbtRt.offsetMin = Vector2.zero;
            nbtRt.offsetMax = Vector2.zero;

            return new RelicCardUI
            {
                relicType = type,
                cardObj = cardObj,
                titleText = titleTxt,
                levelText = lvlTxt,
                currentBuffText = cbTxt,
                nextBuffText = nbTxt,
                upgradeBtn = ubBtn,
                upgradeBtnBg = ubImg,
                upgradeBtnText = uTxt
            };
        }

        private void BuildVoyageTabContent(Transform parent)
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            voyageTabContainer = new GameObject("Tab_VoyageContent");
            voyageTabContainer.transform.SetParent(parent, false);
            RectTransform rt = voyageTabContainer.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(36f, 24f);
            rt.offsetMax = new Vector2(-36f, -212f);

            // 1. Current Harbor Status Card
            GameObject hCard = new GameObject("Card_HarborStatus");
            hCard.transform.SetParent(voyageTabContainer.transform, false);
            RectTransform hcRt = hCard.AddComponent<RectTransform>();
            hcRt.anchorMin = new Vector2(0.5f, 1f);
            hcRt.anchorMax = new Vector2(0.5f, 1f);
            hcRt.pivot = new Vector2(0.5f, 1f);
            hcRt.anchoredPosition = new Vector2(0f, 0f);
            hcRt.sizeDelta = new Vector2(780f, 120f);

            Image hcImg = hCard.AddComponent<Image>();
            hcImg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            hcImg.type = Image.Type.Sliced;
            hcImg.color = new Color(0.08f, 0.14f, 0.22f, 0.95f);

            GameObject hTxtObj = new GameObject("Text");
            hTxtObj.transform.SetParent(hCard.transform, false);
            harborStatusText = hTxtObj.AddComponent<TextMeshProUGUI>();
            harborStatusText.text = "CURRENT HARBOR • LEVEL 1: TEMPERATE BAY\n<color=#FCD34D>0 G</color> Coins  •  <color=#38BDF8>0</color> Pier Upgrades";
            harborStatusText.fontSize = 24f;
            harborStatusText.fontStyle = FontStyles.Normal;
            harborStatusText.alignment = TextAlignmentOptions.Center;
            harborStatusText.color = Color.white;
            harborStatusText.ApplyFont(UIFontCategory.Header);
            RectTransform htRt = hTxtObj.GetComponent<RectTransform>();
            htRt.anchorMin = Vector2.zero;
            htRt.anchorMax = Vector2.one;
            htRt.sizeDelta = Vector2.zero;

            // 2. Big Harvest Pearled Plinth
            GameObject pCard = new GameObject("Card_HarvestPlinth");
            pCard.transform.SetParent(voyageTabContainer.transform, false);
            RectTransform pcRt = pCard.AddComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0.5f, 1f);
            pcRt.anchorMax = new Vector2(0.5f, 1f);
            pcRt.pivot = new Vector2(0.5f, 1f);
            pcRt.anchoredPosition = new Vector2(0f, -140f);
            pcRt.sizeDelta = new Vector2(780f, 150f);

            Image pcImg = pCard.AddComponent<Image>();
            pcImg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            pcImg.type = Image.Type.Sliced;
            pcImg.color = new Color(0.04f, 0.08f, 0.15f, 0.98f);

            GameObject epTxtObj = new GameObject("Text");
            epTxtObj.transform.SetParent(pCard.transform, false);
            earnablePearlsText = epTxtObj.AddComponent<TextMeshProUGUI>();
            earnablePearlsText.text = "HARVEST ON VOYAGE:\n<size=38><color=#FCD34D><b>+0 ANCIENT PEARLS</b></color></size>";
            earnablePearlsText.fontSize = 26f;
            earnablePearlsText.fontStyle = FontStyles.Bold;
            earnablePearlsText.alignment = TextAlignmentOptions.Center;
            earnablePearlsText.color = new Color(0.85f, 0.92f, 1f);
            earnablePearlsText.ApplyFont(UIFontCategory.Header);
            RectTransform eptRt = epTxtObj.GetComponent<RectTransform>();
            eptRt.anchorMin = Vector2.zero;
            eptRt.anchorMax = Vector2.one;
            eptRt.sizeDelta = Vector2.zero;

            // 3. Voyage Permanence Rules Card
            GameObject rCard = new GameObject("Card_Rules");
            rCard.transform.SetParent(voyageTabContainer.transform, false);
            RectTransform rcRt = rCard.AddComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(0.5f, 1f);
            rcRt.anchorMax = new Vector2(0.5f, 1f);
            rcRt.pivot = new Vector2(0.5f, 1f);
            rcRt.anchoredPosition = new Vector2(0f, -310f);
            rcRt.sizeDelta = new Vector2(780f, 260f);

            Image rcImg = rCard.AddComponent<Image>();
            rcImg.sprite = CasualUITheme.CardRound12 ?? whiteSp;
            rcImg.type = Image.Type.Sliced;
            rcImg.color = new Color(0.06f, 0.10f, 0.16f, 0.95f);

            GameObject rTxtObj = new GameObject("Text");
            rTxtObj.transform.SetParent(rCard.transform, false);
            expeditionSummaryText = rTxtObj.AddComponent<TextMeshProUGUI>();
            expeditionSummaryText.text = "VOYAGE RULES";
            expeditionSummaryText.fontSize = 21f;
            expeditionSummaryText.fontStyle = FontStyles.Normal;
            expeditionSummaryText.alignment = TextAlignmentOptions.MidlineLeft;
            expeditionSummaryText.color = Color.white;
            expeditionSummaryText.ApplyFont(UIFontCategory.Default);
            RectTransform rtRt = rTxtObj.GetComponent<RectTransform>();
            rtRt.anchorMin = Vector2.zero;
            rtRt.anchorMax = Vector2.one;
            rtRt.offsetMin = new Vector2(24f, 16f);
            rtRt.offsetMax = new Vector2(-24f, -16f);

            // 4. Big Golden CTA Button [SET SAIL ON EXPEDITION]
            GameObject btnObj = new GameObject("Button_SetSail");
            btnObj.transform.SetParent(voyageTabContainer.transform, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 18f);
            bRt.sizeDelta = new Vector2(680f, 88f);

            setSailBtnBg = btnObj.AddComponent<Image>();
            setSailBtn = btnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(setSailBtn, setSailBtnBg, CasualBtnStyle.Yellow);
            setSailBtn.onClick.AddListener(OnSetSailClicked);

            GameObject sTxtObj = new GameObject("Text");
            sTxtObj.transform.SetParent(btnObj.transform, false);
            setSailBtnText = sTxtObj.AddComponent<TextMeshProUGUI>();
            setSailBtnText.text = "SET SAIL ON EXPEDITION (+0 Pearls)";
            setSailBtnText.fontSize = 28f;
            setSailBtnText.fontStyle = FontStyles.Bold;
            setSailBtnText.alignment = TextAlignmentOptions.Center;
            setSailBtnText.color = new Color(0.20f, 0.12f, 0.02f);
            setSailBtnText.ApplyFont(UIFontCategory.Button);
            RectTransform stRt = sTxtObj.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.sizeDelta = Vector2.zero;

            voyageTabContainer.SetActive(false);
        }

        private void BuildConfirmationDialog(Transform parent)
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            confirmDialogObj = new GameObject("Dialog_ExpeditionConfirmation");
            confirmDialogObj.transform.SetParent(parent, false);
            RectTransform cRt = confirmDialogObj.AddComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero;
            cRt.anchorMax = Vector2.one;
            cRt.sizeDelta = Vector2.zero;

            // Semi-dark blocker
            Image bg = confirmDialogObj.AddComponent<Image>();
            bg.sprite = CasualUITheme.DimmedBg ?? whiteSp;
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            // Dialog Box
            GameObject box = new GameObject("Box");
            box.transform.SetParent(confirmDialogObj.transform, false);
            RectTransform bRt = box.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.5f);
            bRt.anchorMax = new Vector2(0.5f, 0.5f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.sizeDelta = new Vector2(680f, 520f);

            Image bImg = box.AddComponent<Image>();
            CasualUITheme.StylePopupDialog(bImg);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(box.transform, false);
            var title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "SET SAIL CONFIRMATION";
            title.fontSize = 30f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.85f, 0.25f);
            title.ApplyFont(UIFontCategory.Header);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -24f);
            tRt.sizeDelta = new Vector2(600f, 48f);

            // Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(box.transform, false);
            confirmDescText = descObj.AddComponent<TextMeshProUGUI>();
            confirmDescText.text = "Are you ready to set sail into uncharted waters?";
            confirmDescText.fontSize = 23f;
            confirmDescText.fontStyle = FontStyles.Normal;
            confirmDescText.alignment = TextAlignmentOptions.Center;
            confirmDescText.color = Color.white;
            confirmDescText.ApplyFont(UIFontCategory.Default);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 1f);
            dRt.offsetMin = new Vector2(36f, 120f);
            dRt.offsetMax = new Vector2(-36f, -80f);

            // Button Yes: [CONFIRM & SET SAIL]
            GameObject yesBtnObj = new GameObject("Btn_Yes");
            yesBtnObj.transform.SetParent(box.transform, false);
            RectTransform yRt = yesBtnObj.AddComponent<RectTransform>();
            yRt.anchorMin = new Vector2(0.5f, 0f);
            yRt.anchorMax = new Vector2(0.5f, 0f);
            yRt.pivot = new Vector2(0.5f, 0f);
            yRt.anchoredPosition = new Vector2(140f, 24f);
            yRt.sizeDelta = new Vector2(240f, 68f);

            Image yImg = yesBtnObj.AddComponent<Image>();
            confirmYesBtn = yesBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(confirmYesBtn, yImg, CasualBtnStyle.Yellow);
            confirmYesBtn.onClick.AddListener(OnConfirmExpeditionYes);

            GameObject yTxtObj = new GameObject("Text");
            yTxtObj.transform.SetParent(yesBtnObj.transform, false);
            var yTxt = yTxtObj.AddComponent<TextMeshProUGUI>();
            yTxt.text = "SET SAIL!";
            yTxt.fontSize = 26f;
            yTxt.fontStyle = FontStyles.Bold;
            yTxt.alignment = TextAlignmentOptions.Center;
            yTxt.color = new Color(0.2f, 0.12f, 0.02f);
            yTxt.ApplyFont(UIFontCategory.Button);
            RectTransform ytRt = yTxtObj.GetComponent<RectTransform>();
            ytRt.anchorMin = Vector2.zero;
            ytRt.anchorMax = Vector2.one;
            ytRt.sizeDelta = Vector2.zero;

            // Button No: [CANCEL]
            GameObject noBtnObj = new GameObject("Btn_No");
            noBtnObj.transform.SetParent(box.transform, false);
            RectTransform nRt = noBtnObj.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0.5f, 0f);
            nRt.anchorMax = new Vector2(0.5f, 0f);
            nRt.pivot = new Vector2(0.5f, 0f);
            nRt.anchoredPosition = new Vector2(-140f, 24f);
            nRt.sizeDelta = new Vector2(240f, 68f);

            Image nImg = noBtnObj.AddComponent<Image>();
            confirmNoBtn = noBtnObj.AddComponent<Button>();
            CasualUITheme.StyleButton(confirmNoBtn, nImg, CasualBtnStyle.Dark);
            confirmNoBtn.onClick.AddListener(OnConfirmExpeditionNo);

            GameObject nTxtObj = new GameObject("Text");
            nTxtObj.transform.SetParent(noBtnObj.transform, false);
            var nTxt = nTxtObj.AddComponent<TextMeshProUGUI>();
            nTxt.text = "CANCEL";
            nTxt.fontSize = 26f;
            nTxt.fontStyle = FontStyles.Bold;
            nTxt.alignment = TextAlignmentOptions.Center;
            nTxt.color = Color.white;
            nTxt.ApplyFont(UIFontCategory.Button);
            RectTransform ntRt = nTxtObj.GetComponent<RectTransform>();
            ntRt.anchorMin = Vector2.zero;
            ntRt.anchorMax = Vector2.one;
            ntRt.sizeDelta = Vector2.zero;
        }
        #endregion
    }
}

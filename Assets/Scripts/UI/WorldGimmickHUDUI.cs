using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Gimmicks;
using IdleFishing.UI;

namespace IdleFishing.UI
{
    /// <summary>
    /// HUD Widget hiển thị trạng thái và cho phép tương tác nhanh 1-chạm với Cơ Chế Đặc Quyền của từng Vùng:
    /// - Level 2: Tiến độ Ngọc Trai & Đếm ngược Pearl Frenzy
    /// - Level 3: Tiến độ Bếp Nướng Cá BBQ (Bấm để quạt than tăng tốc)
    /// - Level 4: Trạng thái Bão Tuyết & Thanh Máu Tảng Băng (Bấm để đập băng cứu bến)
    /// - Level 5: Đơn Hàng Thương Gia Sa Mạc & Bộ Cổ Vật Thần Bí
    /// </summary>
    public class WorldGimmickHUDUI : MonoBehaviour
    {
        public static WorldGimmickHUDUI Instance { get; private set; }
        public bool IsVisible => containerObj != null && containerObj.activeSelf;

        private GameObject containerObj;
        private RectTransform containerRect;
        private Image bgImage;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI descText;
        private Button actionButton;

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
            BuildProceduralHUD();
            if (WorldGimmickManager.Instance != null)
            {
                WorldGimmickManager.Instance.OnGimmickStateChanged += RefreshHUD;
            }
            RefreshHUD();
        }

        private void OnDestroy()
        {
            if (WorldGimmickManager.Instance != null)
            {
                WorldGimmickManager.Instance.OnGimmickStateChanged -= RefreshHUD;
            }
        }

        private void Update()
        {
            RefreshHUD();
        }

        private void BuildProceduralHUD()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Root Container (Docked to LeftQuestEventDockUI)
            Transform parentContainer = (LeftQuestEventDockUI.Instance != null)
                ? LeftQuestEventDockUI.Instance.ContentContainer
                : transform;

            containerObj = new GameObject("WorldGimmick_HUD_Widget");
            containerObj.transform.SetParent(parentContainer, false);
            containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(0f, 1f);
            containerRect.pivot = new Vector2(0f, 1f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(540f, 58f);

            var le = containerObj.AddComponent<LayoutElement>();
            le.preferredWidth = 540f;
            le.preferredHeight = 58f;
            le.minHeight = 58f;

            bgImage = containerObj.AddComponent<Image>();
            CasualUITheme.StyleCard(bgImage, new Color(0.10f, 0.14f, 0.22f, 0.95f));

            actionButton = containerObj.AddComponent<Button>();
            actionButton.targetGraphic = bgImage;
            actionButton.onClick.AddListener(OnWidgetClicked);

            // 2. Title Text (Top half)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(containerObj.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 13f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.color = new Color(1f, 0.88f, 0.35f);
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.raycastTarget = false;
            titleText.ApplyFont(UIFontCategory.Header);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.48f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.anchoredPosition = new Vector2(10f, 0f);
            tRt.sizeDelta = new Vector2(-20f, 0f);

            // 3. Desc Text (Bottom half)
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(containerObj.transform, false);
            descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.fontSize = 11.5f;
            descText.fontStyle = FontStyles.Normal;
            descText.alignment = TextAlignmentOptions.MidlineLeft;
            descText.color = new Color(0.85f, 0.90f, 0.98f);
            descText.textWrappingMode = TextWrappingModes.NoWrap;
            descText.raycastTarget = false;
            descText.ApplyFont(UIFontCategory.Default);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 0.52f);
            dRt.pivot = new Vector2(0f, 0.5f);
            dRt.anchoredPosition = new Vector2(10f, 0f);
            dRt.sizeDelta = new Vector2(-20f, 0f);
        }

        public void RefreshHUD()
        {
            if (WorldGimmickManager.Instance == null)
            {
                if (containerObj != null) containerObj.SetActive(false);
                return;
            }

            string worldId = WorldGimmickManager.Instance.CurrentWorldId;

            if (worldId == "world_1")
            {
                // Level 1: Bến câu cơ bản, ẩn widget để màn hình thoáng đãng
                if (containerObj != null) containerObj.SetActive(false);
                return;
            }

            if (containerObj != null && !containerObj.activeSelf)
            {
                containerObj.SetActive(true);
            }

            if (worldId == "world_2")
            {
                var coral = WorldGimmickManager.Instance.CoralGimmick;
                if (coral != null)
                {
                    if (coral.IsPearlFrenzyActive)
                    {
                        bgImage.color = new Color(0.12f, 0.45f, 0.28f, 0.95f);
                        titleText.text = "[*] PEARL FRENZY (3x GOLD)!";
                        descText.text = $"Remaining: <b>{coral.FrenzyRemainingTime:0.0}s</b> (3x Sell Value)";
                    }
                    else if (coral.IsClamOpen)
                    {
                        bgImage.color = new Color(0.48f, 0.35f, 0.12f, 0.95f);
                        titleText.text = "[SHELL] CLAM OPEN! TAP TO COLLECT";
                        descText.text = $"Pearls: {coral.CurrentPearls}/3 • Tap now!";
                    }
                    else
                    {
                        bgImage.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
                        titleText.text = "[SHELL] GIANT CLAM (CORAL)";
                        descText.text = $"Pearls: {coral.CurrentPearls}/3 • Opens in: {coral.NextOpenTimer:0}s";
                    }
                }
            }
            else if (worldId == "world_3")
            {
                var maple = WorldGimmickManager.Instance.MapleGimmick;
                if (maple != null)
                {
                    bgImage.color = new Color(0.45f, 0.20f, 0.10f, 0.95f);
                    titleText.text = "[BBQ] SMOKER GRILL (4x VALUE)";
                    int pct = Mathf.RoundToInt(maple.CurrentProgress * 100f);
                    descText.text = $"Cooking: <b>{pct}%</b> (Tap to Fan Flames!)";
                }
            }
            else if (worldId == "world_4")
            {
                var arctic = WorldGimmickManager.Instance.ArcticGimmick;
                if (arctic != null)
                {
                    if (arctic.IsRescueBuffActive)
                    {
                        bgImage.color = new Color(0.10f, 0.38f, 0.48f, 0.95f);
                        titleText.text = "[*] 2x FISHING SPEED (BUFF)";
                        descText.text = $"Active: <b>{arctic.RescueBuffTimer:0.0}s</b>";
                    }
                    else if (arctic.IsBlizzardActive)
                    {
                        bgImage.color = new Color(0.48f, 0.12f, 0.15f, 0.95f);
                        titleText.text = "[ICE] BLIZZARD! BREAK ICE TO RESCUE!";
                        descText.text = $"Ice Health: <b>{arctic.IceCurrentHp}/{arctic.IceMaxHp} HP</b> (Tap!)";
                    }
                    else
                    {
                        bgImage.color = new Color(0.08f, 0.14f, 0.20f, 0.92f);
                        titleText.text = "[ICE] ARCTIC BAY (FREEZING)";
                        descText.text = $"Blizzard incoming in: {arctic.NextBlizzardTimer:0}s";
                    }
                }
            }
            else if (worldId == "world_5")
            {
                var desert = WorldGimmickManager.Instance.DesertGimmick;
                if (desert != null)
                {
                    bgImage.color = new Color(0.38f, 0.28f, 0.10f, 0.95f);
                    string sunIco = desert.HasSunRelic ? "*" : "-";
                    string scaIco = desert.HasScarabRelic ? "#" : "-";
                    string lmpIco = desert.HasLampRelic ? "+" : "-";
                    titleText.text = $"[CARAVAN] DESERT MERCHANTS [{sunIco}{scaIco}{lmpIco}]";
                    descText.text = $"Delivered: <b>{desert.CurrentDeliveredFish}/{desert.RequiredFishPerOrder}</b> (Tap to deliver)";
                }
            }
        }

        private void OnWidgetClicked()
        {
            if (WorldGimmickManager.Instance == null) return;
            string worldId = WorldGimmickManager.Instance.CurrentWorldId;

            if (worldId == "world_2")
            {
                WorldGimmickManager.Instance.CoralGimmick?.TryCollectPearl();
            }
            else if (worldId == "world_3")
            {
                WorldGimmickManager.Instance.MapleGimmick?.TapStokeFire();
            }
            else if (worldId == "world_4")
            {
                WorldGimmickManager.Instance.ArcticGimmick?.TapBreakIce();
            }
            else if (worldId == "world_5")
            {
                WorldGimmickManager.Instance.DesertGimmick?.TapCaravan();
            }
        }
    }
}

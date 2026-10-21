using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;

namespace IdleFishing.UI
{
    public class FloatingHealthBarUI : MonoBehaviour
    {
        [Header("Offset")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 1.6f, 0);

        [Header("Colors")]
        [SerializeField] private Color healthColor = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color hurtColor = new Color(0.95f, 0.25f, 0.2f);
        [SerializeField] private Color ghostHurtColor = new Color(1.0f, 0.8f, 0.2f, 0.8f);
        [SerializeField] private Color knockedOutColor = new Color(0.95f, 0.75f, 0.1f);

        private Transform targetTransform;
        private Canvas canvas;
        private Image fillImage;
        private Image ghostFillImage;
        private Image bgImage;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI hpText;
        private TextMeshProUGUI statusBadge;
        private Camera mainCamera;

        private float targetFill = 1.0f;
        private float ghostFill = 1.0f;
        private static Sprite cachedWhiteSprite;

        private bool isBossBar = false;
        private RectTransform bossIconRect;
        private static Sprite cachedBossIconSprite;
        private static readonly Color bossCrimsonColor = new Color(0.94f, 0.14f, 0.14f, 1f);

        private Vector3 baseScale = Vector3.one * 0.015f;
        private Coroutine punchCoroutine;

        public Vector3 BaseScale => baseScale;

        public static Sprite GetWhiteSprite()
        {
            if (cachedWhiteSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return cachedWhiteSprite;
        }

        public static Sprite GetBossIconSprite()
        {
            if (cachedBossIconSprite != null) return cachedBossIconSprite;

#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_boss_crest.png");
            if (asset != null)
            {
                cachedBossIconSprite = asset;
                return cachedBossIconSprite;
            }
#endif
            return GetWhiteSprite();
        }

        public void Setup(Transform target, string displayName, Vector3 offset, Color barColor, bool isBoss = false)
        {
            targetTransform = target;
            worldOffset = offset;
            healthColor = barColor;
            isBossBar = isBoss || (!string.IsNullOrEmpty(displayName) && displayName.ToUpper().Contains("BOSS"));
            mainCamera = Camera.main;

            BuildProceduralUI(displayName);
        }

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        public void TriggerHitPunch()
        {
            if (!gameObject.activeInHierarchy) return;
            if (punchCoroutine != null)
            {
                StopCoroutine(punchCoroutine);
            }
            punchCoroutine = StartCoroutine(HitPunchRoutine());
        }

        private System.Collections.IEnumerator HitPunchRoutine()
        {
            float punchMult = isBossBar ? 1.18f : 1.12f;
            transform.localScale = baseScale * punchMult;

            float duration = 0.16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Elastic bounce back to baseScale
                transform.localScale = Vector3.Lerp(baseScale * punchMult, baseScale, t);
                yield return null;
            }

            transform.localScale = baseScale;
            punchCoroutine = null;
        }

        private void Update()
        {
            // Smoothly decrease ghost fill bar for satisfying damage feedback
            if (ghostFillImage != null && ghostFill > targetFill)
            {
                ghostFill = Mathf.MoveTowards(ghostFill, targetFill, Time.deltaTime * 0.75f);
                ghostFillImage.fillAmount = ghostFill;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = targetFill;

                // Boss enrage critical flash (< 28% HP)
                if (isBossBar && targetFill <= 0.28f)
                {
                    float flash = (Mathf.Sin(Time.time * 12f) + 1f) * 0.5f;
                    fillImage.color = Color.Lerp(bossCrimsonColor, new Color(1f, 0.45f, 0.1f), flash);
                }
            }

            // Animate breathing pulse on Boss Crest Icon
            if (isBossBar && bossIconRect != null)
            {
                float iconPulse = 1.0f + Mathf.Sin(Time.time * 4.5f) * 0.05f;
                bossIconRect.localScale = new Vector3(iconPulse, iconPulse, 1.0f);
            }
        }

        private void LateUpdate()
        {
            if (targetTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = targetTransform.position + worldOffset;
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }

            // Guarantee scale never remains drifted or stuck when not punching
            if (punchCoroutine == null && transform.localScale != baseScale)
            {
                transform.localScale = baseScale;
            }
        }

        public void UpdateHealth(int current, int max)
        {
            targetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

            if (fillImage != null)
            {
                if (isBossBar)
                {
                    if (CasualUITheme.SliderFillRed != null)
                    {
                        fillImage.sprite = CasualUITheme.SliderFillRed;
                        fillImage.color = targetFill <= 0.28f ? new Color(1f, 0.45f, 0.1f) : Color.white;
                    }
                    else
                    {
                        fillImage.color = targetFill <= 0.28f ? new Color(1f, 0.45f, 0.1f) : bossCrimsonColor;
                    }
                }
                else
                {
                    if (CasualUITheme.SliderFillGreen != null && CasualUITheme.SliderFillRed != null)
                    {
                        fillImage.sprite = targetFill > 0.35f ? CasualUITheme.SliderFillGreen : CasualUITheme.SliderFillRed;
                        fillImage.color = Color.white;
                    }
                    else
                    {
                        fillImage.color = targetFill > 0.35f ? healthColor : hurtColor;
                    }
                }
            }

            if (hpText != null)
            {
                hpText.text = $"{NumberFormatter.Format(current)} / {NumberFormatter.Format(max)} HP";
            }
        }

        public void SetStatusText(string status, bool isKnockedOut = false)
        {
            if (statusBadge != null)
            {
                bool hasStatus = !string.IsNullOrEmpty(status);
                statusBadge.gameObject.SetActive(hasStatus);
                statusBadge.text = status;

                if (isKnockedOut && fillImage != null)
                {
                    fillImage.color = knockedOutColor;
                }
            }
        }

        private void BuildProceduralUI(string displayName)
        {
            Sprite whiteSp = GetWhiteSprite();

            // Canvas Setup
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 20;

            RectTransform canvasRect = GetComponent<RectTransform>();

            if (isBossBar)
            {
                // Grand, prominent Boss health bar
                canvasRect.sizeDelta = new Vector2(210, 48);
                baseScale = Vector3.one * 0.016f;
                transform.localScale = baseScale;

                // 1. Gilded / Imperial Gold Frame
                GameObject frameObj = new GameObject("BossBar_Frame");
                frameObj.transform.SetParent(transform, false);
                Image frameImg = frameObj.AddComponent<Image>();
                frameImg.sprite = whiteSp;
                frameImg.color = new Color(0.96f, 0.78f, 0.22f, 1.0f); // Imperial Gold border
                frameImg.raycastTarget = false;
                RectTransform frameRect = frameObj.GetComponent<RectTransform>();
                frameRect.sizeDelta = new Vector2(185, 20);
                frameRect.anchoredPosition = new Vector2(0, 0);

                // 2. Obsidian Dark Background
                GameObject bgObj = new GameObject("BossBar_BG");
                bgObj.transform.SetParent(frameObj.transform, false);
                bgImage = bgObj.AddComponent<Image>();
                bgImage.sprite = whiteSp;
                bgImage.color = new Color(0.10f, 0.08f, 0.10f, 0.98f);
                bgImage.raycastTarget = false;
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = new Vector2(2, 2);
                bgRect.offsetMax = new Vector2(-2, -2);

                // 3. Ghost Fill Bar (Brilliant flash chunk on hit)
                GameObject ghostObj = new GameObject("BossBar_GhostFill");
                ghostObj.transform.SetParent(bgObj.transform, false);
                ghostFillImage = ghostObj.AddComponent<Image>();
                ghostFillImage.sprite = whiteSp;
                ghostFillImage.color = new Color(1f, 0.85f, 0.28f, 0.95f);
                ghostFillImage.raycastTarget = false;
                ghostFillImage.type = Image.Type.Filled;
                ghostFillImage.fillMethod = Image.FillMethod.Horizontal;
                ghostFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                ghostFillImage.fillAmount = 1.0f;
                RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
                ghostRect.anchorMin = Vector2.zero;
                ghostRect.anchorMax = Vector2.one;
                ghostRect.offsetMin = Vector2.zero;
                ghostRect.offsetMax = Vector2.zero;

                // 4. Primary Boss Fill Bar (Menacing Royal Crimson)
                GameObject fillObj = new GameObject("BossBar_Fill");
                fillObj.transform.SetParent(bgObj.transform, false);
                fillImage = fillObj.AddComponent<Image>();
                fillImage.sprite = whiteSp;
                fillImage.color = bossCrimsonColor;
                fillImage.raycastTarget = false;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.fillAmount = 1.0f;
                RectTransform fillRect = fillObj.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                // 5. Segment Dividers / Phase Tick Marks (25%, 50%, 75%)
                float[] tickFractions = new float[] { 0.25f, 0.50f, 0.75f };
                for (int t = 0; t < tickFractions.Length; t++)
                {
                    GameObject tickObj = new GameObject($"BossBar_Tick_{t}");
                    tickObj.transform.SetParent(bgObj.transform, false);
                    Image tickImg = tickObj.AddComponent<Image>();
                    tickImg.sprite = whiteSp;
                    tickImg.color = new Color(0.18f, 0.12f, 0.15f, 0.85f);
                    tickImg.raycastTarget = false;
                    RectTransform tickRect = tickObj.GetComponent<RectTransform>();
                    tickRect.anchorMin = new Vector2(tickFractions[t], 0f);
                    tickRect.anchorMax = new Vector2(tickFractions[t], 1f);
                    tickRect.sizeDelta = new Vector2(2f, 0f);
                    tickRect.anchoredPosition = Vector2.zero;
                }

                // 6. Boss Icon Badge on top
                GameObject iconObj = new GameObject("Boss_Crest_Icon");
                iconObj.transform.SetParent(transform, false);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = GetBossIconSprite();
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
                bossIconRect = iconObj.GetComponent<RectTransform>();
                bossIconRect.sizeDelta = new Vector2(28, 28);
                bossIconRect.anchoredPosition = new Vector2(0, 18);

                // 7. Boss HP numbers text
                GameObject hpObj = new GameObject("Boss_HPText");
                hpObj.transform.SetParent(bgObj.transform, false);
                hpText = hpObj.AddComponent<TextMeshProUGUI>();
                hpText.fontSize = 12;
                hpText.fontStyle = FontStyles.Bold;
                hpText.alignment = TextAlignmentOptions.Center;
                hpText.color = Color.white;
                hpText.raycastTarget = false;
                hpText.ApplyFont(UIFontCategory.Button);
                RectTransform hpRect = hpObj.GetComponent<RectTransform>();
                hpRect.anchorMin = Vector2.zero;
                hpRect.anchorMax = Vector2.one;
                hpRect.offsetMin = Vector2.zero;
                hpRect.offsetMax = Vector2.zero;
            }
            else
            {
                // Standard Health Bar for regular fish / workers
                canvasRect.sizeDelta = new Vector2(160, 45);
                baseScale = Vector3.one * 0.015f;
                transform.localScale = baseScale;

                // Background Bar
                GameObject bgObj = new GameObject("HealthBar_BG");
                bgObj.transform.SetParent(transform, false);
                bgImage = bgObj.AddComponent<Image>();
                if (CasualUITheme.SliderBg != null)
                {
                    bgImage.sprite = CasualUITheme.SliderBg;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.color = Color.white;
                }
                else
                {
                    bgImage.sprite = whiteSp;
                    bgImage.color = new Color(0.08f, 0.08f, 0.10f, 0.90f);
                }
                bgImage.raycastTarget = false;
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.sizeDelta = new Vector2(140, 18);
                bgRect.anchoredPosition = new Vector2(0, 0);

                // Ghost Fill Bar
                GameObject ghostObj = new GameObject("HealthBar_GhostFill");
                ghostObj.transform.SetParent(bgObj.transform, false);
                ghostFillImage = ghostObj.AddComponent<Image>();
                ghostFillImage.sprite = CasualUITheme.SliderFillYellow != null ? CasualUITheme.SliderFillYellow : whiteSp;
                ghostFillImage.color = CasualUITheme.SliderFillYellow != null ? Color.white : ghostHurtColor;
                ghostFillImage.raycastTarget = false;
                ghostFillImage.type = Image.Type.Filled;
                ghostFillImage.fillMethod = Image.FillMethod.Horizontal;
                ghostFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                ghostFillImage.fillAmount = 1.0f;
                RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
                ghostRect.anchorMin = Vector2.zero;
                ghostRect.anchorMax = Vector2.one;
                ghostRect.offsetMin = new Vector2(2, 2);
                ghostRect.offsetMax = new Vector2(-2, -2);

                // Primary Fill Bar
                GameObject fillObj = new GameObject("HealthBar_Fill");
                fillObj.transform.SetParent(bgObj.transform, false);
                fillImage = fillObj.AddComponent<Image>();
                fillImage.sprite = CasualUITheme.SliderFillGreen != null ? CasualUITheme.SliderFillGreen : whiteSp;
                fillImage.color = Color.white;
                fillImage.raycastTarget = false;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.fillAmount = 1.0f;
                RectTransform fillRect = fillObj.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = new Vector2(2, 2);
                fillRect.offsetMax = new Vector2(-2, -2);

                // Title Text for Chest/Special targets only
                bool isSpecialTarget = !string.IsNullOrEmpty(displayName) && displayName.ToUpper().Contains("CHEST");
                GameObject titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(transform, false);
                titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = displayName;
                titleText.fontSize = 18;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = Color.white;
                titleText.raycastTarget = false;
                titleText.ApplyFont(UIFontCategory.Header);
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.sizeDelta = new Vector2(200, 22);
                titleRect.anchoredPosition = new Vector2(0, 18);
                titleObj.SetActive(isSpecialTarget);
            }

            // Status Badge (e.g. "KNOCKED OUT")
            GameObject badgeObj = new GameObject("StatusBadge");
            badgeObj.transform.SetParent(transform, false);
            statusBadge = badgeObj.AddComponent<TextMeshProUGUI>();
            statusBadge.fontSize = 16;
            statusBadge.fontStyle = FontStyles.Bold;
            statusBadge.alignment = TextAlignmentOptions.Center;
            statusBadge.color = new Color(1f, 0.85f, 0.1f);
            statusBadge.raycastTarget = false;
            statusBadge.ApplyFont(UIFontCategory.Button);
            RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
            badgeRect.sizeDelta = new Vector2(200, 24);
            badgeRect.anchoredPosition = new Vector2(0, -22);
            badgeObj.SetActive(false);
        }
    }
}

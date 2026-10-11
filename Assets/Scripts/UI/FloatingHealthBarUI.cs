using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        public static Sprite GetWhiteSprite()
        {
            if (cachedWhiteSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return cachedWhiteSprite;
        }

        public void Setup(Transform target, string displayName, Vector3 offset, Color barColor)
        {
            targetTransform = target;
            worldOffset = offset;
            healthColor = barColor;
            mainCamera = Camera.main;

            BuildProceduralUI(displayName);
        }

        private void Awake()
        {
            mainCamera = Camera.main;
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
        }

        public void UpdateHealth(int current, int max)
        {
            targetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

            if (fillImage != null)
            {
                fillImage.color = targetFill > 0.35f ? healthColor : hurtColor;
            }

            if (hpText != null)
            {
                hpText.text = $"{current} / {max} HP";
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

            // Canvas
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 20;

            RectTransform canvasRect = GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(160, 45);
            transform.localScale = Vector3.one * 0.015f; // Tăng kích thước thanh máu cho dễ nhìn

            // Background Bar
            GameObject bgObj = new GameObject("HealthBar_BG");
            bgObj.transform.SetParent(transform, false);
            bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = whiteSp;
            bgImage.color = new Color(0.08f, 0.08f, 0.10f, 0.90f);
            bgImage.raycastTarget = false;
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(140, 16);
            bgRect.anchoredPosition = new Vector2(0, 0);

            // Ghost Fill Bar (shows damage chunk before catching up)
            GameObject ghostObj = new GameObject("HealthBar_GhostFill");
            ghostObj.transform.SetParent(bgObj.transform, false);
            ghostFillImage = ghostObj.AddComponent<Image>();
            ghostFillImage.sprite = whiteSp;
            ghostFillImage.color = ghostHurtColor;
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
            fillImage.sprite = whiteSp;
            fillImage.color = healthColor;
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

            // Title / Name Text - Chỉ hiển thị cho BOSS hoặc RƯƠNG để tránh ngập màn hình chữ bé
            bool isSpecialTarget = !string.IsNullOrEmpty(displayName) && 
                (displayName.ToUpper().Contains("BOSS") || displayName.ToUpper().Contains("CHEST") || displayName.ToUpper().Contains("CHEST"));

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = displayName;
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = isSpecialTarget ? new Color(1f, 0.85f, 0.2f) : Color.white;
            titleText.raycastTarget = false;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(200, 22);
            titleRect.anchoredPosition = new Vector2(0, 18);
            titleObj.SetActive(isSpecialTarget);

            // HP Numbers Text - Chỉ hiển thị trên BOSS lớn, ẩn hoàn toàn trên rương và cá/thợ để giao diện gọn gàng
            GameObject hpObj = new GameObject("HPText");
            hpObj.transform.SetParent(bgObj.transform, false);
            hpText = hpObj.AddComponent<TextMeshProUGUI>();
            hpText.fontSize = 13;
            hpText.fontStyle = FontStyles.Bold;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.color = Color.white;
            hpText.raycastTarget = false;
            RectTransform hpRect = hpObj.GetComponent<RectTransform>();
            hpRect.anchorMin = Vector2.zero;
            hpRect.anchorMax = Vector2.one;
            hpRect.offsetMin = Vector2.zero;
            hpRect.offsetMax = Vector2.zero;
            bool isBoss = !string.IsNullOrEmpty(displayName) && displayName.ToUpper().Contains("BOSS");
            hpObj.SetActive(isBoss);

            // Status Badge (e.g. "KNOCKED OUT")
            GameObject badgeObj = new GameObject("StatusBadge");
            badgeObj.transform.SetParent(transform, false);
            statusBadge = badgeObj.AddComponent<TextMeshProUGUI>();
            statusBadge.fontSize = 16;
            statusBadge.fontStyle = FontStyles.Bold;
            statusBadge.alignment = TextAlignmentOptions.Center;
            statusBadge.color = new Color(1f, 0.85f, 0.1f);
            statusBadge.raycastTarget = false;
            RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
            badgeRect.sizeDelta = new Vector2(200, 24);
            badgeRect.anchoredPosition = new Vector2(0, -20);
            badgeObj.SetActive(false);
        }
    }
}

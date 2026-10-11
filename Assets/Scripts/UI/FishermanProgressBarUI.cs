using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Fishing;

namespace IdleFishing.UI
{
    public class FishermanProgressBarUI : MonoBehaviour
    {
        [Header("Target & Positioning")]
        [SerializeField] private FishermanWorker targetWorker;
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 2.0f, 0);

        [Header("Visual Colors")]
        [SerializeField] private Color castingColor = new Color(0.18f, 0.76f, 0.95f);       // Ocean Cyan
        [SerializeField] private Color hookedColor = new Color(0.20f, 0.95f, 0.45f);        // Vibrant Green/Gold
        [SerializeField] private Color crowdedColor = new Color(0.95f, 0.72f, 0.18f);       // Amber Orange

        private Canvas canvas;
        private Image fillImage;
        private Image bgImage;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI timeText;
        private Camera mainCamera;

        private float currentProgress = 0f;
        private float targetProgress = 0f;
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

        public void Setup(FishermanWorker worker, Vector3 offset)
        {
            targetWorker = worker;
            worldOffset = offset;
            mainCamera = Camera.main;

            BuildProceduralUI();
        }

        private void Awake()
        {
            mainCamera = Camera.main;
            if (targetWorker == null)
            {
                targetWorker = GetComponentInParent<FishermanWorker>();
            }
        }

        private void Start()
        {
            if (fillImage == null)
            {
                BuildProceduralUI();
            }
        }

        private void BuildProceduralUI()
        {
            Sprite whiteSp = GetWhiteSprite();

            // 1. World Space Canvas
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();

            var raycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (raycaster == null) gameObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(160, 48);
            transform.localScale = Vector3.one * 0.012f;

            // 2. Background Border & Container
            GameObject bgObj = new GameObject("ProgressBar_BG");
            bgObj.transform.SetParent(transform, false);
            bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = whiteSp;
            bgImage.color = new Color(0.10f, 0.13f, 0.18f, 0.88f); // Dark Slate

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(150, 20);
            bgRect.anchoredPosition = new Vector2(0, -4);

            // 3. Fill Bar
            GameObject fillObj = new GameObject("ProgressBar_Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            fillImage = fillObj.AddComponent<Image>();
            fillImage.sprite = whiteSp;
            fillImage.color = castingColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);

            // 4. Title / Status Text (Above Bar)
            GameObject titleObj = new GameObject("StatusText");
            titleObj.transform.SetParent(transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            titleText.raycastTarget = false;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(180, 24);
            titleRect.anchoredPosition = new Vector2(0, 18);
            titleObj.SetActive(false); // Mặc định ẩn khi đang câu bình thường

            // 5. Timer Text - Ẩn hoàn toàn vì micro-text gây rối mắt
            GameObject timeObj = new GameObject("TimeText");
            timeObj.transform.SetParent(bgObj.transform, false);
            timeText = timeObj.AddComponent<TextMeshProUGUI>();
            timeText.raycastTarget = false;
            timeObj.SetActive(false);
        }

        private void Update()
        {
            if (targetWorker == null) return;

            UpdateProgressAndVisuals();
        }

        private void UpdateProgressAndVisuals()
        {
            FishermanState state = targetWorker.State;
            targetProgress = targetWorker.CurrentCastProgress;

            // Smooth fill lerp
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * 3.0f);
            if (fillImage != null)
            {
                fillImage.fillAmount = currentProgress;
            }

            switch (state)
            {
                case FishermanState.Casting:
                    if (fillImage != null) fillImage.color = castingColor;
                    if (titleText != null) titleText.gameObject.SetActive(false); // Thanh nạp mượt mà, không cần chữ
                    break;

                case FishermanState.HookedReeling:
                    if (fillImage != null) fillImage.color = hookedColor;
                    if (titleText != null)
                    {
                        titleText.gameObject.SetActive(true);
                        titleText.text = "<color=#2ECC71>HOOKED!</color>";
                    }
                    break;

                case FishermanState.CrowdedWait:
                    if (fillImage != null) fillImage.color = crowdedColor;
                    if (titleText != null)
                    {
                        titleText.gameObject.SetActive(true);
                        titleText.text = "<color=#F39C12>PIER FULL!</color>";
                    }
                    break;

                case FishermanState.Idle:
                default:
                    if (titleText != null) titleText.gameObject.SetActive(false);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (targetWorker == null) return;

            transform.position = targetWorker.transform.position + worldOffset;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
        }
    }
}

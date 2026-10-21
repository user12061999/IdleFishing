using UnityEngine;
using UnityEngine.UI;

namespace IdleFishing.UI
{
    public enum SafeAreaFitMode
    {
        FullStretch,   // Fits full-screen panels from (0,0) to (1,1) within safe area
        TopHeader,     // Preserves top anchoring and shifts downward by notch height
        BottomFooter   // Preserves bottom anchoring and shifts upward by home bar height
    }

    /// <summary>
    /// Mobile Safe Area Fitter:
    /// - Fits UI elements inside Screen.safeArea without distorting local anchors.
    /// - Prevents notches, camera cutouts (Dynamic Island / hole punches), and bottom home bars from obscuring UI.
    /// - Operates smoothly across 16:9, 19.5:9, 20:9, and tablet aspect ratios.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform targetRect;
        private CanvasScaler canvasScaler;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;

        [Header("Fit Mode")]
        [SerializeField] private SafeAreaFitMode fitMode = SafeAreaFitMode.TopHeader;

        [Header("FullStretch Constraint Toggles (FullStretch mode only)")]
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;

        public SafeAreaFitMode FitMode
        {
            get => fitMode;
            set { fitMode = value; ApplySafeArea(); }
        }

        private void Awake()
        {
            targetRect = GetComponent<RectTransform>();
            canvasScaler = GetComponentInParent<CanvasScaler>();
            ApplySafeArea();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y || Screen.safeArea != lastSafeArea)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (targetRect == null) targetRect = GetComponent<RectTransform>();
            if (targetRect == null) return;
            if (canvasScaler == null) canvasScaler = GetComponentInParent<CanvasScaler>();

            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width == 0 || Screen.height == 0) return;

            float referenceHeight = (canvasScaler != null && canvasScaler.referenceResolution.y > 0)
                ? canvasScaler.referenceResolution.y
                : 1920f;

            float scaleFactor = referenceHeight / Screen.height;

            switch (fitMode)
            {
                case SafeAreaFitMode.TopHeader:
                {
                    // Calculate pixels from screen top to safe area top
                    float topMarginPixels = Screen.height - (safeArea.y + safeArea.height);
                    float topMarginCanvas = topMarginPixels * scaleFactor;

                    // Ensure top header anchors to top
                    targetRect.anchorMin = new Vector2(0f, 1f);
                    targetRect.anchorMax = new Vector2(1f, 1f);
                    targetRect.pivot = new Vector2(0.5f, 1f);
                    targetRect.anchoredPosition = new Vector2(0f, -Mathf.Max(0f, topMarginCanvas));
                    break;
                }

                case SafeAreaFitMode.BottomFooter:
                {
                    float bottomMarginPixels = safeArea.y;
                    float bottomMarginCanvas = bottomMarginPixels * scaleFactor;

                    targetRect.anchorMin = new Vector2(0f, 0f);
                    targetRect.anchorMax = new Vector2(1f, 0f);
                    targetRect.pivot = new Vector2(0.5f, 0f);
                    targetRect.anchoredPosition = new Vector2(0f, Mathf.Max(0f, bottomMarginCanvas));
                    break;
                }

                case SafeAreaFitMode.FullStretch:
                default:
                {
                    Vector2 minAnchor = safeArea.position;
                    Vector2 maxAnchor = safeArea.position + safeArea.size;

                    minAnchor.x /= Screen.width;
                    minAnchor.y /= Screen.height;
                    maxAnchor.x /= Screen.width;
                    maxAnchor.y /= Screen.height;

                    if (!applyLeft) minAnchor.x = 0f;
                    if (!applyBottom) minAnchor.y = 0f;
                    if (!applyRight) maxAnchor.x = 1f;
                    if (!applyTop) maxAnchor.y = 1f;

                    targetRect.anchorMin = minAnchor;
                    targetRect.anchorMax = maxAnchor;
                    targetRect.offsetMin = Vector2.zero;
                    targetRect.offsetMax = Vector2.zero;
                    break;
                }
            }
        }
    }
}

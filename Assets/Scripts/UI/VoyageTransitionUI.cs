using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Data;
using IdleFishing.Audio;
using IdleFishing.VFX;

namespace IdleFishing.UI
{
    /// <summary>
    /// Màn hình chuyển cảnh Mây Toon theo hình tròn (Radial Circular Cloud Transition UI):
    /// - Lúc xuất phát: Mây từ xung quanh (360 độ ngoài màn hình) bay hướng tâm vào giữa màn hình theo hình tròn và giữ nguyên.
    /// - Khi thuyền bắt đầu đi vào: Mây từ giữa màn hình bay tỏa đều ra xung quanh theo hình tròn (Radial Iris Opening),
    ///   hé lộ con thuyền mới và khung cảnh thế giới mới rực rỡ ngập tràn ánh nắng!
    /// </summary>
    public class VoyageTransitionUI : MonoBehaviour
    {
        private static VoyageTransitionUI _instance;
        public static VoyageTransitionUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<VoyageTransitionUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("--- VOYAGE TRANSITION UI ---");
                        _instance = go.AddComponent<VoyageTransitionUI>();
                    }
                }
                return _instance;
            }
        }

        private class RadialCloudPuff
        {
            public RectTransform rect;
            public Image image;
            public Vector2 closedPos;    // Vị trí khi mây tụ lại ở giữa màn hình
            public Vector2 openPos;      // Vị trí ngoài màn hình (xung quanh theo góc theta)
            public Vector3 baseScale;
            public float bobPhase;
            public float bobSpeed;
        }

        private Canvas transitionCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform cloudsRoot;
        private readonly List<RadialCloudPuff> radialPuffs = new List<RadialCloudPuff>();

        private RectTransform centerContainerRect;
        private RectTransform helmIconRect;
        private TextMeshProUGUI statusHeaderText;
        private TextMeshProUGUI destinationTitleText;
        private TextMeshProUGUI destinationDescText;
        private Image progressBarFill;
        private TextMeshProUGUI progressPercentText;

        private Coroutine activeEngulfCoroutine;
        private Coroutine activeDisperseCoroutine;
        private Coroutine activeTransitionCoroutine;
        private bool isHoldingAtCenter = false;
        private static Sprite _cloudBubbleSprite;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            BuildCloudTransitionUI();
        }

        public static Sprite GetCloudBubbleSprite()
        {
            if (_cloudBubbleSprite != null) return _cloudBubbleSprite;
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            _cloudBubbleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _cloudBubbleSprite;
        }

        private void BuildCloudTransitionUI()
        {
            // 1. Root Screen-Space Overlay Canvas
            GameObject canvasObj = new GameObject("VoyageTransition_Canvas");
            canvasObj.transform.SetParent(transform, false);

            transitionCanvas = canvasObj.AddComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 9999;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Sprite cloudSp = GetCloudBubbleSprite();
            Color cloudTintBase = new Color(0.97f, 0.985f, 1.0f, 1.0f); // Trắng tuyết Toon
            Color cloudTintSubtle = new Color(0.91f, 0.95f, 1.0f, 0.98f); // Ánh xanh pastel viền mềm

            // 2. ROOT CHỨA CÁC ĐÁM MÂY TỎA TRÒN (Radial Clouds Container)
            GameObject cloudsRootObj = new GameObject("Clouds_Radial_Container");
            cloudsRootObj.transform.SetParent(canvasObj.transform, false);
            cloudsRoot = cloudsRootObj.AddComponent<RectTransform>();
            cloudsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            cloudsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            cloudsRoot.pivot = new Vector2(0.5f, 0.5f);
            cloudsRoot.anchoredPosition = Vector2.zero;
            cloudsRoot.sizeDelta = Vector2.zero;

            radialPuffs.Clear();

            // Tầng 0: Tâm trung tâm (1 đám mây khổng lồ che kín lõi)
            CreatePuff(0f, 0f, 880f, 90f, cloudSp, cloudTintBase, 0);

            // Tầng 1: Vòng trong lõi (6 đám mây, bán kính 200px)
            int r1Count = 6;
            for (int i = 0; i < r1Count; i++)
            {
                float deg = i * (360f / r1Count);
                CreatePuff(200f, deg, 700f, deg, cloudSp, (i % 2 == 0) ? cloudTintBase : cloudTintSubtle, 1 + i);
            }

            // Tầng 2: Vòng giữa (12 đám mây, bán kính 460px)
            int r2Count = 12;
            for (int i = 0; i < r2Count; i++)
            {
                float deg = i * (360f / r2Count) + 15f;
                CreatePuff(460f, deg, 740f, deg, cloudSp, (i % 2 == 0) ? cloudTintBase : cloudTintSubtle, 10 + i);
            }

            // Tầng 3: Vòng ngoài (16 đám mây, bán kính 800px)
            int r3Count = 16;
            for (int i = 0; i < r3Count; i++)
            {
                float deg = i * (360f / r3Count);
                CreatePuff(800f, deg, 780f, deg, cloudSp, (i % 2 == 0) ? cloudTintBase : cloudTintSubtle, 30 + i);
            }

            // Tầng 4: Vòng 4 góc màn hình & mép viền (8 đám mây, bán kính 1150px)
            int r4Count = 8;
            for (int i = 0; i < r4Count; i++)
            {
                float deg = i * 45f + 22.5f;
                CreatePuff(1150f, deg, 840f, deg, cloudSp, (i % 2 == 0) ? cloudTintBase : cloudTintSubtle, 50 + i);
            }

            // 3. Center Content Container (Hiển thị nổi bật giữa biển mây)
            GameObject centerObj = new GameObject("Center_Container");
            centerObj.transform.SetParent(canvasObj.transform, false);
            centerContainerRect = centerObj.AddComponent<RectTransform>();
            centerContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerContainerRect.pivot = new Vector2(0.5f, 0.5f);
            centerContainerRect.anchoredPosition = Vector2.zero;
            centerContainerRect.sizeDelta = new Vector2(900, 700);

            // 4. Golden Compass / Helm Icon
            GameObject helmObj = new GameObject("Helm_Icon");
            helmObj.transform.SetParent(centerObj.transform, false);
            helmIconRect = helmObj.AddComponent<RectTransform>();
            helmIconRect.anchorMin = new Vector2(0.5f, 0.76f);
            helmIconRect.anchorMax = new Vector2(0.5f, 0.76f);
            helmIconRect.pivot = new Vector2(0.5f, 0.5f);
            helmIconRect.anchoredPosition = Vector2.zero;
            helmIconRect.sizeDelta = new Vector2(130, 130);

            var helmImg = helmObj.AddComponent<Image>();
            helmImg.sprite = cloudSp;
            helmImg.color = new Color(0.18f, 0.65f, 0.90f, 0.95f); // Deep Ocean Blue Accent

            GameObject innerRingObj = new GameObject("Inner_Ring");
            innerRingObj.transform.SetParent(helmObj.transform, false);
            var irImg = innerRingObj.AddComponent<Image>();
            irImg.sprite = cloudSp;
            irImg.color = Color.white;
            RectTransform irRt = innerRingObj.GetComponent<RectTransform>();
            irRt.anchorMin = new Vector2(0.5f, 0.5f);
            irRt.anchorMax = new Vector2(0.5f, 0.5f);
            irRt.pivot = new Vector2(0.5f, 0.5f);
            irRt.sizeDelta = new Vector2(95, 95);

            GameObject anchorTxtObj = new GameObject("Anchor_Symbol");
            anchorTxtObj.transform.SetParent(innerRingObj.transform, false);
            var anchorTxt = anchorTxtObj.AddComponent<TextMeshProUGUI>();
            anchorTxt.text = "*";
            anchorTxt.fontSize = 52;
            anchorTxt.alignment = TextAlignmentOptions.Center;
            anchorTxt.color = new Color(0.96f, 0.72f, 0.15f); // Sunlight Gold
            RectTransform anRt = anchorTxtObj.GetComponent<RectTransform>();
            anRt.anchorMin = Vector2.zero;
            anRt.anchorMax = Vector2.one;
            anRt.sizeDelta = Vector2.zero;

            // 5. Status Header Label
            GameObject headerObj = new GameObject("Header_Status");
            headerObj.transform.SetParent(centerObj.transform, false);
            statusHeaderText = headerObj.AddComponent<TextMeshProUGUI>();
            statusHeaderText.fontSize = 24;
            statusHeaderText.fontStyle = FontStyles.Bold;
            statusHeaderText.alignment = TextAlignmentOptions.Center;
            statusHeaderText.color = new Color(0.12f, 0.45f, 0.75f); // Oceanic Blue
            statusHeaderText.text = "~ SEA FOG ENVELOPS THE VOYAGE ~";
            statusHeaderText.ApplyFont(UIFontCategory.Header);
            RectTransform hRt = headerObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 0.54f);
            hRt.anchorMax = new Vector2(0.5f, 0.54f);
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(850, 42);

            // 6. Destination Level Title
            GameObject titleObj = new GameObject("Destination_Title");
            titleObj.transform.SetParent(centerObj.transform, false);
            destinationTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            destinationTitleText.fontSize = 38;
            destinationTitleText.fontStyle = FontStyles.Bold;
            destinationTitleText.alignment = TextAlignmentOptions.Center;
            destinationTitleText.color = new Color(0.08f, 0.20f, 0.38f); // Deep Navy Text
            destinationTitleText.text = "LEVEL 2: TROPICAL CORAL ATOLL";
            destinationTitleText.ApplyFont(UIFontCategory.Header);
            RectTransform dtRt = titleObj.GetComponent<RectTransform>();
            dtRt.anchorMin = new Vector2(0.5f, 0.44f);
            dtRt.anchorMax = new Vector2(0.5f, 0.44f);
            dtRt.pivot = new Vector2(0.5f, 0.5f);
            dtRt.anchoredPosition = Vector2.zero;
            dtRt.sizeDelta = new Vector2(850, 60);

            // 7. Destination Flavor Description
            GameObject descObj = new GameObject("Destination_Desc");
            descObj.transform.SetParent(centerObj.transform, false);
            destinationDescText = descObj.AddComponent<TextMeshProUGUI>();
            destinationDescText.fontSize = 20;
            destinationDescText.fontStyle = FontStyles.Italic;
            destinationDescText.alignment = TextAlignmentOptions.Center;
            destinationDescText.color = new Color(0.25f, 0.45f, 0.65f);
            destinationDescText.text = "Explore uncharted waters!";
            destinationDescText.ApplyFont(UIFontCategory.Default);
            RectTransform ddRt = descObj.GetComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(0.5f, 0.34f);
            ddRt.anchorMax = new Vector2(0.5f, 0.34f);
            ddRt.pivot = new Vector2(0.5f, 0.5f);
            ddRt.anchoredPosition = Vector2.zero;
            ddRt.sizeDelta = new Vector2(800, 60);

            // 8. Progress Bar Container
            GameObject pbBgObj = new GameObject("ProgressBar_BG");
            pbBgObj.transform.SetParent(centerObj.transform, false);
            var pbBgImg = pbBgObj.AddComponent<Image>();
            pbBgImg.sprite = cloudSp;
            pbBgImg.color = new Color(0.85f, 0.92f, 0.98f, 0.95f);
            RectTransform pbRt = pbBgObj.GetComponent<RectTransform>();
            pbRt.anchorMin = new Vector2(0.5f, 0.16f);
            pbRt.anchorMax = new Vector2(0.5f, 0.16f);
            pbRt.pivot = new Vector2(0.5f, 0.5f);
            pbRt.anchoredPosition = Vector2.zero;
            pbRt.sizeDelta = new Vector2(520, 26);

            GameObject pbFillObj = new GameObject("ProgressBar_Fill");
            pbFillObj.transform.SetParent(pbBgObj.transform, false);
            progressBarFill = pbFillObj.AddComponent<Image>();
            progressBarFill.sprite = cloudSp;
            progressBarFill.color = new Color(0.18f, 0.65f, 0.90f);
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillAmount = 0f;
            RectTransform pbfRt = pbFillObj.GetComponent<RectTransform>();
            pbfRt.anchorMin = Vector2.zero;
            pbfRt.anchorMax = Vector2.one;
            pbfRt.sizeDelta = Vector2.zero;

            // Progress Text
            GameObject pbtObj = new GameObject("Progress_Text");
            pbtObj.transform.SetParent(centerObj.transform, false);
            progressPercentText = pbtObj.AddComponent<TextMeshProUGUI>();
            progressPercentText.fontSize = 17;
            progressPercentText.fontStyle = FontStyles.Bold;
            progressPercentText.alignment = TextAlignmentOptions.Center;
            progressPercentText.color = new Color(0.20f, 0.40f, 0.60f);
            progressPercentText.text = "Sea mist envelops the vessel...";
            progressPercentText.ApplyFont(UIFontCategory.Default);
            RectTransform pbtRt = pbtObj.GetComponent<RectTransform>();
            pbtRt.anchorMin = new Vector2(0.5f, 0.08f);
            pbtRt.anchorMax = new Vector2(0.5f, 0.08f);
            pbtRt.pivot = new Vector2(0.5f, 0.5f);
            pbtRt.anchoredPosition = Vector2.zero;
            pbtRt.sizeDelta = new Vector2(400, 30);
        }

        private void CreatePuff(float radius, float angleDeg, float diameter, float flyAngleDeg, Sprite sprite, Color tint, int index)
        {
            GameObject puffObj = new GameObject($"RadialPuff_{index}");
            puffObj.transform.SetParent(cloudsRoot, false);

            var rt = puffObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(diameter, diameter);

            var img = puffObj.AddComponent<Image>();
            img.sprite = sprite;
            img.color = tint;

            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 closedPos = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);

            float flyRad = flyAngleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(flyRad), Mathf.Sin(flyRad));
            // Đẩy ra ngoài bán kính 1500px theo hình tròn 360 độ
            Vector2 openPos = closedPos + dir * 1500f;

            rt.anchoredPosition = openPos;
            rt.localScale = Vector3.zero;

            radialPuffs.Add(new RadialCloudPuff
            {
                rect = rt,
                image = img,
                closedPos = closedPos,
                openPos = openPos,
                baseScale = Vector3.one,
                bobPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                bobSpeed = UnityEngine.Random.Range(1.8f, 2.6f)
            });
        }

        private void Update()
        {
            if (canvasGroup != null && canvasGroup.alpha > 0.05f)
            {
                if (helmIconRect != null)
                {
                    helmIconRect.Rotate(Vector3.forward, -90f * Time.unscaledDeltaTime);
                }

                // Hiệu ứng bồng bềnh dập dềnh khi mây đang giữ nguyên ở giữa màn hình
                if (isHoldingAtCenter)
                {
                    float t = Time.unscaledTime;
                    foreach (var puff in radialPuffs)
                    {
                        if (puff.rect != null)
                        {
                            Vector2 bobOffset = new Vector2(
                                Mathf.Cos(t * puff.bobSpeed + puff.bobPhase),
                                Mathf.Sin(t * puff.bobSpeed + puff.bobPhase)
                            ) * 7f;
                            puff.rect.anchoredPosition = puff.closedPos + bobOffset;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Pha 1: Lúc xuất phát - Mây từ xung quanh (360 độ ngoài màn hình) bay vào giữa màn hình và giữ nguyên.
        /// </summary>
        public void StartEngulfInward(WorldData targetWorld, Action onMidpointAction)
        {
            if (activeEngulfCoroutine != null) StopCoroutine(activeEngulfCoroutine);
            activeEngulfCoroutine = StartCoroutine(EngulfToCenterRoutine(targetWorld, onMidpointAction));
        }

        public IEnumerator EngulfToCenterRoutine(WorldData targetWorld, Action onMidpointAction)
        {
            if (activeDisperseCoroutine != null) StopCoroutine(activeDisperseCoroutine);

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.alpha = 1f;
            isHoldingAtCenter = false;

            string wName = targetWorld != null ? targetWorld.worldName : "New Harbor";
            int order = targetWorld != null ? targetWorld.worldOrder : 1;
            string desc = targetWorld != null ? targetWorld.description : "Explore uncharted waters!";

            statusHeaderText.text = "~ SEA FOG ENVELOPS THE VOYAGE ~";
            destinationTitleText.text = $"LEVEL {order}: {wName.ToUpper()}";
            destinationDescText.text = desc;
            progressBarFill.fillAmount = 0f;
            progressPercentText.text = "Sea mist envelops the vessel...";

            centerContainerRect.localScale = Vector3.zero;

            // Đưa toàn bộ mây về vị trí xung quanh ngoài màn hình
            foreach (var puff in radialPuffs)
            {
                if (puff.rect != null)
                {
                    puff.rect.anchoredPosition = puff.openPos;
                    puff.rect.localScale = Vector3.one * 0.35f;
                }
            }

            // 1. MÂY TỪ XUNG QUANH BAY VÀO GIỮA MÀN HÌNH THEO HÌNH TRÒN
            float duration = 0.85f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeMove = EaseOutQuad(t);
                float easeScale = EaseOutBack(t);

                foreach (var puff in radialPuffs)
                {
                    if (puff.rect != null)
                    {
                        puff.rect.anchoredPosition = Vector2.Lerp(puff.openPos, puff.closedPos, easeMove);
                        puff.rect.localScale = Vector3.Lerp(Vector3.one * 0.35f, puff.baseScale, easeScale);
                    }
                }

                centerContainerRect.localScale = Vector3.one * Mathf.Clamp01((t - 0.25f) / 0.75f);
                progressBarFill.fillAmount = Mathf.Lerp(0f, 0.5f, t);

                yield return null;
            }

            // Khớp chính xác tại closedPos
            foreach (var puff in radialPuffs)
            {
                if (puff.rect != null)
                {
                    puff.rect.anchoredPosition = puff.closedPos;
                    puff.rect.localScale = puff.baseScale;
                }
            }

            // 2. GIỮ NGUYÊN Ở GIỮA MÀN HÌNH & THỰC HIỆN MIDPOINT
            isHoldingAtCenter = true;
            centerContainerRect.localScale = Vector3.one;
            progressPercentText.text = $"Parting the sea mist at {wName}...";

            onMidpointAction?.Invoke();

            // Progress bar hoàn tất nốt trong lúc mây giữ nguyên ở giữa
            elapsed = 0f;
            float holdTime = 0.40f;
            while (elapsed < holdTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / holdTime);
                progressBarFill.fillAmount = Mathf.Lerp(0.5f, 1.0f, t);
                yield return null;
            }

            statusHeaderText.text = "~ SEA MIST DISPERSES - WELCOME TO NEW HARBOR ~";
            progressBarFill.fillAmount = 1.0f;
            progressPercentText.text = $"Arrived at {wName}!";
            activeEngulfCoroutine = null;
        }

        /// <summary>
        /// Pha 2: Khi thuyền bắt đầu đi vào -> Mây từ giữa màn hình bay tỏa ra xung quanh đều theo hình tròn!
        /// </summary>
        public void StartDisperseOutward(float duration = 1.2f, Action onCompleteAction = null)
        {
            if (activeDisperseCoroutine != null) StopCoroutine(activeDisperseCoroutine);
            activeDisperseCoroutine = StartCoroutine(DisperseOutwardRoutine(duration, onCompleteAction));
        }

        public IEnumerator DisperseOutwardRoutine(float duration, Action onCompleteAction)
        {
            isHoldingAtCenter = false;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeMove = EaseInOutQuad(t);

                // Mây từ giữa bay tỏa đều ra xung quanh theo hình tròn 360 độ
                foreach (var puff in radialPuffs)
                {
                    if (puff.rect != null)
                    {
                        puff.rect.anchoredPosition = Vector2.Lerp(puff.closedPos, puff.openPos, easeMove);
                        puff.rect.localScale = puff.baseScale * Mathf.Lerp(1.0f, 1.25f, t);
                    }
                }

                // Center info thu nhỏ và biến mất
                centerContainerRect.localScale = Vector3.one * Mathf.Clamp01(1f - t * 2.5f);

                // Mờ dần toàn bộ canvas khi mây đã bay ra gần mép
                if (t > 0.65f)
                {
                    canvasGroup.alpha = Mathf.Clamp01((1f - t) / 0.35f);
                }

                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            foreach (var puff in radialPuffs)
            {
                if (puff.rect != null)
                {
                    puff.rect.anchoredPosition = puff.openPos;
                }
            }

            onCompleteAction?.Invoke();
            activeDisperseCoroutine = null;
        }

        /// <summary>
        /// API chuyển cảnh toàn phần (cho tương thích ngược nếu gọi đơn lẻ)
        /// </summary>
        public void PlayTransition(WorldData targetWorld, Action onMidpointAction, Action onCompleteAction = null)
        {
            if (activeTransitionCoroutine != null) StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = StartCoroutine(FullTransitionRoutine(targetWorld, onMidpointAction, onCompleteAction));
        }

        private IEnumerator FullTransitionRoutine(WorldData targetWorld, Action onMidpointAction, Action onCompleteAction)
        {
            yield return StartCoroutine(EngulfToCenterRoutine(targetWorld, onMidpointAction));
            yield return new WaitForSecondsRealtime(0.20f);
            yield return StartCoroutine(DisperseOutwardRoutine(1.2f, onCompleteAction));
            activeTransitionCoroutine = null;
        }

        private float EaseOutQuad(float x)
        {
            return 1f - (1f - x) * (1f - x);
        }

        private float EaseInOutQuad(float x)
        {
            return x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;
        }

        private float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// <summary>
        /// Kích hoạt chuỗi chuyển cảnh đầy đủ và chụp ảnh tự động 4 giai đoạn chuyển động tròn của mây
        /// </summary>
        public void TriggerTransitionCaptureSequence(int targetWorldIndex = 1)
        {
            StartCoroutine(CaptureSequenceRoutine(targetWorldIndex));
        }

        private IEnumerator CaptureSequenceRoutine(int targetWorldIndex)
        {
            var ship = IdleFishing.World.OffshoreTravelShip.Instance;
            if (ship == null)
            {
                Debug.LogError("[VoyageTransitionUI] OffshoreTravelShip.Instance is null!");
                yield break;
            }

            Debug.Log("[VoyageTransitionUI] Starting circular cloud transition capture sequence...");

            if (!System.IO.Directory.Exists("Assets/Screenshots"))
            {
                System.IO.Directory.CreateDirectory("Assets/Screenshots");
            }

            // 1. Kích hoạt thuyền ra khơi và chuỗi chuyển cảnh
            ship.SailAwayToWorld(targetWorldIndex, false);

            // Thời điểm 1: Khi mây từ 360 độ xung quanh đang bay vào giữa màn hình theo hình tròn
            yield return new WaitForSeconds(1.15f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Assets/Screenshots/cloud_circular_engulf_inward.png");
            Debug.Log("[VoyageTransitionUI] Captured: cloud_circular_engulf_inward.png");

            // Thời điểm 2: Khi mây đã tụ lại ở giữa màn hình che kín và đang giữ nguyên (hold)
            yield return new WaitForSeconds(0.75f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Assets/Screenshots/cloud_circular_hold_center.png");
            Debug.Log("[VoyageTransitionUI] Captured: cloud_circular_hold_center.png");

            // Thời điểm 3: Khi thuyền mới bắt đầu đi vào, mây từ giữa màn hình bay tỏa đều ra xung quanh theo hình tròn
            yield return new WaitForSeconds(0.95f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Assets/Screenshots/cloud_circular_disperse_outward.png");
            Debug.Log("[VoyageTransitionUI] Captured: cloud_circular_disperse_outward.png");

            // Thời điểm 4: Khi thuyền cập bến mới an toàn hoàn tất (confetti, fanfare, docked)
            yield return new WaitForSeconds(1.45f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Assets/Screenshots/cloud_circular_docked_complete.png");
            Debug.Log("[VoyageTransitionUI] Captured: cloud_circular_docked_complete.png");
        }
    }
}

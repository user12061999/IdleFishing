using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IdleFishing.Core;

namespace IdleFishing.UI
{
    public class FlyingCoinManager : MonoBehaviour
    {
        public static FlyingCoinManager Instance { get; private set; }

        [Header("Target HUD")]
        [SerializeField] private RectTransform targetCoinsIcon;
        [SerializeField] private Canvas canvas;

        private RectTransform fxContainer;
        private Sprite coinSprite;

        private readonly Queue<RectTransform> coinPool = new Queue<RectTransform>();
        private const int INITIAL_POOL_SIZE = 30;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            // Create container for coins
            GameObject containerObj = new GameObject("FlyingCoins_FXContainer");
            containerObj.transform.SetParent(transform, false);
            fxContainer = containerObj.AddComponent<RectTransform>();
            fxContainer.anchorMin = Vector2.zero;
            fxContainer.anchorMax = Vector2.one;
            fxContainer.sizeDelta = Vector2.zero;

            coinSprite = GetCoinSprite();
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                coinPool.Enqueue(CreateNewCoin());
            }
        }

        private RectTransform CreateNewCoin()
        {
            GameObject coinObj = new GameObject("FlyCoin");
            coinObj.transform.SetParent(fxContainer, false);
            RectTransform rt = coinObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(28, 28);

            Image img = coinObj.AddComponent<Image>();
            img.sprite = coinSprite;
            img.color = Color.white;
            img.raycastTarget = false;

            coinObj.SetActive(false);
            return rt;
        }

        private RectTransform GetCoin()
        {
            while (coinPool.Count > 0)
            {
                var rt = coinPool.Dequeue();
                if (rt != null && rt.gameObject != null)
                {
                    rt.gameObject.SetActive(true);
                    return rt;
                }
            }
            var newCoin = CreateNewCoin();
            newCoin.gameObject.SetActive(true);
            return newCoin;
        }

        private void ReturnCoin(RectTransform rt)
        {
            if (rt == null || rt.gameObject == null) return;
            rt.gameObject.SetActive(false);
            coinPool.Enqueue(rt);
        }

        private static Sprite cachedCoinSprite;

        public static Sprite GetCoinSprite()
        {
            if (cachedCoinSprite != null) return cachedCoinSprite;

#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Cute-GUI-Pack-Lite/PNG/18_roulette/01/icon_gold.png");
            if (asset != null)
            {
                cachedCoinSprite = asset;
                return cachedCoinSprite;
            }
#endif

            // Procedural 64x64 crisp circular cartoon coin with golden rim & highlight
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "ProceduralCoinSprite";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] cols = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = (size - 1) * 0.5f - 1f;

            Color rimColor = new Color(0.85f, 0.60f, 0.05f, 1f);     // Dark gold rim
            Color innerColor = new Color(1.0f, 0.88f, 0.22f, 1f);    // Bright gold face
            Color highlight = new Color(1.0f, 0.98f, 0.70f, 1f);     // Specular shine

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius + 1f)
                    {
                        cols[y * size + x] = Color.clear;
                    }
                    else
                    {
                        float edgeAlpha = Mathf.Clamp01(radius + 1f - dist);
                        Color pixelColor;
                        if (dist > radius - 3.5f)
                        {
                            pixelColor = rimColor;
                        }
                        else if (dist > radius - 6.5f)
                        {
                            pixelColor = Color.Lerp(rimColor, innerColor, 0.5f);
                        }
                        else
                        {
                            float shine = Mathf.Clamp01(1f - (Vector2.Distance(new Vector2(x, y), center + new Vector2(-6f, 6f)) / 16f));
                            pixelColor = Color.Lerp(innerColor, highlight, shine * 0.7f);
                        }
                        pixelColor.a *= edgeAlpha;
                        cols[y * size + x] = pixelColor;
                    }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();

            cachedCoinSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedCoinSprite;
        }

        public void SetTarget(RectTransform target)
        {
            targetCoinsIcon = target;
        }

        public void PlayFlyingCoins(Vector3 worldOrigin, int count = 4)
        {
            if (canvas == null) return;
            StartCoroutine(SpawnFlyingCoinsRoutine(worldOrigin, count));
        }

        public void PlayFlyingCoinsFromUI(RectTransform sourceRect, int count = 6)
        {
            if (canvas == null || sourceRect == null) return;
            StartCoroutine(SpawnFlyingCoinsFromUIRoutine(sourceRect, count));
        }

        private IEnumerator SpawnFlyingCoinsFromUIRoutine(RectTransform sourceRect, int count)
        {
            Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : Camera.main;
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(cam, sourceRect.position);

            Vector2 canvasStartPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(fxContainer, screenPos, cam, out canvasStartPos);

            Vector2 targetCanvasPos = Vector2.zero;
            if (targetCoinsIcon != null)
            {
                Vector3 targetScreenPos = RectTransformUtility.WorldToScreenPoint(cam, targetCoinsIcon.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(fxContainer, targetScreenPos, cam, out targetCanvasPos);
            }
            else
            {
                targetCanvasPos = new Vector2(-canvas.pixelRect.width * 0.35f, canvas.pixelRect.height * 0.42f);
            }

            for (int i = 0; i < count; i++)
            {
                StartCoroutine(AnimateSingleCoin(canvasStartPos, targetCanvasPos, i * 0.05f));
            }
            yield break;
        }

        private IEnumerator SpawnFlyingCoinsRoutine(Vector3 worldOrigin, int count)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Vector3 screenPos = cam.WorldToScreenPoint(worldOrigin);
            if (screenPos.z < 0) yield break; // Behind camera

            Vector2 canvasStartPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(fxContainer, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, out canvasStartPos);

            Vector2 targetCanvasPos = Vector2.zero;
            if (targetCoinsIcon != null)
            {
                Vector3 targetScreenPos = RectTransformUtility.WorldToScreenPoint(cam, targetCoinsIcon.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(fxContainer, targetScreenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, out targetCanvasPos);
            }
            else
            {
                // Default to top-left coins position if target not linked
                targetCanvasPos = new Vector2(-canvas.pixelRect.width * 0.35f, canvas.pixelRect.height * 0.42f);
            }

            for (int i = 0; i < count; i++)
            {
                StartCoroutine(AnimateSingleCoin(canvasStartPos, targetCanvasPos, i * 0.07f));
            }
        }

        private IEnumerator AnimateSingleCoin(Vector2 startPos, Vector2 endPos, float delay)
        {
            if (delay > 0)
            {
                yield return YieldCache.WaitForSeconds(delay);
            }

            RectTransform rt = GetCoin();
            rt.anchoredPosition = startPos;
            rt.localScale = Vector3.one;

            // Randomize curved arc control point
            float arcOffset = Random.Range(-120f, 120f);
            Vector2 midPoint = (startPos + endPos) * 0.5f + new Vector2(arcOffset, Random.Range(60f, 160f));

            float duration = 0.65f;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    // Quadratic Bezier curve
                    float u = 1f - t;
                    Vector2 currentPos = (u * u * startPos) + (2f * u * t * midPoint) + (t * t * endPos);
                    if (rt != null) rt.anchoredPosition = currentPos;

                    // Scale pop then zoom into icon
                    float scale = 1f;
                    if (t < 0.25f)
                    {
                        scale = Mathf.Lerp(0.3f, 1.25f, t / 0.25f);
                    }
                    else if (t > 0.8f)
                    {
                        scale = Mathf.Lerp(1.25f, 0.6f, (t - 0.8f) / 0.2f);
                    }
                    if (rt != null) rt.localScale = Vector3.one * scale;

                    yield return null;
                }
            }
            finally
            {
                ReturnCoin(rt);
            }

            // Trigger punch animation on Coins HUD
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.PunchCoinsCounter();
            }
        }
    }
}

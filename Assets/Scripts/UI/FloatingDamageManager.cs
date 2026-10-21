using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;

namespace IdleFishing.UI
{
    public enum DamageStyle
    {
        WorkerAttack,
        PlayerTap,
        FishAttack,
        GoldBonus,
        Crit
    }

    public class FloatingDamageItem
    {
        public GameObject CanvasObject;
        public RectTransform CanvasRect;
        public Canvas Canvas;
        public TextMeshProUGUI Text;
    }

    public class FloatingDamageManager : MonoBehaviour
    {
        public static FloatingDamageManager Instance { get; private set; }

        [SerializeField] private int initialPoolSize = 25;
        private readonly Queue<FloatingDamageItem> pool = new Queue<FloatingDamageItem>();
        private Transform poolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            poolRoot = new GameObject("DamageText_Pool").transform;
            poolRoot.SetParent(transform, false);

            for (int i = 0; i < initialPoolSize; i++)
            {
                pool.Enqueue(CreateNewDamageItem());
            }
        }

        private FloatingDamageItem CreateNewDamageItem()
        {
            GameObject canvasObj = new GameObject("DamageTextCanvas");
            canvasObj.transform.SetParent(poolRoot, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(300, 80);
            canvasRt.localScale = Vector3.one * 0.012f;

            GameObject textObj = new GameObject("TextUGUI");
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(300, 80);
            textRt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.ApplyFont(UIFontCategory.Numbers);

            canvasObj.SetActive(false);

            return new FloatingDamageItem
            {
                CanvasObject = canvasObj,
                CanvasRect = canvasRt,
                Canvas = canvas,
                Text = tmp
            };
        }

        private FloatingDamageItem GetItem()
        {
            while (pool.Count > 0)
            {
                var item = pool.Dequeue();
                if (item != null && item.CanvasObject != null)
                {
                    item.CanvasObject.SetActive(true);
                    return item;
                }
            }

            var newItem = CreateNewDamageItem();
            newItem.CanvasObject.SetActive(true);
            return newItem;
        }

        private void ReturnItem(FloatingDamageItem item)
        {
            if (item == null || item.CanvasObject == null) return;
            item.CanvasObject.SetActive(false);
            item.CanvasObject.transform.SetParent(poolRoot, false);
            pool.Enqueue(item);
        }

        public void SpawnDamage(Vector3 worldPos, int amount, DamageStyle style, string customText = null)
        {
            StartCoroutine(SpawnDamageRoutine(worldPos, amount, style, customText));
        }

        private IEnumerator SpawnDamageRoutine(Vector3 worldPos, int amount, DamageStyle style, string customText)
        {
            FloatingDamageItem item = GetItem();

            // Random jitter to prevent overlapping
            Vector3 spawnPos = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, Random.Range(-0.3f, 0.3f));
            item.CanvasObject.transform.position = spawnPos;

            TextMeshProUGUI tmp = item.Text;
            Color baseColor;
            float targetScale = 1.0f;
            string textToDisplay;

            switch (style)
            {
                case DamageStyle.PlayerTap:
                    baseColor = new Color(1f, 0.88f, 0.2f); // Gold
                    targetScale = 1.35f;
                    textToDisplay = customText ?? $"-{NumberFormatter.Format(amount)} CRIT!";
                    tmp.fontSize = 38;
                    break;

                case DamageStyle.FishAttack:
                    baseColor = new Color(1f, 0.25f, 0.25f); // Red
                    targetScale = 1.0f;
                    textToDisplay = customText ?? $"-{NumberFormatter.Format(amount)}";
                    tmp.fontSize = 30;
                    break;

                case DamageStyle.GoldBonus:
                    baseColor = new Color(1f, 0.90f, 0.20f); // Radiant Gold
                    targetScale = 1.45f;
                    textToDisplay = customText ?? $"+{NumberFormatter.Format(amount)} G";
                    tmp.fontSize = 38;
                    break;

                case DamageStyle.Crit:
                    baseColor = new Color(1f, 0.35f, 0.1f); // Vibrant Crimson / Orange Red Crit
                    targetScale = 1.55f;
                    textToDisplay = customText ?? $"-{NumberFormatter.Format(amount)} CRIT!";
                    tmp.fontSize = 42;
                    break;

                case DamageStyle.WorkerAttack:
                default:
                    baseColor = new Color(1f, 1f, 1f); // White
                    targetScale = 1.0f;
                    textToDisplay = customText ?? $"-{NumberFormatter.Format(amount)}";
                    tmp.fontSize = 30;
                    break;
            }

            tmp.color = baseColor;
            tmp.text = textToDisplay;

            // Animate floating up, pop scale, and fade out
            float duration = 0.75f;
            float elapsed = 0f;
            Vector3 startPos = spawnPos;
            Vector3 endPos = spawnPos + Vector3.up * 1.5f;

            item.CanvasObject.transform.localScale = Vector3.zero;
            Camera cam = Camera.main;

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    // Billboard face camera
                    if (cam != null && item.CanvasObject != null)
                    {
                        item.CanvasObject.transform.rotation = Quaternion.LookRotation(item.CanvasObject.transform.position - cam.transform.position);
                    }

                    // Vertical drift
                    if (item.CanvasObject != null)
                    {
                        item.CanvasObject.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                    }

                    // Pop scale
                    float scaleT;
                    if (t < 0.2f)
                    {
                        scaleT = Mathf.Lerp(0f, targetScale * 1.25f, t / 0.2f);
                    }
                    else if (t < 0.35f)
                    {
                        scaleT = Mathf.Lerp(targetScale * 1.25f, targetScale, (t - 0.2f) / 0.15f);
                    }
                    else
                    {
                        scaleT = targetScale;
                    }

                    if (item.CanvasObject != null)
                    {
                        item.CanvasObject.transform.localScale = Vector3.one * (0.012f * scaleT);
                    }

                    // Fade alpha in last 40% of duration
                    if (t > 0.6f && tmp != null)
                    {
                        float alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                        tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                    }

                    yield return null;
                }
            }
            finally
            {
                ReturnItem(item);
            }
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IdleFishing.UI
{
    public enum DamageStyle
    {
        WorkerAttack,
        PlayerTap,
        FishAttack,
        GoldBonus
    }

    public class FloatingDamageManager : MonoBehaviour
    {
        public static FloatingDamageManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SpawnDamage(Vector3 worldPos, int amount, DamageStyle style, string customText = null)
        {
            StartCoroutine(SpawnDamageRoutine(worldPos, amount, style, customText));
        }

        private IEnumerator SpawnDamageRoutine(Vector3 worldPos, int amount, DamageStyle style, string customText)
        {
            // Random jitter to prevent overlapping
            Vector3 spawnPos = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, Random.Range(-0.3f, 0.3f));

            // World-Space Canvas hosting TextMeshProUGUI
            GameObject canvasObj = new GameObject("DamageTextCanvas");
            canvasObj.transform.position = spawnPos;

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

            Color baseColor;
            float targetScale = 1.0f;
            string textToDisplay;

            switch (style)
            {
                case DamageStyle.PlayerTap:
                    baseColor = new Color(1f, 0.88f, 0.2f); // Gold
                    targetScale = 1.35f;
                    textToDisplay = customText ?? $"-{amount} CRIT!";
                    tmp.fontSize = 32;
                    break;

                case DamageStyle.FishAttack:
                    baseColor = new Color(1f, 0.25f, 0.25f); // Red
                    targetScale = 1.0f;
                    textToDisplay = customText ?? $"-{amount}";
                    tmp.fontSize = 24;
                    break;

                case DamageStyle.GoldBonus:
                    baseColor = new Color(1f, 0.85f, 0.15f); // Golden Amber
                    targetScale = 1.45f;
                    textToDisplay = customText ?? $"+{amount} G";
                    tmp.fontSize = 32;
                    break;

                case DamageStyle.WorkerAttack:
                default:
                    baseColor = new Color(1f, 1f, 1f); // White
                    targetScale = 1.0f;
                    textToDisplay = customText ?? $"-{amount}";
                    tmp.fontSize = 24;
                    break;
            }

            tmp.color = baseColor;
            tmp.text = textToDisplay;

            // Animate floating up, pop scale, and fade out
            float duration = 0.75f;
            float elapsed = 0f;
            Vector3 startPos = spawnPos;
            Vector3 endPos = spawnPos + Vector3.up * 1.5f;

            canvasObj.transform.localScale = Vector3.zero;
            Camera cam = Camera.main;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Billboard face camera
                if (cam != null)
                {
                    canvasObj.transform.rotation = Quaternion.LookRotation(canvasObj.transform.position - cam.transform.position);
                }

                // Vertical drift
                canvasObj.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));

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
                canvasObj.transform.localScale = Vector3.one * (0.012f * scaleT);

                // Fade alpha in last 40% of duration
                if (t > 0.6f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                    tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                }

                yield return null;
            }

            Destroy(canvasObj);
        }
    }
}

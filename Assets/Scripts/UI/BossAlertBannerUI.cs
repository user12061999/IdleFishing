using System.Collections;
using UnityEngine;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;

namespace IdleFishing.UI
{
    public class BossAlertBannerUI : MonoBehaviour
    {
        private TextMeshProUGUI alertText;
        private Coroutine alertRoutine;

        private void Awake()
        {
            BuildBanner();
        }

        private void OnEnable()
        {
            GameEvents.BossFishSpawned += OnBossSpawned;
            GameEvents.BossFishDefeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            GameEvents.BossFishSpawned -= OnBossSpawned;
            GameEvents.BossFishDefeated -= OnBossDefeated;
        }

        private void BuildBanner()
        {
            GameObject obj = new GameObject("BossAlertText");
            obj.transform.SetParent(transform, false);
            alertText = obj.AddComponent<TextMeshProUGUI>();
            alertText.alignment = TextAlignmentOptions.Center;
            alertText.fontSize = 34;
            alertText.fontStyle = FontStyles.Bold;
            alertText.color = new Color(1f, 0.85f, 0.1f);
            alertText.textWrappingMode = TextWrappingModes.NoWrap;
            alertText.ApplyFont(UIFontCategory.Header);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.65f);
            rt.anchorMax = new Vector2(0.5f, 0.65f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900, 60);

            obj.SetActive(false);
        }

        private void OnBossSpawned(FishChainData species)
        {
            if (alertRoutine != null) StopCoroutine(alertRoutine);
            string bossName = (species != null && !string.IsNullOrEmpty(species.BossTitle))
                ? species.BossTitle.ToUpperInvariant()
                : (species != null && !string.IsNullOrEmpty(species.SpeciesName) ? species.SpeciesName.ToUpperInvariant() : "OCEAN BOSS");
            alertRoutine = StartCoroutine(ShowBannerRoutine($"[!] APEX BOSS: {bossName} APPEARED! TAP TO BATTLE! [!]", new Color(1f, 0.82f, 0.1f), 3.8f));
        }

        private void OnBossDefeated(FishChainData species, int bonusCoins)
        {
            if (alertRoutine != null) StopCoroutine(alertRoutine);
            string bossName = (species != null && !string.IsNullOrEmpty(species.BossTitle))
                ? species.BossTitle.ToUpperInvariant()
                : "APEX BOSS";
            alertRoutine = StartCoroutine(ShowBannerRoutine($"{bossName} DEFEATED! BOUNTY +{bonusCoins} G", new Color(0.2f, 0.9f, 0.4f), 2.8f));
        }

        private IEnumerator ShowBannerRoutine(string text, Color col, float duration)
        {
            if (alertText == null) yield break;
            alertText.gameObject.SetActive(true);
            alertText.text = text;
            alertText.color = col;

            float elapsed = 0f;
            Transform t = alertText.transform;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Subtle pulse animation
                float pulse = 1.0f + Mathf.Sin(elapsed * 8f) * 0.1f;
                t.localScale = Vector3.one * pulse;
                yield return null;
            }

            t.localScale = Vector3.one;
            alertText.gameObject.SetActive(false);
        }
    }
}

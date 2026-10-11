using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Fishing;

namespace IdleFishing.UI
{
    public class FishingFrenzyHUDUI : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color normalChargeColor = new Color(1.0f, 0.65f, 0.15f); // Orange Gold
        [SerializeField] private Color frenzyActiveColor = new Color(1.0f, 0.25f, 0.15f); // Fiery Red-Orange

        private RectTransform barContainerRect;
        private Image fillImage;
        private Image bgImage;
        private TextMeshProUGUI statusText;

        private float currentFill = 0f;
        private float targetFill = 0f;

        private void Start()
        {
            BuildProceduralHUD();
        }

        private void OnEnable()
        {
            GameEvents.FrenzyChargeChanged += OnChargeChanged;
            GameEvents.FrenzyStarted += OnFrenzyStarted;
            GameEvents.FrenzyEnded += OnFrenzyEnded;
        }

        private void OnDisable()
        {
            GameEvents.FrenzyChargeChanged -= OnChargeChanged;
            GameEvents.FrenzyStarted -= OnFrenzyStarted;
            GameEvents.FrenzyEnded -= OnFrenzyEnded;
        }

        private void BuildProceduralHUD()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Container at Top-Center (below coins HUD)
            GameObject container = new GameObject("Frenzy_HUD_Container");
            container.transform.SetParent(transform, false);
            barContainerRect = container.AddComponent<RectTransform>();
            barContainerRect.anchorMin = new Vector2(0.5f, 1f);
            barContainerRect.anchorMax = new Vector2(0.5f, 1f);
            barContainerRect.pivot = new Vector2(0.5f, 1f);
            barContainerRect.anchoredPosition = new Vector2(0, -70);
            barContainerRect.sizeDelta = new Vector2(360, 44);

            // 2. Background
            bgImage = container.AddComponent<Image>();
            bgImage.sprite = whiteSp;
            bgImage.color = new Color(0.12f, 0.15f, 0.20f, 0.92f); // Dark Charcoal
            bgImage.raycastTarget = false;

            // 3. Fill Bar
            GameObject fillObj = new GameObject("Frenzy_Fill");
            fillObj.transform.SetParent(container.transform, false);
            fillImage = fillObj.AddComponent<Image>();
            fillImage.sprite = whiteSp;
            fillImage.color = normalChargeColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3, 3);
            fillRect.offsetMax = new Vector2(-3, -3);

            // 4. Status Text
            GameObject txtObj = new GameObject("Frenzy_StatusText");
            txtObj.transform.SetParent(container.transform, false);
            statusText = txtObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "FRENZY: 0%";
            statusText.fontSize = 20;
            statusText.fontStyle = FontStyles.Bold;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;
            statusText.raycastTarget = false;

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
        }

        private void OnChargeChanged(float charge)
        {
            targetFill = Mathf.Clamp01(charge / 100f);
        }

        private void OnFrenzyStarted()
        {
            if (fillImage != null)
            {
                fillImage.color = frenzyActiveColor;
            }
        }

        private void OnFrenzyEnded()
        {
            if (fillImage != null)
            {
                fillImage.color = normalChargeColor;
            }
            targetFill = 0f;
        }

        private void Update()
        {
            var frenzyMgr = FishingFrenzyManager.Instance;
            if (frenzyMgr == null) return;

            if (frenzyMgr.IsFrenzyActive)
            {
                float remain = frenzyMgr.RemainingFrenzyTime;
                float duration = frenzyMgr.FrenzyDuration;
                targetFill = Mathf.Clamp01(remain / duration);
                currentFill = targetFill;

                if (fillImage != null)
                {
                    fillImage.fillAmount = currentFill;
                    // Pulsing fire effect
                    float pulse = 0.85f + Mathf.PingPong(Time.time * 3f, 0.25f);
                    fillImage.color = new Color(1.0f, 0.3f * pulse, 0.1f);
                }

                if (statusText != null)
                {
                    statusText.text = $"FRENZY: {remain:0.0}s";
                }

                // Shake container slightly
                if (barContainerRect != null)
                {
                    float shake = Mathf.Sin(Time.time * 25f) * 1.5f;
                    barContainerRect.anchoredPosition = new Vector2(shake, -70);
                }
            }
            else
            {
                currentFill = Mathf.MoveTowards(currentFill, targetFill, Time.deltaTime * 1.5f);
                if (fillImage != null)
                {
                    fillImage.fillAmount = currentFill;
                    fillImage.color = normalChargeColor;
                }

                if (statusText != null)
                {
                    int pct = Mathf.RoundToInt(frenzyMgr.CurrentCharge);
                    statusText.text = $"FRENZY: {pct}%";
                }

                if (barContainerRect != null)
                {
                    barContainerRect.anchoredPosition = new Vector2(0, -70);
                }
            }
        }
    }
}

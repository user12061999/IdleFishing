using UnityEngine;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Economy;

namespace IdleFishing.Logistics
{
    public class WorldTransporterHirePad : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI subText;
        [SerializeField] private Transform visualTransform;
        [SerializeField] private Transform billboardTransform;

        private Transform buttonCapTransform;
        private Vector3 originalScale = Vector3.one;
        private Vector3 capBaseLocalPos = new Vector3(0f, 0.068f, 0f);
        private bool canCurrentlyAfford = false;
        private bool isPressing = false;
        private Camera mainCamera;

        public void Setup(TextMeshProUGUI tTxt, TextMeshProUGUI cTxt, TextMeshProUGUI sTxt, Transform vis, Transform bill)
        {
            titleText = tTxt;
            costText = cTxt;
            subText = sTxt;
            visualTransform = vis;
            billboardTransform = bill;
            CacheButtonCap();
            UpdateVisuals();
        }

        private void CacheButtonCap()
        {
            if (visualTransform != null)
            {
                originalScale = visualTransform.localScale;
                buttonCapTransform = visualTransform.Find("Button_Cap");
                if (buttonCapTransform != null)
                {
                    capBaseLocalPos = buttonCapTransform.localPosition;
                }
            }
        }

        private void LateUpdate()
        {
            if (billboardTransform != null)
            {
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    billboardTransform.rotation = mainCamera.transform.rotation;
                }
            }

            if (buttonCapTransform != null && !isPressing)
            {
                if (canCurrentlyAfford)
                {
                    float bob = (Mathf.Sin(Time.time * 4.2f) * 0.5f + 0.5f) * 0.018f;
                    buttonCapTransform.localPosition = capBaseLocalPos + new Vector3(0f, bob, 0f);
                }
                else
                {
                    buttonCapTransform.localPosition = Vector3.Lerp(buttonCapTransform.localPosition, capBaseLocalPos, Time.deltaTime * 10f);
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.CoinsChanged += OnCoinsChanged;
            CacheButtonCap();
            UpdateVisuals();
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= OnCoinsChanged;
            isPressing = false;
        }

        private void OnCoinsChanged(int coins)
        {
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (LogisticsManager.Instance == null) return;

            int count = LogisticsManager.Instance.WorkerCount;
            int max = 2;
            int cost = 120;
            bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.HasEnoughCoins(cost);
            canCurrentlyAfford = canAfford && (count < max);

            if (count >= max)
            {
                if (titleText != null) titleText.text = "[V] FLEET FULL";
                if (costText != null) costText.text = "<color=#FFD700>MAX</color>";
                if (subText != null) subText.text = "2/2 Transporters";
            }
            else
            {
                if (titleText != null) titleText.text = "[+] HIRE TRANSPORTER";
                string colorTag = canAfford ? "<color=#00FF88>" : "<color=#FF6666>";
                if (costText != null) costText.text = $"{colorTag}{cost} G</color>";
                if (subText != null) subText.text = $"Current: {count}/2 Vehicles";
            }
        }

        private void OnMouseDown()
        {
            if (LogisticsManager.Instance != null)
            {
                if (LogisticsManager.Instance.TryHireAdditionalTransporter())
                {
                    UpdateVisuals();
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(PressButtonRoutine());
                    }
                }
            }
        }

        private System.Collections.IEnumerator PressButtonRoutine()
        {
            isPressing = true;
            if (buttonCapTransform != null)
            {
                Vector3 pressedPos = capBaseLocalPos + new Vector3(0f, -0.052f, 0f);
                float t = 0f;
                while (t < 0.08f)
                {
                    t += Time.deltaTime;
                    buttonCapTransform.localPosition = Vector3.Lerp(capBaseLocalPos, pressedPos, t / 0.08f);
                    yield return null;
                }
                t = 0f;
                while (t < 0.14f)
                {
                    t += Time.deltaTime;
                    float p = t / 0.14f;
                    float spring = Mathf.Sin(p * Mathf.PI) * 0.022f;
                    buttonCapTransform.localPosition = Vector3.Lerp(pressedPos, capBaseLocalPos, p) + new Vector3(0f, spring, 0f);
                    yield return null;
                }
                buttonCapTransform.localPosition = capBaseLocalPos;
            }
            else if (visualTransform != null)
            {
                Vector3 baseScale = originalScale == Vector3.zero ? Vector3.one : originalScale;
                float elapsed = 0f;
                float duration = 0.22f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float scaleMul = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.12f * (1f - t);
                    visualTransform.localScale = baseScale * scaleMul;
                    yield return null;
                }
                visualTransform.localScale = baseScale;
            }
            isPressing = false;
        }
    }
}

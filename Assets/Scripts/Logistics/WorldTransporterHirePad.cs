using UnityEngine;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Economy;
using IdleFishing.UI;

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

            ConfigureBillboardLayout();
            CacheButtonCap();
            UpdateVisuals();
        }

        private void Awake()
        {
            if (gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
            {
                gameObject.layer = LayerMask.NameToLayer("Default");
            }
            foreach (Transform child in transform)
            {
                if (child.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Default");
                }
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.center = new Vector3(0f, 1.0f, 0f);
                col.radius = Mathf.Max(col.radius, 1.35f);
                col.height = Mathf.Max(col.height, 2.4f);
            }

            CacheButtonCap();
            mainCamera = Camera.main;
        }

        private void Start()
        {
            ConfigureBillboardLayout();
            CacheButtonCap();
            UpdateVisuals();
        }

        private void ConfigureBillboardLayout()
        {
            ConfigureText(titleText, 10f, 16f, UIFontCategory.Header);
            ConfigureText(costText, 14f, 20f, UIFontCategory.Button);
            ConfigureText(subText, 9f, 13f, UIFontCategory.Default);

            if (titleText != null && costText != null)
            {
                var titleRt = titleText.rectTransform;
                var costRt = costText.rectTransform;
                titleRt.anchoredPosition = new Vector2(0, 30);
                titleRt.sizeDelta = new Vector2(190, 26);
                costRt.anchoredPosition = new Vector2(0, 0);
                costRt.sizeDelta = new Vector2(190, 28);

                if (subText != null)
                {
                    var subRt = subText.rectTransform;
                    subRt.anchoredPosition = new Vector2(0, -30);
                    subRt.sizeDelta = new Vector2(190, 22);
                }

                if (titleRt.parent is RectTransform bgRt && bgRt.sizeDelta.y < 105)
                {
                    bgRt.sizeDelta = new Vector2(Mathf.Max(bgRt.sizeDelta.x, 200), 105);
                }
            }
        }

        private void ConfigureText(TextMeshProUGUI tmp, float minSize, float maxSize, UIFontCategory category)
        {
            if (tmp == null) return;
            tmp.ApplyFont(category);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
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
                if (costText != null) costText.text = $"{colorTag}{NumberFormatter.Format(cost)} G</color>";
                if (subText != null) subText.text = $"Current: {count}/2 Vehicles";
            }
        }

        private int lastClickFrame = -1;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Camera cam = mainCamera != null ? mainCamera : Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Collide))
                    {
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            TriggerPurchase();
                        }
                    }
                }
            }
        }

        private void OnMouseDown()
        {
            TriggerPurchase();
        }

        public void TriggerPurchase()
        {
            if (Time.frameCount == lastClickFrame) return;
            lastClickFrame = Time.frameCount;

            if (LogisticsManager.Instance == null) return;

            int count = LogisticsManager.Instance.WorkerCount;
            int max = 2;
            int cost = 120;

            if (count >= max)
            {
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 1.5f,
                        0,
                        DamageStyle.FishAttack,
                        "FLEET FULL (2/2)!"
                    );
                }
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(ShakeRoutine());
                }
                return;
            }

            if (EconomyManager.Instance != null && EconomyManager.Instance.HasEnoughCoins(cost))
            {
                bool success = LogisticsManager.Instance.TryHireAdditionalTransporter();
                if (success)
                {
                    UpdateVisuals();
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(PressButtonRoutine());
                    }
                }
            }
            else
            {
                if (FloatingDamageManager.Instance != null)
                {
                    FloatingDamageManager.Instance.SpawnDamage(
                        transform.position + Vector3.up * 1.5f,
                        0,
                        DamageStyle.FishAttack,
                        "NOT ENOUGH GOLD!"
                    );
                }
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(ShakeRoutine());
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

        private System.Collections.IEnumerator ShakeRoutine()
        {
            isPressing = true;
            if (buttonCapTransform != null)
            {
                buttonCapTransform.localPosition = capBaseLocalPos + new Vector3(0f, -0.025f, 0f);
            }
            Vector3 origPos = transform.position;
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin(elapsed * 50f) * 0.08f;
                transform.position = origPos + Vector3.right * offset;
                yield return null;
            }
            transform.position = origPos;
            if (buttonCapTransform != null)
            {
                buttonCapTransform.localPosition = capBaseLocalPos;
            }
            isPressing = false;
        }
    }
}

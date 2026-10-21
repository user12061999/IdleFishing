using System.Collections;
using UnityEngine;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.UI;

namespace IdleFishing.World
{
    public class InWorldPurchasePad : MonoBehaviour
    {
        [Header("Upgrade Target")]
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private string padTitle = "[+] PURCHASE UPGRADE";
        [SerializeField] private string subtitle = "Tap to purchase";

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI subText;

        [Header("Animation & Billboard")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private Transform billboardTransform;

        private Transform buttonCapTransform;
        private Vector3 originalScale = Vector3.one;
        private Vector3 capBaseLocalPos = new Vector3(0f, 0.068f, 0f);
        private bool canCurrentlyAfford = false;
        private bool isPressing = false;
        private Camera mainCamera;

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

            // Self-healing upgradeType mapping for 3 streamlined core pillars:
            // 0 = FishermanTeam, 1 = WranglerTeam, 2 = FishValue
            if (gameObject.name.Contains("Wrangler") || (padTitle != null && padTitle.ToUpper().Contains("WRANGLER")))
            {
                upgradeType = UpgradeType.WranglerTeam;
            }
            else if ((int)upgradeType == 3)
            {
                upgradeType = UpgradeType.WranglerTeam;
            }
            else if ((int)upgradeType >= 4)
            {
                upgradeType = UpgradeType.FishValue;
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

        public void SetBillboardTransform(Transform bTrans)
        {
            billboardTransform = bTrans;
        }

        private void Start()
        {
            ConfigureBillboardLayout();
            CacheButtonCap();
            UpdateVisuals();
        }

        private void OnEnable()
        {
            GameEvents.CoinsChanged += OnCoinsChanged;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            CacheButtonCap();
            UpdateVisuals();
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= OnCoinsChanged;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            isPressing = false;
        }

        public void Setup(UpgradeType type, string title, string sub, TextMeshProUGUI titleTxt, TextMeshProUGUI costTxt, TextMeshProUGUI subTxt = null, Transform visual = null)
        {
            upgradeType = type;
            padTitle = title;
            subtitle = sub;
            titleText = titleTxt;
            costText = costTxt;
            subText = subTxt;
            visualTransform = visual;

            ConfigureBillboardLayout();
            CacheButtonCap();
            UpdateVisuals();
        }

        private void ConfigureBillboardLayout()
        {
            ConfigureText(titleText, 14f, 22f, UIFontCategory.Header);
            ConfigureText(costText, 18f, 28f, UIFontCategory.Button);
            ConfigureText(subText, 12f, 16f, UIFontCategory.Default);

            if (titleText != null && costText != null)
            {
                var titleRt = titleText.rectTransform;
                var costRt = costText.rectTransform;
                titleRt.anchoredPosition = new Vector2(0, 36);
                titleRt.sizeDelta = new Vector2(230, 32);
                costRt.anchoredPosition = new Vector2(0, 0);
                costRt.sizeDelta = new Vector2(230, 36);

                if (subText != null)
                {
                    var subRt = subText.rectTransform;
                    subRt.anchoredPosition = new Vector2(0, -36);
                    subRt.sizeDelta = new Vector2(230, 26);
                }

                if (titleRt.parent is RectTransform bgRt && bgRt.sizeDelta.y < 125)
                {
                    bgRt.sizeDelta = new Vector2(Mathf.Max(bgRt.sizeDelta.x, 240), 125);
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

        public void SetTitle(string newTitle)
        {
            padTitle = newTitle;
            UpdateVisuals();
        }

        public void SetUpgradeType(UpgradeType newType)
        {
            upgradeType = newType;
            UpdateVisuals();
        }

        private bool isForcedMax = false;

        public void SetForcedMax(bool forcedMax)
        {
            isForcedMax = forcedMax;
            UpdateVisuals();
        }

        public void SetSubtitle(string newSub)
        {
            subtitle = newSub;
            if (subText != null)
            {
                subText.text = subtitle;
            }
        }

        private void OnCoinsChanged(int coins)
        {
            UpdateVisuals();
        }

        private void OnUpgradePurchased(UpgradeType type, int level)
        {
            if (type == upgradeType)
            {
                UpdateVisuals();
            }
        }

        public void UpdateVisuals()
        {
            if (UpgradeManager.Instance == null) return;

            int currentLvl = UpgradeManager.Instance.GetLevel(upgradeType);
            int maxLvl = UpgradeManager.Instance.CurrentConfig != null ? UpgradeManager.Instance.CurrentConfig.GetEntry(upgradeType)?.maxLevel ?? 999 : 999;
            bool isMax = isForcedMax || (maxLvl < 999 && currentLvl >= maxLvl);
            int cost = UpgradeManager.Instance.GetCost(upgradeType);
            bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.HasEnoughCoins(cost);
            canCurrentlyAfford = canAfford && !isMax;

            if (titleText != null)
            {
                titleText.text = padTitle;
            }

            if (costText != null)
            {
                if (isMax)
                {
                    costText.text = "<color=#FFD700>MAX</color>";
                }
                else
                {
                    string colorTag = canAfford ? "<color=#00FF88>" : "<color=#FF6666>";
                    costText.text = $"{colorTag}{NumberFormatter.Format(cost)} G</color>";
                }
            }

            if (subText != null)
            {
                subText.text = subtitle;
            }
        }

        private int lastClickFrame = -1;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IdleFishing.Aquarium.AquariumTankVisual.IsPointerOverUI()) return;

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
            if (IdleFishing.Aquarium.AquariumTankVisual.IsPointerOverUI()) return;
            TriggerPurchase();
        }

        public void TriggerPurchase()
        {
            if (Time.frameCount == lastClickFrame) return;
            lastClickFrame = Time.frameCount;

            if (isForcedMax || UpgradeManager.Instance == null) return;

            int cost = UpgradeManager.Instance.GetCost(upgradeType);
            if (EconomyManager.Instance != null && EconomyManager.Instance.HasEnoughCoins(cost))
            {
                bool success = UpgradeManager.Instance.TryPurchaseUpgrade(upgradeType);
                if (success && gameObject.activeInHierarchy)
                {
                    StartCoroutine(PunchRoutine());
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

        private IEnumerator PunchRoutine()
        {
            isPressing = true;
            if (buttonCapTransform != null)
            {
                // Press button cap down into bezel well
                Vector3 pressedPos = capBaseLocalPos + new Vector3(0f, -0.052f, 0f);
                float t = 0f;
                while (t < 0.08f)
                {
                    t += Time.deltaTime;
                    buttonCapTransform.localPosition = Vector3.Lerp(capBaseLocalPos, pressedPos, t / 0.08f);
                    yield return null;
                }
                // Spring back up with slight bounce
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
                    // Tactile spring punch: squash down then spring back
                    float scaleMul = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.12f * (1f - t);
                    visualTransform.localScale = baseScale * scaleMul;
                    yield return null;
                }
                visualTransform.localScale = baseScale;
            }
            isPressing = false;
        }

        private IEnumerator ShakeRoutine()
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

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
            CacheButtonCap();
            UpdateVisuals();
        }

        public void SetTitle(string newTitle)
        {
            padTitle = newTitle;
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
            bool isMax = maxLvl < 999 && currentLvl >= maxLvl;
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
                    costText.text = $"{colorTag}{cost:N0} G</color>";
                }
            }

            if (subText != null)
            {
                subText.text = subtitle;
            }
        }

        private void OnMouseDown()
        {
            if (UpgradeManager.Instance == null) return;

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

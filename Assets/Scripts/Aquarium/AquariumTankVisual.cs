using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.UI;

namespace IdleFishing.Aquarium
{
    public class AquariumTankVisual : MonoBehaviour
    {
        public static AquariumTankVisual Instance { get; private set; }

        [Header("Tank Dimensions")]
        [SerializeField] private Vector3 tankInteriorCenter = new Vector3(7.5f, 1.75f, 8.0f);
        [SerializeField] private Vector3 tankInteriorSize = new Vector3(3.1f, 1.1f, 2.5f);

        [Header("Lighting / Decor Visuals")]
        [SerializeField] private Light tankNeonLight;
        [SerializeField] private Renderer waterInteriorRenderer;

        private Bounds tankBounds;
        private readonly List<SwimmingFishVisual> activeFishVisuals = new List<SwimmingFishVisual>();

        // Billboard UI
        private TextMeshProUGUI statusTitleText;
        private TextMeshProUGUI fishCountText;
        private TextMeshProUGUI revenueText;
        private GameObject claimButtonObj;
        private TextMeshProUGUI claimBtnText;

        private void Awake()
        {
            Instance = this;
            tankBounds = new Bounds(transform.position + new Vector3(0f, 1.35f, 0f), tankInteriorSize);
        }

        private void Start()
        {
            tankBounds = new Bounds(transform.position + new Vector3(0f, 1.35f, 0f), tankInteriorSize);
            BuildBillboardUI();

            if (AquariumManager.Instance != null)
            {
                AquariumManager.Instance.OnAquariumUpdated += SyncFishAndUI;
                SyncFishAndUI();
            }
        }

        private void OnDestroy()
        {
            if (AquariumManager.Instance != null)
            {
                AquariumManager.Instance.OnAquariumUpdated -= SyncFishAndUI;
            }
        }

        private void Update()
        {
            UpdateRevenueDisplay();
        }

        private void OnMouseDown()
        {
            OpenAquariumModal();
        }

        public void OpenAquariumModal()
        {
            if (AquariumPopupUI.Instance != null)
            {
                AquariumPopupUI.Instance.Open();
            }
        }

        public void SyncFishAndUI()
        {
            if (AquariumManager.Instance == null) return;

            var fishList = AquariumManager.Instance.CaptiveFish;

            // Clear old visuals
            for (int i = 0; i < activeFishVisuals.Count; i++)
            {
                if (activeFishVisuals[i] != null)
                {
                    Destroy(activeFishVisuals[i].gameObject);
                }
            }
            activeFishVisuals.Clear();

            // Spawn visual for each captive fish
            for (int i = 0; i < fishList.Count; i++)
            {
                var f = fishList[i];
                SpawnFishVisual(f);
            }

            // Update Billboard Text
            if (fishCountText != null)
            {
                fishCountText.text = $"Exhibited: <b>{fishList.Count} / {AquariumManager.Instance.MaxCapacity}</b> Fish";
            }

            // Decor styling
            UpdateDecorLighting();
            UpdateRevenueDisplay();
        }

        private void SpawnFishVisual(AquariumFishInstance f)
        {
            GameObject fishObj = new GameObject($"Fish_{f.speciesId}_{f.instanceId}");
            fishObj.transform.SetParent(transform, true);

            float rx = Random.Range(tankBounds.min.x + 0.35f, tankBounds.max.x - 0.35f);
            float ry = Random.Range(tankBounds.min.y + 0.2f, tankBounds.max.y - 0.2f);
            float rz = Random.Range(tankBounds.min.z + 0.35f, tankBounds.max.z - 0.35f);
            fishObj.transform.position = new Vector3(rx, ry, rz);
            fishObj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject prefab = GetFishPrefab(f.speciesId);
            GameObject body;
            if (prefab != null)
            {
                body = Instantiate(prefab, fishObj.transform);
                body.name = "Model_" + f.speciesId;
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = GetSpeciesModelRotation(f.speciesId);

                float baseScale = GetSpeciesBaseScale(f.speciesId);
                float weightFactor = Mathf.Clamp(1.0f + ((f.weightKg - 2.0f) * 0.04f), 0.88f, 1.25f);
                float bossFactor = f.isBoss ? 1.45f : 1.0f;
                body.transform.localScale = Vector3.one * (baseScale * weightFactor * bossFactor);

                // Remove colliders from model so they don't block clicks
                foreach (var col in body.GetComponentsInChildren<Collider>())
                {
                    Destroy(col);
                }

                ApplyFishVisualMaterials(body, f.speciesId, f.tier, f.isBoss);
            }
            else
            {
                // Fallback primitive
                body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(fishObj.transform, false);
                body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                float scale = f.isBoss ? 0.75f : Mathf.Clamp(0.35f + (f.weightKg * 0.05f), 0.35f, 0.65f);
                body.transform.localScale = new Vector3(scale * 0.45f, scale * 0.9f, scale * 0.35f);

                var col = body.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            var sw = fishObj.AddComponent<SwimmingFishVisual>();
            sw.Initialize(tankBounds, 1.0f);

            activeFishVisuals.Add(sw);
        }

        private GameObject GetFishPrefab(string speciesId)
        {
            if (BestiaryManager.Instance != null)
            {
                var entry = BestiaryManager.Instance.GetEntry(speciesId);
                if (entry != null && entry.chainData != null && entry.chainData.WholeFish != null && entry.chainData.WholeFish.Prefab != null)
                {
                    return entry.chainData.WholeFish.Prefab;
                }
            }

#if UNITY_EDITOR
            var chain = UnityEditor.AssetDatabase.LoadAssetAtPath<FishChainData>($"Assets/GameData/FishChains/{speciesId}_chain.asset");
            if (chain != null && chain.WholeFish != null && chain.WholeFish.Prefab != null)
            {
                return chain.WholeFish.Prefab;
            }

            string path = null;
            switch (speciesId)
            {
                case "salmon": path = "Assets/Prefabs/Fish/Fish_Salmon_3D.prefab"; break;
                case "cod": path = "Assets/Prefabs/Fish/Fish_Cod_3D.prefab"; break;
                case "herring": path = "Assets/Prefabs/Fish/Fish_Herring_3D.prefab"; break;
                case "arctic_tuna": path = "Assets/Prefabs/Fish/Fish_ArcticTuna_3D.prefab"; break;
                case "snow_crab": path = "Assets/Prefabs/Fish/Fish_SnowCrab_3D.prefab"; break;
                case "clownfish": path = "Assets/Prefabs/Fish/Fish_Clownfish_3D.prefab"; break;
                case "pufferfish": path = "Assets/Prefabs/Fish/Fish_Pufferfish_3D.prefab"; break;
                case "reef_shark": path = "Assets/Prefabs/Fish/Fish_ReefShark_Boss_3D.prefab"; break;
                case "eagle_ray": path = "Assets/Prefabs/Fish/Fish_EagleRay_Boss_3D.prefab"; break;
                case "thresher_shark": path = "Assets/Prefabs/Fish/Fish_ThresherShark_Boss_3D.prefab"; break;
                case "manta_ray": path = "Assets/Prefabs/Fish/Fish_MantaRay_Boss_3D.prefab"; break;
                case "great_white_shark": path = "Assets/Prefabs/Fish/Fish_GreatWhiteShark_Boss_3D.prefab"; break;
            }
            if (!string.IsNullOrEmpty(path))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
#endif
            return null;
        }

        private static float GetSpeciesBaseScale(string speciesId)
        {
            switch (speciesId)
            {
                case "salmon": return 0.62f;
                case "cod": return 0.60f;
                case "herring": return 0.56f;
                case "arctic_tuna": return 0.65f;
                case "snow_crab": return 0.55f;
                case "clownfish": return 0.58f;
                case "pufferfish": return 0.60f;
                case "reef_shark": return 0.58f;
                case "eagle_ray": return 0.56f;
                case "thresher_shark": return 0.58f;
                case "manta_ray": return 0.55f;
                case "great_white_shark": return 0.60f;
                default: return 0.58f;
            }
        }

        private static Quaternion GetSpeciesModelRotation(string speciesId)
        {
            // All Layer Lab 3D Props Fish models face forward (+Z) natively.
            return Quaternion.identity;
        }

        private void ApplyFishVisualMaterials(GameObject body, string speciesId, int tier, bool isBoss)
        {
            // Keep authentic UV-mapped Color.mat texture palette for all 3D fish and Apex Ocean Bosses (Sharks & Rays)!
        }

        private Color GetSpeciesColor(string speciesId, int tier, bool isBoss)
        {
            if (isBoss) return new Color(1.0f, 0.85f, 0.15f); // Brilliant Gold
            switch (speciesId)
            {
                case "salmon": return new Color(1.0f, 0.48f, 0.35f); // Pinkish Orange
                case "arctic_tuna": return new Color(0.18f, 0.42f, 0.88f); // Deep Ocean Blue
                case "clownfish": return new Color(1.0f, 0.45f, 0.05f); // Orange-White
                case "pufferfish": return new Color(0.92f, 0.88f, 0.20f); // Pale Yellow
                case "snow_crab": return new Color(0.95f, 0.25f, 0.25f); // Scarlet Red
                case "herring": return new Color(0.70f, 0.85f, 0.95f); // Silver Cyan
                case "cod": return new Color(0.75f, 0.80f, 0.70f); // Olive Silver
                default: return new Color(0.55f, 0.70f, 0.85f); // Silver Grey
            }
        }

        private void UpdateDecorLighting()
        {
            if (AquariumManager.Instance == null) return;
            int level = AquariumManager.Instance.TankDecorLevel;

            if (tankNeonLight != null)
            {
                switch (level)
                {
                    case 1:
                        tankNeonLight.color = new Color(0.2f, 0.8f, 1f);
                        tankNeonLight.intensity = 1.2f;
                        break;
                    case 2:
                        tankNeonLight.color = new Color(0.1f, 1f, 0.8f);
                        tankNeonLight.intensity = 1.6f;
                        break;
                    case 3:
                        tankNeonLight.color = new Color(0.9f, 0.4f, 1f);
                        tankNeonLight.intensity = 2.0f;
                        break;
                    case 4:
                        tankNeonLight.color = new Color(1f, 0.85f, 0.2f);
                        tankNeonLight.intensity = 2.5f;
                        break;
                }
            }
        }

        private void UpdateRevenueDisplay()
        {
            if (AquariumManager.Instance == null) return;

            float pending = AquariumManager.Instance.AccumulatedRevenue;
            float rate = AquariumManager.Instance.GetCurrentIncomeRatePerSecond();

            if (revenueText != null)
            {
                revenueText.text = $"Income: <color=#FFE600>+{rate:0.0} G/s</color>";
            }

            if (claimButtonObj != null)
            {
                bool canClaim = pending >= 1f;
                claimButtonObj.SetActive(canClaim);
                if (canClaim && claimBtnText != null)
                {
                    claimBtnText.text = $"THU: +{Mathf.FloorToInt(pending):N0} G";
                }
            }
        }

        private void BuildBillboardUI()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            GameObject canvasObj = new GameObject("AquariumBillboard");
            canvasObj.transform.SetParent(transform, false);
            // Positioned elevated behind the tank back wall to keep inside water & fish 100% visible
            canvasObj.transform.localPosition = new Vector3(0, 4.35f, 1.85f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<BillboardLookAtCamera>();

            RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(220, 110);
            canvasRt.localScale = Vector3.one * 0.012f;

            // Background Board
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(240, 120);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = whiteSp;
            bgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bgObj.transform, false);
            var title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "AQUARIUM";
            title.fontSize = 20;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.2f, 0.9f, 1f);
            title.alignment = TextAlignmentOptions.Center;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0, 36);
            tRt.sizeDelta = new Vector2(220, 26);

            // Fish Count
            GameObject cntObj = new GameObject("FishCount");
            cntObj.transform.SetParent(bgObj.transform, false);
            fishCountText = cntObj.AddComponent<TextMeshProUGUI>();
            fishCountText.text = "Exhibited: 0 / 6 Fish";
            fishCountText.fontSize = 17;
            fishCountText.color = Color.white;
            fishCountText.alignment = TextAlignmentOptions.Center;
            RectTransform cRt = cntObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(0, 10);
            cRt.sizeDelta = new Vector2(220, 22);

            // Revenue Rate
            GameObject revObj = new GameObject("RevenueRate");
            revObj.transform.SetParent(bgObj.transform, false);
            revenueText = revObj.AddComponent<TextMeshProUGUI>();
            revenueText.text = "Income: +0 G/s";
            revenueText.fontSize = 16;
            revenueText.color = new Color(1f, 0.85f, 0.2f);
            revenueText.alignment = TextAlignmentOptions.Center;
            RectTransform rRt = revObj.GetComponent<RectTransform>();
            rRt.anchoredPosition = new Vector2(0, -14);
            rRt.sizeDelta = new Vector2(220, 20);

            // Floating Quick Claim Button
            claimButtonObj = new GameObject("QuickClaimBtn");
            claimButtonObj.transform.SetParent(bgObj.transform, false);
            RectTransform clRt = claimButtonObj.AddComponent<RectTransform>();
            clRt.anchoredPosition = new Vector2(0, -40);
            clRt.sizeDelta = new Vector2(190, 28);

            Image clImg = claimButtonObj.AddComponent<Image>();
            clImg.sprite = whiteSp;
            clImg.color = new Color(0.18f, 0.78f, 0.38f);

            Button clBtn = claimButtonObj.AddComponent<Button>();
            clBtn.onClick.AddListener(() =>
            {
                if (AquariumManager.Instance != null)
                {
                    int earned = AquariumManager.Instance.CollectTicketRevenue();
                    if (earned > 0 && FlyingCoinManager.Instance != null)
                    {
                        FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 2f, 5);
                    }
                }
            });

            GameObject clTxtObj = new GameObject("Text");
            clTxtObj.transform.SetParent(claimButtonObj.transform, false);
            claimBtnText = clTxtObj.AddComponent<TextMeshProUGUI>();
            claimBtnText.text = "THU: +0 G";
            claimBtnText.fontSize = 13;
            claimBtnText.fontStyle = FontStyles.Bold;
            claimBtnText.color = Color.white;
            claimBtnText.alignment = TextAlignmentOptions.Center;
            clTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 22);

            claimButtonObj.SetActive(false);
        }
    }
}

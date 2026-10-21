using System.Collections;
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
        [SerializeField] private Vector3 tankInteriorCenter = new Vector3(0.0f, 1.90f, -3.2f);
        [SerializeField] private Vector3 tankInteriorSize = new Vector3(5.8f, 1.40f, 2.40f);

        [Header("Lighting / Decor Visuals")]
        [SerializeField] private Light tankNeonLight;
        [SerializeField] private Renderer waterInteriorRenderer;

        private Bounds tankBounds;
        private readonly Dictionary<string, SwimmingFishVisual> activeFishVisuals = new Dictionary<string, SwimmingFishVisual>();

        // Billboard UI
        private TextMeshProUGUI statusTitleText;
        private TextMeshProUGUI fishCountText;
        private TextMeshProUGUI revenueText;
        private GameObject claimButtonObj;
        private TextMeshProUGUI claimBtnText;

        private void Awake()
        {
            Instance = this;
            tankBounds = CalculateTankBounds();

            // Responsive BoxCollider covering the entire panoramic tank for 1-click interaction
            var col = GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.45f, 0f);
            col.size = new Vector3(7.4f, 2.9f, 3.8f);
        }

        public Bounds CalculateTankBounds()
        {
            Transform waterTrans = transform.Find("Model_Aquarium/Water_Volume");
            if (waterTrans != null)
            {
                var rend = waterTrans.GetComponent<Renderer>();
                if (rend != null)
                {
                    Bounds b = rend.bounds;
                    if (b.size.x > 3.0f && b.size.z > 1.5f)
                    {
                        return b;
                    }
                }
            }
            return new Bounds(new Vector3(0.0f, 2.10f, -3.20f), new Vector3(5.80f, 1.80f, 2.80f));
        }

        [Header("Tourist Visitors")]
        [SerializeField] private GameObject touristPrefab;
        [SerializeField] private float touristIntervalMin = 8.0f;
        [SerializeField] private float touristIntervalMax = 14.0f;
        [SerializeField] private int maxConcurrentTourists = 4;
        private readonly List<AquariumTouristAgent> activeTourists = new List<AquariumTouristAgent>();

        private void Start()
        {
            tankBounds = CalculateTankBounds();
            BuildBillboardUI();

            if (AquariumManager.Instance != null)
            {
                AquariumManager.Instance.OnAquariumUpdated += SyncFishAndUI;
                SyncFishAndUI();
            }

            StartCoroutine(TouristSpawnLoop());
        }

        private IEnumerator TouristSpawnLoop()
        {
            while (true)
            {
                float wait = Random.Range(touristIntervalMin, touristIntervalMax);
                yield return YieldCache.WaitForSeconds(wait);

                if (AquariumManager.Instance != null && AquariumManager.Instance.CurrentFishCount > 0)
                {
                    activeTourists.RemoveAll(t => t == null);
                    if (activeTourists.Count < maxConcurrentTourists)
                    {
                        SpawnTourist();
                    }
                }
            }
        }

        private void SpawnTourist()
        {
            if (touristPrefab == null)
            {
                touristPrefab = Resources.Load<GameObject>("Characters/Stickman_Customer_Tourist");
#if UNITY_EDITOR
                if (touristPrefab == null)
                {
                    touristPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Customer_Tourist.prefab");
                }
#endif
            }

            Vector3 spawnPos = new Vector3(4.0f, 0.05f, -2.5f) + new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.2f, 0.2f));
            GameObject touristObj;
            if (touristPrefab != null)
            {
                touristObj = Instantiate(touristPrefab, spawnPos, Quaternion.Euler(0, 45f, 0));
                touristObj.name = "AquariumTourist";
            }
            else
            {
                touristObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                touristObj.name = "AquariumTourist";
                touristObj.transform.position = spawnPos;
                touristObj.transform.localScale = new Vector3(0.65f, 0.95f, 0.65f);
                var col = touristObj.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
            }

            // Tourists walk along the stone promenade facing the panoramic aquarium tank
            float randomX = Random.Range(-2.2f, 2.2f);
            Vector3 viewSpot = transform.position + new Vector3(randomX, 0, 1.85f);
            var agent = touristObj.AddComponent<AquariumTouristAgent>();
            agent.Initialize(viewSpot, null);
            activeTourists.Add(agent);
        }

        private void OnDestroy()
        {
            if (AquariumManager.Instance != null)
            {
                AquariumManager.Instance.OnAquariumUpdated -= SyncFishAndUI;
            }

            foreach (var kvp in activeFishVisuals)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            activeFishVisuals.Clear();
        }

        private void Update()
        {
            UpdateRevenueDisplay();

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI()) return;

                Camera cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                    {
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            OpenAquariumModal();
                        }
                    }
                }
            }
        }

        private void OnMouseDown()
        {
            if (IsPointerOverUI()) return;
            OpenAquariumModal();
        }

        public static bool IsPointerOverUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;

            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return true;

            for (int i = 0; i < Input.touchCount; i++)
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }

            return false;
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

            tankBounds = CalculateTankBounds();

            var fishList = AquariumManager.Instance.CaptiveFish;
            var currentIds = new HashSet<string>();

            // Curate a balanced, diverse visual exhibition across the whole tank (max 15 visual fish, max 2 per species)
            const int maxTotalVisuals = 15;
            const int maxPerSpecies = 2;

            var exhibitedFish = new List<AquariumFishInstance>();
            var speciesCounts = new Dictionary<string, int>();

            // 1st Pass: Ensure at least 1 specimen of each distinct captured species is exhibited!
            for (int i = 0; i < fishList.Count; i++)
            {
                var f = fishList[i];
                if (!speciesCounts.ContainsKey(f.speciesId))
                {
                    exhibitedFish.Add(f);
                    speciesCounts[f.speciesId] = 1;
                    if (exhibitedFish.Count >= maxTotalVisuals) break;
                }
            }

            // 2nd Pass: Fill remaining visual slots up to maxPerSpecies (e.g. 2nd ray or 2nd shark)
            if (exhibitedFish.Count < maxTotalVisuals)
            {
                for (int i = 0; i < fishList.Count; i++)
                {
                    var f = fishList[i];
                    if (exhibitedFish.Contains(f)) continue;

                    speciesCounts.TryGetValue(f.speciesId, out int cnt);
                    if (cnt < maxPerSpecies)
                    {
                        exhibitedFish.Add(f);
                        speciesCounts[f.speciesId] = cnt + 1;
                        if (exhibitedFish.Count >= maxTotalVisuals) break;
                    }
                }
            }

            // Differential update: Keep already swimming fish uninterrupted
            for (int i = 0; i < exhibitedFish.Count; i++)
            {
                var f = exhibitedFish[i];
                currentIds.Add(f.instanceId);

                if (!activeFishVisuals.TryGetValue(f.instanceId, out var existing) || existing == null)
                {
                    SpawnFishVisual(f);
                }
            }

            // Remove fish that are no longer part of the visual exhibition
            var toRemove = new List<string>();
            foreach (var kvp in activeFishVisuals)
            {
                if (!currentIds.Contains(kvp.Key) || kvp.Value == null)
                {
                    toRemove.Add(kvp.Key);
                    if (kvp.Value != null)
                    {
                        Destroy(kvp.Value.gameObject);
                    }
                }
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                activeFishVisuals.Remove(toRemove[i]);
            }

            // Update Billboard Text (Full collection total remains accurate!)
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

            float rx = Random.Range(tankBounds.min.x + 0.25f, tankBounds.max.x - 0.25f);
            float ry = Random.Range(tankBounds.min.y + 0.15f, tankBounds.max.y - 0.15f);
            float rz = Random.Range(tankBounds.min.z + 0.25f, tankBounds.max.z - 0.25f);
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
                float weightFactor = Mathf.Clamp(1.0f + ((f.weightKg - 2.0f) * 0.02f), 0.90f, 1.15f);
                float bossFactor = f.isBoss ? 1.08f : 1.0f;
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

                float scale = f.isBoss ? 0.65f : Mathf.Clamp(0.35f + (f.weightKg * 0.04f), 0.35f, 0.55f);
                body.transform.localScale = new Vector3(scale * 0.45f, scale * 0.9f, scale * 0.35f);

                var col = body.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            var sw = fishObj.AddComponent<SwimmingFishVisual>();
            sw.Initialize(tankBounds, f.speciesId, f.isBoss, f.weightKg);

            activeFishVisuals[f.instanceId] = sw;
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
                case "lobster": path = "Assets/Prefabs/Fish/Fish_Lobster_3D.prefab"; break;
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
                case "salmon": return 0.65f;
                case "cod": return 0.62f;
                case "herring": return 0.55f;
                case "arctic_tuna": return 0.70f;
                case "snow_crab": return 0.52f;
                case "lobster": return 0.55f;
                case "clownfish": return 0.48f;
                case "blue_tang": return 0.52f;
                case "yellow_tang": return 0.50f;
                case "longnose_butterfly": return 0.52f;
                case "green_chromis": return 0.45f;
                case "pufferfish": return 0.52f;
                case "reef_shark": return 0.78f;
                case "eagle_ray": return 0.72f;
                case "thresher_shark": return 0.80f;
                case "manta_ray": return 0.75f;
                case "great_white_shark": return 0.85f;
                default: return 0.60f;
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
                    claimBtnText.text = $"COLLECT: +{NumberFormatter.Format(Mathf.FloorToInt(pending))} G";
                }
            }
        }

        private void BuildBillboardUI()
        {
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();

            // 1. Sleek Side Information Stand (Positioned on dock plaza to the left of entrance walkway, 100% off the glass)
            GameObject sideKioskObj = new GameObject("Aquarium_SideInfoStand");
            sideKioskObj.transform.SetParent(transform, false);
            sideKioskObj.transform.localPosition = new Vector3(-4.0f, 1.35f, -1.8f);
            sideKioskObj.transform.localRotation = Quaternion.Euler(15f, 25f, 0f);

            Canvas kioskCanvas = sideKioskObj.AddComponent<Canvas>();
            kioskCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform kioskRt = sideKioskObj.GetComponent<RectTransform>();
            kioskRt.sizeDelta = new Vector2(190, 85);
            kioskRt.localScale = Vector3.one * 0.009f;

            // Kiosk background button (clickable to open modal)
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(sideKioskObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(190, 85);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = whiteSp;
            bgImg.color = new Color(0.08f, 0.12f, 0.20f, 0.92f);
            Button kioskBtn = bgObj.AddComponent<Button>();
            kioskBtn.onClick.AddListener(OpenAquariumModal);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bgObj.transform, false);
            var title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "✦ AQUARIUM ✦";
            title.fontSize = 17;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.35f, 0.88f, 1f);
            title.alignment = TextAlignmentOptions.Center;
            title.ApplyFont(UIFontCategory.Header);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0, 26);
            tRt.sizeDelta = new Vector2(180, 22);

            // Fish Count
            GameObject cntObj = new GameObject("FishCount");
            cntObj.transform.SetParent(bgObj.transform, false);
            fishCountText = cntObj.AddComponent<TextMeshProUGUI>();
            fishCountText.text = "Exhibited: 0 / 6 Fish";
            fishCountText.fontSize = 14;
            fishCountText.color = Color.white;
            fishCountText.alignment = TextAlignmentOptions.Center;
            fishCountText.ApplyFont(UIFontCategory.Default);
            RectTransform cRt = cntObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(0, 6);
            cRt.sizeDelta = new Vector2(180, 18);

            // Revenue Rate
            GameObject revObj = new GameObject("RevenueRate");
            revObj.transform.SetParent(bgObj.transform, false);
            revenueText = revObj.AddComponent<TextMeshProUGUI>();
            revenueText.text = "Income: +0 G/s";
            revenueText.fontSize = 13;
            revenueText.color = new Color(1f, 0.85f, 0.2f);
            revenueText.alignment = TextAlignmentOptions.Center;
            revenueText.ApplyFont(UIFontCategory.Numbers);
            RectTransform rRt = revObj.GetComponent<RectTransform>();
            rRt.anchoredPosition = new Vector2(0, -12);
            rRt.sizeDelta = new Vector2(180, 16);

            // Tap hint
            GameObject hintObj = new GameObject("TapHint");
            hintObj.transform.SetParent(bgObj.transform, false);
            var hintTxt = hintObj.AddComponent<TextMeshProUGUI>();
            hintTxt.text = "[ Tap to Manage ]";
            hintTxt.fontSize = 11;
            hintTxt.color = new Color(0.6f, 0.75f, 0.85f);
            hintTxt.alignment = TextAlignmentOptions.Center;
            RectTransform hRt = hintObj.GetComponent<RectTransform>();
            hRt.anchoredPosition = new Vector2(0, -28);
            hRt.sizeDelta = new Vector2(180, 14);

            // 2. Floating Quick Claim Bubble (High above tank roof at Y = 3.45m, zero obstruction of glass)
            GameObject claimCanvasObj = new GameObject("Aquarium_QuickClaimCanvas");
            claimCanvasObj.transform.SetParent(transform, false);
            claimCanvasObj.transform.localPosition = new Vector3(0.0f, 3.45f, 0.0f);

            Canvas claimCanvas = claimCanvasObj.AddComponent<Canvas>();
            claimCanvas.renderMode = RenderMode.WorldSpace;
            claimCanvasObj.AddComponent<BillboardLookAtCamera>();

            RectTransform claimCanvasRt = claimCanvasObj.GetComponent<RectTransform>();
            claimCanvasRt.sizeDelta = new Vector2(200, 42);
            claimCanvasRt.localScale = Vector3.one * 0.010f;

            claimButtonObj = new GameObject("QuickClaimBtn");
            claimButtonObj.transform.SetParent(claimCanvasObj.transform, false);
            RectTransform clRt = claimButtonObj.AddComponent<RectTransform>();
            clRt.anchoredPosition = Vector2.zero;
            clRt.sizeDelta = new Vector2(200, 42);

            Image clImg = claimButtonObj.AddComponent<Image>();
            clImg.sprite = whiteSp;
            clImg.color = new Color(0.12f, 0.75f, 0.38f, 0.95f);

            Button clBtn = claimButtonObj.AddComponent<Button>();
            clBtn.onClick.AddListener(() =>
            {
                if (AquariumManager.Instance != null)
                {
                    int earned = AquariumManager.Instance.CollectTicketRevenue();
                    if (earned > 0 && FlyingCoinManager.Instance != null)
                    {
                        FlyingCoinManager.Instance.PlayFlyingCoins(transform.position + Vector3.up * 2.5f, 6);
                    }
                }
            });

            GameObject clTxtObj = new GameObject("Text");
            clTxtObj.transform.SetParent(claimButtonObj.transform, false);
            claimBtnText = clTxtObj.AddComponent<TextMeshProUGUI>();
            claimBtnText.text = "💰 COLLECT: +0 G";
            claimBtnText.fontSize = 15;
            claimBtnText.fontStyle = FontStyles.Bold;
            claimBtnText.color = Color.white;
            claimBtnText.alignment = TextAlignmentOptions.Center;
            claimBtnText.ApplyFont(UIFontCategory.Button);
            clTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(190, 36);

            claimButtonObj.SetActive(false);
        }
    }
}

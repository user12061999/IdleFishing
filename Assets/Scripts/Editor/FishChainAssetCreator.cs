#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IdleFishing.Data;

namespace IdleFishing.Editor
{
    public static class FishChainAssetCreator
    {
        private const string BASE_DATA_PATH = "Assets/GameData";
        private const string ITEMS_PATH = "Assets/GameData/Items";
        private const string CHAINS_PATH = "Assets/GameData/FishChains";
        private const string UPGRADES_PATH = "Assets/GameData/Upgrades";
        private const string WORLDS_PATH = "Assets/GameData/Worlds";
        private const string BALANCE_PATH = "Assets/GameData/Balance";

        private const string FISH_PREFABS_PATH = "Assets/Prefabs/Fish";
        private const string LAYERLAB_FISH_BASE = "Assets/Layer Lab/3D Props/3D Props Fish/3D Props Fish";

        [MenuItem("Idle Fishing/Generate All Fish Assets & Recipes", false, 1)]
        public static void GenerateAllData()
        {
            EnsureDirectories();
            UpgradeLayerLabMaterialsToURP();
            BuildCalibratedFishPrefabs();

            // ==============================================================
            // === LEVEL 1 (WORLD 1): VỊNH ÔN ĐỚI (TEMPERATE BAY) ===
            // ==============================================================
            var herringChain = CreateCompleteChain(
                speciesId: "herring",
                displayName: "Fresh Herring",
                wholePrefabName: "Fish_Herring_3D.prefab",
                wholePrice: 20,
                tier: 1,
                difficulty: 0.6f, weight: 160f,
                minKg: 0.8f, maxKg: 2.2f
            );

            var greenChromisChain = CreateCompleteChain(
                speciesId: "green_chromis",
                displayName: "Green Chromis",
                wholePrefabName: "Fish_GreenChromis_3D.prefab",
                wholePrice: 35,
                tier: 1,
                difficulty: 0.8f, weight: 120f,
                minKg: 1.0f, maxKg: 2.8f
            );

            var codChain = CreateCompleteChain(
                speciesId: "cod",
                displayName: "Fresh Cod",
                wholePrefabName: "Fish_Cod_3D.prefab",
                wholePrice: 50,
                tier: 1,
                difficulty: 1.1f, weight: 80f,
                minKg: 1.5f, maxKg: 4.2f
            );

            var salmonChain = CreateCompleteChain(
                speciesId: "salmon",
                displayName: "Fresh Salmon",
                wholePrefabName: "Fish_Salmon_3D.prefab",
                wholePrice: 95,
                tier: 2,
                difficulty: 1.6f, weight: 45f,
                minKg: 2.5f, maxKg: 6.5f
            );

            var reefSharkChain = CreateCompleteChain(
                speciesId: "reef_shark",
                displayName: "Blacktip Reef Shark",
                wholePrefabName: "Fish_ReefShark_Boss_3D.prefab",
                wholePrice: 480,
                tier: 2,
                difficulty: 2.4f, weight: 18f,
                minKg: 18.0f, maxKg: 45.0f,
                isApexBoss: true,
                bossTitle: "Blacktip Reef Shark"
            );

            // ==============================================================
            // === LEVEL 2 (WORLD 2): ĐẢO SAN HÔ NHIỆT ĐỚI (TROPICAL CORAL ATOLL) ===
            // ==============================================================
            var clownChain = CreateCompleteChain(
                speciesId: "clownfish",
                displayName: "Darwin Clownfish",
                wholePrefabName: "Fish_Clownfish_3D.prefab",
                wholePrice: 150,
                tier: 2,
                difficulty: 1.8f, weight: 90f,
                minKg: 1.2f, maxKg: 3.5f
            );

            var yellowTangChain = CreateCompleteChain(
                speciesId: "yellow_tang",
                displayName: "Yellow Tang",
                wholePrefabName: "Fish_YellowTang_3D.prefab",
                wholePrice: 180,
                tier: 2,
                difficulty: 2.0f, weight: 70f,
                minKg: 1.5f, maxKg: 4.0f
            );

            var blueTangChain = CreateCompleteChain(
                speciesId: "blue_tang",
                displayName: "Powder Blue Tang",
                wholePrefabName: "Fish_BlueTang_3D.prefab",
                wholePrice: 220,
                tier: 2,
                difficulty: 2.2f, weight: 55f,
                minKg: 1.8f, maxKg: 4.8f
            );

            var longnoseChain = CreateCompleteChain(
                speciesId: "longnose_butterfly",
                displayName: "Longnose Butterflyfish",
                wholePrefabName: "Fish_LongnoseButterfly_3D.prefab",
                wholePrice: 260,
                tier: 2,
                difficulty: 2.4f, weight: 40f,
                minKg: 2.0f, maxKg: 5.2f
            );

            var eagleRayChain = CreateCompleteChain(
                speciesId: "eagle_ray",
                displayName: "Spotted Eagle Ray",
                wholePrefabName: "Fish_EagleRay_Boss_3D.prefab",
                wholePrice: 850,
                tier: 3,
                difficulty: 2.8f, weight: 16f,
                minKg: 25.0f, maxKg: 65.0f,
                isApexBoss: true,
                bossTitle: "Spotted Eagle Ray"
            );

            // ==============================================================
            // === LEVEL 3 (WORLD 3): VỊNH RỪNG PHONG THU (GOLDEN AUTUMN COVE) ===
            // ==============================================================
            var roseGobyChain = CreateCompleteChain(
                speciesId: "rose_goby",
                displayName: "Pretty Rose Goby",
                wholePrefabName: "Fish_RoseGoby_3D.prefab",
                wholePrice: 280,
                tier: 2,
                difficulty: 2.2f, weight: 80f,
                minKg: 1.6f, maxKg: 4.5f
            );

            var flameAngelfishChain = CreateCompleteChain(
                speciesId: "flame_angelfish",
                displayName: "Flame Angelfish",
                wholePrefabName: "Fish_FlameAngelfish_3D.prefab",
                wholePrice: 350,
                tier: 3,
                difficulty: 2.6f, weight: 65f,
                minKg: 2.0f, maxKg: 5.5f
            );

            var lobsterChain = CreateCompleteChain(
                speciesId: "lobster",
                displayName: "Spiny Lobster",
                wholePrefabName: "Fish_Lobster_3D.prefab",
                wholePrice: 450,
                tier: 3,
                difficulty: 2.9f, weight: 45f,
                minKg: 2.5f, maxKg: 6.8f
            );

            var pantherGrouperChain = CreateCompleteChain(
                speciesId: "panther_grouper",
                displayName: "Panther Grouper",
                wholePrefabName: "Fish_PantherGrouper_3D.prefab",
                wholePrice: 550,
                tier: 3,
                difficulty: 3.2f, weight: 35f,
                minKg: 3.5f, maxKg: 9.0f
            );

            var mantaRayChain = CreateCompleteChain(
                speciesId: "manta_ray",
                displayName: "Emperor Manta Ray",
                wholePrefabName: "Fish_MantaRay_Boss_3D.prefab",
                wholePrice: 1600,
                tier: 4,
                difficulty: 3.8f, weight: 14f,
                minKg: 55.0f, maxKg: 130.0f,
                isApexBoss: true,
                bossTitle: "Emperor Manta Ray"
            );

            // ==============================================================
            // === LEVEL 4 (WORLD 4): VỊNH BĂNG BẮC CỰC (ARCTIC FROST HAVEN) ===
            // ==============================================================
            var tunaChain = CreateCompleteChain(
                speciesId: "arctic_tuna",
                displayName: "Arctic Tuna",
                wholePrefabName: "Fish_ArcticTuna_3D.prefab",
                wholePrice: 650,
                tier: 3,
                difficulty: 3.2f, weight: 70f,
                minKg: 4.0f, maxKg: 12.0f
            );

            var crabChain = CreateCompleteChain(
                speciesId: "snow_crab",
                displayName: "Snow Crab",
                wholePrefabName: "Fish_SnowCrab_3D.prefab",
                wholePrice: 750,
                tier: 3,
                difficulty: 3.4f, weight: 50f,
                minKg: 2.5f, maxKg: 7.0f
            );

            var zebraSeahorseChain = CreateCompleteChain(
                speciesId: "zebra_seahorse",
                displayName: "Zebra Seahorse",
                wholePrefabName: "Fish_ZebraSeahorse_3D.prefab",
                wholePrice: 850,
                tier: 3,
                difficulty: 3.6f, weight: 35f,
                minKg: 2.0f, maxKg: 5.5f
            );

            var queenAngelfishChain = CreateCompleteChain(
                speciesId: "queen_angelfish",
                displayName: "Queen Angelfish",
                wholePrefabName: "Fish_QueenAngelfish_3D.prefab",
                wholePrice: 1100,
                tier: 4,
                difficulty: 3.9f, weight: 25f,
                minKg: 3.5f, maxKg: 9.5f
            );

            var thresherSharkChain = CreateCompleteChain(
                speciesId: "thresher_shark",
                displayName: "Arctic Thresher Shark",
                wholePrefabName: "Fish_ThresherShark_Boss_3D.prefab",
                wholePrice: 2800,
                tier: 4,
                difficulty: 4.2f, weight: 12f,
                minKg: 35.0f, maxKg: 85.0f,
                isApexBoss: true,
                bossTitle: "Arctic Thresher Shark"
            );

            // ==============================================================
            // === LEVEL 5 (WORLD 5): ỐC ĐẢO SA MẠC HOÀNG KIM (SUNKEN OASIS DESERT) ===
            // ==============================================================
            var yellowfinTunaChain = CreateCompleteChain(
                speciesId: "yellowfin_tuna",
                displayName: "Golden Yellowfin Tuna",
                wholePrefabName: "Fish_YellowfinTuna_3D.prefab",
                wholePrice: 1400,
                tier: 4,
                difficulty: 3.8f, weight: 60f,
                minKg: 6.0f, maxKg: 18.0f
            );

            var pufferChain = CreateCompleteChain(
                speciesId: "pufferfish",
                displayName: "Sunlight Pufferfish",
                wholePrefabName: "Fish_Pufferfish_3D.prefab",
                wholePrice: 1750,
                tier: 4,
                difficulty: 4.0f, weight: 45f,
                minKg: 3.0f, maxKg: 8.5f
            );

            var sailfinTangChain = CreateCompleteChain(
                speciesId: "sailfin_tang",
                displayName: "Monarch Sailfin Tang",
                wholePrefabName: "Fish_SailfinTang_3D.prefab",
                wholePrice: 2100,
                tier: 4,
                difficulty: 4.3f, weight: 30f,
                minKg: 4.0f, maxKg: 11.0f
            );

            var greenTurtleChain = CreateCompleteChain(
                speciesId: "green_turtle",
                displayName: "Ancient Green Turtle",
                wholePrefabName: "Fish_GreenTurtle_3D.prefab",
                wholePrice: 2600,
                tier: 4,
                difficulty: 4.5f, weight: 20f,
                minKg: 12.0f, maxKg: 35.0f
            );

            var greatWhiteChain = CreateCompleteChain(
                speciesId: "great_white_shark",
                displayName: "Great White Leviathan",
                wholePrefabName: "Fish_GreatWhiteShark_Boss_3D.prefab",
                wholePrice: 6500,
                tier: 5,
                difficulty: 4.9f, weight: 10f,
                minKg: 80.0f, maxKg: 220.0f,
                isApexBoss: true,
                bossTitle: "Great White Leviathan"
            );

            // 4. Default UpgradeConfig & GameBalanceConfig
            var upgradeConfig = CreateDefaultUpgradeConfig();
            CreateDefaultGameBalanceConfig();

            // 5. Create 5 WorldData ScriptableObjects
            CreateWorldDataAssets(
                w1Fish: new[] { herringChain, greenChromisChain, codChain, salmonChain, reefSharkChain },
                w2Fish: new[] { clownChain, yellowTangChain, blueTangChain, longnoseChain, eagleRayChain },
                w3Fish: new[] { roseGobyChain, flameAngelfishChain, lobsterChain, pantherGrouperChain, mantaRayChain },
                w4Fish: new[] { tunaChain, crabChain, zebraSeahorseChain, queenAngelfishChain, thresherSharkChain },
                w5Fish: new[] { yellowfinTunaChain, pufferChain, sailfinTangChain, greenTurtleChain, greatWhiteChain },
                upgradeConfig: upgradeConfig
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[IdleFishing] All 5 Lands & Calibrated 3D Fish Chains created successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(BASE_DATA_PATH)) Directory.CreateDirectory(BASE_DATA_PATH);
            if (!Directory.Exists(ITEMS_PATH)) Directory.CreateDirectory(ITEMS_PATH);
            if (!Directory.Exists(CHAINS_PATH)) Directory.CreateDirectory(CHAINS_PATH);
            if (!Directory.Exists(UPGRADES_PATH)) Directory.CreateDirectory(UPGRADES_PATH);
            if (!Directory.Exists(WORLDS_PATH)) Directory.CreateDirectory(WORLDS_PATH);
            if (!Directory.Exists(BALANCE_PATH)) Directory.CreateDirectory(BALANCE_PATH);
            if (!Directory.Exists(FISH_PREFABS_PATH)) Directory.CreateDirectory(FISH_PREFABS_PATH);
            AssetDatabase.Refresh();
        }

        public static void UpgradeLayerLabMaterialsToURP()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            var colorTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{LAYERLAB_FISH_BASE}/Texture/Color.png");
            string[] matNames = new[] { "Color.mat", "Ground1.mat", "Sea.mat" };

            foreach (var matName in matNames)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{LAYERLAB_FISH_BASE}/Materials/{matName}");
                if (mat != null)
                {
                    Texture mainTex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
                    if (mainTex == null && mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");
                    if (mainTex == null && matName == "Color.mat") mainTex = colorTex;

                    Color col = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                        : (mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white);

                    mat.shader = urpLit;
                    if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                    mat.SetColor("_BaseColor", col);
                    mat.SetFloat("_Smoothness", 0.25f);
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        public static void BuildCalibratedFishPrefabs()
        {
            if (!Directory.Exists(FISH_PREFABS_PATH)) Directory.CreateDirectory(FISH_PREFABS_PATH);

            var fishCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{LAYERLAB_FISH_BASE}/Ani/fish.controller");
            var lobsterCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{LAYERLAB_FISH_BASE}/Ani/lobster.controller");
            var sharkCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{LAYERLAB_FISH_BASE}/Ani/shark.controller");
            var rayCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{LAYERLAB_FISH_BASE}/Ani/ray.controller");
            var seahorseCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{LAYERLAB_FISH_BASE}/Ani/seahorse.controller");
            var turtleCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{LAYERLAB_FISH_BASE}/Ani/turtle.controller");

            // --- World 1 (Level 1) ---
            CreateSingleCalibratedFishPrefab("Fish_Herring_3D", "SkipjackTuna", fishCtrl, 1.05f);
            CreateSingleCalibratedFishPrefab("Fish_GreenChromis_3D", "GreenChromis", fishCtrl, 2.60f);
            CreateSingleCalibratedFishPrefab("Fish_Cod_3D", "PantherGrouper", fishCtrl, 1.85f);
            CreateSingleCalibratedFishPrefab("Fish_Salmon_3D", "YellowfinTuna", fishCtrl, 1.15f);

            // --- World 2 (Level 2) ---
            CreateSingleCalibratedFishPrefab("Fish_Clownfish_3D", "Clownfish", fishCtrl, 2.85f);
            CreateSingleCalibratedFishPrefab("Fish_YellowTang_3D", "YellowTang", fishCtrl, 2.20f);
            CreateSingleCalibratedFishPrefab("Fish_BlueTang_3D", "PowderBlueTang", fishCtrl, 2.20f);
            CreateSingleCalibratedFishPrefab("Fish_LongnoseButterfly_3D", "LongnoseButterflyfish", fishCtrl, 2.40f);

            // --- World 3 (Level 3) ---
            CreateSingleCalibratedFishPrefab("Fish_RoseGoby_3D", "PrettyRoseGoby", fishCtrl, 2.80f);
            CreateSingleCalibratedFishPrefab("Fish_FlameAngelfish_3D", "FlameAngelfish", fishCtrl, 2.40f);
            CreateSingleCalibratedFishPrefab("Fish_Lobster_3D", "Lobster", lobsterCtrl, 1.40f);
            CreateSingleCalibratedFishPrefab("Fish_PantherGrouper_3D", "PantherGrouper", fishCtrl, 2.20f);

            // --- World 4 (Level 4) ---
            CreateSingleCalibratedFishPrefab("Fish_ArcticTuna_3D", "BluefinTuna", fishCtrl, 1.25f);
            CreateSingleCalibratedFishPrefab("Fish_SnowCrab_3D", "BlueLobster", lobsterCtrl, 1.40f);
            CreateSingleCalibratedFishPrefab("Fish_ZebraSeahorse_3D", "ZebraSeahorse", seahorseCtrl, 2.50f);
            CreateSingleCalibratedFishPrefab("Fish_QueenAngelfish_3D", "QueenAngelfish", fishCtrl, 2.30f);

            // --- World 5 (Level 5) ---
            CreateSingleCalibratedFishPrefab("Fish_YellowfinTuna_3D", "YellowfinTuna", fishCtrl, 1.40f);
            CreateSingleCalibratedFishPrefab("Fish_Pufferfish_3D", "QueenAngelfish", fishCtrl, 2.35f);
            CreateSingleCalibratedFishPrefab("Fish_SailfinTang_3D", "SailfinTang", fishCtrl, 2.20f);
            CreateSingleCalibratedFishPrefab("Fish_GreenTurtle_3D", "GreenTurtle", turtleCtrl, 1.10f);

            // --- Unique Ocean Apex Bosses ---
            CreateSingleCalibratedFishPrefab("Fish_ReefShark_Boss_3D", "BlacktipReefShark", sharkCtrl, 0.36f);
            CreateSingleCalibratedFishPrefab("Fish_EagleRay_Boss_3D", "SpottedEagleRay", rayCtrl, 0.56f);
            CreateSingleCalibratedFishPrefab("Fish_MantaRay_Boss_3D", "MantaRay", rayCtrl, 0.54f);
            CreateSingleCalibratedFishPrefab("Fish_ThresherShark_Boss_3D", "CommonThresherShark", sharkCtrl, 0.36f);
            CreateSingleCalibratedFishPrefab("Fish_GreatWhiteShark_Boss_3D", "GreateWhiteShark", sharkCtrl, 0.35f);
        }

        private static void CreateSingleCalibratedFishPrefab(string outPrefabName, string sourceModelName, RuntimeAnimatorController animCtrl, float modelScale)
        {
            var srcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LAYERLAB_FISH_BASE}/Prefabs/{sourceModelName}.prefab");
            if (srcPrefab == null) return;

            GameObject root = new GameObject(outPrefabName);
            GameObject model = Object.Instantiate(srcPrefab, root.transform);
            model.name = $"Model_{sourceModelName}";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * modelScale;

            if (animCtrl != null)
            {
                var anim = model.GetComponent<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                anim.runtimeAnimatorController = animCtrl;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.applyRootMotion = false;
            }

            string savePath = $"{FISH_PREFABS_PATH}/{outPrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, savePath);
            Object.DestroyImmediate(root);
        }

        private static GameObject FindPrefab(string filename)
        {
            string fishPrefabPath = $"{FISH_PREFABS_PATH}/{filename}";
            var fishObj = AssetDatabase.LoadAssetAtPath<GameObject>(fishPrefabPath);
            if (fishObj != null) return fishObj;

            string genModelPath = $"Assets/Art/Models/Generated/{filename}";
            var genObj = AssetDatabase.LoadAssetAtPath<GameObject>(genModelPath);
            if (genObj != null) return genObj;

            string nameNoExt = Path.GetFileNameWithoutExtension(filename);
            string[] guids = AssetDatabase.FindAssets(nameNoExt + " t:Prefab");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).Equals(filename, System.StringComparison.OrdinalIgnoreCase))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            return null;
        }

        private static FishChainData CreateCompleteChain(
            string speciesId,
            string displayName,
            string wholePrefabName,
            int wholePrice,
            int tier,
            float difficulty,
            float weight,
            float minKg,
            float maxKg,
            bool isApexBoss = false,
            string bossTitle = "")
        {
            string wholeItemPath = $"{ITEMS_PATH}/{speciesId}_whole.asset";
            ItemData wholeItem = AssetDatabase.LoadAssetAtPath<ItemData>(wholeItemPath);
            if (wholeItem == null)
            {
                wholeItem = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(wholeItem, wholeItemPath);
            }

            GameObject wholePrefab = FindPrefab(wholePrefabName);
            wholeItem.Initialize(
                id: $"{speciesId}_whole",
                name: displayName,
                type: ItemType.WholeFish,
                price: wholePrice,
                icon: null,
                prefab: wholePrefab,
                desc: isApexBoss ? $"Apex ocean boss fish: {displayName}" : $"Fresh 3D fish: {displayName}"
            );
            EditorUtility.SetDirty(wholeItem);

            string chainPath = $"{CHAINS_PATH}/{speciesId}_chain.asset";
            FishChainData chain = AssetDatabase.LoadAssetAtPath<FishChainData>(chainPath);
            if (chain == null)
            {
                chain = ScriptableObject.CreateInstance<FishChainData>();
                AssetDatabase.CreateAsset(chain, chainPath);
            }

            chain.Initialize(
                id: speciesId,
                name: displayName,
                whole: wholeItem,
                fishTier: tier,
                difficulty: difficulty,
                weight: weight,
                minKg: minKg,
                maxKg: maxKg,
                apexBoss: isApexBoss,
                customBossTitle: bossTitle
            );
            EditorUtility.SetDirty(chain);

            return chain;
        }

        private static UpgradeConfig CreateDefaultUpgradeConfig()
        {
            string upgradePath = $"{UPGRADES_PATH}/DefaultUpgradeConfig.asset";
            UpgradeConfig config = AssetDatabase.LoadAssetAtPath<UpgradeConfig>(upgradePath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<UpgradeConfig>();
                AssetDatabase.CreateAsset(config, upgradePath);
            }

            SerializedObject so = new SerializedObject(config);
            SerializedProperty listProp = so.FindProperty("upgrades");

            var entries = new[]
            {
                new { type = UpgradeType.FishermanSlots, title = "Expand Fishermen", desc = "Unlock additional fishing slots on the pier (Max 5 fishermen)", baseCost = 100, mult = 2.50f, baseVal = 1.0f, valPerLvl = 1.0f, maxLvl = 4 },
                new { type = UpgradeType.FishermanSpeed, title = "Fishing Technique", desc = "Fishermen reel in fish faster, increasing catch rate", baseCost = 50, mult = 1.55f, baseVal = 1.0f, valPerLvl = 0.15f, maxLvl = 10 },
                new { type = UpgradeType.HireWrangler, title = "Hire Fish Wranglers", desc = "Hire wranglers to battle and carry fish (Max 4 wranglers)", baseCost = 100, mult = 2.20f, baseVal = 0.0f, valPerLvl = 1.0f, maxLvl = 4 },
                new { type = UpgradeType.WranglerPower, title = "Wrangler Gear", desc = "Boost wrangler power, health, speed & tap damage", baseCost = 70, mult = 1.55f, baseVal = 1.0f, valPerLvl = 0.25f, maxLvl = 10 },
                new { type = UpgradeType.KitchenSpeed, title = "Express Logistics", desc = "Increase transporter cart speed and delivery rate", baseCost = 80, mult = 1.55f, baseVal = 1.0f, valPerLvl = 0.20f, maxLvl = 10 },
                new { type = UpgradeType.MarketValueMultiplier, title = "Seafood Brand", desc = "Increase wholesale value and gold earned at the market stall", baseCost = 120, mult = 1.65f, baseVal = 1.0f, valPerLvl = 0.15f, maxLvl = 10 }
            };

            listProp.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty elem = listProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("type").enumValueIndex = (int)entries[i].type;
                elem.FindPropertyRelative("title").stringValue = entries[i].title;
                elem.FindPropertyRelative("description").stringValue = entries[i].desc;
                elem.FindPropertyRelative("baseCost").intValue = entries[i].baseCost;
                elem.FindPropertyRelative("costMultiplier").floatValue = entries[i].mult;
                elem.FindPropertyRelative("baseValue").floatValue = entries[i].baseVal;
                elem.FindPropertyRelative("valueIncreasePerLevel").floatValue = entries[i].valPerLvl;
                elem.FindPropertyRelative("maxLevel").intValue = entries[i].maxLvl;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void CreateDefaultGameBalanceConfig()
        {
            string path = $"{BALANCE_PATH}/DefaultGameBalanceConfig.asset";
            GameBalanceConfig balance = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(path);
            if (balance == null)
            {
                balance = ScriptableObject.CreateInstance<GameBalanceConfig>();
                AssetDatabase.CreateAsset(balance, path);
            }

            balance.wranglerBaseHealth = 120;
            balance.wranglerBaseDamage = 15;
            balance.wranglerAttackInterval = 0.85f;
            balance.wranglerKnockoutDuration = 4.0f;
            balance.wranglerBaseMoveSpeed = 3.2f;
            balance.playerClickBaseDamage = 12;
            balance.fishHopIntervalMin = 0.8f;
            balance.fishHopIntervalMax = 1.4f;
            balance.fishHopDistance = 1.2f;
            balance.fishHopHeight = 0.85f;
            balance.bossSpawnChance = 0.10f;
            balance.bossHealthMultiplier = 3.5f;
            balance.bossDamageMultiplier = 1.5f;
            balance.bossScaleMultiplier = 1.8f;
            balance.bossBaseBonusGold = 180;
            balance.offlineBaseCoinsPerSecond = 0.4f;
            balance.offlineMinAwaySeconds = 30f;
            balance.offlineMaxHours = 6.0f;
            balance.fishermanBaseCastTime = 5.5f;
            balance.maxPendingFishOnGround = 3;
            balance.treasureChestSpawnChance = 0.08f;
            balance.treasureChestRequiredTaps = 5;
            balance.treasureChestMinGold = 250;
            balance.treasureChestMaxGold = 500;
            balance.frenzyDuration = 20.0f;
            balance.frenzyChargePerFish = 4.0f;
            balance.frenzyChargePerDefeat = 6.0f;
            balance.frenzyChargePerTap = 1.0f;
            balance.frenzyChargePerChest = 40.0f;
            balance.frenzySpeedMultiplier = 2.0f;
            balance.frenzyDamageMultiplier = 2.0f;

            EditorUtility.SetDirty(balance);
        }

        private static void CreateWorldDataAssets(
            FishChainData[] w1Fish,
            FishChainData[] w2Fish,
            FishChainData[] w3Fish,
            FishChainData[] w4Fish,
            FishChainData[] w5Fish,
            UpgradeConfig upgradeConfig)
        {
            // Level 1: Temperate Bay
            CreateSingleWorld(
                id: "world_1",
                name: "Temperate Bay",
                desc: "Vịnh biển ôn đới mùa xuân xanh tươi với cá hồi, cá tuyết và cá trích trù phú.",
                order: 1,
                waterCol: new Color(0.12f, 0.50f, 0.70f),
                groundCol: new Color(0.35f, 0.55f, 0.35f),
                lightCol: new Color(1f, 0.96f, 0.88f),
                species: w1Fish,
                startCoins: 50,
                reqCoins: 0,
                reqUpgrades: 0,
                uConfig: upgradeConfig
            );

            // Level 2: Tropical Coral Atoll (Requires 1,500 G in Level 1)
            CreateSingleWorld(
                id: "world_2",
                name: "Tropical Coral Atoll",
                desc: "Quần đảo san hô nhiệt đới mùa hè rực rỡ, bãi cát vàng óng và rạn san hô đa sắc.",
                order: 2,
                waterCol: new Color(0.05f, 0.75f, 0.82f),
                groundCol: new Color(0.85f, 0.78f, 0.48f),
                lightCol: new Color(1f, 0.98f, 0.82f),
                species: w2Fish,
                startCoins: 150,
                reqCoins: 1500,
                reqUpgrades: 10,
                uConfig: upgradeConfig
            );

            // Level 3: Golden Autumn Cove (Requires 6,000 G in Level 2)
            CreateSingleWorld(
                id: "world_3",
                name: "Golden Autumn Cove",
                desc: "Vịnh đá rừng phong mùa thu lãng mạn, lá đỏ cam rực rỡ và các loài giáp xác quý hiếm.",
                order: 3,
                waterCol: new Color(0.08f, 0.38f, 0.58f),
                groundCol: new Color(0.68f, 0.50f, 0.28f),
                lightCol: new Color(1f, 0.82f, 0.60f),
                species: w3Fish,
                startCoins: 350,
                reqCoins: 6000,
                reqUpgrades: 25,
                uConfig: upgradeConfig
            );

            // Level 4: Arctic Frost Haven (Requires 25,000 G in Level 3)
            CreateSingleWorld(
                id: "world_4",
                name: "Arctic Frost Haven",
                desc: "Vịnh băng tuyết vĩnh cửu Bắc Cực, nơi cư ngụ của cá ngừ đại dương và cua tuyết khổng lồ.",
                order: 4,
                waterCol: new Color(0.04f, 0.28f, 0.48f),
                groundCol: new Color(0.88f, 0.92f, 0.98f),
                lightCol: new Color(0.85f, 0.92f, 1.0f),
                species: w4Fish,
                startCoins: 800,
                reqCoins: 25000,
                reqUpgrades: 45,
                uConfig: upgradeConfig
            );

            // Level 5: Sunken Oasis Desert (Requires 80,000 G in Level 4)
            CreateSingleWorld(
                id: "world_5",
                name: "Sunken Oasis Desert",
                desc: "Ốc đảo sa mạc Ai Cập thần thoại, tàn tích hoàng kim và vùng nước sâu nơi ngự trị của Đại Bạch Thần Thú.",
                order: 5,
                waterCol: new Color(0.10f, 0.65f, 0.60f),
                groundCol: new Color(0.92f, 0.75f, 0.42f),
                lightCol: new Color(1f, 0.92f, 0.70f),
                species: w5Fish,
                startCoins: 2000,
                reqCoins: 80000,
                reqUpgrades: 70,
                uConfig: upgradeConfig
            );
        }

        private static void CreateSingleWorld(
            string id, string name, string desc, int order,
            Color waterCol, Color groundCol, Color lightCol,
            FishChainData[] species, int startCoins, int reqCoins, int reqUpgrades,
            UpgradeConfig uConfig)
        {
            string path = $"{WORLDS_PATH}/{id}.asset";
            WorldData w = AssetDatabase.LoadAssetAtPath<WorldData>(path);
            if (w == null)
            {
                w = ScriptableObject.CreateInstance<WorldData>();
                AssetDatabase.CreateAsset(w, path);
            }

            w.worldId = id;
            w.worldName = name;
            w.description = desc;
            w.worldOrder = order;
            w.waterColor = waterCol;
            w.groundColor = groundCol;
            w.lightColor = lightCol;
            w.lightIntensity = 1.0f;
            w.startingCoins = startCoins;
            w.unlockRequiredCoins = reqCoins;
            w.unlockRequiredUpgrades = reqUpgrades;
            w.worldUpgradeConfig = uConfig;

            w.nativeSpecies.Clear();
            if (species != null)
            {
                foreach (var s in species)
                {
                    if (s != null) w.nativeSpecies.Add(s);
                }
            }

            EditorUtility.SetDirty(w);
        }

        [MenuItem("Idle Fishing/Reset All Save Data & Progress", false, 10)]
        public static void ResetAllSaveData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[IdleFishing] Cleared all PlayerPrefs (gold, upgrades, unlocked worlds, timestamp) to test game flow!");
        }
    }
}
#endif

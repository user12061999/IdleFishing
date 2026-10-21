#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using IdleFishing.World;

namespace IdleFishing.Editor
{
    public static class ToonEnvironmentBuilder
    {
        private const string RootName = "--- TOON HARBOR ENVIRONMENT ---";
        private const string AssetBasePath = "Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite";
        private const string MarinePropsPath = "Assets/Layer Lab/3D Props/3D Props Fish/3D Props Fish/Prefabs";

        // Curated 9 perimeter framing trees (3 left flank, 3 right flank, 3 rear backdrop)
        // Leaving front offshore water channel & right-side ship sightline completely open.
        private static readonly Vector3[] TreePerimeterPositions = new Vector3[]
        {
            // Left Flank (3 trees)
            new Vector3(-8.8f, 0.0f, 8.8f),    // 0: Young Coastal Accent (~5.0m)
            new Vector3(-10.5f, 0.0f, 0.0f),   // 1: Ancient West Landmark (~8.7m)
            new Vector3(-8.2f, 0.0f, -6.5f),   // 2: Mature Southwest Canopy (~6.8m)
            // Right Flank (3 trees - kept clear of ship and pier water view)
            new Vector3(12.2f, 0.0f, 4.0f),    // 3: Mature East Canopy (~6.8m)
            new Vector3(9.8f, 0.0f, -3.2f),    // 4: Ancient Southeast Landmark (~8.7m)
            new Vector3(8.0f, 0.0f, -8.2f),    // 5: Young Southeast Accent (~5.0m)
            // Rear Horizon Backdrop (3 trees framing the south)
            new Vector3(-5.5f, 0.0f, -12.5f),  // 6: Mature Rear-West Canopy (~6.8m)
            new Vector3(0.0f, 0.0f, -13.2f),   // 7: Ancient Center Horizon Landmark (~8.7m)
            new Vector3(5.5f, 0.0f, -12.5f)    // 8: Young Rear-East Accent (~5.0m)
        };

        private static readonly float[] TreeRotationsY = new float[]
        {
            45f, -25f, 110f,
            -65f, 85f, -120f,
            35f, -15f, 125f
        };

        // Level 1 Temperate Trees (Raw prefab height = 2.0m -> scaled up to reach 5.0m - 8.7m matching custom biomes)
        private static readonly float[] TreeScalesLevel1 = new float[]
        {
            2.50f, 4.35f, 3.40f,
            3.40f, 4.35f, 2.50f,
            3.40f, 4.35f, 2.50f
        };

        // Level 2 & 5 Tropical Coconut Palm Trees (Raw prefab height = 6.5m -> 5.2m - 9.0m)
        private static readonly float[] TreeScalesPalm = new float[]
        {
            0.80f, 1.38f, 1.10f,
            1.10f, 1.38f, 0.80f,
            1.10f, 1.38f, 0.80f
        };

        // Level 3 Golden Autumn Maple Trees (Raw prefab height = 6.4m -> 5.1m - 8.8m)
        private static readonly float[] TreeScalesMaple = new float[]
        {
            0.80f, 1.38f, 1.10f,
            1.10f, 1.38f, 0.80f,
            1.10f, 1.38f, 0.80f
        };

        // Level 4 Arctic Frosted Spruces (Raw prefab height = 6.0m -> 5.1m - 8.7m)
        private static readonly float[] TreeScalesSpruce = new float[]
        {
            0.85f, 1.45f, 1.15f,
            1.15f, 1.45f, 0.85f,
            1.15f, 1.45f, 0.85f
        };

        [MenuItem("Tools/Idle Fishing/Fix Floating Trees & Rebuild Environment", false, 9)]
        [MenuItem("Tools/Idle Fishing/Build Toon Environment", false, 10)]
        public static void BuildMenuCommand()
        {
            BuildEnvironment(saveScene: true);
        }

        public static GameObject BuildEnvironment(bool saveScene = true)
        {
            // 0. Ensure proper Ground Plane and Water Area framing
            GameObject waterObj = GameObject.Find("Water Area");
            if (waterObj != null)
            {
                waterObj.transform.position = new Vector3(0f, -0.2f, 26f);
                waterObj.transform.localScale = new Vector3(50f, 0.5f, 34f);
            }

            GameObject groundObj = GameObject.Find("Ground Plane");
            if (groundObj != null)
            {
                groundObj.transform.position = new Vector3(0f, -0.45f, -10f);
                groundObj.transform.localScale = new Vector3(6f, 1f, 4f);
            }

            // 1. Remove existing environment if present
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // 2. Load Materials
            Material springMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Spring.mat");
            Material summerMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Summer.mat");
            Material fallMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Fall.mat");
            Material winterMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Winter.mat");
            Material desertMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Desert.mat");

            Material springAsphaltMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Spring_Asphalt.mat");
            Material summerAsphaltMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Summer_Asphalt.mat");
            Material fallAsphaltMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Fall_Asphalt.mat");
            Material winterAsphaltMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Winter_Asphalt.mat");
            Material desertAsphaltMat = AssetDatabase.LoadAssetAtPath<Material>($"{AssetBasePath}/Materials/Desert_Asphalt.mat");

            // 3. Load Prefabs
            GameObject cabinPrefab = LoadPrefab("Props/Cabin");
            GameObject treePrefab = LoadPrefab("Props/Tree");
            GameObject treeChoppedPrefab = LoadPrefab("Props/Tree_Chopped");
            GameObject shrubPrefab = LoadPrefab("Props/Shrub");
            GameObject grassPrefab = LoadPrefab("Props/Grass");
            GameObject flowerPrefab = LoadPrefab("Props/Flower");
            GameObject rockPrefab = LoadPrefab("Props/Rock4");
            GameObject stonePrefab = LoadPrefab("Props/Stone1");
            GameObject ladderPrefab = LoadPrefab("Props/Ladder");
            GameObject cloudBoxPrefab = LoadPrefab("Props/Cloud_Box");
            GameObject cloudLongPrefab = LoadPrefab("Props/Cloud_Long");

            GameObject tileEdgePrefab = LoadPrefab("Tiles/Tile_Edge");
            GameObject tileCornerPrefab = LoadPrefab("Tiles/Tile_Corner");

            // Custom Biome Prefabs (Tropical Palm Trees, Tiki Bungalows)
            GameObject palmTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Tropical_PalmTree.prefab");
            GameObject tikiBungalowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Tropical_TikiBungalow.prefab");

            // Level 3 Autumn Assets
            GameObject autumnLodgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_TimberLodge.prefab") ?? cabinPrefab;
            GameObject autumnMapleTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_MapleTree.prefab") ?? treePrefab;
            GameObject autumnBarrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_HarvestBarrel.prefab");
            GameObject autumnPumpkinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_Pumpkin.prefab");
            GameObject autumnHarvestCartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_HarvestCart.prefab");
            GameObject autumnCampfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_Campfire.prefab");
            GameObject autumnPicnicTablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_PicnicTable.prefab");
            GameObject autumnRowboatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_Rowboat.prefab");
            GameObject autumnMushroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_MushroomCluster.prefab");
            GameObject autumnLanternPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Autumn_LanternPost.prefab");

            // Level 4 Arctic Assets
            GameObject arcticCabinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Arctic_SnowCabin.prefab") ?? cabinPrefab;
            GameObject arcticFrostedSprucePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Arctic_FrostedSpruce.prefab") ?? treePrefab;
            GameObject glacialIcebergPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Glacial_Iceberg.prefab");
            GameObject floatingIceFloePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Floating_IceFloe.prefab");
            GameObject arcticIglooPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Arctic_Igloo.prefab");
            GameObject arcticSnowmanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Arctic_Snowman.prefab");
            GameObject arcticBonfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Arctic_Bonfire.prefab");
            GameObject arcticSledPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Arctic_Sled.prefab");
            GameObject arcticSupplyCratesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Arctic_SupplyCrates.prefab");
            GameObject arcticIceSpirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Arctic_IceSpire.prefab");
            GameObject arcticSignpostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Arctic_Signpost.prefab");

            // Level 5 Desert Assets
            GameObject desertPavilionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Desert_OasisPavilion.prefab") ?? (tikiBungalowPrefab ?? cabinPrefab);

            // Ground Terrain & Pathway Prefabs
            GameObject islandBasePrefab          = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Island_Base.prefab");
            GameObject cobblestoneStraightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Cobblestone_Straight.prefab");
            GameObject cobblestoneCurvePrefab    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Cobblestone_Curve.prefab");
            GameObject cobblestonePlazaPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Cobblestone_Plaza.prefab");
            GameObject rusticFencePrefab         = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Rustic_Fence.prefab");
            GameObject gardenPatchPrefab         = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Garden_Patch.prefab");
            GameObject beachTemperatePrefab      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Beach_Temperate.prefab");
            GameObject beachTropicalPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Beach_Tropical.prefab");
            GameObject timberBoardwalkPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Timber_Boardwalk.prefab");
            GameObject slateRoadwayPrefab        = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Slate_Roadway.prefab");
            GameObject stoneCurbPrefab           = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Stone_Curb.prefab");
            GameObject autumnLeafPatchPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Autumn_LeafPatch.prefab");
            GameObject glacialIceShelfPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Glacial_IceShelf.prefab");
            GameObject snowTrailPrefab           = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Snow_Trail.prefab");
            GameObject snowDriftPrefab           = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Snow_Drift.prefab");
            GameObject desertSandstonePrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Desert_SandstonePaver.prefab");
            GameObject desertDuneRidgePrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Desert_DuneRidge.prefab");
            GameObject oasisTurfPrefab           = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terrain/Toon_Ground_Oasis_Turf.prefab");

            // 4. Create Root Object
            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Toon Harbor Environment");

            // Universal Island Base Landmass (Organic rounded island with sandy shoreline bevel)
            Transform gTerrain = CreateGroup("--- ISLAND GROUND BASE ---", root.transform);
            if (islandBasePrefab != null)
            {
                InstantiateProp(islandBasePrefab, gTerrain, Vector3.zero, Quaternion.identity, 1.0f);
            }

            // Common Permanent Bedrock Infrastructure
            Transform gRocks = CreateGroup("--- BREAKWATERS & BOULDERS ---", root.transform);
            Transform gClouds = CreateGroup("--- SKY & FLOATING CLOUDS ---", root.transform);
            Transform gCliffs = CreateGroup("--- COASTAL CLIFF TILES ---", root.transform);

            // 5 Distinct Land Decor Groups (Each level has its own unique houses, trees, pathways, and ground scenery)
            Transform d1 = CreateGroup("--- DECOR LEVEL 1 (TEMPERATE) ---", root.transform);
            Transform d2 = CreateGroup("--- DECOR LEVEL 2 (TROPICAL CORAL) ---", root.transform);
            Transform d3 = CreateGroup("--- DECOR LEVEL 3 (GOLDEN AUTUMN) ---", root.transform);
            Transform d4 = CreateGroup("--- DECOR LEVEL 4 (ARCTIC FROST) ---", root.transform);
            Transform d5 = CreateGroup("--- DECOR LEVEL 5 (DESERT OASIS) ---", root.transform);

            // ==========================================
            // C. BREAKWATERS & BOULDERS (Grounded)
            // ==========================================
            InstantiatePropAtGround(rockPrefab, gRocks, new Vector3(-7.8f, 0.0f, 11.8f), Quaternion.Euler(0f, 35f, 0f), 1.4f, 0.06f);
            InstantiatePropAtGround(stonePrefab, gRocks, new Vector3(-8.6f, 0.0f, 11.2f), Quaternion.Euler(0f, -20f, 0f), 1.2f, 0.05f);
            InstantiatePropAtGround(rockPrefab, gRocks, new Vector3(-9.2f, 0.0f, 12.8f), Quaternion.Euler(0f, 75f, 0f), 1.6f, 0.06f);
            InstantiatePropAtGround(rockPrefab, gRocks, new Vector3(7.8f, 0.0f, 11.8f), Quaternion.Euler(0f, -45f, 0f), 1.35f, 0.06f);
            InstantiatePropAtGround(stonePrefab, gRocks, new Vector3(8.5f, 0.0f, 11.0f), Quaternion.Euler(0f, 40f, 0f), 1.1f, 0.05f);
            InstantiatePropAtGround(rockPrefab, gRocks, new Vector3(9.0f, 0.0f, 13.0f), Quaternion.Euler(0f, -80f, 0f), 1.5f, 0.06f);

            // ==========================================
            // D. SKY & FLOATING CLOUDS
            // ==========================================
            CreateCloud(cloudBoxPrefab, gClouds, new Vector3(-9.5f, 11.5f, 28.0f), 1.3f, 0.45f);
            CreateCloud(cloudLongPrefab, gClouds, new Vector3(9.5f, 12.0f, 29.0f), 1.4f, 0.55f);
            CreateCloud(cloudBoxPrefab, gClouds, new Vector3(0.0f, 13.0f, 32.0f), 1.5f, 0.35f);
            CreateCloud(cloudLongPrefab, gClouds, new Vector3(-11.0f, 11.0f, 19.5f), 1.1f, -0.35f);
            CreateCloud(cloudBoxPrefab, gClouds, new Vector3(11.0f, 11.5f, 20.5f), 1.2f, -0.45f);

            // ==========================================
            // E. COASTAL CLIFF TILES
            // ==========================================
            InstantiateProp(tileEdgePrefab, gCliffs, new Vector3(-10.5f, -0.9f, 2.0f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
            InstantiateProp(tileEdgePrefab, gCliffs, new Vector3(-10.5f, -0.9f, 0.0f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
            InstantiateProp(tileCornerPrefab, gCliffs, new Vector3(-10.5f, -0.9f, 4.0f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
            InstantiateProp(tileEdgePrefab, gCliffs, new Vector3(10.5f, -0.9f, 2.0f), Quaternion.Euler(0f, -90f, 0f), 1.0f);
            InstantiateProp(tileEdgePrefab, gCliffs, new Vector3(10.5f, -0.9f, 0.0f), Quaternion.Euler(0f, -90f, 0f), 1.0f);
            InstantiateProp(tileCornerPrefab, gCliffs, new Vector3(10.5f, -0.9f, 4.0f), Quaternion.Euler(0f, -90f, 0f), 1.0f);

            // ==========================================
            // BUILD 5 DEDICATED LAND DECOR GROUPS WITH UNIQUE THEMED TERRAINS
            // ==========================================
            BuildLevel1TemperateDecor(d1, cabinPrefab, ladderPrefab, treeChoppedPrefab, treePrefab, flowerPrefab, grassPrefab, shrubPrefab, rockPrefab, stonePrefab,
                cobblestoneStraightPrefab, cobblestoneCurvePrefab, cobblestonePlazaPrefab, beachTemperatePrefab, rusticFencePrefab, gardenPatchPrefab);
            BuildLevel2TropicalCoralDecor(d2, tikiBungalowPrefab, palmTreePrefab, stonePrefab, rockPrefab,
                beachTropicalPrefab, timberBoardwalkPrefab);
            BuildLevel3AutumnDecor(d3, autumnLodgePrefab, autumnMapleTreePrefab, autumnBarrelPrefab, autumnPumpkinPrefab,
                autumnHarvestCartPrefab, autumnCampfirePrefab, autumnPicnicTablePrefab, autumnRowboatPrefab, autumnMushroomPrefab, autumnLanternPrefab,
                ladderPrefab, treeChoppedPrefab, shrubPrefab, flowerPrefab, grassPrefab, rockPrefab, stonePrefab,
                slateRoadwayPrefab, stoneCurbPrefab, autumnLeafPatchPrefab, rusticFencePrefab);
            BuildLevel4ArcticFrostDecor(d4, arcticCabinPrefab, arcticFrostedSprucePrefab, glacialIcebergPrefab, floatingIceFloePrefab, arcticIglooPrefab,
                arcticSnowmanPrefab, arcticBonfirePrefab, arcticSledPrefab, arcticSupplyCratesPrefab, arcticIceSpirePrefab, arcticSignpostPrefab,
                rockPrefab, stonePrefab,
                glacialIceShelfPrefab, snowTrailPrefab, snowDriftPrefab, timberBoardwalkPrefab);
            BuildLevel5DesertOasisDecor(d5, desertPavilionPrefab, palmTreePrefab, desertMat, rockPrefab, stonePrefab,
                desertSandstonePrefab, desertDuneRidgePrefab, oasisTurfPrefab);

            // Setup Theme Controller
            var themeCtrl = root.AddComponent<ToonEnvironmentThemeController>();
            themeCtrl.SetThemeMaterials(
                springMat, summerMat, fallMat, winterMat, desertMat,
                springAsphaltMat, summerAsphaltMat, fallAsphaltMat, winterAsphaltMat, desertAsphaltMat);
            if (groundObj != null)
            {
                themeCtrl.RegisterGroundRenderer(groundObj.GetComponent<Renderer>());
            }
            themeCtrl.RegisterDecorGroups(d1.gameObject, d2.gameObject, d3.gameObject, d4.gameObject, d5.gameObject);
            themeCtrl.CollectAllRenderers();

            // Default: Activate Level 1 Decor only
            d1.gameObject.SetActive(true);
            d2.gameObject.SetActive(false);
            d3.gameObject.SetActive(false);
            d4.gameObject.SetActive(false);
            d5.gameObject.SetActive(false);

            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[ToonEnvironmentBuilder] Successfully built 5 distinct land decor systems!");
            return root;
        }

        // --- LEVEL 1 DECOR: TEMPERATE BAY ---
        private static void BuildLevel1TemperateDecor(
            Transform g, GameObject cabin, GameObject ladder, GameObject choppedTree, GameObject tree,
            GameObject flower, GameObject grass, GameObject shrub, GameObject rock, GameObject stone,
            GameObject cobblestoneStraight, GameObject cobblestoneCurve, GameObject cobblestonePlaza, GameObject beachTemperate,
            GameObject rusticFence, GameObject gardenPatch)
        {
            // 0. Level 1 Ground Terrain & Cobblestone Pathway Network
            if (beachTemperate != null)
            {
                InstantiateProp(beachTemperate, g, new Vector3(0.0f, 0.0f, 10.6f), Quaternion.identity, 1.0f);
            }
            if (cobblestonePlaza != null)
            {
                InstantiateProp(cobblestonePlaza, g, new Vector3(0.0f, 0.0f, 2.8f), Quaternion.identity, 1.0f);
            }
            if (cobblestoneStraight != null)
            {
                // North pathway from Pier to Central Plaza
                InstantiateProp(cobblestoneStraight, g, new Vector3(0.0f, 0.0f, 8.2f), Quaternion.identity, 1.0f);
                // South pathway from Plaza to Southern border
                InstantiateProp(cobblestoneStraight, g, new Vector3(0.0f, 0.0f, -2.6f), Quaternion.identity, 1.0f);
                // Straight extensions to cabins
                InstantiateProp(cobblestoneStraight, g, new Vector3(-5.6f, 0.0f, 2.6f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
                InstantiateProp(cobblestoneStraight, g, new Vector3( 5.6f, 0.0f, 2.2f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
            }
            if (cobblestoneCurve != null)
            {
                // West curved junction from plaza
                InstantiateProp(cobblestoneCurve, g, new Vector3(-2.4f, 0.0f, 2.8f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
                // East curved junction from plaza
                InstantiateProp(cobblestoneCurve, g, new Vector3( 2.4f, 0.0f, 2.8f), Quaternion.Euler(0f, 180f, 0f), 1.0f);
            }

            // 0b. Cute Rustic Split-Rail Fences framing cabin yards
            if (rusticFence != null)
            {
                InstantiateProp(rusticFence, g, new Vector3(-9.2f, 0.0f, 5.5f), Quaternion.Euler(0f, 15f, 0f), 1.0f);
                InstantiateProp(rusticFence, g, new Vector3(-6.8f, 0.0f, -0.6f), Quaternion.Euler(0f, 105f, 0f), 1.0f);
                InstantiateProp(rusticFence, g, new Vector3( 8.2f, 0.0f, 5.2f), Quaternion.Euler(0f, -25f, 0f), 1.0f);
            }

            // 0c. Charming Vegetable Garden Patch on eastern lawn
            if (gardenPatch != null)
            {
                InstantiateProp(gardenPatch, g, new Vector3(6.8f, 0.0f, -1.8f), Quaternion.Euler(0f, -15f, 0f), 1.05f);
            }

            // 1. Rustic Log Cabins & Outposts
            InstantiatePropAtGround(cabin, g, new Vector3(-8.8f, 0.0f, 2.5f), Quaternion.Euler(0f, 55f, 0f), 1.15f, 0.02f);
            InstantiatePropAtGround(ladder, g, new Vector3(-7.8f, 0.0f, 3.2f), Quaternion.Euler(0f, 140f, 15f), 1.0f, 0.02f);
            InstantiatePropAtGround(choppedTree, g, new Vector3(-7.6f, 0.0f, 1.5f), Quaternion.Euler(0f, 25f, 0f), 1.1f, 0.02f);
            InstantiatePropAtGround(cabin, g, new Vector3(9.2f, 0.0f, 2.0f), Quaternion.Euler(0f, -55f, 0f), 1.1f, 0.02f);
            InstantiatePropAtGround(choppedTree, g, new Vector3(8.0f, 0.0f, 2.8f), Quaternion.Euler(0f, -40f, 0f), 1.05f, 0.02f);
            InstantiatePropAtGround(cabin, g, new Vector3(-12.5f, 0.0f, 12.0f), Quaternion.Euler(0f, 45f, 0f), 0.95f, 0.02f);

            // 2. Temperate European Forest Trees (Scaled 2.50x to 4.35x to match 5.0m - 8.7m canopy height)
            if (tree != null)
            {
                for (int i = 0; i < TreePerimeterPositions.Length; i++)
                {
                    InstantiatePropAtGround(tree, g, TreePerimeterPositions[i], Quaternion.Euler(0f, TreeRotationsY[i], 0f), TreeScalesLevel1[i], 0.05f);
                }
            }

            // Shrubs along paths & cabins
            InstantiatePropAtGround(shrub, g, new Vector3(-7.8f, 0.0f, 6.5f), Quaternion.identity, 1.1f, 0.03f);
            InstantiatePropAtGround(shrub, g, new Vector3(-8.8f, 0.0f, 1.0f), Quaternion.identity, 1.0f, 0.03f);
            InstantiatePropAtGround(shrub, g, new Vector3(7.8f, 0.0f, 6.5f), Quaternion.identity, 1.1f, 0.03f);
            InstantiatePropAtGround(shrub, g, new Vector3(8.8f, 0.0f, 1.0f), Quaternion.identity, 1.0f, 0.03f);
            InstantiatePropAtGround(shrub, g, new Vector3(-6.2f, 0.0f, -4.5f), Quaternion.identity, 1.05f, 0.03f);
            InstantiatePropAtGround(shrub, g, new Vector3(6.2f, 0.0f, -4.5f), Quaternion.identity, 1.05f, 0.03f);

            // Wild Spring Flowers
            InstantiatePropAtGround(flower, g, new Vector3(-7.2f, 0.0f, 8.5f), Quaternion.Euler(0f, 35f, 0f), 1.2f, 0.02f);
            InstantiatePropAtGround(flower, g, new Vector3(-6.8f, 0.0f, 5.0f), Quaternion.Euler(0f, -40f, 0f), 1.1f, 0.02f);
            InstantiatePropAtGround(flower, g, new Vector3(7.2f, 0.0f, 8.5f), Quaternion.Euler(0f, -30f, 0f), 1.2f, 0.02f);
            InstantiatePropAtGround(flower, g, new Vector3(6.8f, 0.0f, 5.0f), Quaternion.Euler(0f, 45f, 0f), 1.1f, 0.02f);
            InstantiatePropAtGround(flower, g, new Vector3(-9.2f, 0.0f, 3.2f), Quaternion.Euler(0f, 15f, 0f), 1.15f, 0.02f);
            InstantiatePropAtGround(flower, g, new Vector3(9.2f, 0.0f, 3.2f), Quaternion.Euler(0f, -60f, 0f), 1.15f, 0.02f);

            // Lush Spring Grass tufts
            InstantiatePropAtGround(grass, g, new Vector3(-8.0f, 0.0f, 4.0f), Quaternion.identity, 1.2f, 0.01f);
            InstantiatePropAtGround(grass, g, new Vector3(8.0f, 0.0f, 4.0f), Quaternion.identity, 1.2f, 0.01f);
            InstantiatePropAtGround(grass, g, new Vector3(-6.5f, 0.0f, 9.5f), Quaternion.identity, 1.3f, 0.01f);
            InstantiatePropAtGround(grass, g, new Vector3(6.5f, 0.0f, 9.5f), Quaternion.identity, 1.3f, 0.01f);

            // Coastal Smooth Stones & Boulders along Shoreline
            InstantiatePropAtGround(stone, g, new Vector3(-6.2f, 0.0f, 10.5f), Quaternion.Euler(0f, 25f, 0f), 1.4f, 0.04f);
            InstantiatePropAtGround(stone, g, new Vector3(6.2f, 0.0f, 10.5f), Quaternion.Euler(0f, -35f, 0f), 1.4f, 0.04f);
            InstantiatePropAtGround(rock, g, new Vector3(-7.0f, 0.0f, 11.2f), Quaternion.Euler(0f, 55f, 0f), 1.5f, 0.05f);
            InstantiatePropAtGround(rock, g, new Vector3(7.0f, 0.0f, 11.2f), Quaternion.Euler(0f, -50f, 0f), 1.5f, 0.05f);

            // Rustic Timber logs & Fish cleaning bench
            if (choppedTree != null)
            {
                InstantiatePropAtGround(choppedTree, g, new Vector3(-7.5f, 0.0f, 2.5f), Quaternion.Euler(0f, 20f, 0f), 1.15f, 0.02f);
                InstantiatePropAtGround(choppedTree, g, new Vector3(7.5f, 0.0f, 2.5f), Quaternion.Euler(0f, -30f, 0f), 1.15f, 0.02f);
            }

            // Starfish on Dock posts
            var starfishA = LoadMarineProp("StarfishA");
            if (starfishA != null)
            {
                InstantiateProp(starfishA, g, new Vector3(-5.3f, 0.42f, 12.2f), Quaternion.Euler(0f, 25f, 0f), 1.8f);
                InstantiateProp(starfishA, g, new Vector3(5.3f, 0.42f, 12.2f), Quaternion.Euler(0f, -45f, 0f), 1.8f);
            }

            // 19. DISTANT DEEP OCEAN VISTA (Theme 1: Ancient Lighthouse Islet, 3-Masted Merchant Tall Ship & Channel Buoys)
            Transform gDistant = CreateGroup("--- DISTANT OCEAN VISTA ---", g);
            var beaconIsland = LoadDistantOceanProp("Toon_Distant_Beacon_Island");
            var tallShip = LoadDistantOceanProp("Toon_Distant_Merchant_TallShip");
            var seaBuoy = LoadDistantOceanProp("Toon_Distant_Sea_Buoy");

            if (beaconIsland != null)
            {
                InstantiateProp(beaconIsland, gDistant, new Vector3(-6.8f, -0.2f, 29.5f), Quaternion.Euler(0f, 20f, 0f), 0.90f);
            }
            if (tallShip != null)
            {
                InstantiateProp(tallShip, gDistant, new Vector3(1.8f, -0.15f, 30.0f), Quaternion.Euler(0f, -75f, 0f), 0.95f);
            }
            if (seaBuoy != null)
            {
                InstantiateProp(seaBuoy, gDistant, new Vector3(-1.8f, -0.15f, 23.5f), Quaternion.Euler(0f, 45f, 0f), 1.0f);
                InstantiateProp(seaBuoy, gDistant, new Vector3( 6.0f, -0.15f, 26.5f), Quaternion.Euler(0f, -20f, 0f), 0.9f);
            }
        }

        // --- LEVEL 2 DECOR: TROPICAL CORAL ATOLL ---
        private static void BuildLevel2TropicalCoralDecor(
            Transform g, GameObject tiki, GameObject palm, GameObject stone, GameObject rock,
            GameObject beachTropical, GameObject timberBoardwalk)
        {
            // 0. Tropical White Coral Sand Beach Fringe
            if (beachTropical != null)
            {
                InstantiateProp(beachTropical, g, new Vector3(0.0f, 0.0f, 10.6f), Quaternion.identity, 1.0f);
            }

            // 0b. Weathered Timber Boardwalks connecting Pier & Tiki Bungalows
            if (timberBoardwalk != null)
            {
                // North boardwalk from Pier to island
                InstantiateProp(timberBoardwalk, g, new Vector3(0.0f, 0.0f, 8.2f), Quaternion.identity, 1.0f);
                // Central island spine boardwalk
                InstantiateProp(timberBoardwalk, g, new Vector3(0.0f, 0.0f, 2.8f), Quaternion.identity, 1.0f);
                // West boardwalk branch to West Tiki Bungalow
                InstantiateProp(timberBoardwalk, g, new Vector3(-4.8f, 0.0f, 2.5f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
                // East boardwalk branch to East Tiki Bungalow
                InstantiateProp(timberBoardwalk, g, new Vector3( 4.8f, 0.0f, 2.0f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
            }

            // 1. Beachfront Open-Air Tiki Bungalows with Thatched Roofs & Surfboards
            if (tiki != null)
            {
                InstantiateProp(tiki, g, new Vector3(-8.8f, 0.0f, 2.5f), Quaternion.Euler(0f, 55f, 0f), 1.1f);
                InstantiateProp(tiki, g, new Vector3(9.2f, 0.0f, 2.0f), Quaternion.Euler(0f, -55f, 0f), 1.05f);
                InstantiateProp(tiki, g, new Vector3(-12.5f, 0.0f, 12.0f), Quaternion.Euler(0f, 45f, 0f), 0.95f);
            }

            // 1b. Curving Tropical Coconut Palm Trees
            if (palm != null)
            {
                for (int i = 0; i < TreePerimeterPositions.Length; i++)
                {
                    InstantiateProp(palm, g, TreePerimeterPositions[i], Quaternion.Euler(0f, TreeRotationsY[i], 0f), TreeScalesPalm[i]);
                }
            }

            var coralAA = LoadMarineProp("CoralAA");
            var coralBA = LoadMarineProp("CoralBA");
            var coralCA = LoadMarineProp("CoralCA");
            var coralD = LoadMarineProp("CoralD");
            var coralF = LoadMarineProp("CoralF");
            var coralG = LoadMarineProp("CoralG");
            var coralIA = LoadMarineProp("CoralIA");
            var coralLA = LoadMarineProp("CoralLA");
            var coralMA = LoadMarineProp("CoralMA");
            var coralOA = LoadMarineProp("CoralOA");

            var seaweedA = LoadMarineProp("SeaweedA");
            var seaweedB = LoadMarineProp("SeaweedB");
            var seaweedC = LoadMarineProp("SeaweedC");
            var seaweedD = LoadMarineProp("SeaweedD");

            var starfishB = LoadMarineProp("StarfishB");
            var shellA = LoadMarineProp("ShellA");
            var shellB = LoadMarineProp("ShellB");
            var shellC = LoadMarineProp("ShellC");
            var shellD = LoadMarineProp("ShellD");

            // 1. Shallow Crystal Lagoon Coral Reef (Directly in front of Fishing Pier)
            InstantiateProp(coralAA, g, new Vector3(-4.0f, -0.15f, 15.6f), Quaternion.Euler(0f, 15f, 0f), 3.2f);
            InstantiateProp(coralCA, g, new Vector3(-3.3f, -0.18f, 16.2f), Quaternion.Euler(0f, 80f, 0f), 2.8f);
            InstantiateProp(seaweedA, g, new Vector3(-4.6f, -0.35f, 16.0f), Quaternion.Euler(0f, 40f, 0f), 1.4f);

            InstantiateProp(coralF, g, new Vector3(0.0f, -0.15f, 17.0f), Quaternion.Euler(0f, -35f, 0f), 3.0f);
            InstantiateProp(coralLA, g, new Vector3(0.8f, -0.18f, 17.5f), Quaternion.Euler(0f, 65f, 0f), 2.8f);
            InstantiateProp(seaweedB, g, new Vector3(-0.6f, -0.35f, 17.4f), Quaternion.Euler(0f, -20f, 0f), 1.5f);

            InstantiateProp(coralMA, g, new Vector3(1.0f, -0.15f, 15.4f), Quaternion.Euler(0f, 45f, 0f), 2.8f);
            InstantiateProp(coralOA, g, new Vector3(-1.8f, -0.18f, 18.2f), Quaternion.Euler(0f, -75f, 0f), 2.8f);
            InstantiateProp(seaweedC, g, new Vector3(-2.8f, -0.35f, 17.0f), Quaternion.Euler(0f, 15f, 0f), 1.4f);

            // Flanking Deep Corals (West reefs, keeping eastern ship channel clear)
            InstantiateProp(coralD, g, new Vector3(-5.8f, -0.2f, 17.8f), Quaternion.Euler(0f, -20f, 0f), 3.4f);
            InstantiateProp(coralG, g, new Vector3(-6.8f, -0.2f, 19.0f), Quaternion.Euler(0f, 30f, 0f), 3.2f);
            InstantiateProp(seaweedD, g, new Vector3(-5.2f, -0.35f, 18.2f), Quaternion.Euler(0f, 50f, 0f), 1.6f);
            InstantiateProp(seaweedD, g, new Vector3(-4.5f, -0.35f, 18.8f), Quaternion.Euler(0f, -40f, 0f), 1.6f);

            // 2. Animated Tropical Marine Wildlife
            InstantiateAnimatedMarineCreature("GreenTurtle", "turtle", g, new Vector3(-2.6f, -0.18f, 15.8f), Quaternion.Euler(5f, 40f, -6f), 1.7f);
            InstantiateAnimatedMarineCreature("MantaRay", "ray", g, new Vector3(3.4f, -0.30f, 17.6f), Quaternion.Euler(-3f, -35f, 4f), 1.5f);

            // 3. Sandy Shoreline Corals, Shells & Boulders
            InstantiateProp(coralBA, g, new Vector3(-6.4f, 0.02f, 11.8f), Quaternion.Euler(0f, 30f, 0f), 2.8f);
            InstantiateProp(coralIA, g, new Vector3(6.4f, 0.02f, 11.8f), Quaternion.Euler(0f, -30f, 0f), 2.8f);

            InstantiateProp(shellA, g, new Vector3(-6.1f, 0.12f, 10.6f), Quaternion.Euler(0f, 70f, 0f), 2.5f);
            InstantiateProp(shellB, g, new Vector3(6.1f, 0.12f, 10.8f), Quaternion.Euler(0f, -65f, 0f), 2.5f);
            if (shellC != null) InstantiateProp(shellC, g, new Vector3(-5.2f, 0.18f, 9.8f), Quaternion.Euler(0f, 15f, 0f), 2.2f);
            if (shellD != null) InstantiateProp(shellD, g, new Vector3(5.2f, 0.18f, 9.8f), Quaternion.Euler(0f, -25f, 0f), 2.2f);

            InstantiateProp(starfishB, g, new Vector3(6.6f, 0.25f, 10.4f), Quaternion.Euler(8f, -50f, 12f), 2.2f);
            InstantiateProp(starfishB, g, new Vector3(-6.6f, 0.25f, 10.4f), Quaternion.Euler(8f, 50f, -12f), 2.2f);

            // Tropical Coral Rocks on Sandy Shore
            if (stone != null)
            {
                InstantiatePropAtGround(stone, g, new Vector3(-7.2f, 0.0f, 11.2f), Quaternion.Euler(0f, 45f, 0f), 1.5f, 0.04f);
                InstantiatePropAtGround(stone, g, new Vector3(7.2f, 0.0f, 11.2f), Quaternion.Euler(0f, -45f, 0f), 1.5f, 0.04f);
            }

            // 19. DISTANT DEEP OCEAN VISTA (Theme 2: Sunken Pirate Galleon, Coral Atolls, Leaping Dolphins & Palm Sandbars)
            Transform gDistant = CreateGroup("--- DISTANT OCEAN VISTA ---", g);
            var sunkenGalleon = LoadDistantOceanProp("Toon_Distant_Sunken_Galleon");
            var coralAtoll = LoadDistantOceanProp("Toon_Distant_Coral_Atoll");
            var leapingDolphin = LoadDistantOceanProp("Toon_Distant_Leaping_Dolphin");
            var palmAtoll = LoadDistantOceanProp("Toon_Distant_Palm_Atoll");

            if (sunkenGalleon != null)
            {
                InstantiateProp(sunkenGalleon, gDistant, new Vector3(-5.5f, -0.18f, 27.0f), Quaternion.Euler(0f, 35f, 0f), 1.1f);
            }
            if (coralAtoll != null)
            {
                InstantiateProp(coralAtoll, gDistant, new Vector3(-4.8f, -0.10f, 26.0f), Quaternion.Euler(0f, 110f, 0f), 1.15f);
                InstantiateProp(coralAtoll, gDistant, new Vector3( 1.5f, -0.10f, 28.5f), Quaternion.Euler(0f, 45f, 0f), 0.95f);
            }
            if (leapingDolphin != null)
            {
                InstantiateProp(leapingDolphin, gDistant, new Vector3(-0.5f, -0.15f, 24.5f), Quaternion.Euler(0f, 60f, 0f), 1.15f);
            }
            if (palmAtoll != null)
            {
                InstantiateProp(palmAtoll, gDistant, new Vector3(5.5f, -0.18f, 29.5f), Quaternion.Euler(0f, -40f, 0f), 1.0f);
            }
        }

        // --- LEVEL 3 DECOR: GOLDEN AUTUMN COVE ---
        private static void BuildLevel3AutumnDecor(
            Transform g, GameObject autumnLodge, GameObject mapleTree, GameObject harvestBarrel, GameObject pumpkin,
            GameObject harvestCart, GameObject campfire, GameObject picnicTable, GameObject rowboat, GameObject mushroom, GameObject lantern,
            GameObject ladder, GameObject choppedTree, GameObject shrub, GameObject flower, GameObject grass, GameObject rock, GameObject stone,
            GameObject slateRoadway, GameObject stoneCurb, GameObject autumnLeafPatch, GameObject rusticFence)
        {
            // 0. Autumn Slate Roadways
            if (slateRoadway != null)
            {
                // North slate roadway from Pier to harvest plaza
                InstantiateProp(slateRoadway, g, new Vector3(0.0f, 0.0f, 8.2f), Quaternion.identity, 1.0f);
                // Central harvest square roadway
                InstantiateProp(slateRoadway, g, new Vector3(0.0f, 0.0f, 3.2f), Quaternion.identity, 1.0f);
                // West branch to Timber Lodge
                InstantiateProp(slateRoadway, g, new Vector3(-4.6f, 0.0f, 2.5f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
                // East branch to Timber Lodge / market
                InstantiateProp(slateRoadway, g, new Vector3( 4.8f, 0.0f, 2.0f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
            }

            // 0b. Stone Retaining Curbs along avenue & plaza
            if (stoneCurb != null)
            {
                InstantiateProp(stoneCurb, g, new Vector3(-2.8f, 0.0f, 5.5f), Quaternion.Euler(0f, 20f, 0f), 1.0f);
                InstantiateProp(stoneCurb, g, new Vector3( 2.8f, 0.0f, 5.5f), Quaternion.Euler(0f, -20f, 0f), 1.0f);
            }

            // 0c. Rustic Timber Fences
            if (rusticFence != null)
            {
                InstantiateProp(rusticFence, g, new Vector3(-9.5f, 0.0f, 5.2f), Quaternion.Euler(0f, 20f, 0f), 1.0f);
                InstantiateProp(rusticFence, g, new Vector3( 9.5f, 0.0f, 5.0f), Quaternion.Euler(0f, -20f, 0f), 1.0f);
            }

            // 0d. Clusters of Fallen Autumn Maple Leaves
            if (autumnLeafPatch != null)
            {
                InstantiateProp(autumnLeafPatch, g, new Vector3(-2.6f, 0.0f, 5.5f), Quaternion.Euler(0f, 25f, 0f), 1.1f);
                InstantiateProp(autumnLeafPatch, g, new Vector3( 2.6f, 0.0f, 5.5f), Quaternion.Euler(0f, -40f, 0f), 1.1f);
                InstantiateProp(autumnLeafPatch, g, new Vector3(-5.2f, 0.0f, 1.8f), Quaternion.Euler(0f, 65f, 0f), 1.15f);
                InstantiateProp(autumnLeafPatch, g, new Vector3( 5.2f, 0.0f, 1.6f), Quaternion.Euler(0f, -55f, 0f), 1.15f);
                InstantiateProp(autumnLeafPatch, g, new Vector3(-1.2f, 0.0f, 1.2f), Quaternion.Euler(0f, 15f, 0f), 1.05f);
                InstantiateProp(autumnLeafPatch, g, new Vector3( 1.2f, 0.0f, 1.2f), Quaternion.Euler(0f, -70f, 0f), 1.05f);
                InstantiateProp(autumnLeafPatch, g, new Vector3(-7.5f, 0.0f, 7.5f), Quaternion.Euler(0f, 35f, 0f), 1.2f);
                InstantiateProp(autumnLeafPatch, g, new Vector3( 7.5f, 0.0f, 7.5f), Quaternion.Euler(0f, -45f, 0f), 1.2f);
            }
            // 1. Autumn Timber Lodges with Stone Chimneys & Firewood Sheds
            if (autumnLodge != null)
            {
                InstantiateProp(autumnLodge, g, new Vector3(-8.8f, 0.0f, 2.5f), Quaternion.Euler(0f, 55f, 0f), 1.15f);
                InstantiateProp(autumnLodge, g, new Vector3(9.2f, 0.0f, 2.0f), Quaternion.Euler(0f, -55f, 0f), 1.10f);
                InstantiateProp(autumnLodge, g, new Vector3(-12.5f, 0.0f, 12.0f), Quaternion.Euler(0f, 45f, 0f), 0.95f);
            }

            // 2. Custom Autumn Maple Trees (Multi-tiered Golden-Amber & Crimson Canopies with Fallen Leaves)
            if (mapleTree != null)
            {
                for (int i = 0; i < TreePerimeterPositions.Length; i++)
                {
                    InstantiateProp(mapleTree, g, TreePerimeterPositions[i], Quaternion.Euler(0f, TreeRotationsY[i], 0f), TreeScalesMaple[i]);
                }
            }

            // 3. Autumn Harvest Wagons Brimming with Pumpkins & Apples
            if (harvestCart != null)
            {
                InstantiateProp(harvestCart, g, new Vector3(-6.8f, 0.0f, 4.2f), Quaternion.Euler(0f, 35f, 0f), 1.05f);
                InstantiateProp(harvestCart, g, new Vector3( 7.4f, 0.0f, 4.0f), Quaternion.Euler(0f, -40f, 0f), 1.05f);
            }

            // 4. Cozy Autumn Stone Campfire with Glowing Embers & Log Stools
            if (campfire != null)
            {
                InstantiateProp(campfire, g, new Vector3(-9.2f, 0.0f, 6.5f), Quaternion.Euler(0f, 15f, 0f), 1.15f);
            }

            // 5. Timber Picnic Tables with Harvest Baskets & Cider Jugs
            if (picnicTable != null)
            {
                InstantiateProp(picnicTable, g, new Vector3(-7.2f, 0.0f, -2.5f), Quaternion.Euler(0f, 65f, 0f), 1.05f);
                InstantiateProp(picnicTable, g, new Vector3( 7.5f, 0.0f, -2.2f), Quaternion.Euler(0f, -65f, 0f), 1.05f);
            }

            // 6. Warm Amber Lantern Posts
            if (lantern != null)
            {
                InstantiateProp(lantern, g, new Vector3(-6.2f, 0.0f, 8.8f), Quaternion.Euler(0f, 35f, 0f), 1.05f);
                InstantiateProp(lantern, g, new Vector3( 6.2f, 0.0f, 8.8f), Quaternion.Euler(0f, -35f, 0f), 1.05f);
                InstantiateProp(lantern, g, new Vector3(-7.5f, 0.0f, 0.0f), Quaternion.Euler(0f, 80f, 0f), 1.05f);
                InstantiateProp(lantern, g, new Vector3( 7.5f, 0.0f, 0.0f), Quaternion.Euler(0f, -80f, 0f), 1.05f);
            }

            // 7. Autumn Harvest Barrels overflowing with apples
            if (harvestBarrel != null)
            {
                InstantiateProp(harvestBarrel, g, new Vector3(-7.2f, 0.0f, 1.8f), Quaternion.Euler(0f, 25f, 0f), 1.1f);
                InstantiateProp(harvestBarrel, g, new Vector3( 7.2f, 0.0f, 1.8f), Quaternion.Euler(0f, -35f, 0f), 1.1f);
                InstantiateProp(harvestBarrel, g, new Vector3(-6.6f, 0.0f, 7.6f), Quaternion.Euler(0f, 60f, 0f), 1.15f);
                InstantiateProp(harvestBarrel, g, new Vector3( 6.6f, 0.0f, 7.6f), Quaternion.Euler(0f, -40f, 0f), 1.15f);
            }

            // 8. Carved Autumn Pumpkins
            if (pumpkin != null)
            {
                InstantiateProp(pumpkin, g, new Vector3(-7.8f, 0.0f, 3.4f), Quaternion.Euler(0f, 45f, 0f), 1.2f);
                InstantiateProp(pumpkin, g, new Vector3( 8.2f, 0.0f, 2.8f), Quaternion.Euler(0f, -25f, 0f), 1.25f);
                InstantiateProp(pumpkin, g, new Vector3(-1.4f, 0.0f, 2.2f), Quaternion.Euler(0f, 15f, 0f), 1.1f);
                InstantiateProp(pumpkin, g, new Vector3( 1.4f, 0.0f, 2.2f), Quaternion.Euler(0f, -50f, 0f), 1.1f);
                InstantiateProp(pumpkin, g, new Vector3(-5.8f, 0.0f, 9.6f), Quaternion.Euler(0f, 20f, 0f), 1.15f);
                InstantiateProp(pumpkin, g, new Vector3( 5.8f, 0.0f, 9.6f), Quaternion.Euler(0f, -30f, 0f), 1.15f);
            }

            // 9. Stacks of Chopped Firewood
            if (choppedTree != null)
            {
                InstantiatePropAtGround(choppedTree, g, new Vector3(-7.5f, 0.0f, 2.2f), Quaternion.Euler(0f, 20f, 0f), 1.15f, 0.02f);
                InstantiatePropAtGround(choppedTree, g, new Vector3( 7.5f, 0.0f, 2.2f), Quaternion.Euler(0f, -30f, 0f), 1.15f, 0.02f);
                InstantiatePropAtGround(choppedTree, g, new Vector3(-6.5f, 0.0f, 8.5f), Quaternion.Euler(0f, 30f, 0f), 1.20f, 0.02f);
                InstantiatePropAtGround(choppedTree, g, new Vector3( 6.5f, 0.0f, 8.5f), Quaternion.Euler(0f, -40f, 0f), 1.20f, 0.02f);
                InstantiatePropAtGround(choppedTree, g, new Vector3(-11.2f, 0.0f, 12.8f), Quaternion.Euler(0f, 50f, 0f), 1.10f, 0.02f);
            }

            // 10. Timber Ladders along Lodge Roofs
            if (ladder != null)
            {
                InstantiatePropAtGround(ladder, g, new Vector3(-8.2f, 0.0f, 6.2f), Quaternion.Euler(0f, 70f, 12f), 1.0f, 0.02f);
                InstantiatePropAtGround(ladder, g, new Vector3( 8.2f, 0.0f, 6.2f), Quaternion.Euler(0f, -70f, 12f), 1.0f, 0.02f);
            }

            // 11. Woodland Mushroom Clusters under Trees
            if (mushroom != null)
            {
                InstantiateProp(mushroom, g, new Vector3(-8.2f, 0.0f, 8.2f), Quaternion.Euler(0f, 25f, 0f), 1.10f);
                InstantiateProp(mushroom, g, new Vector3( 8.2f, 0.0f, 8.2f), Quaternion.Euler(0f, -45f, 0f), 1.10f);
                InstantiateProp(mushroom, g, new Vector3(-10.2f, 0.0f, 0.2f), Quaternion.Euler(0f, 60f, 0f), 1.15f);
                InstantiateProp(mushroom, g, new Vector3( 10.2f, 0.0f, 0.2f), Quaternion.Euler(0f, -70f, 0f), 1.15f);
                InstantiateProp(mushroom, g, new Vector3(-7.2f, 0.0f, -5.5f), Quaternion.Euler(0f, 15f, 0f), 1.05f);
                InstantiateProp(mushroom, g, new Vector3( 7.2f, 0.0f, -5.5f), Quaternion.Euler(0f, -35f, 0f), 1.05f);
            }

            // 12. Autumn Foliage Shrubs, Wild Flowers & Golden Grass
            if (shrub != null)
            {
                InstantiatePropAtGround(shrub, g, new Vector3(-7.5f, 0.0f, 6.0f), Quaternion.identity, 1.10f, 0.03f);
                InstantiatePropAtGround(shrub, g, new Vector3( 7.5f, 0.0f, 6.0f), Quaternion.identity, 1.10f, 0.03f);
                InstantiatePropAtGround(shrub, g, new Vector3(-6.0f, 0.0f, -4.0f), Quaternion.identity, 1.05f, 0.03f);
                InstantiatePropAtGround(shrub, g, new Vector3( 6.0f, 0.0f, -4.0f), Quaternion.identity, 1.05f, 0.03f);
            }
            if (flower != null)
            {
                InstantiatePropAtGround(flower, g, new Vector3(-8.5f, 0.0f, 3.2f), Quaternion.Euler(0f, 40f, 0f), 1.2f, 0.02f);
                InstantiatePropAtGround(flower, g, new Vector3( 8.5f, 0.0f, 3.2f), Quaternion.Euler(0f, -50f, 0f), 1.2f, 0.02f);
                InstantiatePropAtGround(flower, g, new Vector3(-6.5f, 0.0f, 8.0f), Quaternion.Euler(0f, 25f, 0f), 1.15f, 0.02f);
                InstantiatePropAtGround(flower, g, new Vector3( 6.5f, 0.0f, 8.0f), Quaternion.Euler(0f, -30f, 0f), 1.15f, 0.02f);
            }
            if (grass != null)
            {
                InstantiatePropAtGround(grass, g, new Vector3(-7.0f, 0.0f, 5.0f), Quaternion.identity, 1.2f, 0.02f);
                InstantiatePropAtGround(grass, g, new Vector3( 7.0f, 0.0f, 5.0f), Quaternion.identity, 1.2f, 0.02f);
                InstantiatePropAtGround(grass, g, new Vector3(-9.5f, 0.0f, 8.5f), Quaternion.identity, 1.25f, 0.02f);
                InstantiatePropAtGround(grass, g, new Vector3( 9.5f, 0.0f, 8.5f), Quaternion.identity, 1.25f, 0.02f);
            }

            // 13. Shoreline Rocks & Coastal Boulders
            if (stone != null)
            {
                InstantiatePropAtGround(stone, g, new Vector3(-5.5f, 0.0f, 10.2f), Quaternion.Euler(0f, 25f, 0f), 1.35f, 0.03f);
                InstantiatePropAtGround(stone, g, new Vector3( 5.5f, 0.0f, 10.2f), Quaternion.Euler(0f, -35f, 0f), 1.35f, 0.03f);
            }
            if (rock != null)
            {
                InstantiatePropAtGround(rock, g, new Vector3(-7.0f, 0.0f, 11.2f), Quaternion.Euler(0f, 55f, 0f), 1.5f, 0.05f);
                InstantiatePropAtGround(rock, g, new Vector3( 7.0f, 0.0f, 11.2f), Quaternion.Euler(0f, -50f, 0f), 1.5f, 0.05f);
            }

            // 14. IN THE WATER: Moored Autumn Fishing Rowboat
            if (rowboat != null)
            {
                InstantiateProp(rowboat, g, new Vector3(-4.6f, -0.16f, 14.8f), Quaternion.Euler(-2f, 32f, 3f), 1.15f);
            }

            // 15. IN THE WATER: Riverbed & Cove Boulders (Dense Aquatic Formations matching Level 2 Coral density!)
            var rockA = LoadMarineProp("RockA");
            var rockB = LoadMarineProp("RockB");
            var rockD = LoadMarineProp("RockD");
            var rockF = LoadMarineProp("RockF");
            if (rockA != null) InstantiateProp(rockA, g, new Vector3(-3.8f, -0.25f, 15.8f), Quaternion.Euler(0f, 40f, 0f), 2.2f);
            if (rockB != null) InstantiateProp(rockB, g, new Vector3( 1.0f, -0.25f, 15.8f), Quaternion.Euler(0f, -40f, 0f), 2.0f);
            if (rockD != null) InstantiateProp(rockD, g, new Vector3( 0.0f, -0.28f, 17.0f), Quaternion.Euler(0f, 75f, 0f), 2.5f);
            if (rockF != null) InstantiateProp(rockF, g, new Vector3(-5.5f, -0.20f, 17.5f), Quaternion.Euler(0f, -30f, 0f), 2.4f);
            if (rockB != null) InstantiateProp(rockB, g, new Vector3(-6.8f, -0.20f, 18.5f), Quaternion.Euler(0f, 50f, 0f), 2.2f);

            // 16. Golden Kelp / Autumn Seaweed Beds in Cove Waters (West & Central bays, keeping ship channel clear)
            var seaweedA = LoadMarineProp("SeaweedA");
            var seaweedB = LoadMarineProp("SeaweedB");
            var seaweedC = LoadMarineProp("SeaweedC");
            var seaweedD = LoadMarineProp("SeaweedD");
            if (seaweedA != null)
            {
                InstantiateProp(seaweedA, g, new Vector3(-3.8f, -0.32f, 16.0f), Quaternion.Euler(0f, 35f, 0f), 1.6f);
                InstantiateProp(seaweedA, g, new Vector3(-2.8f, -0.32f, 17.2f), Quaternion.Euler(0f, -45f, 0f), 1.5f);
            }
            if (seaweedB != null)
            {
                InstantiateProp(seaweedB, g, new Vector3( 0.8f, -0.32f, 16.0f), Quaternion.Euler(0f, -25f, 0f), 1.5f);
                InstantiateProp(seaweedB, g, new Vector3(-4.5f, -0.32f, 17.2f), Quaternion.Euler(0f, 55f, 0f), 1.5f);
            }
            if (seaweedC != null)
            {
                InstantiateProp(seaweedC, g, new Vector3(-1.8f, -0.32f, 17.2f), Quaternion.Euler(0f, 75f, 0f), 1.5f);
                InstantiateProp(seaweedC, g, new Vector3(-0.5f, -0.32f, 18.2f), Quaternion.Euler(0f, -65f, 0f), 1.6f);
            }
            if (seaweedD != null)
            {
                InstantiateProp(seaweedD, g, new Vector3( 1.8f, -0.32f, 17.5f), Quaternion.Euler(0f, -60f, 0f), 1.6f);
                InstantiateProp(seaweedD, g, new Vector3(-2.2f, -0.32f, 18.2f), Quaternion.Euler(0f, 40f, 0f), 1.6f);
            }

            // 17. Active Swimming Cove Fish: Yellowfin Tuna, Skipjack Tuna & Upstream Salmon
            InstantiateAnimatedMarineCreature("YellowfinTuna", "fish", g, new Vector3(0.0f, -0.25f, 17.2f), Quaternion.Euler(2f, -35f, 4f), 1.8f);
            InstantiateAnimatedMarineCreature("SkipjackTuna", "fish", g, new Vector3(-2.6f, -0.30f, 16.5f), Quaternion.Euler(-3f, 45f, -2f), 1.6f);
            var salmonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fish/Fish_Salmon_3D.prefab");
            if (salmonPrefab != null)
            {
                InstantiateProp(salmonPrefab, g, new Vector3(1.6f, -0.24f, 16.0f), Quaternion.Euler(4f, -40f, 0f), 1.8f);
                InstantiateProp(salmonPrefab, g, new Vector3(-1.2f, -0.26f, 15.6f), Quaternion.Euler(-4f, 50f, 0f), 1.6f);
            }

            // 18. Shoreline Lobsters & Sea Starfish
            InstantiateAnimatedMarineCreature("Lobster", "lobster", g, new Vector3(-5.8f, 0.18f, 11.2f), Quaternion.Euler(12f, 40f, -6f), 2.2f);
            InstantiateAnimatedMarineCreature("BlueLobster", "lobster", g, new Vector3(0.8f, -0.12f, 15.5f), Quaternion.Euler(-4f, -40f, 5f), 2.0f);
            var starfishA = LoadMarineProp("StarfishA");
            var starfishB = LoadMarineProp("StarfishB");
            if (starfishA != null) InstantiateProp(starfishA, g, new Vector3(-6.2f, 0.16f, 10.6f), Quaternion.Euler(0f, 30f, 0f), 2.0f);
            if (starfishB != null) InstantiateProp(starfishB, g, new Vector3( 6.2f, 0.16f, 10.6f), Quaternion.Euler(0f, -45f, 0f), 2.0f);

            // 19. DISTANT DEEP OCEAN VISTA (Theme 3: Autumn Beacon Islet with Scarlet Maples, Timber Logging Barge & Fjord Rock Stacks)
            Transform gDistant = CreateGroup("--- DISTANT OCEAN VISTA ---", g);
            var autumnBeacon = LoadDistantOceanProp("Toon_Distant_Autumn_Beacon");
            var timberBarge = LoadDistantOceanProp("Toon_Distant_Timber_Barge");
            var fjordStack = LoadDistantOceanProp("Toon_Distant_Fjord_Stack");

            if (autumnBeacon != null)
            {
                InstantiateProp(autumnBeacon, gDistant, new Vector3(-6.8f, -0.2f, 29.5f), Quaternion.Euler(0f, 15f, 0f), 0.90f);
            }
            if (timberBarge != null)
            {
                InstantiateProp(timberBarge, gDistant, new Vector3(1.2f, -0.15f, 29.0f), Quaternion.Euler(0f, -85f, 0f), 1.05f);
            }
            if (fjordStack != null)
            {
                InstantiateProp(fjordStack, gDistant, new Vector3(-9.5f, -0.2f, 31.0f), Quaternion.Euler(0f, 45f, 0f), 1.2f);
                InstantiateProp(fjordStack, gDistant, new Vector3(-2.0f, -0.2f, 32.5f), Quaternion.Euler(0f, -110f, 0f), 1.0f);
                InstantiateProp(fjordStack, gDistant, new Vector3( 6.5f, -0.2f, 28.5f), Quaternion.Euler(0f, 25f, 0f), 0.95f);
            }
        }

        // --- LEVEL 4 DECOR: ARCTIC FROST HAVEN ---
        private static void BuildLevel4ArcticFrostDecor(
            Transform g, GameObject arcticCabin, GameObject frostedSpruce,
            GameObject glacialIceberg, GameObject floatingIceFloe, GameObject arcticIgloo,
            GameObject snowman, GameObject bonfire, GameObject sled, GameObject supplyCrates, GameObject iceSpire, GameObject signpost,
            GameObject rock, GameObject stone,
            GameObject glacialIceShelf, GameObject snowTrail, GameObject snowDrift, GameObject frostBoardwalk)
        {
            // 0. Glacial Ice Shelf removed per user request (open water fishing)

            // 0b. Packed Snow Trail System through basecamp
            if (snowTrail != null)
            {
                // North snow trail from Pier to camp
                InstantiateProp(snowTrail, g, new Vector3(0.0f, 0.0f, 8.2f), Quaternion.identity, 1.0f);
                // Central snow trail spine
                InstantiateProp(snowTrail, g, new Vector3(0.0f, 0.0f, 2.8f), Quaternion.identity, 1.0f);
                // West branch to Igloo & Cabin
                InstantiateProp(snowTrail, g, new Vector3(-4.8f, 0.0f, 2.5f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
                // East branch to Bonfire & Cabin
                InstantiateProp(snowTrail, g, new Vector3( 4.8f, 0.0f, 2.0f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
            }

            // 0c. Sculpted Soft Snow Drifts banked along walls and trails
            if (snowDrift != null)
            {
                InstantiateProp(snowDrift, g, new Vector3(-8.8f, 0.0f, 4.2f), Quaternion.Euler(0f, 45f, 0f), 1.15f);
                InstantiateProp(snowDrift, g, new Vector3( 9.0f, 0.0f, 3.8f), Quaternion.Euler(0f, -45f, 0f), 1.15f);
                InstantiateProp(snowDrift, g, new Vector3(-6.2f, 0.0f, 8.8f), Quaternion.Euler(0f, 15f, 0f), 1.10f);
                InstantiateProp(snowDrift, g, new Vector3( 6.2f, 0.0f, 8.8f), Quaternion.Euler(0f, -15f, 0f), 1.10f);
            }

            // 0d. Frost-dusted Wooden Boardwalks
            if (frostBoardwalk != null)
            {
                InstantiateProp(frostBoardwalk, g, new Vector3(-8.5f, 0.0f, 4.8f), Quaternion.Euler(0f, 35f, 0f), 1.0f);
                InstantiateProp(frostBoardwalk, g, new Vector3( 8.5f, 0.0f, 4.8f), Quaternion.Euler(0f, -35f, 0f), 1.0f);
            }

            // 1. Nordic Arctic Snow Cabins on Frost Stilts
            if (arcticCabin != null)
            {
                InstantiateProp(arcticCabin, g, new Vector3(-8.8f, 0.0f, 2.5f), Quaternion.Euler(0f, 55f, 0f), 1.15f);
                InstantiateProp(arcticCabin, g, new Vector3( 9.2f, 0.0f, 2.0f), Quaternion.Euler(0f, -55f, 0f), 1.10f);
                InstantiateProp(arcticCabin, g, new Vector3(-12.5f, 0.0f, 12.0f), Quaternion.Euler(0f, 45f, 0f), 0.95f);
            }

            // 2. Custom Arctic Frosted Spruces (5 Cascading Tiers with Heavy Snow Mantles & Icicles)
            if (frostedSpruce != null)
            {
                for (int i = 0; i < TreePerimeterPositions.Length; i++)
                {
                    InstantiateProp(frostedSpruce, g, TreePerimeterPositions[i], Quaternion.Euler(0f, TreeRotationsY[i], 0f), TreeScalesSpruce[i]);
                }
            }

            // 3. Arctic Igloo Basecamp with Cozy Glowing Campfire Light
            if (arcticIgloo != null)
            {
                InstantiateProp(arcticIgloo, g, new Vector3(-11.5f, 0.0f, 9.5f), Quaternion.Euler(0f, 45f, 0f), 1.15f);
            }

            // 4. Classic Toon Snowman with Carrot Nose, Top Hat & Red Scarf
            if (snowman != null)
            {
                InstantiateProp(snowman, g, new Vector3(-6.8f, 0.0f, 4.5f), Quaternion.Euler(0f, 140f, 0f), 1.15f);
            }

            // 5. Roaring Basecamp Bonfire with Iron Tripod Cooking Cauldron & Warm Glow
            if (bonfire != null)
            {
                InstantiateProp(bonfire, g, new Vector3(7.4f, 0.0f, 4.5f), Quaternion.Euler(0f, 30f, 0f), 1.20f);
            }

            // 6. Arctic Expedition Cargo Sled Loaded with Crates & Bedroll
            if (sled != null)
            {
                InstantiateProp(sled, g, new Vector3(-7.2f, 0.0f, -1.8f), Quaternion.Euler(0f, 25f, 0f), 1.15f);
            }

            // 7. Stacks of Expedition Supply Crates & Fuel Oil Drums with Snow Caps
            if (supplyCrates != null)
            {
                InstantiateProp(supplyCrates, g, new Vector3(-6.2f, 0.0f, 7.8f), Quaternion.Euler(0f, 45f, 0f), 1.10f);
                InstantiateProp(supplyCrates, g, new Vector3( 6.2f, 0.0f, 7.8f), Quaternion.Euler(0f, -35f, 0f), 1.10f);
                InstantiateProp(supplyCrates, g, new Vector3( 7.5f, 0.0f, -2.0f), Quaternion.Euler(0f, -60f, 0f), 1.10f);
            }

            // 8. Arctic Trail Directional Signpost
            if (signpost != null)
            {
                InstantiateProp(signpost, g, new Vector3(-5.8f, 0.0f, 9.2f), Quaternion.Euler(0f, 25f, 0f), 1.15f);
            }

            // 9. Frosted Shoreline Boulders
            if (rock != null)
            {
                InstantiatePropAtGround(rock, g, new Vector3(-6.2f, 0.0f, 11.2f), Quaternion.Euler(0f, 25f, 0f), 1.6f, 0.04f);
                InstantiatePropAtGround(rock, g, new Vector3( 6.2f, 0.0f, 11.2f), Quaternion.Euler(0f, -35f, 0f), 1.6f, 0.04f);
                InstantiatePropAtGround(rock, g, new Vector3(-7.5f, 0.0f, 10.0f), Quaternion.Euler(0f, 50f, 0f), 1.4f, 0.04f);
                InstantiatePropAtGround(rock, g, new Vector3( 7.5f, 0.0f, 10.0f), Quaternion.Euler(0f, -50f, 0f), 1.4f, 0.04f);
            }

            // 10. IN THE WATER: 6 Submerged Glacial Ice Spires (The Frozen Crystal Reef - Dense underwater formations!)
            // 10. IN THE WATER: Submerged Glacial Ice Spires (West & Central Reefs, clear of ship corridor)
            if (iceSpire != null)
            {
                InstantiateProp(iceSpire, g, new Vector3(-4.0f, -0.22f, 15.6f), Quaternion.Euler(0f, 25f, 0f), 1.35f);
                InstantiateProp(iceSpire, g, new Vector3(-2.2f, -0.22f, 18.2f), Quaternion.Euler(0f, -35f, 0f), 1.35f);
                InstantiateProp(iceSpire, g, new Vector3(-1.8f, -0.26f, 16.5f), Quaternion.Euler(0f, 65f, 0f), 1.25f);
                InstantiateProp(iceSpire, g, new Vector3( 0.6f, -0.26f, 16.2f), Quaternion.Euler(0f, -50f, 0f), 1.25f);
                InstantiateProp(iceSpire, g, new Vector3(-5.6f, -0.28f, 17.6f), Quaternion.Euler(0f, 40f, 0f), 1.45f);
                InstantiateProp(iceSpire, g, new Vector3(-6.8f, -0.28f, 16.2f), Quaternion.Euler(0f, -30f, 0f), 1.45f);
            }

            // 11. Crystalline Glacial Icebergs in Deeper Water (Framing north horizon)
            if (glacialIceberg != null)
            {
                InstantiateProp(glacialIceberg, g, new Vector3(-5.2f, -0.35f, 19.5f), Quaternion.Euler(0f, 35f, 0f), 1.20f);
                InstantiateProp(glacialIceberg, g, new Vector3(-8.8f, -0.38f, 21.5f), Quaternion.Euler(0f, -45f, 0f), 1.30f);
                InstantiateProp(glacialIceberg, g, new Vector3(-3.0f, -0.40f, 23.5f), Quaternion.Euler(0f, 180f, 0f), 1.50f);
            }

            // 12. IN THE WATER: 6 Floating Pack Ice Floes with Wildlife & Ice Fishing Station
            if (floatingIceFloe != null)
            {
                // Ice Floe 1 with Giant King Snow Crab
                InstantiateProp(floatingIceFloe, g, new Vector3(-3.2f, -0.18f, 15.8f), Quaternion.Euler(0f, 25f, 0f), 1.15f);
                var crabPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fish/Fish_SnowCrab_3D.prefab");
                if (crabPrefab != null)
                {
                    InstantiateProp(crabPrefab, g, new Vector3(-3.2f, 0.02f, 15.8f), Quaternion.Euler(5f, 45f, 0f), 1.8f);
                }

                // Ice Floe 2 with Frost Starfish
                InstantiateProp(floatingIceFloe, g, new Vector3(-0.6f, -0.18f, 15.4f), Quaternion.Euler(0f, -60f, 0f), 1.05f);
                var starfishB = LoadMarineProp("StarfishB");
                if (starfishB != null)
                {
                    InstantiateProp(starfishB, g, new Vector3(-0.8f, 0.02f, 15.2f), Quaternion.Euler(0f, 30f, 0f), 2.2f);
                    InstantiateProp(starfishB, g, new Vector3(-0.4f, 0.02f, 15.6f), Quaternion.Euler(0f, -50f, 0f), 1.9f);
                }

                // Ice Floe 3 with Ice Fishing Stool
                InstantiateProp(floatingIceFloe, g, new Vector3(-1.2f, -0.18f, 17.6f), Quaternion.Euler(0f, 45f, 0f), 1.10f);
                var stoolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_FishingPier_Stool.prefab");
                if (stoolPrefab != null)
                {
                    InstantiateProp(stoolPrefab, g, new Vector3(-1.2f, 0.04f, 17.6f), Quaternion.Euler(0f, 15f, 0f), 1.0f);
                }

                // Ice Floes 4, 5, 6 across the bay
                InstantiateProp(floatingIceFloe, g, new Vector3(-6.2f, -0.20f, 18.2f), Quaternion.Euler(0f, 70f, 0f), 1.0f);
                InstantiateProp(floatingIceFloe, g, new Vector3(-4.5f, -0.20f, 20.2f), Quaternion.Euler(0f, -55f, 0f), 1.0f);
                InstantiateProp(floatingIceFloe, g, new Vector3(-2.2f, -0.22f, 19.8f), Quaternion.Euler(0f, 110f, 0f), 1.15f);
            }

            // 13. Frosted Marine Rocks in the Water (Adding texture to seabed)
            var rockA = LoadMarineProp("RockA");
            var rockC = LoadMarineProp("RockC");
            var rockF = LoadMarineProp("RockF");
            if (rockA != null) InstantiateProp(rockA, g, new Vector3(-4.8f, -0.25f, 16.8f), Quaternion.Euler(0f, 20f, 0f), 2.0f);
            if (rockC != null) InstantiateProp(rockC, g, new Vector3(-1.5f, -0.28f, 18.5f), Quaternion.Euler(0f, -20f, 0f), 2.0f);
            if (rockF != null) InstantiateProp(rockF, g, new Vector3( 0.0f, -0.30f, 18.5f), Quaternion.Euler(0f, 60f, 0f), 2.2f);

            // 14. Starfish on Pier posts
            var starfishA = LoadMarineProp("StarfishA");
            if (starfishA != null)
            {
                InstantiateProp(starfishA, g, new Vector3(-5.3f, 0.42f, 12.2f), Quaternion.Euler(0f, 25f, 0f), 1.8f);
                InstantiateProp(starfishA, g, new Vector3( 5.3f, 0.42f, 12.2f), Quaternion.Euler(0f, -45f, 0f), 1.8f);
            }

            // 15. Cold-Water Arctic Marine Wildlife Swimming through Ice Channels
            InstantiateAnimatedMarineCreature("RightWhaleDolphin", "dolphin", g, new Vector3(-1.6f, -0.22f, 17.5f), Quaternion.Euler(6f, 35f, -5f), 1.8f);
            InstantiateAnimatedMarineCreature("BluefinTuna", "fish", g, new Vector3(2.4f, -0.26f, 16.8f), Quaternion.Euler(-4f, -40f, 6f), 2.0f);
            var arcticTuna = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fish/Fish_ArcticTuna_3D.prefab");
            if (arcticTuna != null)
            {
                InstantiateProp(arcticTuna, g, new Vector3(-2.8f, -0.26f, 16.2f), Quaternion.Euler(3f, 50f, 0f), 1.8f);
            }

            // 16. DISTANT DEEP OCEAN VISTA (Theme 4: Colossal Glacial Iceberg Range, Breaching Humpback Whale & Penguin Pack Ice Floes)
            Transform gDistant = CreateGroup("--- DISTANT OCEAN VISTA ---", g);
            var icebergRange = LoadDistantOceanProp("Toon_Distant_Glacial_IcebergRange");
            var breachingWhale = LoadDistantOceanProp("Toon_Distant_Breaching_Whale");
            var penguinPack = LoadDistantOceanProp("Toon_Distant_Penguin_PackIce");

            if (icebergRange != null)
            {
                InstantiateProp(icebergRange, gDistant, new Vector3(-3.5f, -0.3f, 30.5f), Quaternion.Euler(0f, 10f, 0f), 1.15f);
            }
            if (breachingWhale != null)
            {
                InstantiateProp(breachingWhale, gDistant, new Vector3(-6.2f, -0.2f, 26.0f), Quaternion.Euler(0f, 50f, 0f), 1.1f);
            }
            if (penguinPack != null)
            {
                InstantiateProp(penguinPack, gDistant, new Vector3(2.0f, -0.15f, 26.5f), Quaternion.Euler(0f, -35f, 0f), 1.1f);
            }
        }

        // --- LEVEL 5 DECOR: SUNKEN OASIS DESERT ---
        private static void BuildLevel5DesertOasisDecor(
            Transform g, GameObject desertPavilion, GameObject palm, Material desertMat, GameObject rock, GameObject stone,
            GameObject desertSandstone, GameObject desertDuneRidge, GameObject oasisTurf)
        {
            // 0. Ancient Sandstone Walkway
            if (desertSandstone != null)
            {
                // North sandstone walkway from Pier to oasis
                InstantiateProp(desertSandstone, g, new Vector3(0.0f, 0.0f, 8.2f), Quaternion.identity, 1.0f);
                // Oasis plaza walkway
                InstantiateProp(desertSandstone, g, new Vector3(0.0f, 0.0f, 3.2f), Quaternion.identity, 1.0f);
                // West branch to Pavilion
                InstantiateProp(desertSandstone, g, new Vector3(-4.6f, 0.0f, 2.5f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
                // East branch to Pavilion
                InstantiateProp(desertSandstone, g, new Vector3( 4.8f, 0.0f, 2.0f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
            }

            // 0b. Wind-Rippled Sand Dune Ridges
            if (desertDuneRidge != null)
            {
                // Northern shoreline dune ridge
                InstantiateProp(desertDuneRidge, g, new Vector3(0.0f, 0.0f, 10.8f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
                // Southwest perimeter dune
                InstantiateProp(desertDuneRidge, g, new Vector3(-8.2f, 0.0f, -5.0f), Quaternion.Euler(0f, 40f, 0f), 1.0f);
                // Southeast perimeter dune
                InstantiateProp(desertDuneRidge, g, new Vector3( 8.2f, 0.0f, -5.0f), Quaternion.Euler(0f, -40f, 0f), 1.0f);
            }

            // 0c. Lush Emerald Oasis Turf Ring surrounding spring
            if (oasisTurf != null)
            {
                InstantiateProp(oasisTurf, g, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity, 1.0f);
            }

            // 1. Arabian Sandstone Oasis Pavilions with Golden Domes & Water Urns (Elevated cleanly above ground)
            if (desertPavilion != null)
            {
                InstantiateProp(desertPavilion, g, new Vector3(-8.8f, 0.04f, 2.5f), Quaternion.Euler(0f, 55f, 0f), 1.15f);
                InstantiateProp(desertPavilion, g, new Vector3( 9.2f, 0.04f, 2.0f), Quaternion.Euler(0f, -55f, 0f), 1.10f);
                InstantiateProp(desertPavilion, g, new Vector3(-12.5f, 0.08f, 12.0f), Quaternion.Euler(0f, 45f, 0f), 0.95f);
            }

            // 2. Curving Oasis Date Palm Trees with Date Fruit Bunches
            if (palm != null)
            {
                for (int i = 0; i < TreePerimeterPositions.Length; i++)
                {
                    InstantiateProp(palm, g, TreePerimeterPositions[i], Quaternion.Euler(0f, TreeRotationsY[i], 0f), TreeScalesPalm[i]);
                }
            }

            // 3. Ancient Sandstone Monoliths & Shoreline Ruins (West Reefs, clear of eastern ship corridor)
            if (rock != null)
            {
                InstantiateProp(rock, g, new Vector3(-5.2f, -0.2f, 15.5f), Quaternion.Euler(0f, 35f, 0f), 2.8f);
                InstantiateProp(rock, g, new Vector3(-7.8f, -0.2f, 18.2f), Quaternion.Euler(0f, -45f, 0f), 2.6f);
            }
            if (stone != null)
            {
                InstantiateProp(stone, g, new Vector3(-4.5f, 0.0f, 13.8f), Quaternion.Euler(0f, 75f, 0f), 2.2f);
                InstantiateProp(stone, g, new Vector3( 1.8f, 0.0f, 13.5f), Quaternion.Euler(0f, -80f, 0f), 2.0f);
            }

            // 4. Sunken Ancient Golden Treasure Chests in Shallow Oasis (Clear of ship corridor)
            GameObject chestObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Generated/Sunken_Treasure_Chest.obj");
            if (chestObj != null)
            {
                var c1 = Object.Instantiate(chestObj, g);
                c1.name = "Oasis_SunkenTreasure_Left";
                c1.transform.localPosition = new Vector3(-2.8f, -0.22f, 15.2f);
                c1.transform.localRotation = Quaternion.Euler(8f, 25f, -5f);
                c1.transform.localScale = Vector3.one * 1.4f;
                if (desertMat != null)
                {
                    foreach (var r in c1.GetComponentsInChildren<Renderer>()) r.sharedMaterial = desertMat;
                }

                var c2 = Object.Instantiate(chestObj, g);
                c2.name = "Oasis_SunkenTreasure_Right";
                c2.transform.localPosition = new Vector3(1.2f, -0.22f, 15.2f);
                c2.transform.localRotation = Quaternion.Euler(-5f, -30f, 8f);
                c2.transform.localScale = Vector3.one * 1.4f;
                if (desertMat != null)
                {
                    foreach (var r in c2.GetComponentsInChildren<Renderer>()) r.sharedMaterial = desertMat;
                }
            }

            // 5. Desert Seashells on Warm Sands
            var shellA = LoadMarineProp("ShellA");
            var shellB = LoadMarineProp("ShellB");
            var shellC = LoadMarineProp("ShellC");
            var shellD = LoadMarineProp("ShellD");
            if (shellA != null) InstantiateProp(shellA, g, new Vector3(-6.2f, 0.10f, 11.0f), Quaternion.Euler(0f, 40f, 0f), 2.4f);
            if (shellC != null) InstantiateProp(shellC, g, new Vector3( 2.2f, 0.10f, 11.0f), Quaternion.Euler(0f, -50f, 0f), 2.2f);
            if (shellB != null) InstantiateProp(shellB, g, new Vector3(-5.4f, 0.15f, 10.2f), Quaternion.Euler(0f, 15f, 0f), 2.0f);
            if (shellD != null) InstantiateProp(shellD, g, new Vector3( 1.6f, 0.15f, 10.4f), Quaternion.Euler(0f, -25f, 0f), 2.0f);

            // 6. Oasis Seaweeds (Clear of ship corridor)
            var seaweedA = LoadMarineProp("SeaweedA");
            var seaweedC = LoadMarineProp("SeaweedC");
            if (seaweedA != null) InstantiateProp(seaweedA, g, new Vector3(-4.5f, -0.32f, 16.2f), Quaternion.Euler(0f, 40f, 0f), 1.4f);
            if (seaweedC != null) InstantiateProp(seaweedC, g, new Vector3(-2.2f, -0.32f, 17.5f), Quaternion.Euler(0f, -30f, 0f), 1.4f);

            // 7. Legendary Apex Predators Patrolling the Deep Oasis
            // Great White Shark patrolling the oasis center
            InstantiateAnimatedMarineCreature("GreateWhiteShark", "shark", g, new Vector3(0.0f, -0.35f, 18.0f), Quaternion.Euler(2f, -25f, 4f), 1.8f);
            // Blacktip Reef Shark circling the left reef
            InstantiateAnimatedMarineCreature("BlacktipReefShark", "shark", g, new Vector3(-3.8f, -0.30f, 16.5f), Quaternion.Euler(-3f, 40f, -4f), 1.6f);

            // 8. DISTANT DEEP OCEAN VISTA (Theme 5: Sunken Pharaoh Colossus, Ancient Obelisk Colonnade & Mirage Sand Dunes)
            Transform gDistant = CreateGroup("--- DISTANT OCEAN VISTA ---", g);
            var sunkenPharaoh = LoadDistantOceanProp("Toon_Distant_Sunken_Pharaoh");
            var sunkenObelisk = LoadDistantOceanProp("Toon_Distant_Sunken_Obelisk");
            var mirageDune = LoadDistantOceanProp("Toon_Distant_Mirage_Dune");

            if (sunkenPharaoh != null)
            {
                InstantiateProp(sunkenPharaoh, gDistant, new Vector3(-4.8f, -0.25f, 27.5f), Quaternion.Euler(0f, 25f, 0f), 1.15f);
            }
            if (sunkenObelisk != null)
            {
                InstantiateProp(sunkenObelisk, gDistant, new Vector3(1.5f, -0.25f, 28.5f), Quaternion.Euler(0f, -30f, 0f), 1.1f);
            }
            if (mirageDune != null)
            {
                InstantiateProp(mirageDune, gDistant, new Vector3(-8.5f, -0.2f, 31.0f), Quaternion.Euler(0f, 40f, 0f), 1.1f);
                InstantiateProp(mirageDune, gDistant, new Vector3( 6.2f, -0.2f, 30.5f), Quaternion.Euler(0f, -120f, 0f), 0.95f);
            }
        }

        private static GameObject InstantiateAnimatedMarineCreature(string prefabName, string controllerName, Transform parent, Vector3 localPos, Quaternion localRot, float scale)
        {
            var prefab = LoadMarineProp(prefabName);
            if (prefab == null) return null;
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = prefabName;
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = localRot;
            instance.transform.localScale = Vector3.one * scale;

            if (!string.IsNullOrEmpty(controllerName))
            {
                string ctrlPath = $"Assets/Layer Lab/3D Props/3D Props Fish/3D Props Fish/Ani/{controllerName}.controller";
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
                if (ctrl != null)
                {
                    var anim = instance.GetComponent<Animator>();
                    if (anim == null) anim = instance.AddComponent<Animator>();
                    anim.runtimeAnimatorController = ctrl;
                }
            }
            return instance;
        }

        private static GameObject LoadPrefab(string subPath)
        {
            string fullPath = $"{AssetBasePath}/Prefabs/{subPath}.prefab";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
            if (go == null)
            {
                Debug.LogWarning($"[ToonEnvironmentBuilder] Prefab not found: {fullPath}");
            }
            return go;
        }

        private static GameObject LoadMarineProp(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{MarinePropsPath}/{name}.prefab");
        }

        private static GameObject LoadDistantOceanProp(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/DistantOcean/{name}.prefab");
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void InstantiateProp(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot, float scale)
        {
            if (prefab == null) return;
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = prefab.name;
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = localRot;
            instance.transform.localScale = Vector3.one * scale;
        }

        private static void InstantiatePropAtGround(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, float scale, float sinkCompensation = 0.02f)
        {
            if (prefab == null) return;
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = prefab.name;

            float yOffset = -(1.0f * scale) - sinkCompensation;
            instance.transform.position = new Vector3(pos.x, yOffset, pos.z);
            instance.transform.rotation = rot;
            instance.transform.localScale = Vector3.one * scale;
        }

        private static void CreateCloud(GameObject prefab, Transform parent, Vector3 pos, float scale, float driftSpeed)
        {
            if (prefab == null) return;
            GameObject cloud = Object.Instantiate(prefab, parent);
            cloud.name = prefab.name;
            cloud.transform.position = pos;
            cloud.transform.localScale = Vector3.one * scale;

            var drifter = cloud.AddComponent<ToonCloudDrifter>();
            if (drifter != null)
            {
                SerializedObject so = new SerializedObject(drifter);
                so.FindProperty("driftSpeed").floatValue = driftSpeed;
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif

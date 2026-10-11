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
                groundObj.transform.position = new Vector3(0f, 0f, -10f);
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

            // 4. Create Root Object
            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Toon Harbor Environment");

            // Common Base Infrastructure
            Transform gCabins = CreateGroup("--- CABINS & OUTPOSTS ---", root.transform);
            Transform gTrees = CreateGroup("--- FORESTS & COASTAL TREES ---", root.transform);
            Transform gRocks = CreateGroup("--- BREAKWATERS & BOULDERS ---", root.transform);
            Transform gClouds = CreateGroup("--- SKY & FLOATING CLOUDS ---", root.transform);
            Transform gCliffs = CreateGroup("--- COASTAL CLIFF TILES ---", root.transform);

            // 5 Distinct Land Decor Groups
            Transform d1 = CreateGroup("--- DECOR LEVEL 1 (TEMPERATE) ---", root.transform);
            Transform d2 = CreateGroup("--- DECOR LEVEL 2 (TROPICAL CORAL) ---", root.transform);
            Transform d3 = CreateGroup("--- DECOR LEVEL 3 (GOLDEN AUTUMN) ---", root.transform);
            Transform d4 = CreateGroup("--- DECOR LEVEL 4 (ARCTIC FROST) ---", root.transform);
            Transform d5 = CreateGroup("--- DECOR LEVEL 5 (DESERT OASIS) ---", root.transform);

            // ==========================================
            // A. CABINS & OUTPOSTS (Grounded at Y = 0.0)
            // ==========================================
            InstantiatePropAtGround(cabinPrefab, gCabins, new Vector3(-8.8f, 0.0f, 2.5f), Quaternion.Euler(0f, 55f, 0f), 1.15f, 0.02f);
            InstantiatePropAtGround(ladderPrefab, gCabins, new Vector3(-7.8f, 0.0f, 3.2f), Quaternion.Euler(0f, 140f, 15f), 1.0f, 0.02f);
            InstantiatePropAtGround(treeChoppedPrefab, gCabins, new Vector3(-7.6f, 0.0f, 1.5f), Quaternion.Euler(0f, 25f, 0f), 1.1f, 0.02f);
            InstantiatePropAtGround(cabinPrefab, gCabins, new Vector3(9.2f, 0.0f, 2.0f), Quaternion.Euler(0f, -55f, 0f), 1.1f, 0.02f);
            InstantiatePropAtGround(treeChoppedPrefab, gCabins, new Vector3(8.0f, 0.0f, 2.8f), Quaternion.Euler(0f, -40f, 0f), 1.05f, 0.02f);
            InstantiatePropAtGround(cabinPrefab, gCabins, new Vector3(-12.5f, 0.0f, 12.0f), Quaternion.Euler(0f, 45f, 0f), 0.95f, 0.02f);

            // ==========================================
            // B. FORESTS & COASTAL TREES (Grounded)
            // ==========================================
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(-8.5f, 0.0f, 7.5f), Quaternion.Euler(0f, 15f, 0f), 1.25f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(-10.5f, 0.0f, 9.5f), Quaternion.Euler(0f, 85f, 0f), 1.4f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(-9.8f, 0.0f, -0.5f), Quaternion.Euler(0f, -30f, 0f), 1.2f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(-8.5f, 0.0f, -4.5f), Quaternion.Euler(0f, 40f, 0f), 1.25f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(-7.8f, 0.0f, -8.0f), Quaternion.Euler(0f, 110f, 0f), 1.15f, 0.05f);

            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(8.5f, 0.0f, 7.5f), Quaternion.Euler(0f, -20f, 0f), 1.25f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(10.5f, 0.0f, 9.5f), Quaternion.Euler(0f, 65f, 0f), 1.4f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(9.8f, 0.0f, -0.5f), Quaternion.Euler(0f, -55f, 0f), 1.2f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(8.5f, 0.0f, -6.0f), Quaternion.Euler(0f, 95f, 0f), 1.25f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(7.8f, 0.0f, -8.5f), Quaternion.Euler(0f, -120f, 0f), 1.15f, 0.05f);

            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(-6.5f, 0.0f, -12.0f), Quaternion.Euler(0f, 10f, 0f), 1.2f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(-3.2f, 0.0f, -12.5f), Quaternion.Euler(0f, 55f, 0f), 1.1f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(3.2f, 0.0f, -12.5f), Quaternion.Euler(0f, -45f, 0f), 1.1f, 0.05f);
            InstantiatePropAtGround(treePrefab, gTrees, new Vector3(6.5f, 0.0f, -12.0f), Quaternion.Euler(0f, 130f, 0f), 1.2f, 0.05f);

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
            // BUILD 5 DEDICATED LAND DECOR GROUPS
            // ==========================================
            BuildLevel1TemperateDecor(d1, flowerPrefab, grassPrefab, shrubPrefab, rockPrefab, stonePrefab);
            BuildLevel2TropicalCoralDecor(d2);
            BuildLevel3AutumnDecor(d3, treeChoppedPrefab, ladderPrefab, rockPrefab, stonePrefab);
            BuildLevel4ArcticFrostDecor(d4, rockPrefab, stonePrefab);
            BuildLevel5DesertOasisDecor(d5, rockPrefab, stonePrefab);

            // Setup Theme Controller
            var themeCtrl = root.AddComponent<ToonEnvironmentThemeController>();
            themeCtrl.SetThemeMaterials(springMat, summerMat, fallMat, winterMat, desertMat);
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
        private static void BuildLevel1TemperateDecor(Transform g, GameObject flower, GameObject grass, GameObject shrub, GameObject rock, GameObject stone)
        {
            InstantiatePropAtGround(shrub, g, new Vector3(-7.8f, 0.0f, 6.5f), Quaternion.identity, 1.1f, 0.03f);
            InstantiatePropAtGround(shrub, g, new Vector3(-8.8f, 0.0f, 1.0f), Quaternion.identity, 1.0f, 0.03f);
            InstantiatePropAtGround(shrub, g, new Vector3(7.8f, 0.0f, 6.5f), Quaternion.identity, 1.1f, 0.03f);
            InstantiatePropAtGround(shrub, g, new Vector3(8.8f, 0.0f, 1.0f), Quaternion.identity, 1.0f, 0.03f);

            InstantiatePropAtGround(flower, g, new Vector3(-7.2f, 0.0f, 8.5f), Quaternion.Euler(0f, 35f, 0f), 1.2f, 0.02f);
            InstantiatePropAtGround(flower, g, new Vector3(-6.8f, 0.0f, 5.0f), Quaternion.Euler(0f, -40f, 0f), 1.1f, 0.02f);
            InstantiatePropAtGround(flower, g, new Vector3(7.2f, 0.0f, 8.5f), Quaternion.Euler(0f, -30f, 0f), 1.2f, 0.02f);
            InstantiatePropAtGround(flower, g, new Vector3(6.8f, 0.0f, 5.0f), Quaternion.Euler(0f, 45f, 0f), 1.1f, 0.02f);

            InstantiatePropAtGround(grass, g, new Vector3(-8.0f, 0.0f, 4.0f), Quaternion.identity, 1.2f, 0.01f);
            InstantiatePropAtGround(grass, g, new Vector3(8.0f, 0.0f, 4.0f), Quaternion.identity, 1.2f, 0.01f);

            var starfishA = LoadMarineProp("StarfishA");
            if (starfishA != null)
            {
                InstantiateProp(starfishA, g, new Vector3(-5.3f, 0.42f, 12.2f), Quaternion.Euler(0f, 25f, 0f), 1.8f);
                InstantiateProp(starfishA, g, new Vector3(5.3f, 0.42f, 12.2f), Quaternion.Euler(0f, -45f, 0f), 1.8f);
            }
        }

        // --- LEVEL 2 DECOR: TROPICAL CORAL ATOLL ---
        private static void BuildLevel2TropicalCoralDecor(Transform g)
        {
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

            // Shallow Reef in front of Fishing Pier
            InstantiateProp(coralAA, g, new Vector3(-4.0f, -0.15f, 15.6f), Quaternion.Euler(0f, 15f, 0f), 3.2f);
            InstantiateProp(coralCA, g, new Vector3(-3.3f, -0.18f, 16.2f), Quaternion.Euler(0f, 80f, 0f), 2.8f);
            InstantiateProp(seaweedA, g, new Vector3(-4.6f, -0.35f, 16.0f), Quaternion.Euler(0f, 40f, 0f), 1.4f);

            InstantiateProp(coralF, g, new Vector3(0.0f, -0.15f, 17.0f), Quaternion.Euler(0f, -35f, 0f), 3.0f);
            InstantiateProp(coralLA, g, new Vector3(0.8f, -0.18f, 17.5f), Quaternion.Euler(0f, 65f, 0f), 2.8f);
            InstantiateProp(seaweedB, g, new Vector3(-0.6f, -0.35f, 17.4f), Quaternion.Euler(0f, -20f, 0f), 1.5f);

            InstantiateProp(coralMA, g, new Vector3(4.0f, -0.15f, 15.4f), Quaternion.Euler(0f, 45f, 0f), 3.0f);
            InstantiateProp(coralOA, g, new Vector3(4.7f, -0.18f, 16.0f), Quaternion.Euler(0f, -75f, 0f), 2.8f);
            InstantiateProp(seaweedC, g, new Vector3(3.5f, -0.35f, 16.1f), Quaternion.Euler(0f, 15f, 0f), 1.4f);

            // Coastline Corals & Shells
            InstantiateProp(coralBA, g, new Vector3(-6.4f, 0.02f, 11.8f), Quaternion.Euler(0f, 30f, 0f), 2.8f);
            InstantiateProp(coralD, g, new Vector3(-7.2f, 0.05f, 12.6f), Quaternion.Euler(0f, -45f, 0f), 2.6f);
            InstantiateProp(coralG, g, new Vector3(6.4f, 0.02f, 11.8f), Quaternion.Euler(0f, -30f, 0f), 2.8f);
            InstantiateProp(coralIA, g, new Vector3(7.2f, 0.05f, 12.5f), Quaternion.Euler(0f, 50f, 0f), 2.6f);

            InstantiateProp(shellA, g, new Vector3(-6.1f, 0.12f, 10.6f), Quaternion.Euler(0f, 70f, 0f), 2.5f);
            InstantiateProp(shellB, g, new Vector3(6.1f, 0.12f, 10.8f), Quaternion.Euler(0f, -65f, 0f), 2.5f);
            InstantiateProp(starfishB, g, new Vector3(6.6f, 0.25f, 10.4f), Quaternion.Euler(8f, -50f, 12f), 2.2f);
        }

        // --- LEVEL 3 DECOR: GOLDEN AUTUMN COVE ---
        private static void BuildLevel3AutumnDecor(Transform g, GameObject choppedTree, GameObject ladder, GameObject rock, GameObject stone)
        {
            InstantiatePropAtGround(choppedTree, g, new Vector3(-6.5f, 0.0f, 8.5f), Quaternion.Euler(0f, 30f, 0f), 1.2f, 0.02f);
            InstantiatePropAtGround(choppedTree, g, new Vector3(-6.8f, 0.0f, 4.2f), Quaternion.Euler(0f, -65f, 0f), 1.1f, 0.02f);
            InstantiatePropAtGround(choppedTree, g, new Vector3(6.5f, 0.0f, 8.5f), Quaternion.Euler(0f, -40f, 0f), 1.2f, 0.02f);
            InstantiatePropAtGround(choppedTree, g, new Vector3(6.8f, 0.0f, 4.2f), Quaternion.Euler(0f, 55f, 0f), 1.1f, 0.02f);

            InstantiatePropAtGround(ladder, g, new Vector3(-8.2f, 0.0f, 6.2f), Quaternion.Euler(0f, 70f, 12f), 1.0f, 0.02f);
            InstantiatePropAtGround(ladder, g, new Vector3(8.2f, 0.0f, 6.2f), Quaternion.Euler(0f, -70f, 12f), 1.0f, 0.02f);

            InstantiatePropAtGround(stone, g, new Vector3(-5.5f, 0.0f, 10.2f), Quaternion.Euler(0f, 25f, 0f), 1.3f, 0.03f);
            InstantiatePropAtGround(stone, g, new Vector3(5.5f, 0.0f, 10.2f), Quaternion.Euler(0f, -35f, 0f), 1.3f, 0.03f);
        }

        // --- LEVEL 4 DECOR: ARCTIC FROST HAVEN ---
        private static void BuildLevel4ArcticFrostDecor(Transform g, GameObject rock, GameObject stone)
        {
            // Giant Icebergs floating in the sea in front of pier
            InstantiateProp(rock, g, new Vector3(-4.5f, -0.6f, 16.5f), Quaternion.Euler(15f, 45f, -10f), 3.8f);
            InstantiateProp(stone, g, new Vector3(-3.2f, -0.7f, 18.0f), Quaternion.Euler(5f, 110f, 0f), 2.8f);

            InstantiateProp(rock, g, new Vector3(4.8f, -0.6f, 16.2f), Quaternion.Euler(-12f, -60f, 8f), 3.9f);
            InstantiateProp(stone, g, new Vector3(3.5f, -0.7f, 17.8f), Quaternion.Euler(0f, -125f, 0f), 2.9f);

            InstantiateProp(rock, g, new Vector3(0.0f, -0.7f, 19.5f), Quaternion.Euler(0f, 180f, 0f), 4.2f);

            // Frosted Shoreline Rocks
            InstantiatePropAtGround(rock, g, new Vector3(-6.2f, 0.0f, 11.2f), Quaternion.Euler(0f, 25f, 0f), 1.6f, 0.04f);
            InstantiatePropAtGround(rock, g, new Vector3(6.2f, 0.0f, 11.2f), Quaternion.Euler(0f, -35f, 0f), 1.6f, 0.04f);
        }

        // --- LEVEL 5 DECOR: SUNKEN OASIS DESERT ---
        private static void BuildLevel5DesertOasisDecor(Transform g, GameObject rock, GameObject stone)
        {
            // Ancient Sandstone Monoliths
            InstantiateProp(rock, g, new Vector3(-5.2f, -0.2f, 15.5f), Quaternion.Euler(0f, 35f, 0f), 2.8f);
            InstantiateProp(stone, g, new Vector3(-4.5f, 0.0f, 14.0f), Quaternion.Euler(0f, 75f, 0f), 2.2f);
            InstantiateProp(rock, g, new Vector3(5.2f, -0.2f, 15.5f), Quaternion.Euler(0f, -45f, 0f), 2.8f);
            InstantiateProp(stone, g, new Vector3(4.5f, 0.0f, 14.0f), Quaternion.Euler(0f, -80f, 0f), 2.2f);

            // Sunken Treasure Chests in Shallow Water
            GameObject chestObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Generated/Sunken_Treasure_Chest.obj");
            if (chestObj != null)
            {
                InstantiateProp(chestObj, g, new Vector3(-2.8f, -0.25f, 15.2f), Quaternion.Euler(8f, 25f, -5f), 1.4f);
                InstantiateProp(chestObj, g, new Vector3(2.8f, -0.25f, 15.2f), Quaternion.Euler(-5f, -30f, 8f), 1.4f);
            }

            var shellA = LoadMarineProp("ShellA");
            var shellC = LoadMarineProp("ShellC");
            if (shellA != null) InstantiateProp(shellA, g, new Vector3(-6.2f, 0.1f, 11.0f), Quaternion.Euler(0f, 40f, 0f), 2.2f);
            if (shellC != null) InstantiateProp(shellC, g, new Vector3(6.2f, 0.1f, 11.0f), Quaternion.Euler(0f, -50f, 0f), 2.2f);
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

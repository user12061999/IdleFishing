#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleFishing.Core;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Fishing;
using IdleFishing.Inventory;
using IdleFishing.Market;
using IdleFishing.UI;
using IdleFishing.World;
using IdleFishing.CameraControl;
using IdleFishing.Logistics;

namespace IdleFishing.Editor
{
    public static class DemoSceneBuilder
    {
        [MenuItem("Idle Fishing/Create Demo Core Loop Scene", false, 2)]
        public static void CreateDemoScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1. Environment & Lighting
            GameObject lightObj = GameObject.Find("Directional Light");
            if (lightObj != null)
            {
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Ground & Water (Toon Stylized Materials)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground Plane";
            ground.transform.position = new Vector3(0f, 0f, -10f);
            ground.transform.localScale = new Vector3(6f, 1f, 4f);
            var asphaltMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Selcuk Gerceker/Toon Environments - World Creator Pack Lite/Materials/Summer_Asphalt.mat");
            if (asphaltMat != null)
            {
                var gMr = ground.GetComponent<MeshRenderer>();
                if (gMr != null) gMr.sharedMaterial = asphaltMat;
            }

            // Water Area (Lake / Ocean with Stylized Water Shader & Flow VFX)
            GameObject waterObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            waterObj.name = "Water Area";
            waterObj.transform.position = new Vector3(0f, -0.2f, 26f);
            waterObj.transform.localScale = new Vector3(50f, 0.5f, 34f);
            var waterFlowCtrl = waterObj.AddComponent<OceanWaterFlowController>();
            waterFlowCtrl.InitializeWaterSystem();

            // 2. Core Managers
            GameObject managersRoot = new GameObject("--- MANAGERS ---");
            var gameMgr = managersRoot.AddComponent<GameManager>();
            var ecoMgr = managersRoot.AddComponent<EconomyManager>();
            var upgMgr = managersRoot.AddComponent<UpgradeManager>();
            var dmgMgr = managersRoot.AddComponent<FloatingDamageManager>();
            var saveMgr = managersRoot.AddComponent<SaveDataManager>();
            var worldMgr = managersRoot.AddComponent<WorldManager>();
            var frenzyMgr = managersRoot.AddComponent<FishingFrenzyManager>();
            var trendMgr = managersRoot.AddComponent<MarketTrendManager>();
            var contractMgr = managersRoot.AddComponent<ContractManager>();
            var bestiaryMgr = managersRoot.AddComponent<IdleFishing.Aquarium.BestiaryManager>();
            var aquariumMgr = managersRoot.AddComponent<IdleFishing.Aquarium.AquariumManager>();
            var audioMgr = managersRoot.AddComponent<IdleFishing.Audio.AudioManager>();
            var vfxMgr = managersRoot.AddComponent<IdleFishing.VFX.VFXManager>();
            var dayNightMgr = managersRoot.AddComponent<IdleFishing.World.DayNightCycleManager>();
            var prestigeMgr = managersRoot.AddComponent<IdleFishing.Economy.PrestigeManager>();

            UpgradeConfig upgradeConfig = AssetDatabase.LoadAssetAtPath<UpgradeConfig>("Assets/GameData/Upgrades/DefaultUpgradeConfig.asset");
            if (upgradeConfig != null)
            {
                var so = new SerializedObject(upgMgr);
                so.FindProperty("upgradeConfig").objectReferenceValue = upgradeConfig;
                so.ApplyModifiedProperties();
            }

            GameBalanceConfig balanceConfig = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>("Assets/GameData/Balance/DefaultGameBalanceConfig.asset");
            if (balanceConfig != null)
            {
                var gmSO = new SerializedObject(gameMgr);
                gmSO.FindProperty("balanceConfig").objectReferenceValue = balanceConfig;
                gmSO.ApplyModifiedProperties();
            }

            // 3. Fishing Spot in Water
            GameObject fishingSpotObj = new GameObject("FishingSpot_Water");
            fishingSpotObj.transform.position = new Vector3(0f, 0.2f, 15f);
            var spot = fishingSpotObj.AddComponent<FishingSpot>();

            WorldData w1 = AssetDatabase.LoadAssetAtPath<WorldData>("Assets/GameData/Worlds/world_1.asset");
            WorldData w2 = AssetDatabase.LoadAssetAtPath<WorldData>("Assets/GameData/Worlds/world_2.asset");
            WorldData w3 = AssetDatabase.LoadAssetAtPath<WorldData>("Assets/GameData/Worlds/world_3.asset");
            WorldData w4 = AssetDatabase.LoadAssetAtPath<WorldData>("Assets/GameData/Worlds/world_4.asset");
            WorldData w5 = AssetDatabase.LoadAssetAtPath<WorldData>("Assets/GameData/Worlds/world_5.asset");
            if (w1 != null) worldMgr.AddWorld(w1);
            if (w2 != null) worldMgr.AddWorld(w2);
            if (w3 != null) worldMgr.AddWorld(w3);
            if (w4 != null) worldMgr.AddWorld(w4);
            if (w5 != null) worldMgr.AddWorld(w5);
            worldMgr.RegisterReferences(spot, ground.GetComponent<Renderer>(), waterObj.GetComponent<Renderer>(), lightObj != null ? lightObj.GetComponent<Light>() : null);

            FishChainData salmonChain = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/salmon_chain.asset");
            FishChainData codChain = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/cod_chain.asset");
            FishChainData herringChain = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/herring_chain.asset");

            spot.AddSpecies(salmonChain);
            spot.AddSpecies(codChain);
            spot.AddSpecies(herringChain);

            var initialSpecies = new System.Collections.Generic.List<FishChainData>();
            if (salmonChain != null) initialSpecies.Add(salmonChain);
            if (codChain != null) initialSpecies.Add(codChain);
            if (herringChain != null) initialSpecies.Add(herringChain);
            contractMgr.SetSpeciesPool(initialSpecies);

            // 4. Fishing Pier & Sitting Fisherman Worker Slots
            GameObject fishingPierRoot = new GameObject("--- FISHING ZONE (PIER) ---");

            // Wooden Pier (Expanded to 12.0m width with Toon Timber Deck & Pilings)
            GameObject pierPlank = null;
            var pierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_FishingPier_Plank.prefab");
            if (pierPrefab != null)
            {
                pierPlank = Object.Instantiate(pierPrefab, fishingPierRoot.transform);
                pierPlank.name = "Pier_Plank";
                pierPlank.transform.position = new Vector3(0f, 0.4f, 8f);
                pierPlank.transform.rotation = Quaternion.identity;
                pierPlank.transform.localScale = Vector3.one;
            }
            else
            {
                pierPlank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pierPlank.name = "Pier_Plank";
                pierPlank.transform.SetParent(fishingPierRoot.transform);
                pierPlank.transform.position = new Vector3(0f, 0.4f, 8f);
                pierPlank.transform.localScale = new Vector3(12.0f, 0.4f, 6.5f);
            }

            // Landing Zone on Ground for flapping fish
            GameObject landingZoneObj = new GameObject("LandingZone_FishFight");
            landingZoneObj.transform.SetParent(fishingPierRoot.transform);
            landingZoneObj.transform.position = new Vector3(0f, 0.6f, 6.5f);

            // 5. Harbor Intake Storage (Kho tiếp nhận cá từ cần thủ / thợ bắt cá)
            GameObject harborStorageObj = new GameObject("Harbor_IntakeStorage");
            harborStorageObj.transform.position = new Vector3(-3.5f, 0.5f, 3.5f);
            var hCol = harborStorageObj.AddComponent<BoxCollider>();
            hCol.size = new Vector3(2.2f, 0.6f, 2.2f);
            hCol.isTrigger = true;
            var harborPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Harbor_IntakeStorage.prefab");
            if (harborPrefab != null)
            {
                var hVis = Object.Instantiate(harborPrefab, harborStorageObj.transform);
                hVis.name = "Model_HarborStorage";
                hVis.transform.localPosition = Vector3.zero;
            }
            var harborInv = harborStorageObj.AddComponent<Inventory.Inventory>();
            harborInv.SetMaxCapacity(48);
            var harborVis = harborStorageObj.AddComponent<WorldItemStackVisualizer>();
            var harborVisSO = new SerializedObject(harborVis);
            harborVisSO.FindProperty("stackOriginOffset").vector3Value = new Vector3(0, 0.65f, 0);
            harborVisSO.ApplyModifiedProperties();

            // Coordinator: Fishing Pier Manager
            var pierMgr = fishingPierRoot.AddComponent<FishingPierManager>();
            var pierMgrSO = new SerializedObject(pierMgr);
            pierMgrSO.FindProperty("intakeStorage").objectReferenceValue = harborInv;
            pierMgrSO.FindProperty("landingZoneCenter").objectReferenceValue = landingZoneObj.transform;
            pierMgrSO.ApplyModifiedProperties();

            // 4b. 5 Fishing Slots (Slot 1 free from start, Slots 2-5 purchasable)
            var slot1 = CreateFishermanSlot(fishingPierRoot.transform, spot, landingZoneObj.transform, 0, "Fisherman 1", new Vector3(0f, 1.3f, 9.5f), true);
            var slot2 = CreateFishermanSlot(fishingPierRoot.transform, spot, landingZoneObj.transform, 1, "Fisherman 2", new Vector3(-2.3f, 1.3f, 9.5f), false);
            var slot3 = CreateFishermanSlot(fishingPierRoot.transform, spot, landingZoneObj.transform, 2, "Fisherman 3", new Vector3(2.3f, 1.3f, 9.5f), false);
            var slot4 = CreateFishermanSlot(fishingPierRoot.transform, spot, landingZoneObj.transform, 3, "Fisherman 4", new Vector3(-4.6f, 1.3f, 9.5f), false);
            var slot5 = CreateFishermanSlot(fishingPierRoot.transform, spot, landingZoneObj.transform, 4, "Fisherman 5", new Vector3(4.6f, 1.3f, 9.5f), false);
            pierMgr.RegisterSlot(slot1);
            pierMgr.RegisterSlot(slot2);
            pierMgr.RegisterSlot(slot3);
            pierMgr.RegisterSlot(slot4);
            pierMgr.RegisterSlot(slot5);

            // 5. Fish Wranglers & Wrangler Manager (Unlimited wranglers dynamically hired)
            GameObject wranglerGroup = new GameObject("--- WRANGLERS (COMBAT) ---");
            var wranglerMgr = wranglerGroup.AddComponent<WranglerManager>();

            // 3D Recruitment Station Pad for Wranglers
            GameObject hirePadObj = CreateWorldPurchasePad(
                "Pad_Hire_Wrangler",
                new Vector3(3.2f, 0.52f, 4.8f),
                wranglerGroup.transform,
                UpgradeType.HireWrangler,
                "[+] HIRE FISH WRANGLER",
                "Auto catch & collect fish!"
            );
            var hirePadScript = hirePadObj.GetComponent<InWorldPurchasePad>();
            GameObject wranglerModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Wrangler.prefab");
            wranglerMgr.Setup(harborInv, new Vector3(3.5f, 0.5f, 6.2f), hirePadObj, hirePadScript, wranglerModelPrefab);

            // 5b. World Portal Gate (Cổng Thế Giới Mới ven bến cảng)
            GameObject portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_WorldPortal_Gate.prefab");
            GameObject portalObj = new GameObject("WorldPortal_Gate");
            portalObj.transform.position = new Vector3(-7.5f, 0.4f, 8.0f);
            portalObj.transform.rotation = Quaternion.Euler(0, 45f, 0);
            if (portalPrefab != null)
            {
                var pVis = Object.Instantiate(portalPrefab, portalObj.transform);
                pVis.name = "Model_PortalGate";
                pVis.transform.localPosition = Vector3.zero;
                pVis.transform.localRotation = Quaternion.identity;
            }
            var pCol = portalObj.AddComponent<BoxCollider>();
            pCol.size = new Vector3(3.6f, 4.2f, 1.6f);
            pCol.center = new Vector3(0, 2.1f, 0);
            portalObj.AddComponent<IdleFishing.World.WorldGateTrigger>();

            // 6B. Luxury Aquarium 3D Zone (Phase 2 - Right side of the Pier)
            GameObject aquariumRoot = new GameObject("--- LUXURY AQUARIUM ZONE ---");
            aquariumRoot.transform.position = new Vector3(7.5f, 0.4f, 8.0f);

            // Luxury Aquarium Model (Transparent Glass & Water)
            GameObject aquariumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Aquarium_LuxuryTank.prefab");
            if (aquariumPrefab != null)
            {
                var aqVis = Object.Instantiate(aquariumPrefab, aquariumRoot.transform);
                aqVis.name = "Model_Aquarium";
                aqVis.transform.localPosition = Vector3.zero;

                foreach (var mr in aqVis.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.name.Contains("Glass") || mr.name.Contains("Water"))
                    {
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }

            // Underwater Rising Bubbles VFX
            GameObject bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Aquarium/BubbleRisingUnderwater.prefab");
            if (bubblePrefab != null)
            {
                var bubbleObj = Object.Instantiate(bubblePrefab, aquariumRoot.transform);
                bubbleObj.name = "VFX_Tank_Bubbles";
                bubbleObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                bubbleObj.transform.localScale = Vector3.one * 0.45f;
                foreach (var ps in bubbleObj.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = ps.main;
                    main.loop = true;
                    main.playOnAwake = true;
                }
            }

            // Tank Lighting
            GameObject tankLightObj = new GameObject("Tank_NeonLight");
            tankLightObj.transform.SetParent(aquariumRoot.transform, false);
            tankLightObj.transform.localPosition = new Vector3(0, 2.2f, 0);
            var nLight = tankLightObj.AddComponent<Light>();
            nLight.type = LightType.Point;
            nLight.color = new Color(0.2f, 0.85f, 1.0f);
            nLight.intensity = 1.8f;
            nLight.range = 6.0f;

            // Interactive Viewing Pad in front (Toon Stepped Stone Pad)
            GameObject viewPad = null;
            var padPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_WorldPurchasePad.prefab");
            if (padPrefab != null)
            {
                viewPad = Object.Instantiate(padPrefab, aquariumRoot.transform);
                viewPad.name = "Pad_Aquarium_View";
                viewPad.transform.localPosition = new Vector3(0, 0.05f, -2.2f);
                viewPad.transform.localRotation = Quaternion.identity;
                viewPad.transform.localScale = Vector3.one * 0.9f;
                var viewCol = viewPad.AddComponent<CapsuleCollider>();
                viewCol.center = new Vector3(0f, 0.5f, 0f);
                viewCol.radius = 1.0f;
                viewCol.height = 1.2f;
                viewCol.isTrigger = true;
            }
            else
            {
                viewPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                viewPad.name = "Pad_Aquarium_View";
                viewPad.transform.SetParent(aquariumRoot.transform, false);
                viewPad.transform.localPosition = new Vector3(0, 0.05f, -2.2f);
                viewPad.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
                var padCol = viewPad.GetComponent<Collider>();
                if (padCol != null) padCol.isTrigger = true;
            }
            viewPad.AddComponent<IdleFishing.Aquarium.AquariumPadTrigger>();

            // Aquarium Visual script
            aquariumRoot.AddComponent<IdleFishing.Aquarium.AquariumTankVisual>();

            // 7. Market Zone & Customers (Cá nguyên con 3D từ Kho Bến Cảng chuyển thẳng ra Quầy Chợ Hải Sản Tươi)
            GameObject marketRoot = new GameObject("--- MARKET ZONE ---");
            GameObject shelfObj = new GameObject("Market_DisplayShelf");
            shelfObj.transform.SetParent(marketRoot.transform);
            shelfObj.transform.position = new Vector3(0f, 0.5f, -8f);
            var sCol = shelfObj.AddComponent<BoxCollider>();
            sCol.size = new Vector3(3.5f, 0.7f, 1.5f);
            sCol.isTrigger = true;

            GameObject counterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Market_DisplayCounter.prefab");
            if (counterPrefab != null)
            {
                var cVis = Object.Instantiate(counterPrefab, shelfObj.transform);
                cVis.name = "Model_DisplayCounter";
                cVis.transform.localPosition = Vector3.zero;
                foreach (var mr in cVis.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.name.Contains("Glass"))
                    {
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }

            var shelf = shelfObj.AddComponent<DisplayShelf>();
            var shelfInv = shelfObj.AddComponent<Inventory.Inventory>();
            shelfInv.SetMaxCapacity(32);
            var shelfVis = shelfObj.AddComponent<WorldItemStackVisualizer>();
            shelfVis.ConfigureLayout(new Vector3(0, 0.35f, -0.05f), 3, 2, 0.8f, 0.45f, Vector3.one * 0.75f);

            // 7b. Logistics & Cart Transporter Workers (Physical cart delivery chain: Harbor -> Market / Cargo Ship)
            GameObject logisticsRoot = new GameObject("--- LOGISTICS & TRANSPORTERS ---");
            var logMgr = logisticsRoot.AddComponent<LogisticsManager>();

            // Route 1: Fresh Whole Fish from Harbor Intake -> Market Display Shelf (Priority 1)
            logMgr.RegisterRoute(new LogisticsRoute
            {
                routeName = "Harbor_To_Market",
                sourceInventory = harborInv,
                pickupPosition = new Vector3(-3.5f, 0.5f, 2.1f),
                targetInventory = shelfInv,
                dropoffPosition = new Vector3(0f, 0.5f, -6.6f),
                itemType = ItemType.WholeFish,
                priority = 1
            });

            // In-World Hire Pad for Transporters
            GameObject transHirePadObj = CreateTransporterHirePad(new Vector3(-3.8f, 0.52f, 1.8f), logisticsRoot.transform);
            GameObject porterModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Transporter.prefab");
            logMgr.SetupHirePad(transHirePadObj, null, new Vector3(-3.8f, 0.5f, 0.6f), porterModelPrefab);

            // 3D Harbor Contract Board Signpost
            CreateWorldContractBoard(new Vector3(-4.5f, 0f, -6f), marketRoot.transform);

            // Customer Waypoints & Spawner (Framed nicely in portrait view)
            GameObject custSpawnObj = new GameObject("Customer_SpawnPoint");
            custSpawnObj.transform.SetParent(marketRoot.transform);
            custSpawnObj.transform.position = new Vector3(-6.5f, 0.5f, -10.5f);

            GameObject custExitObj = new GameObject("Customer_ExitPoint");
            custExitObj.transform.SetParent(marketRoot.transform);
            custExitObj.transform.position = new Vector3(6.5f, 0.5f, -10.5f);

            GameObject spawnerObj = new GameObject("CustomerSpawner");
            spawnerObj.transform.SetParent(marketRoot.transform);
            var spawner = spawnerObj.AddComponent<CustomerSpawner>();
            var spawnerSO = new SerializedObject(spawner);
            spawnerSO.FindProperty("spawnPoint").objectReferenceValue = custSpawnObj.transform;
            spawnerSO.FindProperty("exitPoint").objectReferenceValue = custExitObj.transform;

            var custCasual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Customer_Casual.prefab");
            var custVIP = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Customer_VIP.prefab");
            var custTourist = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Customer_Tourist.prefab");

            SerializedProperty custArray = spawnerSO.FindProperty("customerPrefabs");
            custArray.arraySize = 2;
            custArray.GetArrayElementAtIndex(0).objectReferenceValue = custCasual;
            custArray.GetArrayElementAtIndex(1).objectReferenceValue = custVIP;

            SerializedProperty touristArray = spawnerSO.FindProperty("touristPrefabs");
            touristArray.arraySize = 2;
            touristArray.GetArrayElementAtIndex(0).objectReferenceValue = custTourist;
            touristArray.GetArrayElementAtIndex(1).objectReferenceValue = custCasual;

            SerializedProperty shelvesProp = spawnerSO.FindProperty("shelves");
            shelvesProp.arraySize = 1;
            shelvesProp.GetArrayElementAtIndex(0).objectReferenceValue = shelf;
            spawnerSO.ApplyModifiedProperties();

            // 8c. Harbor Expansion & Architecture (Lighthouse, Seaside Cafe, Atmosphere Decor)
            GameObject expansionRoot = new GameObject("--- HARBOR EXPANSION & ARCHITECTURE ---");
            CreateLighthouse(expansionRoot.transform);
            CreateSeasideCafe(expansionRoot.transform);
            CreateHarborAtmosphereDecor(expansionRoot.transform);

            // 8d. Toon Harbor Environment & Modular Scenery
            ToonEnvironmentBuilder.BuildEnvironment(false);

            // 9. Camera Setup (Optimized for Mobile Portrait 9:16 / 9:19.5 with full-harbor view)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 29f, -18.5f);
                mainCam.transform.rotation = Quaternion.Euler(55.5f, 0f, 0f);
                mainCam.fieldOfView = 50f;

                var camCtrl = mainCam.GetComponent<MobilePortraitCameraController>();
                if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<MobilePortraitCameraController>();
                camCtrl.ApplyDefaultFraming();
            }

            // 10. UI Canvas
            CreateSceneCanvas(upgradeConfig);

            // Save Scene
            string scenePath = "Assets/Scenes/IdleFishing_CoreLoop.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[IdleFishing] New Core Loop Scene (Fisherman + Fish Fighter Combat + Direct Whole Fish Market & Export) completed: {scenePath}");
        }

        private static void CreateSceneCanvas(UpgradeConfig upgradeConfig)
        {
            GameObject canvasObj = new GameObject("UI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f; // Match Width for mobile portrait
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<IdleFishing.UI.UIInputEnforcer>();

            // Ensure EventSystem (StandaloneInputModule only, no new input system)
            var existingES = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            GameObject esObj = existingES != null ? existingES.gameObject : new GameObject("EventSystem");
            if (existingES == null)
            {
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }

            var existingModules = esObj.GetComponents<UnityEngine.EventSystems.BaseInputModule>();
            foreach (var mod in existingModules)
            {
                if (!(mod is UnityEngine.EventSystems.StandaloneInputModule))
                {
                    Object.DestroyImmediate(mod);
                }
            }
            var sim = esObj.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (sim == null)
            {
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Top HUD
            GameObject topPanel = new GameObject("TopHUD");
            topPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform topRect = topPanel.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 100);

            // Coins Text
            GameObject coinsTextObj = new GameObject("CoinsText");
            coinsTextObj.transform.SetParent(topPanel.transform, false);
            TextMeshProUGUI coinsText = coinsTextObj.AddComponent<TextMeshProUGUI>();
            coinsText.text = "100 G";
            coinsText.fontSize = 38;
            coinsText.fontStyle = FontStyles.Bold;
            coinsText.alignment = TextAlignmentOptions.Left;
            coinsText.color = new Color(1f, 0.88f, 0.2f);
            coinsText.raycastTarget = false;
            RectTransform coinsRect = coinsTextObj.GetComponent<RectTransform>();
            coinsRect.anchorMin = new Vector2(0, 0.5f);
            coinsRect.anchorMax = new Vector2(0, 0.5f);
            coinsRect.anchoredPosition = new Vector2(120, 0);
            coinsRect.sizeDelta = new Vector2(300, 50);

            var gameHUD = topPanel.AddComponent<GameHUD>();
            var hudSO = new SerializedObject(gameHUD);
            hudSO.FindProperty("coinsText").objectReferenceValue = coinsText;
            hudSO.ApplyModifiedProperties();

            // Level & Land Indicator Badge (Directly in TopHUD)
            Sprite whiteSp = FloatingHealthBarUI.GetWhiteSprite();
            GameObject lvlBadgeObj = new GameObject("LevelBadge_Button");
            lvlBadgeObj.transform.SetParent(topPanel.transform, false);
            RectTransform lvlBadgeRt = lvlBadgeObj.AddComponent<RectTransform>();
            lvlBadgeRt.anchorMin = new Vector2(0.5f, 0.5f);
            lvlBadgeRt.anchorMax = new Vector2(0.5f, 0.5f);
            lvlBadgeRt.pivot = new Vector2(0.5f, 0.5f);
            lvlBadgeRt.anchoredPosition = new Vector2(-40f, 0f);
            lvlBadgeRt.sizeDelta = new Vector2(240f, 64f);

            Image lvlBadgeImg = lvlBadgeObj.AddComponent<Image>();
            lvlBadgeImg.sprite = whiteSp;
            lvlBadgeImg.color = new Color(0.10f, 0.20f, 0.32f, 0.92f);

            Button lvlBadgeBtn = lvlBadgeObj.AddComponent<Button>();
            lvlBadgeBtn.targetGraphic = lvlBadgeImg;

            // Level Title Text (e.g., "LEVEL 1")
            GameObject lvlTitleObj = new GameObject("Text_LevelTitle");
            lvlTitleObj.transform.SetParent(lvlBadgeObj.transform, false);
            var lvlTitleTxt = lvlTitleObj.AddComponent<TextMeshProUGUI>();
            lvlTitleTxt.text = "LEVEL 1";
            lvlTitleTxt.fontSize = 20;
            lvlTitleTxt.fontStyle = FontStyles.Bold;
            lvlTitleTxt.alignment = TextAlignmentOptions.Center;
            lvlTitleTxt.color = new Color(0.35f, 0.85f, 1f);
            lvlTitleTxt.raycastTarget = false;
            RectTransform ltRt = lvlTitleObj.GetComponent<RectTransform>();
            ltRt.anchorMin = new Vector2(0f, 0.45f);
            ltRt.anchorMax = new Vector2(1f, 1f);
            ltRt.sizeDelta = Vector2.zero;

            // Land Name Text (e.g., "Vịnh Ôn Đới")
            GameObject landNameObj = new GameObject("Text_LandName");
            landNameObj.transform.SetParent(lvlBadgeObj.transform, false);
            var landNameTxt = landNameObj.AddComponent<TextMeshProUGUI>();
            landNameTxt.text = "Vịnh Ôn Đới";
            landNameTxt.fontSize = 15;
            landNameTxt.fontStyle = FontStyles.Normal;
            landNameTxt.alignment = TextAlignmentOptions.Center;
            landNameTxt.color = Color.white;
            landNameTxt.raycastTarget = false;
            RectTransform lnRt = landNameObj.GetComponent<RectTransform>();
            lnRt.anchorMin = new Vector2(0f, 0f);
            lnRt.anchorMax = new Vector2(1f, 0.52f);
            lnRt.sizeDelta = Vector2.zero;

            // Next Level Alert Badge (Pulsing tag if ready to level up)
            GameObject nextLvlAlertObj = new GameObject("NextLevel_AlertBadge");
            nextLvlAlertObj.transform.SetParent(lvlBadgeObj.transform, false);
            RectTransform nlRt = nextLvlAlertObj.AddComponent<RectTransform>();
            nlRt.anchorMin = new Vector2(0.5f, 0f);
            nlRt.anchorMax = new Vector2(0.5f, 0f);
            nlRt.pivot = new Vector2(0.5f, 1f);
            nlRt.anchoredPosition = new Vector2(0, -6);
            nlRt.sizeDelta = new Vector2(190, 22);

            Image nlImg = nextLvlAlertObj.AddComponent<Image>();
            nlImg.sprite = whiteSp;
            nlImg.color = new Color(0.18f, 0.80f, 0.44f, 0.95f);

            GameObject nlTxtObj = new GameObject("Text");
            nlTxtObj.transform.SetParent(nextLvlAlertObj.transform, false);
            var nlTxt = nlTxtObj.AddComponent<TextMeshProUGUI>();
            nlTxt.text = "NEW LAND READY!";
            nlTxt.fontSize = 13;
            nlTxt.fontStyle = FontStyles.Bold;
            nlTxt.alignment = TextAlignmentOptions.Center;
            nlTxt.color = Color.white;
            nlTxt.raycastTarget = false;
            RectTransform nltRt = nlTxtObj.GetComponent<RectTransform>();
            nltRt.anchorMin = Vector2.zero;
            nltRt.anchorMax = Vector2.one;
            nltRt.sizeDelta = Vector2.zero;
            nextLvlAlertObj.SetActive(false);

            gameHUD.SetupLevelBadge(lvlBadgeBtn, lvlTitleTxt, landNameTxt, nextLvlAlertObj, nlTxt);

            // Active Tap Hint Banner (Gọn gàng, chữ to, trực quan)
            GameObject hintObj = new GameObject("PlayerTapHint");
            hintObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI hintText = hintObj.AddComponent<TextMeshProUGUI>();
            hintText.text = "TAP FISH TO ASSIST";
            hintText.fontSize = 24;
            hintText.fontStyle = FontStyles.Bold;
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.color = new Color(1f, 1f, 1f, 0.90f);
            hintText.raycastTarget = false;
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0, 85);
            hintRect.sizeDelta = new Vector2(700, 36);

            // Collapsible Upgrade Menu
            var upgradeMenu = canvasObj.AddComponent<CollapsibleUpgradeMenuUI>();
            var uMenuSO = new SerializedObject(upgradeMenu);
            uMenuSO.FindProperty("upgradeConfig").objectReferenceValue = upgradeConfig;
            uMenuSO.ApplyModifiedProperties();

            // Flying Coin Manager
            var coinMgr = canvasObj.AddComponent<FlyingCoinManager>();
            coinMgr.SetTarget(coinsRect);

            // Boss Alert Banner
            canvasObj.AddComponent<BossAlertBannerUI>();

            // Offline Earnings Welcome Popup
            canvasObj.AddComponent<OfflineEarningsPopupUI>();

            // World Selection & Travel Map Modal
            canvasObj.AddComponent<WorldSelectionPopupUI>();

            // Fishing Frenzy HUD (Combo Bar & Countdown)
            canvasObj.AddComponent<FishingFrenzyHUDUI>();

            // Market Trend Ticker UI (Dynamic hot fish prices)
            canvasObj.AddComponent<MarketTrendTickerUI>();

            // Harbor Commercial Contracts Modal Popup
            canvasObj.AddComponent<ContractBoardPopupUI>();

            // Luxury Aquarium & Bestiary Modal Popup (Phase 2)
            canvasObj.AddComponent<IdleFishing.UI.AquariumPopupUI>();

            // Contracts Board Button at Top-Right (Placed next to Map Button)
            GameObject contractNavBtnObj = new GameObject("Btn_ContractsBoard");
            contractNavBtnObj.transform.SetParent(canvasObj.transform, false);

            RectTransform cBtnRt = contractNavBtnObj.AddComponent<RectTransform>();
            cBtnRt.anchorMin = new Vector2(1f, 1f);
            cBtnRt.anchorMax = new Vector2(1f, 1f);
            cBtnRt.pivot = new Vector2(1f, 1f);
            cBtnRt.anchoredPosition = new Vector2(-240, -20);
            cBtnRt.sizeDelta = new Vector2(210, 64);

            Image cBtnImg = contractNavBtnObj.AddComponent<Image>();
            cBtnImg.sprite = whiteSp;
            cBtnImg.color = new Color(0.12f, 0.22f, 0.35f, 0.95f);

            Button cBtn = contractNavBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBtnImg;

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(contractNavBtnObj.transform, false);
            var cTxt = cTxtObj.AddComponent<TextMeshProUGUI>();
            cTxt.text = "CONTRACTS";
            cTxt.fontSize = 22;
            cTxt.fontStyle = FontStyles.Bold;
            cTxt.alignment = TextAlignmentOptions.Center;
            cTxt.color = new Color(1f, 0.88f, 0.4f);
            cTxt.raycastTarget = false;
            RectTransform cbTxtRt = cTxtObj.GetComponent<RectTransform>();
            cbTxtRt.anchorMin = Vector2.zero;
            cbTxtRt.anchorMax = Vector2.one;
            cbTxtRt.sizeDelta = Vector2.zero;

            // Aquarium Navigation Button at Top-Right (Next to Contracts Button)
            GameObject aqNavBtnObj = new GameObject("Btn_Aquarium");
            aqNavBtnObj.transform.SetParent(canvasObj.transform, false);

            RectTransform aqBtnRt = aqNavBtnObj.AddComponent<RectTransform>();
            aqBtnRt.anchorMin = new Vector2(1f, 1f);
            aqBtnRt.anchorMax = new Vector2(1f, 1f);
            aqBtnRt.pivot = new Vector2(1f, 1f);
            aqBtnRt.anchoredPosition = new Vector2(-460, -20);
            aqBtnRt.sizeDelta = new Vector2(210, 64);

            Image aqBtnImg = aqNavBtnObj.AddComponent<Image>();
            aqBtnImg.sprite = whiteSp;
            aqBtnImg.color = new Color(0.08f, 0.28f, 0.45f, 0.95f);

            Button aqBtn = aqNavBtnObj.AddComponent<Button>();
            aqBtn.targetGraphic = aqBtnImg;

            GameObject aqTxtObj = new GameObject("Text");
            aqTxtObj.transform.SetParent(aqNavBtnObj.transform, false);
            var aqTxt = aqTxtObj.AddComponent<TextMeshProUGUI>();
            aqTxt.text = "AQUARIUM";
            aqTxt.fontSize = 22;
            aqTxt.fontStyle = FontStyles.Bold;
            aqTxt.alignment = TextAlignmentOptions.Center;
            aqTxt.color = new Color(0.4f, 0.9f, 1f);
            aqTxt.raycastTarget = false;
            RectTransform aqTxtRt = aqTxtObj.GetComponent<RectTransform>();
            aqTxtRt.anchorMin = Vector2.zero;
            aqTxtRt.anchorMax = Vector2.one;
            aqTxtRt.sizeDelta = Vector2.zero;
        }

        private static GameObject CreateWorldPurchasePad(
            string padName,
            Vector3 position,
            Transform parent,
            UpgradeType upgradeType,
            string title,
            string subtitle
        )
        {
            // Root has uniform scale (1, 1, 1) to prevent any matrix shear / skewing on billboard canvas
            GameObject padRoot = new GameObject(padName);
            padRoot.transform.SetParent(parent);
            padRoot.transform.position = position;
            padRoot.transform.localScale = Vector3.one;

            // Visual Disc / Toon Stepped Stone Pad Plinth
            GameObject padFloor = null;
            var toonPadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_WorldPurchasePad.prefab");
            if (toonPadPrefab != null)
            {
                padFloor = Object.Instantiate(toonPadPrefab, padRoot.transform);
                padFloor.name = "Visual_PadFloor";
                padFloor.transform.localPosition = Vector3.zero;
                padFloor.transform.localRotation = Quaternion.identity;
                padFloor.transform.localScale = Vector3.one;
            }
            else
            {
                padFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                padFloor.name = "Visual_PadFloor";
                padFloor.transform.SetParent(padRoot.transform, false);
                padFloor.transform.localPosition = Vector3.zero;
                padFloor.transform.localScale = new Vector3(1.6f, 0.05f, 1.6f);
                var floorCol = padFloor.GetComponent<Collider>();
                if (floorCol != null) Object.DestroyImmediate(floorCol);
            }

            var col = padRoot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.radius = 0.9f;
            col.height = 1.2f;
            col.isTrigger = true;

            // Billboard Canvas in WorldSpace with clean uniform scale (0.008f)
            GameObject canvasObj = new GameObject("BillboardCanvas");
            canvasObj.transform.SetParent(padRoot.transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            canvasObj.transform.localScale = Vector3.one * 0.008f;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform cRect = canvasObj.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(160, 90);

            // Background panel
            GameObject bgObj = new GameObject("Pad_BG");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.12f, 0.20f, 0.92f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(180, 95);

            // Title Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.fontSize = 17;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(1f, 0.9f, 0.2f);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 26);
            titleRect.sizeDelta = new Vector2(170, 26);

            // Cost Text
            GameObject costObj = new GameObject("CostText");
            costObj.transform.SetParent(bgObj.transform, false);
            var costTxt = costObj.AddComponent<TextMeshProUGUI>();
            costTxt.fontSize = 22;
            costTxt.fontStyle = FontStyles.Bold;
            costTxt.alignment = TextAlignmentOptions.Center;
            costTxt.color = new Color(0f, 1f, 0.5f);
            RectTransform costRect = costObj.GetComponent<RectTransform>();
            costRect.anchoredPosition = new Vector2(0, -2);
            costRect.sizeDelta = new Vector2(170, 26);

            // Subtitle Text
            GameObject subObj = new GameObject("SubText");
            subObj.transform.SetParent(bgObj.transform, false);
            var subTxt = subObj.AddComponent<TextMeshProUGUI>();
            subTxt.fontSize = 13;
            subTxt.alignment = TextAlignmentOptions.Center;
            subTxt.color = new Color(0.8f, 0.85f, 0.9f);
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0, -28);
            subRect.sizeDelta = new Vector2(170, 20);

            var pad = padRoot.AddComponent<InWorldPurchasePad>();
            pad.Setup(upgradeType, title, subtitle, titleTxt, costTxt, subTxt, padFloor.transform);
            pad.SetBillboardTransform(canvasObj.transform);

            return padRoot;
        }

        private static GameObject CreateTransporterHirePad(Vector3 position, Transform parent)
        {
            GameObject padRoot = new GameObject("Pad_Hire_Transporter");
            padRoot.transform.SetParent(parent);
            padRoot.transform.position = position;

            // Visual Disc / Toon Stepped Stone Pad Plinth
            GameObject padFloor = null;
            var toonPadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_WorldPurchasePad.prefab");
            if (toonPadPrefab != null)
            {
                padFloor = Object.Instantiate(toonPadPrefab, padRoot.transform);
                padFloor.name = "PadFloor";
                padFloor.transform.localPosition = Vector3.zero;
                padFloor.transform.localRotation = Quaternion.identity;
                padFloor.transform.localScale = Vector3.one;
            }
            else
            {
                padFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                padFloor.name = "PadFloor";
                padFloor.transform.SetParent(padRoot.transform, false);
                padFloor.transform.localPosition = Vector3.zero;
                padFloor.transform.localScale = new Vector3(2.0f, 0.05f, 2.0f);
                var c = padFloor.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }

            var col = padRoot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.radius = 1.0f;
            col.height = 1.2f;
            col.isTrigger = true;

            GameObject canvasObj = new GameObject("BillboardCanvas");
            canvasObj.transform.SetParent(padRoot.transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            canvasObj.transform.localScale = Vector3.one * 0.008f;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform cRect = canvasObj.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(200, 100);

            GameObject bgObj = new GameObject("Pad_BG");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(200, 100);

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.fontSize = 16;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(1f, 0.88f, 0.35f);
            titleTxt.text = "[+] HIRE TRANSPORTER";
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 24);
            titleRect.sizeDelta = new Vector2(190, 24);

            GameObject costObj = new GameObject("CostText");
            costObj.transform.SetParent(bgObj.transform, false);
            var costTxt = costObj.AddComponent<TextMeshProUGUI>();
            costTxt.fontSize = 19;
            costTxt.fontStyle = FontStyles.Bold;
            costTxt.alignment = TextAlignmentOptions.Center;
            costTxt.color = new Color(0.2f, 1f, 0.5f);
            costTxt.text = "120 G";
            RectTransform costRect = costObj.GetComponent<RectTransform>();
            costRect.anchoredPosition = new Vector2(0, 0);
            costRect.sizeDelta = new Vector2(190, 24);

            GameObject subObj = new GameObject("SubText");
            subObj.transform.SetParent(bgObj.transform, false);
            var subTxt = subObj.AddComponent<TextMeshProUGUI>();
            subTxt.fontSize = 13;
            subTxt.fontStyle = FontStyles.Normal;
            subTxt.alignment = TextAlignmentOptions.Center;
            subTxt.color = new Color(0.8f, 0.85f, 0.9f);
            subTxt.text = "Auto-deliver to market & ship";
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0, -24);
            subRect.sizeDelta = new Vector2(190, 20);

            var trigger = padRoot.AddComponent<WorldTransporterHirePad>();
            trigger.Setup(titleTxt, costTxt, subTxt, padFloor.transform, canvasObj.transform);

            return padRoot;
        }

        private static GameObject CreateWorldContractBoard(Vector3 position, Transform parent)
        {
            GameObject boardRoot = new GameObject("Board_HarborContracts");
            boardRoot.transform.SetParent(parent);
            boardRoot.transform.position = position;
            boardRoot.transform.localScale = Vector3.one;

            // 1. Stylized Toon Notice Board with Wooden Posts, Shingle Roof & Fish Emblem
            GameObject boardVis = null;
            var boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_ContractBoard.prefab");
            if (boardPrefab != null)
            {
                boardVis = Object.Instantiate(boardPrefab, boardRoot.transform);
                boardVis.name = "Model_ContractBoard";
                boardVis.transform.localPosition = Vector3.zero;
                boardVis.transform.localRotation = Quaternion.identity;
                boardVis.transform.localScale = Vector3.one;
            }
            else
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "WoodenPost";
                post.transform.SetParent(boardRoot.transform, false);
                post.transform.localPosition = new Vector3(0, 0.6f, 0);
                post.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);
                var postCol = post.GetComponent<Collider>();
                if (postCol != null) Object.DestroyImmediate(postCol);

                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = "SignPlank";
                plank.transform.SetParent(boardRoot.transform, false);
                plank.transform.localPosition = new Vector3(0, 1.25f, 0);
                plank.transform.localScale = new Vector3(1.5f, 0.85f, 0.15f);
                var plankCol = plank.GetComponent<Collider>();
                if (plankCol != null) Object.DestroyImmediate(plankCol);
                boardVis = plank;
            }

            // Clickable Box Collider on boardRoot
            var col = boardRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.4f, 0);
            col.size = new Vector3(2.5f, 2.8f, 1.2f);

            // 3. Billboard Canvas in WorldSpace positioned above the roof (y=2.85f)
            GameObject canvasObj = new GameObject("BillboardCanvas");
            canvasObj.transform.SetParent(boardRoot.transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 2.85f, 0f);
            canvasObj.transform.localScale = Vector3.one * 0.008f;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform cRect = canvasObj.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(210, 90);

            GameObject bgObj = new GameObject("Sign_BG");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(210, 90);

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.fontSize = 17;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(1f, 0.88f, 0.35f);
            titleTxt.text = "HARBOR CONTRACTS";
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 16);
            titleRect.sizeDelta = new Vector2(200, 26);

            GameObject subObj = new GameObject("StatusText");
            subObj.transform.SetParent(bgObj.transform, false);
            var subTxt = subObj.AddComponent<TextMeshProUGUI>();
            subTxt.fontSize = 14;
            subTxt.fontStyle = FontStyles.Bold;
            subTxt.alignment = TextAlignmentOptions.Center;
            subTxt.color = new Color(0.2f, 1f, 0.6f);
            subTxt.text = "[TAP TO VIEW ORDERS]";
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0, -16);
            subRect.sizeDelta = new Vector2(200, 24);

            var trigger = boardRoot.AddComponent<WorldContractBoardTrigger>();
            trigger.Setup(canvasObj.transform, titleTxt, subTxt, boardVis != null ? boardVis.transform : boardRoot.transform);

            return boardRoot;
        }

        private static FishingSlot CreateFishermanSlot(
            Transform pierRoot,
            FishingSpot spot,
            Transform landingZone,
            int slotIndex,
            string slotTitle,
            Vector3 position,
            bool isFreeDefault
        )
        {
            GameObject slotRoot = new GameObject($"FishingSlot_{slotIndex + 1}");
            slotRoot.transform.SetParent(pierRoot);
            slotRoot.transform.position = position;

            // Stool
            GameObject stoolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_FishingPier_Stool.prefab");
            GameObject stool = new GameObject($"Fisherman_Stool_{slotIndex + 1}");
            stool.transform.SetParent(slotRoot.transform);
            stool.transform.position = position + new Vector3(0f, -0.5f, 0f);
            if (stoolPrefab != null)
            {
                var sVis = Object.Instantiate(stoolPrefab, stool.transform);
                sVis.name = "Model_Toon_FishingPier_Stool";
                sVis.transform.localPosition = Vector3.zero;
            }
            var stoolCol = stool.AddComponent<BoxCollider>();
            stoolCol.size = new Vector3(0.8f, 0.4f, 0.8f);
            stoolCol.center = new Vector3(0f, 0.2f, 0f);
            stoolCol.isTrigger = true;

            // Fisherman Character
            GameObject fishermanObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fishermanObj.name = $"Worker_Fisherman_{slotIndex + 1}";
            fishermanObj.transform.SetParent(slotRoot.transform);
            fishermanObj.transform.position = position;
            fishermanObj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            var fCol = fishermanObj.GetComponent<CapsuleCollider>();
            if (fCol != null) fCol.isTrigger = true;

            // Visual Model
            GameObject fishermanModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Fisherman.prefab");
            GameObject rodObj = null;
            GameObject rodTip = null;

            if (fishermanModelPrefab != null)
            {
                var vis = Object.Instantiate(fishermanModelPrefab, fishermanObj.transform);
                vis.name = "Model_Fisherman";
                vis.transform.localPosition = new Vector3(0f, -0.82f, 0.06f);
                vis.transform.localRotation = Quaternion.identity;
                vis.transform.localScale = Vector3.one;
                var fMr = fishermanObj.GetComponent<MeshRenderer>();
                if (fMr != null) Object.DestroyImmediate(fMr);
                var fMf = fishermanObj.GetComponent<MeshFilter>();
                if (fMf != null) Object.DestroyImmediate(fMf);

                // Find FishingRod and RodTip inside Stickman prefab
                foreach (var t in vis.GetComponentsInChildren<Transform>())
                {
                    if (t.name == "FishingRod") rodObj = t.gameObject;
                    if (t.name == "RodTip") rodTip = t.gameObject;
                }
            }

            // Fallback Fishing Rod if not in model
            if (rodObj == null)
            {
                rodObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rodObj.name = "FishingRod";
                rodObj.transform.SetParent(fishermanObj.transform);
                rodObj.transform.localPosition = new Vector3(0.4f, 0.2f, 0.8f);
                rodObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                rodObj.transform.localScale = new Vector3(0.08f, 1.2f, 0.08f);

                rodTip = new GameObject("RodTip");
                rodTip.transform.SetParent(rodObj.transform);
                rodTip.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            }

            // Fisherman Component
            var fishermanWorker = fishermanObj.AddComponent<FishermanWorker>();
            var fCrowd = fishermanObj.AddComponent<CrowdAvoidance>();
            fCrowd.Setup(radius: 0.5f, priority: 5.0f, stationary: true);

            var lineVisual = fishermanObj.AddComponent<FishingLineVisual>();
            lineVisual.Setup(rodObj.transform, rodTip.transform, spot != null ? spot.transform.position : fishermanObj.transform.position + fishermanObj.transform.forward * 4.5f);

            var fSo = new SerializedObject(fishermanWorker);
            fSo.FindProperty("targetSpot").objectReferenceValue = spot;
            fSo.FindProperty("landingZoneCenter").objectReferenceValue = landingZone;
            fSo.FindProperty("fishingRodTip").objectReferenceValue = rodTip.transform;
            fSo.FindProperty("fishingRodTransform").objectReferenceValue = rodObj.transform;
            fSo.FindProperty("lineVisual").objectReferenceValue = lineVisual;
            fSo.FindProperty("baseCastTime").floatValue = 6.0f;
            fSo.FindProperty("maxPendingFishOnGround").intValue = 3;
            fSo.ApplyModifiedProperties();

            // Unlock Pad (if not free default)
            GameObject padObj = null;
            TextMeshProUGUI padTitleTxt = null;
            TextMeshProUGUI padCostTxt = null;

            if (!isFreeDefault)
            {
                padObj = CreateWorldPurchasePad(
                    $"Pad_Unlock_Slot_{slotIndex + 1}",
                    new Vector3(position.x, 0.62f, position.z),
                    slotRoot.transform,
                    UpgradeType.FishermanSlots,
                    $"[+] UNLOCK {slotTitle.ToUpper()}",
                    "Tap to unlock slot"
                );
                var texts = padObj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) padTitleTxt = texts[0];
                if (texts.Length > 1) padCostTxt = texts[1];
            }

            var slot = slotRoot.AddComponent<FishingSlot>();
            slot.Setup(slotIndex, slotTitle, fishermanObj, stool, padObj, padTitleTxt, padCostTxt);

            return slot;
        }

        private static FishWranglerWorker CreateWranglerWorker(
            string name,
            Vector3 position,
            Transform parent,
            Inventory.Inventory intakeStorage,
            Transform restWaypoint
        )
        {
            GameObject wranglerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            wranglerObj.name = name;
            wranglerObj.transform.SetParent(parent);
            wranglerObj.transform.position = position;
            wranglerObj.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);
            var wCol = wranglerObj.GetComponent<CapsuleCollider>();
            if (wCol != null) wCol.isTrigger = true;

            // Visual Model
            GameObject wranglerModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Stickman_Wrangler.prefab");
            GameObject weaponObj = null;
            GameObject carryPoint = null;

            if (wranglerModelPrefab != null)
            {
                var vis = Object.Instantiate(wranglerModelPrefab, wranglerObj.transform);
                vis.name = "Model_Wrangler";
                vis.transform.localPosition = new Vector3(0, -0.9f, 0);
                vis.transform.localRotation = Quaternion.identity;
                vis.transform.localScale = Vector3.one;
                var wMr = wranglerObj.GetComponent<MeshRenderer>();
                if (wMr != null) Object.DestroyImmediate(wMr);
                var wMf = wranglerObj.GetComponent<MeshFilter>();
                if (wMf != null) Object.DestroyImmediate(wMf);

                // Find bat & carry point inside stickman prefab
                foreach (var t in vis.GetComponentsInChildren<Transform>())
                {
                    if (t.name == "Wrangler_Bat") weaponObj = t.gameObject;
                    if (t.name == "CarryPoint_Back") carryPoint = t.gameObject;
                }
            }

            // Fallback Weapon
            if (weaponObj == null)
            {
                weaponObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                weaponObj.name = "Wrangler_Bat";
                weaponObj.transform.SetParent(wranglerObj.transform);
                weaponObj.transform.localPosition = new Vector3(0.5f, 0.1f, 0.5f);
                weaponObj.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
                weaponObj.transform.localScale = new Vector3(0.12f, 0.6f, 0.12f);
            }

            // Fallback Carry Point
            if (carryPoint == null)
            {
                carryPoint = new GameObject("CarryPoint_Back");
                carryPoint.transform.SetParent(wranglerObj.transform);
                carryPoint.transform.localPosition = new Vector3(0f, 0.4f, -0.5f);
            }

            var worker = wranglerObj.AddComponent<FishWranglerWorker>();
            var wSo = new SerializedObject(worker);
            wSo.FindProperty("intakeStorage").objectReferenceValue = intakeStorage;
            wSo.FindProperty("carryPoint").objectReferenceValue = carryPoint.transform;
            wSo.FindProperty("restWaypoint").objectReferenceValue = restWaypoint;
            wSo.FindProperty("weaponTransform").objectReferenceValue = weaponObj.transform;
            wSo.FindProperty("baseMoveSpeed").floatValue = 4.0f;
            wSo.FindProperty("baseAttackDamage").intValue = 16;
            wSo.FindProperty("attackInterval").floatValue = 0.6f;
            wSo.FindProperty("maxHealth").intValue = 100;
            wSo.FindProperty("knockoutDuration").floatValue = 4.0f;
            wSo.ApplyModifiedProperties();

            return worker;
        }

        private static GameObject CreateLighthouse(Transform parent)
        {
            GameObject lhRoot = new GameObject("--- LIGHTHOUSE (BEACON) ---");
            lhRoot.transform.SetParent(parent);
            lhRoot.transform.position = new Vector3(-10.5f, 0.4f, 13.5f);

            GameObject lhPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Lighthouse_Beacon.prefab");
            GameObject lhVis = null;
            if (lhPrefab != null)
            {
                lhVis = Object.Instantiate(lhPrefab, lhRoot.transform);
                lhVis.name = "Model_Lighthouse";
                lhVis.transform.localPosition = Vector3.zero;
                lhVis.transform.localRotation = Quaternion.identity;
            }

            // Spotlight Beam sweeping the ocean
            GameObject beaconHead = new GameObject("Beacon_Head");
            beaconHead.transform.SetParent(lhRoot.transform, false);
            beaconHead.transform.localPosition = new Vector3(0f, 7.95f, 0f);

            GameObject lightObj = new GameObject("Beacon_Light");
            lightObj.transform.SetParent(beaconHead.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            lightObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            var sLight = lightObj.AddComponent<Light>();
            sLight.type = LightType.Spot;
            sLight.spotAngle = 48f;
            sLight.innerSpotAngle = 20f;
            sLight.range = 28f;
            sLight.color = new Color(1f, 0.92f, 0.55f);
            sLight.intensity = 2.4f;

            // In-World Upgrade Pad in front
            GameObject lhPadObj = CreateWorldPurchasePad(
                "Pad_Lighthouse_Upgrade",
                new Vector3(-8.2f, 0.52f, 11.8f),
                lhRoot.transform,
                UpgradeType.FishermanSpeed,
                "[+] UPGRADE LIGHTHOUSE",
                "Boost pier fishing speed!"
            );
            var padScript = lhPadObj.GetComponent<InWorldPurchasePad>();

            var lhCtrl = lhRoot.AddComponent<LighthouseController>();
            lhCtrl.Setup(beaconHead.transform, sLight, lhVis != null ? lhVis.GetComponentInChildren<Renderer>() : null, padScript);

            return lhRoot;
        }

        private static GameObject CreateSeasideCafe(Transform parent)
        {
            GameObject cafeRoot = new GameObject("--- SEASIDE CAFE (JUICE BAR) ---");
            cafeRoot.transform.SetParent(parent);
            cafeRoot.transform.position = new Vector3(8.5f, 0.4f, -3.5f);

            GameObject cafePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Seaside_Cafe.prefab");
            if (cafePrefab != null)
            {
                var cafeVis = Object.Instantiate(cafePrefab, cafeRoot.transform);
                cafeVis.name = "Model_SeasideCafe";
                cafeVis.transform.localPosition = Vector3.zero;
                cafeVis.transform.localRotation = Quaternion.identity;
            }

            var seats = new System.Collections.Generic.List<Transform>();
            var s1_1 = cafeRoot.transform.Find("Model_SeasideCafe/Stool_1_1");
            var s1_2 = cafeRoot.transform.Find("Model_SeasideCafe/Stool_1_2");
            var s2_1 = cafeRoot.transform.Find("Model_SeasideCafe/Stool_2_1");
            var s2_2 = cafeRoot.transform.Find("Model_SeasideCafe/Stool_2_2");
            if (s1_1 != null) seats.Add(s1_1);
            if (s1_2 != null) seats.Add(s1_2);
            if (s2_1 != null) seats.Add(s2_1);
            if (s2_2 != null) seats.Add(s2_2);

            if (seats.Count == 0)
            {
                GameObject seat1 = new GameObject("Seat_1");
                seat1.transform.SetParent(cafeRoot.transform, false);
                seat1.transform.localPosition = new Vector3(-2.4f, 0.2f, -1.35f);
                seats.Add(seat1.transform);

                GameObject seat2 = new GameObject("Seat_2");
                seat2.transform.SetParent(cafeRoot.transform, false);
                seat2.transform.localPosition = new Vector3(2.4f, 0.2f, -1.35f);
                seats.Add(seat2.transform);
            }

            var cafeCtrl = cafeRoot.AddComponent<SeasideCafeController>();
            cafeCtrl.SetupSeats(seats);

            return cafeRoot;
        }

        private static GameObject CreateCargoShip(
            Transform parent,
            Inventory.Inventory harborStorage)
        {
            GameObject shipRoot = new GameObject("--- CARGO TRAWLER SHIP ---");
            shipRoot.transform.SetParent(parent);
            shipRoot.transform.position = new Vector3(7.5f, 0.1f, 16.5f);
            shipRoot.transform.rotation = Quaternion.Euler(0, -35f, 0);

            GameObject shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Cargo_Trawler_Ship.prefab");
            GameObject shipVis = null;
            if (shipPrefab != null)
            {
                shipVis = Object.Instantiate(shipPrefab, shipRoot.transform);
                shipVis.name = "Model_TrawlerShip";
                shipVis.transform.localPosition = Vector3.zero;
                shipVis.transform.localRotation = Quaternion.identity;
            }

            var col = shipRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(2.4f, 2.6f, 5.0f);
            col.center = new Vector3(0f, 1.3f, 0f);

            // 5. Gangplank (Cầu tàu gỗ kết nối bến cảng ra boong tàu)
            GameObject gangplank = null;
            var gangplankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Dock_Gangplank.prefab");
            if (gangplankPrefab != null)
            {
                gangplank = Object.Instantiate(gangplankPrefab, parent, false);
                gangplank.name = "Ship_Gangplank";
                gangplank.transform.position = new Vector3(6.35f, 0.55f, 13.8f);
                gangplank.transform.rotation = Quaternion.Euler(-6f, -35f, 0f);
                gangplank.transform.localScale = Vector3.one;
            }
            else
            {
                gangplank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gangplank.name = "Ship_Gangplank";
                gangplank.transform.SetParent(parent, false);
                gangplank.transform.position = new Vector3(6.35f, 0.55f, 13.8f);
                gangplank.transform.rotation = Quaternion.Euler(-6f, -35f, 0f);
                gangplank.transform.localScale = new Vector3(0.9f, 0.12f, 2.8f);
                RemoveCollider(gangplank);
            }

            // 6. Dock Export Loading Pallet (Khu vực bốc xếp hàng xuất khẩu)
            GameObject palletRoot = new GameObject("Dock_Export_Pallet");
            palletRoot.transform.SetParent(parent, false);
            palletRoot.transform.position = new Vector3(5.0f, 0.45f, 11.2f);

            var palletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Dock_ExportPallet.prefab");
            if (palletPrefab != null)
            {
                var palletVisModel = Object.Instantiate(palletPrefab, palletRoot.transform);
                palletVisModel.name = "Model_Dock_ExportPallet";
                palletVisModel.transform.localPosition = Vector3.zero;
                palletVisModel.transform.localRotation = Quaternion.identity;
                palletVisModel.transform.localScale = Vector3.one;
            }
            else
            {
                GameObject palletBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                palletBase.name = "Pallet_Base";
                palletBase.transform.SetParent(palletRoot.transform, false);
                palletBase.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                palletBase.transform.localScale = new Vector3(1.6f, 0.16f, 1.4f);
                RemoveCollider(palletBase);
            }

            var palletInv = palletRoot.AddComponent<Inventory.Inventory>();
            palletInv.SetMaxCapacity(8);

            var palletVis = palletRoot.AddComponent<WorldItemStackVisualizer>();
            palletVis.ConfigureLayout(new Vector3(0f, 0.22f, 0f), 2, 2, 0.32f, 0.32f, Vector3.one * 0.58f);

            // 7. CargoShipExportManager (Hệ thống Hợp Đồng Xuất Khẩu Viễn Dương)
            var exportMgr = shipRoot.AddComponent<CargoShipExportManager>();
            exportMgr.Setup(palletInv, harborStorage, shipVis != null ? shipVis.transform : shipRoot.transform);

            return shipRoot;
        }

        private static GameObject CreateHarborAtmosphereDecor(Transform parent)
        {
            GameObject decorRoot = new GameObject("--- HARBOR ATMOSPHERE & DECOR ---");
            decorRoot.transform.SetParent(parent);

            // 1. Street Lamps
            Vector3[] lampPositions = new Vector3[] {
                new Vector3(-5.8f, 0.4f, 5.2f),
                new Vector3(5.8f, 0.4f, 5.2f),
                new Vector3(-5.8f, 0.4f, -4.5f),
                new Vector3(5.8f, 0.4f, -4.5f)
            };
            Quaternion[] lampRotations = new Quaternion[] {
                Quaternion.Euler(0f, 90f, 0f),
                Quaternion.Euler(0f, -90f, 0f),
                Quaternion.Euler(0f, 90f, 0f),
                Quaternion.Euler(0f, -90f, 0f)
            };

            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Harbor_StreetLamp.prefab");
            for (int i = 0; i < lampPositions.Length; i++)
            {
                CreateStreetLamp(decorRoot.transform, lampPositions[i], lampRotations[i], $"Lamp_{i + 1}", lampPrefab);
            }

            // 2. Mooring Bollards
            Vector3[] bollardPositions = new Vector3[] {
                new Vector3(-5.8f, 0.5f, 10.8f),
                new Vector3(5.8f, 0.5f, 10.8f),
                new Vector3(-5.8f, 0.5f, 7.5f),
                new Vector3(5.8f, 0.5f, 7.5f)
            };
            var bollardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Mooring_Bollard.prefab");
            for (int i = 0; i < bollardPositions.Length; i++)
            {
                if (bollardPrefab != null)
                {
                    var bollard = Object.Instantiate(bollardPrefab, decorRoot.transform);
                    bollard.name = $"Bollard_{i + 1}";
                    bollard.transform.position = bollardPositions[i];
                    bollard.transform.rotation = Quaternion.identity;
                    bollard.transform.localScale = Vector3.one;
                }
                else
                {
                    GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bollard.name = $"Bollard_{i + 1}";
                    bollard.transform.SetParent(decorRoot.transform);
                    bollard.transform.position = bollardPositions[i];
                    bollard.transform.localScale = new Vector3(0.24f, 0.25f, 0.24f);
                    SetObjectColor(bollard, new Color(0.22f, 0.24f, 0.26f));
                }
            }

            // 3. Lifebuoys
            var buoyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Toon_Lifebuoy.prefab");
            CreateLifebuoy(decorRoot.transform, new Vector3(-6.02f, 0.6f, 9.5f), Quaternion.Euler(0, 90f, 0), buoyPrefab);
            CreateLifebuoy(decorRoot.transform, new Vector3(6.02f, 0.6f, 9.5f), Quaternion.Euler(0, -90f, 0), buoyPrefab);

            // 4. Ambient Seagulls
            decorRoot.AddComponent<HarborAtmosphereDecor>();

            return decorRoot;
        }

        private static void CreateStreetLamp(Transform parent, Vector3 pos, Quaternion rot, string name, GameObject lampPrefab)
        {
            if (lampPrefab != null)
            {
                var lamp = Object.Instantiate(lampPrefab, parent);
                lamp.name = name;
                lamp.transform.position = pos;
                lamp.transform.rotation = rot;
                lamp.transform.localScale = Vector3.one;
            }
            else
            {
                GameObject lamp = new GameObject(name);
                lamp.transform.SetParent(parent);
                lamp.transform.position = pos;

                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Pole";
                pole.transform.SetParent(lamp.transform, false);
                pole.transform.localPosition = new Vector3(0, 1.2f, 0);
                pole.transform.localScale = new Vector3(0.12f, 1.2f, 0.12f);
                SetObjectColor(pole, new Color(0.18f, 0.20f, 0.22f));

                GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lantern.name = "Lantern";
                lantern.transform.SetParent(lamp.transform, false);
                lantern.transform.localPosition = new Vector3(0, 2.5f, 0);
                lantern.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
                SetObjectColor(lantern, new Color(1.0f, 0.90f, 0.60f));

                GameObject lightObj = new GameObject("Light");
                lightObj.transform.SetParent(lamp.transform, false);
                lightObj.transform.localPosition = new Vector3(0, 2.4f, 0);
                var l = lightObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.88f, 0.65f);
                l.range = 5.5f;
                l.intensity = 1.2f;
            }
        }

        private static void CreateLifebuoy(Transform parent, Vector3 pos, Quaternion rot, GameObject buoyPrefab)
        {
            if (buoyPrefab != null)
            {
                var buoy = Object.Instantiate(buoyPrefab, parent);
                buoy.name = "Lifebuoy";
                buoy.transform.position = pos;
                buoy.transform.rotation = rot;
                buoy.transform.localScale = Vector3.one;
            }
            else
            {
                GameObject buoy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                buoy.name = "Lifebuoy";
                buoy.transform.SetParent(parent);
                buoy.transform.position = pos;
                buoy.transform.rotation = rot;
                buoy.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
                SetObjectColor(buoy, new Color(0.95f, 0.25f, 0.20f));
            }
        }

        private static void SetObjectColor(GameObject obj, Color color)
        {
            var mr = obj.GetComponent<Renderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                mr.sharedMaterial = mat;
            }
        }

        private static void RemoveCollider(GameObject obj)
        {
            var c = obj.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
    }
}
#endif

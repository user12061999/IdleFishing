using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using IdleFishing.Data;
using IdleFishing.Progression;

namespace IdleFishing.Editor
{
    public static class GenerateAllStages
    {
        [MenuItem("Window/Idle Fishing/Batch Generate Stages 13-30")]
        public static void Generate()
        {
            EnsureDirectories();

            // Fish Chain references
            var salmonFish = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/salmon_chain.asset");
            var thresherShark = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/thresher_shark_chain.asset");
            var arcticTuna = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/arctic_tuna_chain.asset");
            var snowCrab = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/snow_crab_chain.asset");
            var greatWhiteShark = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/great_white_shark_chain.asset");
            var yellowfinTuna = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/yellowfin_tuna_chain.asset");

            // ================= WORLD 3 (Stages 13 - 18) =================
            CreateStage("Assets/GameData/Stages/World_3/Stage_13.asset", "stage_13", 13, "world_3", 1, "Golden Autumn Arrival",
                "Arrive at Golden Autumn Cove! Catch salmon and introduce autumn delicacies to the harbor.",
                3.0f, 48000, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.CatchFish, targetCount = 8, customDescription = "Catch 8 Autumn Salmon" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 8, customDescription = "Sell 8 Fish at Market" }
                },
                guaranteedFirst: salmonFish, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "autumn_cove", "salmon_fishery" });

            CreateStage("Assets/GameData/Stages/World_3/Stage_14.asset", "stage_14", 14, "world_3", 2, "Smokehouse Ignition",
                "Cove ovens are lit! Upgrade fish value and brawl with hefty autumn catches.",
                3.5f, 65760, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.DefeatFish, targetCount = 8, customDescription = "Defeat 8 Fish on Pier" },
                    new StageGoalConfig { goalType = StageGoalType.UpgradeReached, targetId = "FishValue", targetLevel = 3, customDescription = "Upgrade Fish Value to Level 3" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "smoker_grill" });

            CreateStage("Assets/GameData/Stages/World_3/Stage_15.asset", "stage_15", 15, "world_3", 3, "Processing Line",
                "High fish density requires seamless cart transport. Stock shelves with processed catches.",
                4.0f, 90091, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.TransportPayload, targetCount = 12, customDescription = "Transport 12 Fish in Cart" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 15, customDescription = "Sell 15 Fish at Market" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "recipe_queue", "auto_route" });

            CreateStage("Assets/GameData/Stages/World_3/Stage_16.asset", "stage_16", 16, "world_3", 4, "VIP Autumn Feast",
                "Wealthy merchants arrive seeking premium catches. Upgrade wranglers to handle tougher fish.",
                4.0f, 123425, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 18, customDescription = "Sell 18 Fish at Market" },
                    new StageGoalConfig { goalType = StageGoalType.UpgradeReached, targetId = "WranglerTeam", targetLevel = 3, customDescription = "Upgrade Wrangler Team to Level 3" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "vip_priority_orders" });

            CreateStage("Assets/GameData/Stages/World_3/Stage_17.asset", "stage_17", 17, "world_3", 5, "Foliage Frenzy",
                "Scarlet leaves drift as feeding frenzy strikes the cove. Maximize high-speed harvesting.",
                5.0f, 169092, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.ActivateFrenzy, targetCount = 1, customDescription = "Activate Fishing Frenzy 1x" },
                    new StageGoalConfig { goalType = StageGoalType.DefeatFish, targetCount = 12, customDescription = "Defeat 12 Fish on Pier" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "stun_hammer" });

            CreateStage("Assets/GameData/Stages/World_3/Stage_18.asset", "stage_18", 18, "world_3", 6, "Smoker Kraken Ascent",
                "World 3 Apex Boss: Thresher Shark Kraken! Slash through turbulent waters and unlock World 4 voyage.",
                6.0f, 231656, 1.25f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.DefeatBoss, targetCount = 1, targetId = "thresher_shark", customDescription = "Defeat Thresher Shark Kraken" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 25, customDescription = "Sell 25 Fish at Market" }
                },
                guaranteedFirst: null, boss: thresherShark, isWorldBoss: true, voyageUnlock: "world_4",
                unlocks: new List<string> { "world_4_voyage", "kraken_trophy" });

            // ================= WORLD 4 (Stages 19 - 24) =================
            CreateStage("Assets/GameData/Stages/World_4/Stage_19.asset", "stage_19", 19, "world_4", 1, "Subzero Shore",
                "Arrive at Arctic Frost Haven! Brave subzero waters and haul giant arctic tuna.",
                3.5f, 987399, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.CatchFish, targetCount = 10, customDescription = "Catch 10 Arctic Fish" },
                    new StageGoalConfig { goalType = StageGoalType.DefeatFish, targetCount = 6, customDescription = "Defeat 6 Fish on Pier" }
                },
                guaranteedFirst: arcticTuna, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "arctic_haven", "polar_fishing" });

            CreateStage("Assets/GameData/Stages/World_4/Stage_20.asset", "stage_20", 20, "world_4", 2, "Ice Drill Operation",
                "Drill into deep glacial currents. Upgrade fisherman gear to endure polar storms.",
                4.0f, 1352737, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.DefeatFish, targetCount = 10, customDescription = "Defeat 10 Fish on Pier" },
                    new StageGoalConfig { goalType = StageGoalType.UpgradeReached, targetId = "FishermanTeam", targetLevel = 4, customDescription = "Upgrade Fisherman Team to Level 4" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "thermal_drill" });

            CreateStage("Assets/GameData/Stages/World_4/Stage_21.asset", "stage_21", 21, "world_4", 3, "Snow Crab Brawl",
                "Armored Snow Crabs leap onto the deck with hardened ice shells. Shatter armor with wranglers.",
                4.5f, 1853249, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.DefeatFish, targetCount = 4, targetId = "snow_crab", customDescription = "Defeat 4 Snow Crabs" },
                    new StageGoalConfig { goalType = StageGoalType.CatchFish, targetCount = 12, customDescription = "Catch 12 Fish" }
                },
                guaranteedFirst: snowCrab, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "ice_break_visual" });

            CreateStage("Assets/GameData/Stages/World_4/Stage_22.asset", "stage_22", 22, "world_4", 4, "Blizzard Haul",
                "Polar blizzards threaten transport routes. Upgrade logistics to keep shelves stocked with frost catches.",
                4.5f, 2468499, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.TransportPayload, targetCount = 15, customDescription = "Transport 15 Fish in Cart" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 20, customDescription = "Sell 20 Fish at Market" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "warehouse_status" });

            CreateStage("Assets/GameData/Stages/World_4/Stage_23.asset", "stage_23", 23, "world_4", 5, "Glacier Treasure",
                "Icebergs thaw during polar daylight, releasing crystal fish. Trigger frenzy to harvest fortune.",
                5.5f, 3381843, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.ActivateFrenzy, targetCount = 1, customDescription = "Activate Fishing Frenzy 1x" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 25, customDescription = "Sell 25 Fish at Market" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "iceberg_trophy" });

            CreateStage("Assets/GameData/Stages/World_4/Stage_24.asset", "stage_24", 24, "world_4", 6, "Glacier Behemoth",
                "World 4 Apex Boss: Glacier Behemoth! Smash icy weakpoints and unlock the voyage to the Desert Oasis.",
                7.0f, 4633126, 1.25f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.DefeatBoss, targetCount = 1, targetId = "great_white_shark", customDescription = "Defeat Glacier Behemoth" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 30, customDescription = "Sell 30 Fish at Market" }
                },
                guaranteedFirst: null, boss: greatWhiteShark, isWorldBoss: true, voyageUnlock: "world_5",
                unlocks: new List<string> { "world_5_voyage", "narwhal_trophy" });

            // ================= WORLD 5 (Stages 25 - 30) =================
            CreateStage("Assets/GameData/Stages/World_5/Stage_25.asset", "stage_25", 25, "world_5", 1, "Sands of the Sea",
                "Arrive at Sunken Oasis Desert! Sacred turquoise waters amidst sunlit ruins harbor mythic fish.",
                4.0f, 19200000, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.CatchFish, targetCount = 12, customDescription = "Catch 12 Oasis Fish" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 15, customDescription = "Sell 15 Fish at Market" }
                },
                guaranteedFirst: yellowfinTuna, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "oasis_haven", "desert_fishery" });

            CreateStage("Assets/GameData/Stages/World_5/Stage_26.asset", "stage_26", 26, "world_5", 2, "Caravan Route",
                "Desert caravans arrive across sand dunes to purchase golden catches. Upgrade wranglers for swift logistics.",
                4.5f, 26304000, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.TransportPayload, targetCount = 18, customDescription = "Transport 18 Fish in Cart" },
                    new StageGoalConfig { goalType = StageGoalType.UpgradeReached, targetId = "WranglerTeam", targetLevel = 4, customDescription = "Upgrade Wrangler Team to Level 4" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "caravan_route" });

            CreateStage("Assets/GameData/Stages/World_5/Stage_27.asset", "stage_27", 27, "world_5", 3, "Oasis Mirage",
                "Mirage waters shimmer with elusive fish. Maintain rapid pier brawling and high-volume market sales.",
                5.0f, 36036480, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.DefeatFish, targetCount = 15, customDescription = "Defeat 15 Fish on Pier" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 25, customDescription = "Sell 25 Fish at Market" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "speed_relic" });

            CreateStage("Assets/GameData/Stages/World_5/Stage_28.asset", "stage_28", 28, "world_5", 4, "Sultan's Feast",
                "The Sultan orders a grand royal feast of sacred oasis catches. Upgrade fish value to harvest untold riches.",
                5.0f, 49369978, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 30, customDescription = "Sell 30 Fish at Market" },
                    new StageGoalConfig { goalType = StageGoalType.UpgradeReached, targetId = "FishValue", targetLevel = 5, customDescription = "Upgrade Fish Value to Level 5" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "price_relic" });

            CreateStage("Assets/GameData/Stages/World_5/Stage_29.asset", "stage_29", 29, "world_5", 5, "Sandstorm Gold",
                "Golden sandstorms surge across the oasis, supercharging fish frenzy. Reap colossal tycoon wealth.",
                6.0f, 67636869, 0.35f, 1,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.ActivateFrenzy, targetCount = 1, customDescription = "Activate Fishing Frenzy 1x" },
                    new StageGoalConfig { goalType = StageGoalType.TransportPayload, targetCount = 20, customDescription = "Transport 20 Fish in Cart" }
                },
                guaranteedFirst: null, boss: null, isWorldBoss: false, voyageUnlock: "",
                unlocks: new List<string> { "genie_lamp" });

            CreateStage("Assets/GameData/Stages/World_5/Stage_30.asset", "stage_30", 30, "world_5", 6, "Golden Leviathan Finale",
                "Final Grand Boss of Idle Fishing: The Mythic Golden Leviathan! Conquer the ultimate apex beast and claim the Master Tycoon Trophy!",
                8.0f, 92662511, 2.50f, 5,
                new List<StageGoalConfig> {
                    new StageGoalConfig { goalType = StageGoalType.DefeatBoss, targetCount = 1, targetId = "great_white_shark", customDescription = "Defeat Golden Leviathan" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 40, customDescription = "Sell 40 Fish at Market" }
                },
                guaranteedFirst: null, boss: greatWhiteShark, isWorldBoss: true, voyageUnlock: "grand_master",
                unlocks: new List<string> { "leviathan_trophy", "grand_master_tycoon" });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Auto-register into Scene
            StageProgressionDashboardWindow.RegisterAllStagesInScene(null);
            Debug.Log("[GenerateAllStages] All 30 Stages generated and registered into StageProgressionManager successfully!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                "Assets/GameData/Stages/World_3",
                "Assets/GameData/Stages/World_4",
                "Assets/GameData/Stages/World_5"
            };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
        }

        private static void CreateStage(string path, string stageId, int stageNum, string worldId, int stageInWorld,
            string title, string desc, float targetMins, int baseGoldPerMin, float rewardGoldEquiv, int fame,
            List<StageGoalConfig> goals, FishChainData guaranteedFirst, FishChainData boss, bool isWorldBoss,
            string voyageUnlock, List<string> unlocks)
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (stage == null)
            {
                stage = ScriptableObject.CreateInstance<StageData>();
                AssetDatabase.CreateAsset(stage, path);
            }

            stage.stageId = stageId;
            stage.stageNumber = stageNum;
            stage.worldId = worldId;
            stage.stageInWorld = stageInWorld;
            stage.stageTitle = title;
            stage.mechanicDescription = desc;
            stage.targetMinutes = targetMins;
            stage.estimatedBaseGoldPerMin = baseGoldPerMin;
            stage.rewardGoldEquivalentMinutes = rewardGoldEquiv;
            stage.grantFameOnFirstClear = fame;
            stage.replayable = false;
            stage.goals = goals;
            stage.guaranteedFirstCatch = guaranteedFirst;
            stage.bossToSpawn = boss;
            stage.isWorldBossStage = isWorldBoss;
            stage.worldVoyageUnlock = voyageUnlock;
            stage.featureUnlocks = unlocks;

            EditorUtility.SetDirty(stage);
        }
    }
}

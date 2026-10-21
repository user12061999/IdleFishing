#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using IdleFishing.Data;
using IdleFishing.Progression;

namespace IdleFishing.EditorTools
{
    public static class StageAssetGenerator
    {
        [MenuItem("IdleFishing/Progression/Generate World 1 Stages")]
        public static void GenerateWorld1Stages()
        {
            string folderPath = "Assets/GameData/Stages/World_1";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            // Load species
            var herring = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/herring_chain.asset");
            var cod = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/cod_chain.asset");
            var reefShark = AssetDatabase.LoadAssetAtPath<FishChainData>("Assets/GameData/FishChains/reef_shark_chain.asset");

            // Stage 01
            CreateOrUpdateStage(
                $"{folderPath}/Stage_01.asset",
                "stage_01", 1, "world_1", 1, "First Catch",
                "Tutorial fishing -> fish hops onto pier -> tap/wrangler whack -> sell for gold.",
                1.0f, 120, 0.35f, 1, false, "",
                herring, null,
                new StageGoalConfig[]
                {
                    new StageGoalConfig { goalType = StageGoalType.CatchFish, targetCount = 1, targetId = "herring", customDescription = "Catch 1 Herring" },
                    new StageGoalConfig { goalType = StageGoalType.DefeatFish, targetCount = 1, targetId = "", customDescription = "Defeat 1 Fish on Pier" },
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 1, targetId = "", customDescription = "Sell 1 Fish" }
                },
                new string[] { "core_fishing", "tutorial_auto_wrangler" }
            );

            // Stage 02
            CreateOrUpdateStage(
                $"{folderPath}/Stage_02.asset",
                "stage_02", 2, "world_1", 2, "Pier Commotion",
                "Fish hops aggressively; introduces automatic brawling and hiring wranglers.",
                1.75f, 164, 0.35f, 1, false, "",
                cod, null,
                new StageGoalConfig[]
                {
                    new StageGoalConfig { goalType = StageGoalType.DefeatFish, targetCount = 4, targetId = "", customDescription = "Defeat 4 Fish" },
                    new StageGoalConfig { goalType = StageGoalType.HireWorker, targetCount = 1, targetId = "wrangler", customDescription = "Hire 1 Fish Wrangler" }
                },
                new string[] { "wrangler_hiring" }
            );

            // Stage 03
            CreateOrUpdateStage(
                $"{folderPath}/Stage_03.asset",
                "stage_03", 3, "world_1", 3, "First Fish Cart",
                "Intake crates reach capacity; unlock transporter cart to haul fish to market.",
                2.5f, 225, 0.35f, 1, false, "",
                null, null,
                new StageGoalConfig[]
                {
                    new StageGoalConfig { goalType = StageGoalType.HireWorker, targetCount = 1, targetId = "transporter", customDescription = "Hire 1 Transporter" },
                    new StageGoalConfig { goalType = StageGoalType.TransportPayload, targetCount = 6, targetId = "", customDescription = "Transport 6 Fish in Cart" }
                },
                new string[] { "logistics", "market_shelf", "bestiary" }
            );

            // Stage 04
            CreateOrUpdateStage(
                $"{folderPath}/Stage_04.asset",
                "stage_04", 4, "world_1", 4, "Fish Market Fair",
                "Casual shoppers arrive; introduce harbor commercial contracts and market trends.",
                3.0f, 309, 0.35f, 1, false, "",
                null, null,
                new StageGoalConfig[]
                {
                    new StageGoalConfig { goalType = StageGoalType.SellItem, targetCount = 8, targetId = "", customDescription = "Sell 8 Fish at Market" },
                    new StageGoalConfig { goalType = StageGoalType.CompleteOrder, targetCount = 1, targetId = "", customDescription = "Complete 1 Harbor Contract" }
                },
                new string[] { "harbor_fame", "contracts", "market_trends" }
            );

            // Stage 05
            CreateOrUpdateStage(
                $"{folderPath}/Stage_05.asset",
                "stage_05", 5, "world_1", 5, "Fishing Frenzy",
                "Charge frenzy bar (+6% per defeat); auto-triggers after 30s or on tap.",
                3.5f, 423, 0.35f, 1, false, "",
                null, null,
                new StageGoalConfig[]
                {
                    new StageGoalConfig { goalType = StageGoalType.DefeatFish, targetCount = 8, targetId = "", customDescription = "Defeat 8 Fish" },
                    new StageGoalConfig { goalType = StageGoalType.ActivateFrenzy, targetCount = 1, targetId = "", customDescription = "Activate Fishing Frenzy 1x" }
                },
                new string[] { "standard_frenzy" }
            );

            // Stage 06
            CreateOrUpdateStage(
                $"{folderPath}/Stage_06.asset",
                "stage_06", 6, "world_1", 6, "Shark Rampage",
                "World 1 Apex Boss: Apex Blacktip Reef Shark! Defeat boss to unlock World 2 voyage.",
                5.0f, 579, 1.25f, 1, true, "world_2",
                null, reefShark,
                new StageGoalConfig[]
                {
                    new StageGoalConfig { goalType = StageGoalType.DefeatBoss, targetCount = 1, targetId = "reef_shark", customDescription = "Defeat Blacktip Reef Shark" },
                    new StageGoalConfig { goalType = StageGoalType.CompleteOrder, targetCount = 1, targetId = "", customDescription = "Complete 1 Harbor Contract" }
                },
                new string[] { "world_2_voyage", "shark_trophy" }
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StageAssetGenerator] Successfully generated World 1 Stages (Stage 01 to 06)!");
        }

        private static void CreateOrUpdateStage(
            string assetPath,
            string stageId,
            int stageNumber,
            string worldId,
            int stageInWorld,
            string stageTitle,
            string mechanicDesc,
            float targetMin,
            int baseGoldPerMin,
            float rewardGoldEqMin,
            int fameReward,
            bool isBossStage,
            string worldVoyageUnlock,
            FishChainData firstCatch,
            FishChainData bossFish,
            StageGoalConfig[] goals,
            string[] unlocks)
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(assetPath);
            if (stage == null)
            {
                stage = ScriptableObject.CreateInstance<StageData>();
                AssetDatabase.CreateAsset(stage, assetPath);
            }

            stage.stageId = stageId;
            stage.stageNumber = stageNumber;
            stage.worldId = worldId;
            stage.stageInWorld = stageInWorld;
            stage.stageTitle = stageTitle;
            stage.mechanicDescription = mechanicDesc;
            stage.targetMinutes = targetMin;
            stage.estimatedBaseGoldPerMin = baseGoldPerMin;
            stage.rewardGoldEquivalentMinutes = rewardGoldEqMin;
            stage.grantFameOnFirstClear = fameReward;
            stage.isWorldBossStage = isBossStage;
            stage.worldVoyageUnlock = worldVoyageUnlock;
            stage.guaranteedFirstCatch = firstCatch;
            stage.bossToSpawn = bossFish;

            stage.goals.Clear();
            if (goals != null)
            {
                stage.goals.AddRange(goals);
            }

            stage.featureUnlocks.Clear();
            if (unlocks != null)
            {
                stage.featureUnlocks.AddRange(unlocks);
            }

            EditorUtility.SetDirty(stage);
        }
    }
}
#endif

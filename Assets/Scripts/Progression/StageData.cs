using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Data;

namespace IdleFishing.Progression
{
    [CreateAssetMenu(fileName = "Stage_01", menuName = "IdleFishing/Progression/StageData")]
    public class StageData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique stage identifier, e.g. stage_01")]
        public string stageId = "stage_01";
        [Tooltip("Absolute stage index across all worlds (1 to 30)")]
        public int stageNumber = 1;
        [Tooltip("World ID, e.g. world_1")]
        public string worldId = "world_1";
        [Tooltip("Stage index within world (1 to 6)")]
        public int stageInWorld = 1;
        [Tooltip("Display name for the stage")]
        public string stageTitle = "First Catch";
        [TextArea(2, 4)]
        public string mechanicDescription = "Tutorial fishing -> hop on deck -> combat -> sell.";

        [Header("Timing & Tuning")]
        [Tooltip("Target duration in minutes for balance benchmark")]
        public float targetMinutes = 1.0f;
        [Tooltip("Estimated base gold per minute for this stage")]
        public int estimatedBaseGoldPerMin = 120;
        [Tooltip("Clear gold reward equivalent in minutes of income")]
        public float rewardGoldEquivalentMinutes = 0.35f;
        [Tooltip("Harbor fame granted on first clear")]
        public int grantFameOnFirstClear = 1;
        public bool replayable = false;

        [Header("Stage Goals (All Required = AND Evaluation)")]
        public List<StageGoalConfig> goals = new List<StageGoalConfig>();

        [Header("Deterministic Spawning / Pity Directing")]
        [Tooltip("Guaranteed species caught on immediate cast when entering this stage.")]
        public FishChainData guaranteedFirstCatch;
        [Tooltip("Boss to queue immediately if this is a boss stage.")]
        public FishChainData bossToSpawn;

        [Header("Unlocks & World Voyage")]
        public bool isWorldBossStage = false;
        public string worldVoyageUnlock = ""; // e.g. "world_2"
        public List<string> featureUnlocks = new List<string>();

        public int CalculateGoldReward()
        {
            return Mathf.Max(50, Mathf.RoundToInt(estimatedBaseGoldPerMin * rewardGoldEquivalentMinutes));
        }
    }
}

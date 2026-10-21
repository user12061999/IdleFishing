using System;
using UnityEngine;

namespace IdleFishing.Progression
{
    public enum StageGoalType
    {
        CatchFish,          // Catch N fish (optionally matching targetId / speciesId)
        DefeatFish,         // Defeat N live fish on the pier deck
        SellItem,           // Sell N fish items (at dock or market shelf)
        HireWorker,         // Hire/unlock worker (role: "wrangler", "transporter", "fisherman")
        TransportPayload,   // Logistics cart delivers N fish payloads to market/sale
        CompleteOrder,      // Fulfill N harbor commercial contracts
        ActivateFrenzy,     // Trigger fishing frenzy mode N times
        DefeatBoss,         // Defeat apex world boss
        UpgradeReached      // Upgrade pillar reaches target level
    }

    [Serializable]
    public class StageGoalConfig
    {
        [Tooltip("The type of objective to track.")]
        public StageGoalType goalType;

        [Tooltip("Target quantity required to satisfy this goal.")]
        public int targetCount = 1;

        [Tooltip("Target identifier (e.g. speciesId 'herring', bossId 'reef_shark', worker role 'wrangler' or 'transporter').")]
        public string targetId = "";

        [Tooltip("Target level for UpgradeReached objectives.")]
        public int targetLevel = 1;

        [Tooltip("Human-readable description for UI display.")]
        public string customDescription = "";

        public string GetDefaultDescription()
        {
            if (!string.IsNullOrEmpty(customDescription)) return customDescription;

            return goalType switch
            {
                StageGoalType.CatchFish => string.IsNullOrEmpty(targetId) ? $"Catch {targetCount} Fish" : $"Catch {targetCount} {targetId}",
                StageGoalType.DefeatFish => string.IsNullOrEmpty(targetId) ? $"Defeat {targetCount} Fish" : $"Defeat {targetCount} {targetId}",
                StageGoalType.SellItem => $"Sell {targetCount} Fish",
                StageGoalType.HireWorker => $"Hire {targetCount} {targetId.ToUpper()}",
                StageGoalType.TransportPayload => $"Transport {targetCount} Fish in Cart",
                StageGoalType.CompleteOrder => $"Complete {targetCount} Order",
                StageGoalType.ActivateFrenzy => $"Activate Fishing Frenzy {targetCount}x",
                StageGoalType.DefeatBoss => string.IsNullOrEmpty(targetId) ? "Defeat World Boss" : $"Defeat Boss: {targetId}",
                StageGoalType.UpgradeReached => $"Reach Level {targetLevel} Upgrade",
                _ => "Complete Goal"
            };
        }
    }

    [Serializable]
    public class StageGoalProgress
    {
        public StageGoalConfig config;
        public int currentCount;
        public bool isCompleted => currentCount >= config.targetCount;

        public StageGoalProgress(StageGoalConfig cfg)
        {
            config = cfg;
            currentCount = 0;
        }
    }
}

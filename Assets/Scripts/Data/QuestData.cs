using System;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Economy;

namespace IdleFishing.Data
{
    public enum QuestType
    {
        CatchFish = 0,          // Bắt cá biển bất kỳ
        PurchaseUpgrade = 1,    // Mua bất kỳ nâng cấp nào
        UpgradePillarLevel = 2, // Đạt cấp độ nhất định của 1 trụ cột (FishermanTeam, WranglerTeam, FishValue)
        EarnCoins = 3,          // Thu nhập vàng từ bán cá
        TriggerFrenzy = 4,      // Kích hoạt Fishing Frenzy Mode
        DefeatBoss = 5,         // Đánh bại cá Boss khổng lồ
        CompleteContract = 6,   // Hoàn thành đơn đặt hàng bến cảng
        InteractGimmick = 7,    // Thu hoạch tính năng đặc quyền đảo (Giant Clam, Smoker BBQ, ...)
        UnlockWorld = 8         // Mở khóa và nhổ neo sang bến cảng mới
    }

    [System.Serializable]
    public class QuestStep
    {
        public string questId;
        public string title;
        public string description;
        public string iconTag = "[FISH]";
        public QuestType type;
        public int targetAmount = 1;
        public int rewardCoins = 50;

        // Conditional Parameters
        public UpgradeType targetPillar = UpgradeType.FishermanTeam;
        public int targetPillarLevel = 1;
        public int targetWorldOrder = 1;
        public string hintActionText = "";

        public QuestStep() { }

        public QuestStep(
            string id,
            string title,
            string description,
            string iconTag,
            QuestType type,
            int targetAmount,
            int rewardCoins,
            string hintActionText = "",
            UpgradeType targetPillar = UpgradeType.FishermanTeam,
            int targetPillarLevel = 1,
            int targetWorldOrder = 1)
        {
            this.questId = id;
            this.title = title;
            this.description = description;
            this.iconTag = iconTag;
            this.type = type;
            this.targetAmount = Mathf.Max(1, targetAmount);
            this.rewardCoins = rewardCoins;
            this.hintActionText = hintActionText;
            this.targetPillar = targetPillar;
            this.targetPillarLevel = targetPillarLevel;
            this.targetWorldOrder = targetWorldOrder;
        }

        public float GetProgressNormalized(int current)
        {
            if (targetAmount <= 0) return 1f;
            return Mathf.Clamp01((float)current / targetAmount);
        }

        public string GetFormattedProgress(int current)
        {
            int displayCur = Mathf.Min(current, targetAmount);
            if (type == QuestType.EarnCoins)
            {
                return $"{NumberFormatter.Format(displayCur)} / {NumberFormatter.Format(targetAmount)} G";
            }
            return $"{displayCur} / {targetAmount}";
        }
    }
}

using System;
using UnityEngine;
using IdleFishing.Data;

namespace IdleFishing.Market
{
    [Serializable]
    public class HarborContract
    {
        public string contractId;
        public string clientName;       // e.g. "Tokyo Sushi Restaurant", "Fresh Mart Supermarket"
        public string clientIcon;       // e.g. "[SUSHI]", "[STORE]", "[PHARMA]", "[FACTORY]"
        public string contractTitle;    // e.g. "Salmon Tray Supply", "Cod Bone Collection"
        public string contractDesc;     // e.g. "Urgent request for 10 packaged salmon trays for tonight's banquet."
        public ItemData targetItem;     // Target item type
        public int requiredCount;       // Required count (e.g. 10, 15, 20)
        public int rewardGold;          // Gold reward (higher than retail)
        public int rewardFame;          // Harbor Fame reward
        public float frenzyChargeBonus; // Frenzy charge bonus (+25%)
        public float durationSeconds;   // Contract duration (seconds)
        public float timeRemaining;     // Time remaining
        public bool isCompleted;

        public HarborContract(
            string id,
            string client,
            string icon,
            string title,
            string desc,
            ItemData item,
            int count,
            int gold,
            int fame,
            float frenzyBonus = 25f,
            float duration = 180f)
        {
            contractId = id;
            clientName = client;
            clientIcon = icon;
            contractTitle = title;
            contractDesc = desc;
            targetItem = item;
            requiredCount = count;
            rewardGold = gold;
            rewardFame = fame;
            frenzyChargeBonus = frenzyBonus;
            durationSeconds = duration;
            timeRemaining = duration;
            isCompleted = false;
        }
    }
}

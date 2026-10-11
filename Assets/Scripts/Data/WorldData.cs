using System.Collections.Generic;
using UnityEngine;

namespace IdleFishing.Data
{
    [CreateAssetMenu(fileName = "NewWorldData", menuName = "Idle Fishing/World Data")]
    public class WorldData : ScriptableObject
    {
        [Header("World Identity")]
        public string worldId = "world_1";
        public string worldName = "Temperate Bay";
        [TextArea(2, 4)]
        public string description = "A lush temperate bay teeming with delicious salmon and cod.";
        public int worldOrder = 1;

        [Header("Visual Theme")]
        public Color waterColor = new Color(0.12f, 0.45f, 0.65f);
        public Color groundColor = new Color(0.35f, 0.55f, 0.35f);
        public Color lightColor = new Color(1f, 0.95f, 0.85f);
        public float lightIntensity = 1.0f;

        [Header("Native Fish Species")]
        public List<FishChainData> nativeSpecies = new List<FishChainData>();

        [Header("Economy & Unlock Conditions")]
        [Tooltip("Starting coins for this world upon unlock")]
        public int startingCoins = 100;

        [Tooltip("Coins required in previous world to unlock this world (0 if World 1)")]
        public int unlockRequiredCoins = 0;

        [Tooltip("Total upgrade levels required in previous world to unlock this world")]
        public int unlockRequiredUpgrades = 0;

        [Header("World Upgrade Config")]
        public UpgradeConfig worldUpgradeConfig;
    }
}

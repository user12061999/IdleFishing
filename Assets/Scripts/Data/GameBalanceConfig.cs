using UnityEngine;

namespace IdleFishing.Data
{
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Idle Fishing/Game Balance Config")]
    public class GameBalanceConfig : ScriptableObject
    {
        [Header("--- WRANGLER COMBAT TUNING ---")]
        [Tooltip("Initial base health of Fish Wrangler")]
        public int wranglerBaseHealth = 120;

        [Tooltip("Initial base damage per strike of Fish Wrangler")]
        public int wranglerBaseDamage = 15;

        [Tooltip("Interval between strikes (seconds)")]
        public float wranglerAttackInterval = 0.85f;

        [Tooltip("Knockout recovery duration (seconds)")]
        public float wranglerKnockoutDuration = 4.0f;

        [Tooltip("Base movement speed of Fish Wrangler")]
        public float wranglerBaseMoveSpeed = 3.2f;

        [Header("--- PLAYER ACTIVE TAP ---")]
        [Tooltip("Base damage when player taps/clicks on fish")]
        public int playerClickBaseDamage = 12;

        [Header("--- FISH BEHAVIOR & MOVEMENT ---")]
        [Tooltip("Minimum hop interval (seconds)")]
        public float fishHopIntervalMin = 0.8f;

        [Tooltip("Maximum hop interval (seconds)")]
        public float fishHopIntervalMax = 1.4f;

        [Tooltip("Hop distance per jump (meters)")]
        public float fishHopDistance = 1.2f;

        [Tooltip("Arc jump height (meters)")]
        public float fishHopHeight = 0.85f;

        [Header("--- BOSS FISH EVENT ---")]
        [Tooltip("Giant Golden Fish Boss spawn chance (0.0 - 1.0)")]
        [Range(0f, 1f)]
        public float bossSpawnChance = 0.12f;

        [Tooltip("Boss health multiplier")]
        public float bossHealthMultiplier = 3.0f;

        [Tooltip("Boss tackle damage multiplier")]
        public float bossDamageMultiplier = 1.5f;

        [Tooltip("Boss 3D scale multiplier")]
        public float bossScaleMultiplier = 1.8f;

        [Tooltip("Base gold dropped when defeating Boss")]
        public int bossBaseBonusGold = 150;

        [Header("--- OFFLINE EARNINGS ---")]
        [Tooltip("Gold generated per second when offline")]
        public float offlineBaseCoinsPerSecond = 0.4f;

        [Tooltip("Minimum away time to receive offline reward (seconds)")]
        public float offlineMinAwaySeconds = 30f;

        [Tooltip("Maximum offline calculation time (hours)")]
        public float offlineMaxHours = 6.0f;

        [Header("--- SITTING FISHERMAN ---")]
        [Tooltip("Base fisherman casting duration")]
        public float fishermanBaseCastTime = 5.5f;

        [Tooltip("Maximum flopping fish on ground simultaneously")]
        public int maxPendingFishOnGround = 3;

        [Header("--- SUNKEN TREASURE CHEST ---")]
        [Tooltip("Chance to reel in Sunken Treasure Chest (0.0 - 1.0)")]
        [Range(0f, 1f)]
        public float treasureChestSpawnChance = 0.08f;

        [Tooltip("Number of taps to open treasure chest")]
        public int treasureChestRequiredTaps = 5;

        [Tooltip("Minimum gold rewarded from chest")]
        public int treasureChestMinGold = 250;

        [Tooltip("Maximum gold rewarded from chest")]
        public int treasureChestMaxGold = 500;

        [Header("--- FISHING FRENZY MODE ---")]
        [Tooltip("Fishing Frenzy duration (seconds)")]
        public float frenzyDuration = 20.0f;

        [Tooltip("Frenzy charge % per fish reeled in")]
        public float frenzyChargePerFish = 4.0f;

        [Tooltip("Frenzy charge % per fish defeated by wrangler")]
        public float frenzyChargePerDefeat = 6.0f;

        [Tooltip("Frenzy charge % per player tap")]
        public float frenzyChargePerTap = 1.0f;

        [Tooltip("Frenzy charge % when opening treasure chest")]
        public float frenzyChargePerChest = 40.0f;

        [Tooltip("Fishing speed multiplier during Frenzy")]
        public float frenzySpeedMultiplier = 2.5f;

        [Tooltip("Attack & tap damage multiplier during Frenzy")]
        public float frenzyDamageMultiplier = 2.5f;

        [Header("--- AQUARIUM BALANCING ---")]
        [Tooltip("Initial base captive fish capacity")]
        public int aquariumBaseCapacity = 8;

        [Tooltip("Base cost to upgrade aquarium capacity")]
        public int aquariumCapacityUpgradeBaseCost = 150;

        [Tooltip("Cost multiplier per capacity upgrade")]
        public float aquariumCapacityUpgradeMultiplier = 1.9f;

        [Tooltip("Number of slots added per capacity upgrade")]
        public int aquariumCapacitySlotsPerUpgrade = 2;

        [Tooltip("Base cost to upgrade aquarium decoration")]
        public int aquariumDecorUpgradeBaseCost = 300;

        [Tooltip("Cost multiplier per decor upgrade")]
        public float aquariumDecorUpgradeMultiplier = 2.4f;

        [Tooltip("Ticket income multiplier for Decor Level 2")]
        public float aquariumDecorMultiplierLevel2 = 1.35f;

        [Tooltip("Ticket income multiplier for Decor Level 3")]
        public float aquariumDecorMultiplierLevel3 = 1.80f;

        [Tooltip("Ticket income multiplier for Decor Level 4")]
        public float aquariumDecorMultiplierLevel4 = 2.50f;

        [Header("--- LOGISTICS & MARKET ---")]
        [Tooltip("Transporter crate capacity")]
        public int transporterCapacity = 6;

        [Tooltip("Transporter movement speed")]
        public float transporterMoveSpeed = 3.5f;

        [Tooltip("Customer base spawn interval in seconds")]
        public float customerSpawnInterval = 3.0f;
    }
}

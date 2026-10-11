using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Fishing;

namespace IdleFishing.Core
{
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance { get; private set; }

        [Header("Worlds Database")]
        [SerializeField] private List<WorldData> worlds = new List<WorldData>();
        [SerializeField] private int activeWorldIndex = 0;

        [Header("Scene Re-Skin References")]
        [SerializeField] private FishingSpot fishingSpot;
        [SerializeField] private Renderer groundRenderer;
        [SerializeField] private Renderer waterRenderer;
        [SerializeField] private Light directionalLight;

        public IReadOnlyList<WorldData> Worlds => worlds;
        public int ActiveWorldIndex => activeWorldIndex;
        public WorldData CurrentWorld => (worlds != null && activeWorldIndex >= 0 && activeWorldIndex < worlds.Count) ? worlds[activeWorldIndex] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            activeWorldIndex = PlayerPrefs.GetInt("IdleFishing_ActiveWorldIndex", 0);
        }

        private void Start()
        {
            ApplyWorld(activeWorldIndex, notifyEvents: false);
        }

        public void RegisterReferences(FishingSpot spot, Renderer ground, Renderer water, Light light)
        {
            fishingSpot = spot;
            groundRenderer = ground;
            waterRenderer = water;
            directionalLight = light;
        }

        public void AddWorld(WorldData world)
        {
            if (world != null && !worlds.Contains(world))
            {
                worlds.Add(world);
            }
        }

        public bool IsWorldUnlocked(WorldData world)
        {
            if (world == null) return false;
            if (world.worldOrder <= 1 || world.worldId == "world_1") return true;
            return PlayerPrefs.GetInt($"IdleFishing_World_{world.worldId}_Unlocked", 0) == 1;
        }

        public bool CanUnlockWorld(WorldData targetWorld, out string reason)
        {
            reason = string.Empty;
            if (targetWorld == null) return false;
            if (IsWorldUnlocked(targetWorld)) return true;

            int prevIndex = targetWorld.worldOrder - 2;
            if (prevIndex < 0 || prevIndex >= worlds.Count)
            {
                reason = "Previous harbor not found";
                return false;
            }

            WorldData prevWorld = worlds[prevIndex];
            int prevCoins = EconomyManager.Instance != null ? EconomyManager.Instance.GetCoinsForWorld(prevWorld.worldId) : 0;
            int prevUpgrades = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetTotalUpgradeLevelsForWorld(prevWorld.worldId) : 0;

            if (prevCoins < targetWorld.unlockRequiredCoins)
            {
                reason = $"Requires {targetWorld.unlockRequiredCoins:N0} G in {prevWorld.worldName} (Current: {prevCoins:N0} G)";
                return false;
            }

            if (prevUpgrades < targetWorld.unlockRequiredUpgrades)
            {
                reason = $"Requires a total of {targetWorld.unlockRequiredUpgrades} upgrade levels in {prevWorld.worldName} (Current: {prevUpgrades})";
                return false;
            }

            return true;
        }

        public bool TryUnlockWorld(WorldData targetWorld, out string error)
        {
            if (IsWorldUnlocked(targetWorld))
            {
                error = "This world is already unlocked!";
                return true;
            }

            if (!CanUnlockWorld(targetWorld, out error))
            {
                return false;
            }

            // Deduct unlock fee from previous world
            int prevIndex = targetWorld.worldOrder - 2;
            if (prevIndex >= 0 && prevIndex < worlds.Count)
            {
                WorldData prevWorld = worlds[prevIndex];
                int currentPrevCoins = EconomyManager.Instance.GetCoinsForWorld(prevWorld.worldId);
                int remaining = Mathf.Max(0, currentPrevCoins - targetWorld.unlockRequiredCoins);
                PlayerPrefs.SetInt($"IdleFishing_{prevWorld.worldId}_Coins", remaining);
                if (EconomyManager.Instance.CurrentWorldId == prevWorld.worldId)
                {
                    EconomyManager.Instance.TrySpendCoins(targetWorld.unlockRequiredCoins);
                }
            }

            PlayerPrefs.SetInt($"IdleFishing_World_{targetWorld.worldId}_Unlocked", 1);
            PlayerPrefs.Save();

            GameEvents.TriggerWorldUnlocked(targetWorld);
            Debug.Log($"[WorldManager] Congratulations! Successfully unlocked: {targetWorld.worldName}!");
            return true;
        }

        public void SwitchToWorld(int index)
        {
            if (index < 0 || index >= worlds.Count) return;
            WorldData target = worlds[index];
            if (!IsWorldUnlocked(target))
            {
                Debug.LogWarning($"[WorldManager] World {target.worldName} is not yet unlocked!");
                return;
            }

            activeWorldIndex = index;
            PlayerPrefs.SetInt("IdleFishing_ActiveWorldIndex", index);
            PlayerPrefs.Save();

            ApplyWorld(index, notifyEvents: true);
        }

        private void ApplyWorld(int index, bool notifyEvents)
        {
            if (index < 0 || index >= worlds.Count) return;
            WorldData world = worlds[index];
            if (world == null) return;

            // 1. Switch isolated wallet in EconomyManager
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SwitchWorld(world.worldId, world.startingCoins);
            }

            // 2. Switch isolated upgrades in UpgradeManager
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.SwitchWorld(world.worldId, world.worldUpgradeConfig);
            }

            // 3. Update FishingSpot native species
            if (fishingSpot != null)
            {
                fishingSpot.ClearAndSetSpecies(world.nativeSpecies);
            }

            // 4. Update visual ground and water colors
            if (IdleFishing.World.OceanWaterFlowController.Instance != null)
            {
                IdleFishing.World.OceanWaterFlowController.Instance.SyncShaderParameters();
            }
            else if (waterRenderer != null && waterRenderer.sharedMaterial != null)
            {
                if (waterRenderer.sharedMaterial.HasProperty("_BaseColor")) waterRenderer.sharedMaterial.SetColor("_BaseColor", world.waterColor);
                else if (waterRenderer.sharedMaterial.HasProperty("_Color")) waterRenderer.sharedMaterial.color = world.waterColor;
            }

            if (groundRenderer != null && groundRenderer.material != null)
            {
                if (groundRenderer.material.HasProperty("_BaseColor")) groundRenderer.material.SetColor("_BaseColor", world.groundColor);
                else if (groundRenderer.material.HasProperty("_Color")) groundRenderer.material.color = world.groundColor;
            }

            if (directionalLight != null)
            {
                directionalLight.color = world.lightColor;
                directionalLight.intensity = world.lightIntensity;
            }

            if (notifyEvents)
            {
                GameEvents.TriggerWorldChanged(world);
            }

            Debug.Log($"[WorldManager] Switched to harbor: {world.worldName} ({world.worldId})");
        }
    }
}

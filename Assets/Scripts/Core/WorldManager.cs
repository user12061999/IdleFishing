using System.Collections.Generic;
using UnityEngine;
using IdleFishing.Data;
using IdleFishing.Economy;
using IdleFishing.Fishing;

namespace IdleFishing.Core
{
    public class WorldManager : MonoBehaviour
    {
        private static WorldManager _instance;
        public static WorldManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<WorldManager>();
                }
                return _instance;
            }
            private set { _instance = value; }
        }

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
                reason = $"Requires {NumberFormatter.Format(targetWorld.unlockRequiredCoins)} G in {prevWorld.worldName} (Current: {NumberFormatter.Format(prevCoins)} G)";
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

        /// <summary>
        /// Yêu cầu chuyển cấp độ hoặc xem trước cấp độ.
        /// Tự động gọi chuỗi animation Thuyền Ngoài Khơi nhổ neo rẽ sóng ra xa và màn Load In/Out nếu có.
        /// </summary>
        public void RequestWorldTransition(int index, bool isPreview = false)
        {
            if (index < 0 || index >= worlds.Count) return;

            if (IdleFishing.World.OffshoreTravelShip.Instance != null && !IdleFishing.World.OffshoreTravelShip.Instance.IsTransitioning)
            {
                IdleFishing.World.OffshoreTravelShip.Instance.SailAwayToWorld(index, isPreview);
            }
            else
            {
                if (isPreview) PreviewWorld(index);
                else SwitchToWorld(index);
            }
        }

        /// <summary>
        /// Cho phép chọn và preview bất kỳ Level/Vùng đất nào ngay lập tức trong game
        /// </summary>
        public void PreviewWorld(int index)
        {
            if (index < 0 || index >= worlds.Count) return;
            activeWorldIndex = index;
            ApplyWorld(index, notifyEvents: true);
            Debug.Log($"[WorldManager] PREVIEW MODE: Switched to Level {index + 1} ({worlds[index].worldName})");
        }

        public int GetUnlockedWorldsCount()
        {
            int count = 0;
            if (worlds == null) return 1;
            for (int i = 0; i < worlds.Count; i++)
            {
                if (IsWorldUnlocked(worlds[i])) count++;
            }
            return Mathf.Max(1, count);
        }

        /// <summary>
        /// Reset toàn bộ tiến độ các Level khi khởi hành Viễn Chinh (Voyage):
        /// - Khóa lại toàn bộ các Level 2, 3, 4, 5.
        /// - Đưa người chơi trở về Level 1 (World 1).
        /// </summary>
        public void ResetAllWorldsProgress()
        {
            if (worlds != null)
            {
                for (int i = 0; i < worlds.Count; i++)
                {
                    var w = worlds[i];
                    if (w != null && w.worldId != "world_1")
                    {
                        PlayerPrefs.DeleteKey($"IdleFishing_World_{w.worldId}_Unlocked");
                    }
                }
            }
            activeWorldIndex = 0;
            PlayerPrefs.SetInt("IdleFishing_ActiveWorldIndex", 0);
            PlayerPrefs.Save();

            ApplyWorld(0, notifyEvents: true);
            Debug.Log("[WorldManager] VOYAGE RESET: All levels locked except Level 1. Returned to Level 1.");
        }

        /// <summary>
        /// Mở khóa nhanh toàn bộ các Level để thử nghiệm
        /// </summary>
        public void UnlockAllWorldsForTesting()
        {
            for (int i = 0; i < worlds.Count; i++)
            {
                if (worlds[i] != null)
                {
                    PlayerPrefs.SetInt($"IdleFishing_World_{worlds[i].worldId}_Unlocked", 1);
                }
            }
            PlayerPrefs.Save();
            Debug.Log("[WorldManager] All worlds unlocked for testing!");
            if (activeWorldIndex >= 0 && activeWorldIndex < worlds.Count)
            {
                ApplyWorld(activeWorldIndex, notifyEvents: true);
            }
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

            if (groundRenderer != null && groundRenderer.sharedMaterial != null && groundRenderer.sharedMaterial.mainTexture == null)
            {
                if (groundRenderer.sharedMaterial.HasProperty("_BaseColor")) groundRenderer.sharedMaterial.SetColor("_BaseColor", world.groundColor);
                else if (groundRenderer.sharedMaterial.HasProperty("_Color")) groundRenderer.sharedMaterial.color = world.groundColor;
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

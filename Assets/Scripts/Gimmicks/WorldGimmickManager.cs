using System;
using UnityEngine;
using IdleFishing.Core;
using IdleFishing.Data;

namespace IdleFishing.Gimmicks
{
    /// <summary>
    /// Quản lý Cơ Chế Đặc Quyền Theo Từng Vùng Đất (Signature World Gimmicks):
    /// - World 1: Bến Câu Cơ Bản (Thư giãn, nền tảng)
    /// - World 2: Đảo San Hô -> Sò Khổng Lồ Săn Ngọc Trai (Pearl Frenzy x3 Vàng)
    /// - World 3: Vịnh Rừng Phong -> Lò Nướng Đặc Sản BBQ (Smoked Fish x4 Vàng)
    /// - World 4: Vịnh Băng Bắc Cực -> Lỗ Băng & Bão Tuyết Sinh Tồn (Đập băng giải cứu)
    /// - World 5: Ốc Đảo Sa Mạc -> Đoàn Lạc Đà Thương Gia & Cổ Vật Thần Bí (Relics Buff)
    /// </summary>
    public class WorldGimmickManager : MonoBehaviour
    {
        public static WorldGimmickManager Instance { get; private set; }

        [Header("Gimmick Controllers")]
        [SerializeField] private GiantClamPearlGimmick coralGimmick;
        [SerializeField] private AutumnSmokerBBQGimmick mapleGimmick;
        [SerializeField] private ArcticBlizzardGimmick arcticGimmick;
        [SerializeField] private DesertCaravanGimmick desertGimmick;

        private string currentWorldId = "world_1";

        public string CurrentWorldId => currentWorldId;
        public GiantClamPearlGimmick CoralGimmick => coralGimmick;
        public AutumnSmokerBBQGimmick MapleGimmick => mapleGimmick;
        public ArcticBlizzardGimmick ArcticGimmick => arcticGimmick;
        public DesertCaravanGimmick DesertGimmick => desertGimmick;

        public event Action OnGimmickStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.WorldChanged += HandleWorldChanged;
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= HandleWorldChanged;
        }

        private void Start()
        {
            if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
            {
                ApplyGimmickForWorld(WorldManager.Instance.CurrentWorld.worldId);
            }
            else
            {
                ApplyGimmickForWorld("world_1");
            }
        }

        private void HandleWorldChanged(WorldData newWorld)
        {
            if (newWorld == null) return;
            ApplyGimmickForWorld(newWorld.worldId);
        }

        public void RegisterControllers(
            GiantClamPearlGimmick coral,
            AutumnSmokerBBQGimmick maple,
            ArcticBlizzardGimmick arctic,
            DesertCaravanGimmick desert)
        {
            coralGimmick = coral;
            mapleGimmick = maple;
            arcticGimmick = arctic;
            desertGimmick = desert;

            if (WorldManager.Instance != null && WorldManager.Instance.CurrentWorld != null)
            {
                ApplyGimmickForWorld(WorldManager.Instance.CurrentWorld.worldId);
            }
        }

        public void ApplyGimmickForWorld(string worldId)
        {
            currentWorldId = worldId;

            // Bật/tắt các gimmick tương ứng theo level
            bool isWorld2 = (worldId == "world_2");
            bool isWorld3 = (worldId == "world_3");
            bool isWorld4 = (worldId == "world_4");
            bool isWorld5 = (worldId == "world_5");

            if (coralGimmick != null) coralGimmick.gameObject.SetActive(isWorld2);
            if (mapleGimmick != null) mapleGimmick.gameObject.SetActive(isWorld3);
            if (arcticGimmick != null) arcticGimmick.gameObject.SetActive(isWorld4);
            if (desertGimmick != null) desertGimmick.gameObject.SetActive(isWorld5);

            OnGimmickStateChanged?.Invoke();
            Debug.Log($"[WorldGimmickManager] Applied Signature Gimmick for world: {worldId}");
        }

        public void NotifyStateChanged()
        {
            OnGimmickStateChanged?.Invoke();
        }

        /// <summary>
        /// Nhân thêm vào giá bán cá tại quầy Market
        /// </summary>
        public float GetGlobalPriceMultiplier()
        {
            float multiplier = 1.0f;

            // Level 2 Pearl Frenzy Buff (x3 tiền bán)
            if (coralGimmick != null && coralGimmick.gameObject.activeInHierarchy && coralGimmick.IsPearlFrenzyActive)
            {
                multiplier *= 3.0f;
            }

            // Level 5 Desert Relic Buff
            if (desertGimmick != null && desertGimmick.gameObject.activeInHierarchy)
            {
                multiplier *= desertGimmick.PriceMultiplier;
            }

            return multiplier;
        }

        /// <summary>
        /// Nhân thêm vào tốc độ câu của cần thủ
        /// </summary>
        public float GetFishingSpeedMultiplier()
        {
            float multiplier = 1.0f;

            // Level 4 Arctic Blizzard Slow / Rescue Speed Buff
            if (arcticGimmick != null && arcticGimmick.gameObject.activeInHierarchy)
            {
                multiplier *= arcticGimmick.FishingSpeedMultiplier;
            }

            // Level 5 Desert Relic Buff
            if (desertGimmick != null && desertGimmick.gameObject.activeInHierarchy)
            {
                multiplier *= desertGimmick.SpeedMultiplier;
            }

            return multiplier;
        }
    }
}

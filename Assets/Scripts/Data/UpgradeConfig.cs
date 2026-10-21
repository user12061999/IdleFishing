using UnityEngine;

namespace IdleFishing.Data
{
    public enum UpgradeType
    {
        // 3 Trụ Cột Nâng Cấp Tinh Gọn (3 Streamlined Core Pillars)
        FishermanTeam = 0,          // [Trụ Cột 1] Đội Cần Thủ (Tốc độ câu & Tự động mở rộng cần thủ)
        WranglerTeam = 1,           // [Trụ Cột 2] Đội Thợ Săn Cá (Sát thương, HP, Tốc chạy & Thêm thợ bắt cá)
        FishValue = 2,              // [Trụ Cột 3] Giá Trị Hải Sản (Tăng giá bán tất cả cá tại quầy)

        // Tương thích ngược với các script hiện tại:
        FishermanSlots = FishermanTeam,
        FishermanSpeed = FishermanTeam,
        HireWrangler = WranglerTeam,
        WranglerPower = WranglerTeam,
        KitchenSpeed = FishValue,
        MarketValueMultiplier = FishValue
    }

    [System.Serializable]
    public class UpgradeDataEntry
    {
        public UpgradeType type;
        public string title;
        [TextArea(1, 3)]
        public string description;
        public int baseCost = 50;
        public float costMultiplier = 1.6f;
        public float baseValue = 1.0f;
        public float valueIncreasePerLevel = 0.25f;
        public int maxLevel = 10;
        public Sprite icon;

        public int GetCostForLevel(int currentLevel)
        {
            if (currentLevel >= maxLevel) return int.MaxValue;
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
        }

        public float GetValueForLevel(int currentLevel)
        {
            return baseValue + (currentLevel * valueIncreasePerLevel);
        }
    }

    [CreateAssetMenu(fileName = "UpgradeConfig", menuName = "IdleFishing/Upgrade Config", order = 4)]
    public class UpgradeConfig : ScriptableObject
    {
        [SerializeField] private UpgradeDataEntry[] upgrades;

        public UpgradeDataEntry[] Upgrades => upgrades;

        public UpgradeDataEntry GetEntry(UpgradeType type)
        {
            if (upgrades == null) return null;
            for (int i = 0; i < upgrades.Length; i++)
            {
                if (upgrades[i].type == type) return upgrades[i];
            }
            return null;
        }
    }
}
